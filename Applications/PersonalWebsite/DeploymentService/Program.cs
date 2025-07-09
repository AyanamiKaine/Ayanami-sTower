using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AyanamisTower.PersonalWebsite;

static class DeploymentService
{


    private enum StatusLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    // --- Configuration ---
    private const string RepoPath = "/home/ayanami/Ayanami-sTower/";
    private const string ImageName = "my-personal-website";
    private const string StateFile = "/home/ayanami/deployment.state";
    private const string ProjectPath = "/home/ayanami/Ayanami-sTower/PersonalWebsite/";
    private const string NginxUpstreamConfig = "/etc/nginx/astro_upstream.conf";
    private const int BluePort = 8080;
    private const int GreenPort = 8081;
    private const string EmailCliPath = "/home/ayanami/Ayanami-sTower/Build/bin/AyanamisTower.EmailSenderCLI/Release/net9.0/AyanamisTower.EmailSenderCLI";

    public static async Task Main(string[] _)
    {
        Console.WriteLine($"\n--- {DateTime.Now}: C# Deployer started by systemd timer. ---");
        // The rest of the code is the same TriggerDeployment and helper methods...
        await TriggerDeployment();
    }

    private static async Task<bool> HasNewCommits()
    {
        await RunProcessAsync("git", "fetch", RepoPath);
        var local = await RunProcessAsync("git", "rev-parse @", RepoPath);
        var remote = await RunProcessAsync("git", "rev-parse @{u}", RepoPath);
        return local.Trim() != remote.Trim();
    }

    private static async Task<bool> IsLiveContainerRunning()
    {
        if (!File.Exists(StateFile)) return false;

        string liveColor = await File.ReadAllTextAsync(StateFile);
        string containerName = $"astro-site-{liveColor}";

        // This command asks Podman for running containers with the exact name.
        // If it returns any text, the container exists and is running.
        string result = await RunProcessAsync("podman", $"ps --filter name={containerName} --filter status=running --format \"{{{{.ID}}}}\"", RepoPath, ignoreErrors: true);

        return !string.IsNullOrWhiteSpace(result);
    }

    /// <summary>
    /// Sends an email notification by executing the external EmailSenderCLI tool.
    /// </summary>
    private static async Task SendNotificationAsync(StatusLevel level, string subject, string message)
    {
        if (!File.Exists(EmailCliPath))
        {
            Console.WriteLine($"Warning: Email CLI executable not found at '{EmailCliPath}'. Skipping notification.");
            return;
        }

        try
        {
            string subcommand = level.ToString().ToLower();
            // The CLI expects: <subcommand> "<subject>" "<message>"
            string args = $"{subcommand} \"{subject}\" \"{message}\"";

            Console.WriteLine($"Executing notification CLI to send {level} email...");
            await RunProcessAsync(EmailCliPath, args, workingDirectory: RepoPath);
            Console.WriteLine("Email notification CLI executed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! CRITICAL: Failed to execute email notification CLI. Error: {ex.Message}");
        }
    }

    private static async Task TriggerDeployment(CancellationToken cancellationToken = default)
    {
        try
        {

            bool isLive = await IsLiveContainerRunning();
            bool hasNewCommits = await HasNewCommits();

            // We only exit if the site is already running AND there are no new changes.
            if (isLive && !hasNewCommits)
            {
                Console.WriteLine("Site is running and no new commits found. Exiting.");
                return;
            }

            if (!isLive)
            {
                Console.WriteLine("Live container is not running. Forcing deployment to recover.");
            }

            if (hasNewCommits)
            {
                Console.WriteLine("New commits detected. Starting deployment.");
            }

            string startReason = !isLive ? "Live container is not running. Forcing deployment to recover."
                                   : "New commits detected. Starting deployment.";
            Console.WriteLine(startReason);
            await SendNotificationAsync(StatusLevel.Info, "Deployment Started", startReason);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Step 2: Determine live and standby environments.
            string liveColor = File.Exists(StateFile) ? await File.ReadAllTextAsync(StateFile, cancellationToken) : "blue";
            string standbyColor = liveColor == "blue" ? "green" : "blue";
            int livePort = liveColor == "blue" ? BluePort : GreenPort;
            int standbyPort = liveColor == "blue" ? GreenPort : BluePort;

            Console.WriteLine($"Current LIVE: {liveColor} ({livePort}). Deploying to STANDBY: {standbyColor} ({standbyPort}).");

            // Step 3: Pull changes and build the new image.
            await RunProcessAsync("git", "pull", RepoPath, cancellationToken: cancellationToken);
            await RunProcessAsync("podman", $"build -t {ImageName}:latest .", ProjectPath, cancellationToken: cancellationToken);

            // Step 4: Stop any old standby container and start the new one.
            await RunProcessAsync("podman", $"rm -f astro-site-{standbyColor}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
            await RunProcessAsync("podman", $"run -d --name astro-site-{standbyColor} -p {standbyPort}:80 {ImageName}:latest", RepoPath, cancellationToken: cancellationToken);

            // Step 5: Health Check.
            Console.WriteLine("Performing health check...");
            await Task.Delay(5000, cancellationToken); // Give the container time to start.
            if (!await IsHealthy(standbyPort, cancellationToken))
            {
                var failureMsg = $"Health check FAILED for container astro-site-{standbyColor} on port {standbyPort}. Aborting deployment.";
                Console.WriteLine("!!! Health check FAILED. Aborting deployment.");
                await SendNotificationAsync(StatusLevel.Error, "Deployment FAILED", failureMsg);
                return;
            }
            Console.WriteLine("Health check PASSED.");

            // Step 6: Switch Nginx traffic.
            Console.WriteLine("Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken);

            // Step 7: Update state and clean up.
            await File.WriteAllTextAsync(StateFile, standbyColor, cancellationToken);

            var successMsg = $"Deployment successful! {standbyColor} is now LIVE.";
            Console.WriteLine($"SUCCESS! {successMsg}");
            await SendNotificationAsync(StatusLevel.Success, "Deployment Successful", successMsg);

            await RunProcessAsync("podman", $"stop astro-site-{liveColor}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
            Console.WriteLine("--- Deployment Complete ---\n");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Deployment cancelled.");
            await SendNotificationAsync(StatusLevel.Warning, "Deployment Cancelled", "The deployment process was cancelled.");
        }
        catch (Exception ex)
        {
            var errorMsg = $"An unexpected error occurred during deployment: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            Console.WriteLine($"!!! {errorMsg}");
            await SendNotificationAsync(StatusLevel.Error, "Deployment FAILED", errorMsg);
        }
    }

    private static async Task<bool> IsHealthy(int port, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var response = await client.GetAsync($"http://localhost:{port}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static Task<string> RunProcessAsync(string command, string args, string workingDirectory, string? input = null, bool ignoreErrors = false, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"> Running: {command} {args}");
        var process = new Process
        {
            StartInfo =
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = input != null,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var tcs = new TaskCompletionSource<string>();

        // Handle cancellation
        var registration = cancellationToken.Register(() =>
        {
            try
            {
                process?.Kill();
            }
            catch { }
            tcs.TrySetCanceled();
        });

        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) =>
        {
            registration.Dispose();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                tcs.TrySetResult(output);
            }
            else
            {
                if (ignoreErrors)
                {
                    tcs.TrySetResult(string.Empty);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Command '{command} {args}' failed with exit code {process.ExitCode}.\nError: {error}"));
                }
            }
            process.Dispose();
        };

        process.Start();

        if (input != null)
        {
            process.StandardInput.Write(input);
            process.StandardInput.Close();
        }

        return tcs.Task;
    }
}