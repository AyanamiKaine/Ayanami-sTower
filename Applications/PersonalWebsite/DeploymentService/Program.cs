// Refactored DeploymentService.cs
// Now capable of managing multiple applications (Astro site and Docusaurus wiki)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using AyanamisTower.Email;

namespace AyanamisTower.MultiDeployer;

/// <summary>
/// Represents the configuration for a single deployable application
/// </summary>
public class AppConfig
{
    /// <summary>
    /// A unique name for the application being deployed.
    /// </summary>
    public required string Name { get; init; } // A unique identifier, e.g., "AstroSite"
    /// <summary>
    /// The absolute path to the root directory of the project within the repository.
    /// </summary>
    public required string ProjectPath { get; init; } // Absolute path to the project's root directory
    /// <summary>
    /// The Docker image name used for this application.
    /// </summary>
    public required string ImageName { get; init; } // Docker image name, e.g., "personal-website"
    /// <summary>
    /// The path to the file that stores the current live color (blue/green).
    /// </summary>
    public required string StateFile { get; init; } // Path to the file storing the current color (blue/green)
    /// <summary>
    /// The path to the file that stores the last successfully deployed commit hash for this app.
    /// </summary>
    public required string DeployedCommitFile { get; init; }
    /// <summary>
    ///     The path to the Nginx upstream configuration file that needs to be updated during deployment.
    /// </summary>
    public required string NginxUpstreamConfig { get; init; } // Path to the Nginx upstream .conf file
    /// <summary>
    /// The port number for the blue deployment.
    /// </summary>
    public int BluePort { get; init; }
    /// <summary>
    /// The port number for the green deployment.
    /// </summary>
    public int GreenPort { get; init; }
}

static class DeploymentService
{
    // --- Global Configuration ---
    private const string RepoPath = "/home/ayanami/Ayanami-sTower/";
    private static readonly EmailStatusService _emailService = new();
    // The current HEAD commit hash after pulling (used to compare against last deployed commit)
    private static string _currentHeadCommit = string.Empty;

    // --- Application Definitions ---
    // A list of all applications to be managed by this script.
    // Adding a new application in the future is as simple as adding a new entry here.
    private static readonly List<AppConfig> AppsToDeploy = new()
    {
        new AppConfig
        {
            Name = "PersonalWebsite",
            ProjectPath = "/home/ayanami/Ayanami-sTower/PersonalWebsite/",
            ImageName = "my-personal-website",
            StateFile = "/home/ayanami/deployment.state",
            DeployedCommitFile = "/home/ayanami/deployment_commit_personalwebsite.txt",
            NginxUpstreamConfig = "/etc/nginx/astro_upstream.conf",
            BluePort = 8080,
            GreenPort = 8081
        },
        new AppConfig
        {
            Name = "StellaWiki",
            // Assuming the project is in a subfolder named 'StellaWiki' based on your provided files.
            // Please adjust this path if your Docusaurus project is located elsewhere in the repository.
            ProjectPath = "/home/ayanami/Ayanami-sTower/StellaEcs/AyanamisTower.StellaEcs.Wiki/",
            ImageName = "stella-wiki",
            StateFile = "/home/ayanami/wiki_deployment.state",
            DeployedCommitFile = "/home/ayanami/deployment_commit_stellawiki.txt",
            NginxUpstreamConfig = "/etc/nginx/wiki_upstream.conf",
            BluePort = 9080,
            GreenPort = 9081
        }
    };

    public static async Task Main(string[] args)
    {
        Console.WriteLine($"\n--- {DateTime.Now}: C# Multi-Deployer started. ---");
        bool isForced = args.Contains("-f") || args.Contains("--force");
        if (isForced)
        {
            Console.WriteLine("Force flag detected. All applications will be redeployed regardless of git changes.");
        }

        // Always fetch and pull to ensure we're at the latest commit
        Console.WriteLine("Fetching and pulling latest changes...");
        await FetchAndPullLatest();

        // Iterate through each defined application and trigger its deployment logic.
        foreach (var app in AppsToDeploy)
        {
            Console.WriteLine($"\n>>>>>> Processing Application: {app.Name} <<<<<<");
            await TriggerDeploymentForApp(app, isForced);
        }

        Console.WriteLine("\n--- Multi-Deployer run complete. ---\n");
    }

    /// <summary>
    /// Fetches and pulls latest changes, then captures current HEAD commit.
    /// </summary>
    private static async Task FetchAndPullLatest()
    {
        await RunProcessAsync("git", "fetch", RepoPath);
        await RunProcessAsync("git", "pull", RepoPath);
        _currentHeadCommit = (await RunProcessAsync("git", "rev-parse HEAD", RepoPath)).Trim();
        Console.WriteLine($"Current HEAD commit: {_currentHeadCommit}");
    }

    /// <summary>
    /// Checks if the app has changes that need deployment by comparing current HEAD 
    /// against the last successfully deployed commit for this specific app.
    /// </summary>
    private static async Task<bool> HasChangesForApp(AppConfig app)
    {
        // If we don't have a record of what was deployed, we need to deploy
        if (!File.Exists(app.DeployedCommitFile))
        {
            Console.WriteLine($"[{app.Name}] No deployed commit record found. Will deploy.");
            return true;
        }

        string lastDeployedCommit = (await File.ReadAllTextAsync(app.DeployedCommitFile)).Trim();

        // If commits are identical, no changes
        if (string.Equals(lastDeployedCommit, _currentHeadCommit, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[{app.Name}] Already at deployed commit {lastDeployedCommit[..Math.Min(8, lastDeployedCommit.Length)]}. No changes.");
            return false;
        }

        // Check if there are any changes in the app's project path between the two commits
        try
        {
            string relativePath = Path.GetRelativePath(RepoPath, app.ProjectPath).Replace('\\', '/');
            string diffResult = await RunProcessAsync(
                "git",
                $"diff --name-only {lastDeployedCommit} {_currentHeadCommit} -- \"{relativePath}\"",
                RepoPath,
                ignoreErrors: true);

            var changedFiles = diffResult
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (changedFiles.Count > 0)
            {
                Console.WriteLine($"[{app.Name}] {changedFiles.Count} file(s) changed since last deploy:");
                foreach (var file in changedFiles.Take(10)) // Show first 10
                {
                    Console.WriteLine($"  - {file}");
                }
                if (changedFiles.Count > 10)
                {
                    Console.WriteLine($"  ... and {changedFiles.Count - 10} more");
                }
                return true;
            }

            Console.WriteLine($"[{app.Name}] No changes in project path since last deploy.");
            return false;
        }
        catch (Exception ex)
        {
            // If git diff fails (e.g., commit doesn't exist after force push), treat as needing deployment
            Console.WriteLine($"[{app.Name}] Warning: Could not compare commits ({ex.Message}). Will redeploy to be safe.");
            return true;
        }
    }

    /// <summary>
    /// Records the current HEAD as the successfully deployed commit for this app.
    /// </summary>
    private static async Task RecordDeployedCommit(AppConfig app)
    {
        await File.WriteAllTextAsync(app.DeployedCommitFile, _currentHeadCommit);
        Console.WriteLine($"[{app.Name}] Recorded deployed commit: {_currentHeadCommit[..Math.Min(8, _currentHeadCommit.Length)]}");
    }



    private static async Task<bool> IsLiveContainerRunning(AppConfig app)
    {
        if (!File.Exists(app.StateFile)) return false;

        string liveColor = await File.ReadAllTextAsync(app.StateFile);
        string containerName = $"{app.ImageName}-{liveColor}";

        string result = await RunProcessAsync("podman", $"ps --filter name={containerName} --filter status=running --format \"{{{{.ID}}}}\"", RepoPath, ignoreErrors: true);
        return !string.IsNullOrWhiteSpace(result);
    }

    private static async Task SendNotificationAsync(StatusLevel level, string subject, string message)
    {
        try
        {
            Console.WriteLine($"Sending {level} email: {subject}");
            await _emailService.SendStatusUpdateAsync(subject, message, level);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! CRITICAL: Failed to send email notification. Error: {ex.Message}");
        }
    }

    private static async Task TriggerDeploymentForApp(AppConfig app, bool isForced, CancellationToken cancellationToken = default)
    {
        try
        {
            bool isLive = await IsLiveContainerRunning(app);
            bool hasChanges = await HasChangesForApp(app);

            if (isLive && !hasChanges && !isForced)
            {
                Console.WriteLine($"[{app.Name}] Site is running and no changes detected. Skipping.");
                return;
            }

            string startReason;
            if (isForced)
            {
                startReason = $"[{app.Name}] Deployment is being forced by the '--force' flag.";
            }
            else if (!isLive)
            {
                startReason = $"[{app.Name}] Live container is not running. Forcing deployment to recover.";
            }
            else
            {
                startReason = $"[{app.Name}] Changes detected since last deployment. Starting deployment.";
            }


            Console.WriteLine(startReason);
            await SendNotificationAsync(StatusLevel.Info, $"[{app.Name}] Deployment Started", startReason);

            if (cancellationToken.IsCancellationRequested) return;

            string liveColor = File.Exists(app.StateFile) ? await File.ReadAllTextAsync(app.StateFile, cancellationToken) : "blue";
            string standbyColor = liveColor == "blue" ? "green" : "blue";
            int livePort = liveColor == "blue" ? app.BluePort : app.GreenPort;
            int standbyPort = liveColor == "blue" ? app.GreenPort : app.BluePort;
            string standbyContainerName = $"{app.ImageName}-{standbyColor}";
            string liveContainerName = $"{app.ImageName}-{liveColor}";

            Console.WriteLine($"[{app.Name}] Current LIVE: {liveColor} ({livePort}). Deploying to STANDBY: {standbyColor} ({standbyPort}).");

            // The git pull is now done once at the start.
            // We now build the specific Docker image for the application.
            // Note: Dockerfile should be present in the app.ProjectPath directory.
            await RunProcessAsync("podman", $"build -t {app.ImageName}:latest .", app.ProjectPath, cancellationToken: cancellationToken);

            // --- FIX STARTS HERE ---
            // Proactively clean up any leftover resources from a previously failed deployment.
            // This prevents the 'Unit was already loaded' error from systemd.

            Console.WriteLine($"[{app.Name}] Cleaning up old standby resources for '{standbyContainerName}' before starting...");

            // 1. Stop any running systemd service with the standby name.
            await RunProcessAsync("systemctl", $"--user stop {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);

            // 2. Reset the failed state of the service, in case it failed previously.
            await RunProcessAsync("systemctl", $"--user reset-failed {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);

            // 3. Force-remove any existing podman container with the standby name. (This was already here and is correct)
            await RunProcessAsync("podman", $"rm -f {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
            // --- FIX ENDS HERE ---

            // Now we can safely create the new service.
            await RunProcessAsync(
                "systemd-run",
                $"--user --service-type=exec --unit={standbyContainerName} podman run --rm --name {standbyContainerName} -p {standbyPort}:80 {app.ImageName}:latest",
                RepoPath,
                cancellationToken: cancellationToken);

            Console.WriteLine($"[{app.Name}] Performing health check... waiting 5 seconds.");
            await Task.Delay(5000, cancellationToken);
            if (!await IsHealthy(standbyPort, cancellationToken))
            {
                var failureMsg = $"Health check FAILED for container {standbyContainerName} on port {standbyPort}. Aborting deployment.";
                Console.WriteLine($"!!! [{app.Name}] {failureMsg}");
                await SendNotificationAsync(StatusLevel.Error, $"[{app.Name}] Deployment FAILED", failureMsg);
                // Also attempt to clean up the failed container and service
                await RunProcessAsync("systemctl", $"--user stop {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
                await RunProcessAsync("systemctl", $"--user reset-failed {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
                return;
            }
            Console.WriteLine($"[{app.Name}] Health check PASSED.");

            Console.WriteLine($"[{app.Name}] Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {app.NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken);

            await File.WriteAllTextAsync(app.StateFile, standbyColor, cancellationToken);

            // Record the commit hash that was successfully deployed
            await RecordDeployedCommit(app);

            var successMsg = $"Deployment successful! {standbyColor} is now LIVE. Commit: {_currentHeadCommit[..Math.Min(8, _currentHeadCommit.Length)]}";
            Console.WriteLine($"SUCCESS! [{app.Name}] {successMsg}");
            await SendNotificationAsync(StatusLevel.Success, $"[{app.Name}] Deployment Successful", successMsg);

            // Stop the old live service via systemd, which will in turn stop the container.
            await RunProcessAsync("systemctl", $"--user stop {liveContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);

            Console.WriteLine($"[{app.Name}] Pruning old container images...");
            await RunProcessAsync("podman", "image prune -f", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);

            Console.WriteLine($"--- [{app.Name}] Deployment Complete ---");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{app.Name}] Deployment cancelled.");
            await SendNotificationAsync(StatusLevel.Warning, $"[{app.Name}] Deployment Cancelled", "The deployment process was cancelled.");
        }
        catch (Exception ex)
        {
            var errorMsg = $"An unexpected error occurred during [{app.Name}] deployment: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            Console.WriteLine($"!!! {errorMsg}");
            await SendNotificationAsync(StatusLevel.Error, $"[{app.Name}] Deployment FAILED", errorMsg);
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

