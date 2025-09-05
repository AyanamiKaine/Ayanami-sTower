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
    // Cached refs & changed files between local and remote (populated after fetch)
    private static string? _latestLocalRef;
    private static string? _latestRemoteRef;
    private static List<string> _changedFilesBetweenRefs = new();

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
            NginxUpstreamConfig = "/etc/nginx/wiki_upstream.conf",
            BluePort = 9080,
            GreenPort = 9081
        }
    };

    public static async Task Main(string[] _)
    {
        Console.WriteLine($"\n--- {DateTime.Now}: C# Multi-Deployer started. ---");

        // Check for new commits in the entire repo first.
        bool hasAnyNewCommits = await HasNewCommitsInRepo();
        if (hasAnyNewCommits)
        {
            Console.WriteLine("New commits detected in the repository. Pulling latest changes...");
            // _changedFilesBetweenRefs was populated by HasNewCommitsInRepo (it runs after fetch)
            await RunProcessAsync("git", "pull", RepoPath);
        }

        // Iterate through each defined application and trigger its deployment logic.
        foreach (var app in AppsToDeploy)
        {
            Console.WriteLine($"\n>>>>>> Processing Application: {app.Name} <<<<<<");
            await TriggerDeploymentForApp(app, hasAnyNewCommits);
        }

        Console.WriteLine("\n--- Multi-Deployer run complete. ---\n");
    }

    private static async Task<bool> HasNewCommitsInRepo()
    {
        // Fetch remote refs first, then compare local vs remote and capture changed files
        await RunProcessAsync("git", "fetch", RepoPath);
        try
        {
            _latestLocalRef = (await RunProcessAsync("git", "rev-parse @", RepoPath)).Trim();
            _latestRemoteRef = (await RunProcessAsync("git", "rev-parse @{u}", RepoPath)).Trim();

            if (string.Equals(_latestLocalRef, _latestRemoteRef, StringComparison.Ordinal))
            {
                _changedFilesBetweenRefs = new List<string>();
                return false;
            }

            var nameOnly = await RunProcessAsync("git", $"diff --name-only {_latestLocalRef} {_latestRemoteRef}", RepoPath);
            _changedFilesBetweenRefs = nameOnly
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (_changedFilesBetweenRefs.Count > 0)
            {
                Console.WriteLine($"Changed files detected: {string.Join(", ", _changedFilesBetweenRefs)}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to determine changed files between refs: {ex.Message}");
            // Fallback to conservative behavior: indicate there are changes so we pull, but per-path checks will be less precise.
            _changedFilesBetweenRefs = new List<string>();
            return true;
        }
    }

    // Check whether any of the changed files affect the given project path
    private static bool ChangedFilesAffectPath(string projectPath)
    {
        if (_changedFilesBetweenRefs == null || _changedFilesBetweenRefs.Count == 0)
            return false;

        // Convert projectPath into repo-relative path (unix-style) and trim trailing slashes
        var relProject = Path.GetRelativePath(RepoPath, projectPath).Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(relProject)) return true; // defensive: root changes affect everything

        foreach (var changed in _changedFilesBetweenRefs)
        {
            var norm = changed.Replace('\\', '/').Trim('/');
            if (string.Equals(norm, relProject, StringComparison.Ordinal) || norm.StartsWith(relProject + "/", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    // Checks if a specific path within the repo has new commits.
    private static async Task<bool> HasNewCommitsForPath(string projectPath)
    {
        string relativePath = Path.GetRelativePath(RepoPath, projectPath);
        string result = await RunProcessAsync("git", $"log HEAD..@{{u}} --oneline -- \"{relativePath}\"", RepoPath);
        return !string.IsNullOrWhiteSpace(result.Trim());
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

    private static async Task TriggerDeploymentForApp(AppConfig app, bool repoHasChanges, CancellationToken cancellationToken = default)
    {
        try
        {
            bool isLive = await IsLiveContainerRunning(app);
            // We deploy if the container isn't running, OR if the repo was updated AND the specific project has changes.
            bool hasNewCommitsForApp = repoHasChanges && ChangedFilesAffectPath(app.ProjectPath);

            if (isLive && !hasNewCommitsForApp)
            {
                Console.WriteLine($"[{app.Name}] Site is running and no new commits found for this project. Skipping.");
                return;
            }

            string startReason = !isLive
                ? $"[{app.Name}] Live container is not running. Forcing deployment to recover."
                : $"[{app.Name}] New commits detected for this project. Starting deployment.";

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

            await RunProcessAsync("podman", $"rm -f {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
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
                return;
            }
            Console.WriteLine($"[{app.Name}] Health check PASSED.");

            Console.WriteLine($"[{app.Name}] Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {app.NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken);

            await File.WriteAllTextAsync(app.StateFile, standbyColor, cancellationToken);

            var successMsg = $"Deployment successful! {standbyColor} is now LIVE.";
            Console.WriteLine($"SUCCESS! [{app.Name}] {successMsg}");
            await SendNotificationAsync(StatusLevel.Success, $"[{app.Name}] Deployment Successful", successMsg);

            await RunProcessAsync("podman", $"stop {liveContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);

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

    // (IsHealthy and RunProcessAsync methods remain unchanged from your original script)
    // ... Please include the full IsHealthy and RunProcessAsync methods from your script here ...
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
