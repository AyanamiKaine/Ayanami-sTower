// Refactored DeploymentService.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using AyanamisTower.Email; // --- CHANGE: Added using statement for the email library

namespace AyanamisTower.PersonalWebsite;

static class DeploymentService
{
    // --- CHANGE: The StatusLevel enum is no longer needed here.
    // It will use the one defined in the AyanamisTower.Email namespace.

    // --- Configuration ---
    private const string RepoPath = "/home/ayanami/Ayanami-sTower/";
    private const string ImageName = "my-personal-website";
    private const string StateFile = "/home/ayanami/deployment.state";
    private const string ProjectPath = "/home/ayanami/Ayanami-sTower/PersonalWebsite/";
    private const string NginxUpstreamConfig = "/etc/nginx/astro_upstream.conf";
    private const int BluePort = 8080;
    private const int GreenPort = 8081;

    // --- CHANGE: The path to the email CLI is no longer needed.
    // private const string EmailCliPath = "/home/ayanami/Ayanami-sTower/Build/bin/AyanamisTower.EmailSenderCLI/Release/net9.0/AyanamisTower.EmailSenderCLI";

    // --- CHANGE: Added a static instance of the EmailStatusService.
    // This service will be used to send all email notifications.
    // It will automatically pick up its configuration from environment variables.
    private static readonly EmailStatusService _emailService = new EmailStatusService();


    public static async Task Main(string[] _)
    {
        Console.WriteLine($"\n--- {DateTime.Now}: C# Deployer started by systemd timer. ---");
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

        string result = await RunProcessAsync("podman", $"ps --filter name={containerName} --filter status=running --format \"{{{{.ID}}}}\"", RepoPath, ignoreErrors: true);

        return !string.IsNullOrWhiteSpace(result);
    }

    /// <summary>
    /// --- REFACTORED METHOD ---
    /// Sends an email notification by directly using the EmailStatusService library.
    /// This is cleaner, more efficient, and safer than invoking an external CLI tool.
    /// </summary>
    private static async Task SendNotificationAsync(StatusLevel level, string subject, string message)
    {
        try
        {
            Console.WriteLine($"Sending {level} email notification...");
            // Directly call the library method. It handles formatting and sending.
            await _emailService.SendStatusUpdateAsync(subject, message, level);
            Console.WriteLine("Email notification sent successfully via library.");
        }
        catch (Exception ex)
        {
            // Log the error to the console, but don't let a notification failure
            // interrupt the core deployment process.
            Console.WriteLine($"!!! CRITICAL: Failed to send email notification via library. Error: {ex.Message}");
        }
    }


    private static async Task TriggerDeployment(CancellationToken cancellationToken = default)
    {
        try
        {
            bool isLive = await IsLiveContainerRunning();
            bool hasNewCommits = await HasNewCommits();

            if (isLive && !hasNewCommits)
            {
                Console.WriteLine("Site is running and no new commits found. Exiting.");
                return;
            }

            string startReason = !isLive
                ? "Live container is not running. Forcing deployment to recover."
                : "New commits detected. Starting deployment.";

            Console.WriteLine(startReason);
            await SendNotificationAsync(StatusLevel.Info, "Deployment Started", startReason);

            if (cancellationToken.IsCancellationRequested) return;

            string liveColor = File.Exists(StateFile) ? await File.ReadAllTextAsync(StateFile, cancellationToken) : "blue";
            string standbyColor = liveColor == "blue" ? "green" : "blue";
            int livePort = liveColor == "blue" ? BluePort : GreenPort;
            int standbyPort = liveColor == "blue" ? GreenPort : BluePort;

            Console.WriteLine($"Current LIVE: {liveColor} ({livePort}). Deploying to STANDBY: {standbyColor} ({standbyPort}).");

            await RunProcessAsync("git", "pull", RepoPath, cancellationToken: cancellationToken);
            await RunProcessAsync("podman", $"build -t {ImageName}:latest .", ProjectPath, cancellationToken: cancellationToken);

            await RunProcessAsync("podman", $"rm -f astro-site-{standbyColor}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
            await RunProcessAsync("podman", $"run -d --name astro-site-{standbyColor} -p {standbyPort}:4321 {ImageName}:latest", RepoPath, cancellationToken: cancellationToken);

            Console.WriteLine("Performing health check... first waiting 5 seconds");
            await Task.Delay(5000, cancellationToken);
            if (!await IsHealthy(standbyPort, cancellationToken))
            {
                var failureMsg = $"Health check FAILED for container astro-site-{standbyColor} on port {standbyPort}. Aborting deployment.";
                Console.WriteLine("!!! Health check FAILED. Aborting deployment.");
                await SendNotificationAsync(StatusLevel.Error, "Deployment FAILED", failureMsg);
                return;
            }
            Console.WriteLine("Health check PASSED.");

            Console.WriteLine("Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken);

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
        var tcs = new TaskCompletionSource<string>();

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
                CreateNoWindow = true,
            }
        };

        var registration = cancellationToken.Register(() =>
        {
            try { process?.Kill(true); } catch { }
            tcs.TrySetCanceled();
        });

        process.EnableRaisingEvents = true;
        process.Exited += async (sender, e) =>
        {
            registration.Dispose();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Warning from command '{command} {args}': {error}");
                }
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
                    var fullError = new StringBuilder();
                    fullError.AppendLine($"Command '{command} {args}' failed with exit code {process.ExitCode}.");
                    if (!string.IsNullOrWhiteSpace(output)) fullError.AppendLine($"Output:\n{output}");
                    if (!string.IsNullOrWhiteSpace(error)) fullError.AppendLine($"Error:\n{error}");
                    tcs.TrySetException(new Exception(fullError.ToString()));
                    await SendNotificationAsync(StatusLevel.Error, "Deployment FAILED", fullError.ToString());
                }
            }
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            tcs.SetException(new Exception($"Failed to start process '{command}'. Is it in your PATH? Error: {ex.Message}"));
            return tcs.Task;
        }

        if (input != null)
        {
            process.StandardInput.Write(input);
            process.StandardInput.Close();
        }

        return tcs.Task;
    }
}