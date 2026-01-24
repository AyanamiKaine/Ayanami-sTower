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
using System.Globalization;
using AyanamisTower.Email;

namespace AyanamisTower.MultiDeployer;

/// <summary>
/// A simple file-based logger for deployment diagnostics.
/// Logs are written to a timestamped file and can be reviewed to understand failures or hangs.
/// </summary>
public sealed class DeploymentLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Stopwatch _sessionStopwatch;
    private readonly object _lock = new();
    private readonly string _logFilePath;

    /// <summary>Gets the full path to the current log file.</summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Creates a new deployment logger that writes to a timestamped file in the specified directory.
    /// </summary>
    /// <param name="logDirectory">The directory where log files will be stored.</param>
    public DeploymentLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        _logFilePath = Path.Combine(logDirectory, $"deployment_{timestamp}.log");

        _writer = new StreamWriter(_logFilePath, append: false, Encoding.UTF8) { AutoFlush = true };
        _sessionStopwatch = Stopwatch.StartNew();

        WriteHeader();
    }

    private void WriteHeader()
    {
        _writer.WriteLine("================================================================================");
        _writer.WriteLine($"  DEPLOYMENT LOG - Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($"  Machine: {Environment.MachineName}");
        _writer.WriteLine($"  Log File: {_logFilePath}");
        _writer.WriteLine("================================================================================");
        _writer.WriteLine();
    }

    /// <summary>Logs a message with optional app context and log level.</summary>
    public void Log(string message, string? appName = null, LogLevel level = LogLevel.Info)
    {
        lock (_lock)
        {
            string elapsed = _sessionStopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
            string levelStr = level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Info => "INFO ",
                LogLevel.Warning => "WARN ",
                LogLevel.Error => "ERROR",
                LogLevel.Process => "PROC ",
                _ => "     "
            };

            string prefix = string.IsNullOrEmpty(appName) ? "" : $"[{appName}] ";
            _writer.WriteLine($"[{elapsed}] [{levelStr}] {prefix}{message}");
        }
    }

    /// <summary>Logs the start of a process execution.</summary>
    public void LogProcessStart(string command, string args, string? appName = null)
    {
        Log($">>> STARTING: {command} {args}", appName, LogLevel.Process);
    }

    /// <summary>Logs process standard output.</summary>
    public void LogProcessOutput(string command, string output, string? appName = null)
    {
        if (string.IsNullOrWhiteSpace(output)) return;

        lock (_lock)
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Log($"    [stdout] {line.TrimEnd()}", appName, LogLevel.Debug);
            }
        }
    }

    /// <summary>Logs process standard error output.</summary>
    public void LogProcessError(string command, string error, string? appName = null)
    {
        if (string.IsNullOrWhiteSpace(error)) return;

        lock (_lock)
        {
            foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Log($"    [stderr] {line.TrimEnd()}", appName, LogLevel.Warning);
            }
        }
    }

    /// <summary>Logs the completion of a process with exit code and duration.</summary>
    public void LogProcessComplete(string command, int exitCode, TimeSpan duration, string? appName = null)
    {
        string status = exitCode == 0 ? "SUCCESS" : $"FAILED (exit code {exitCode})";
        Log($"<<< FINISHED: {command} - {status} - Duration: {duration.TotalSeconds:F2}s", appName, LogLevel.Process);
    }

    /// <summary>Logs a section header for organizing log output.</summary>
    public void LogSection(string sectionName, string? appName = null)
    {
        lock (_lock)
        {
            _writer.WriteLine();
            _writer.WriteLine($"--- {(appName != null ? $"[{appName}] " : "")}{sectionName} ---");
            _writer.WriteLine();
        }
    }

    /// <summary>Logs an exception with full stack trace.</summary>
    public void LogException(Exception ex, string context, string? appName = null)
    {
        lock (_lock)
        {
            Log($"EXCEPTION in {context}: {ex.Message}", appName, LogLevel.Error);
            foreach (var line in (ex.StackTrace ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Log($"    {line.TrimEnd()}", appName, LogLevel.Error);
            }
        }
    }

    /// <summary>Disposes the logger and writes the footer.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _writer.WriteLine();
            _writer.WriteLine("================================================================================");
            _writer.WriteLine($"  DEPLOYMENT LOG - Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine($"  Total Duration: {_sessionStopwatch.Elapsed}");
            _writer.WriteLine("================================================================================");
            _writer.Dispose();
        }
    }
}

/// <summary>Log severity levels for deployment logging.</summary>
public enum LogLevel
{
    /// <summary>Detailed diagnostic information.</summary>
    Debug,
    /// <summary>General informational messages.</summary>
    Info,
    /// <summary>Warning messages that don't prevent deployment.</summary>
    Warning,
    /// <summary>Error messages indicating deployment failures.</summary>
    Error,
    /// <summary>Process execution tracking.</summary>
    Process
}

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
    private const string LogDirectory = "/home/ayanami/deployment_logs/";
    private static readonly EmailStatusService _emailService = new();
    private static DeploymentLogger _logger = null!;
    // The current HEAD commit hash after pulling (used to compare against last deployed commit)
    private static string _currentHeadCommit = string.Empty;

    // --- Timeout Configuration ---
    // Different operations have different expected durations
    private static readonly TimeSpan GitTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(30);  // Container builds can take a while
    private static readonly TimeSpan ContainerStartTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan NginxTimeout = TimeSpan.FromSeconds(30);

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
        using var logger = new DeploymentLogger(LogDirectory);
        _logger = logger;

        Console.WriteLine($"\n--- {DateTime.Now}: C# Multi-Deployer started. ---");
        Console.WriteLine($"Log file: {logger.LogFilePath}");

        _logger.Log("Multi-Deployer started");

        bool isForced = args.Contains("-f") || args.Contains("--force");
        if (isForced)
        {
            Console.WriteLine("Force flag detected. All applications will be redeployed regardless of git changes.");
            _logger.Log("Force flag detected - all applications will be redeployed");
        }

        // Always fetch and pull to ensure we're at the latest commit
        Console.WriteLine("Fetching and pulling latest changes...");
        _logger.LogSection("Git Fetch & Pull");
        await FetchAndPullLatest();

        // Iterate through each defined application and trigger its deployment logic.
        foreach (var app in AppsToDeploy)
        {
            Console.WriteLine($"\n>>>>>> Processing Application: {app.Name} <<<<<<");
            _logger.LogSection($"Processing Application", app.Name);
            await TriggerDeploymentForApp(app, isForced);
        }

        Console.WriteLine("\n--- Multi-Deployer run complete. ---\n");
        _logger.Log("Multi-Deployer run complete");
        Console.WriteLine($"Full log available at: {logger.LogFilePath}");
    }

    /// <summary>
    /// Fetches and pulls latest changes, then captures current HEAD commit.
    /// </summary>
    private static async Task FetchAndPullLatest()
    {
        await RunProcessAsync("git", "fetch", RepoPath, timeout: GitTimeout);
        await RunProcessAsync("git", "pull", RepoPath, timeout: GitTimeout);
        _currentHeadCommit = (await RunProcessAsync("git", "rev-parse HEAD", RepoPath, timeout: GitTimeout)).Trim();
        Console.WriteLine($"Current HEAD commit: {_currentHeadCommit}");
        _logger.Log($"Current HEAD commit: {_currentHeadCommit}");
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
            _logger.Log("No deployed commit record found. Will deploy.", app.Name);
            return true;
        }

        string lastDeployedCommit = (await File.ReadAllTextAsync(app.DeployedCommitFile)).Trim();
        _logger.Log($"Last deployed commit: {lastDeployedCommit}", app.Name);

        // If commits are identical, no changes
        if (string.Equals(lastDeployedCommit, _currentHeadCommit, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[{app.Name}] Already at deployed commit {lastDeployedCommit[..Math.Min(8, lastDeployedCommit.Length)]}. No changes.");
            _logger.Log($"Already at deployed commit. No changes.", app.Name);
            return false;
        }

        // Check if there are any changes in the app's project path between the two commits
        try
        {
            string relativePath = Path.GetRelativePath(RepoPath, app.ProjectPath).Replace('\\', '/');
            _logger.Log($"Checking for changes in path: {relativePath}", app.Name);

            string diffResult = await RunProcessAsync(
                "git",
                $"diff --name-only {lastDeployedCommit} {_currentHeadCommit} -- \"{relativePath}\"",
                RepoPath,
                ignoreErrors: true,
                appName: app.Name,
                timeout: GitTimeout);

            var changedFiles = diffResult
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (changedFiles.Count > 0)
            {
                Console.WriteLine($"[{app.Name}] {changedFiles.Count} file(s) changed since last deploy:");
                _logger.Log($"{changedFiles.Count} file(s) changed since last deploy", app.Name);
                foreach (var file in changedFiles.Take(10)) // Show first 10
                {
                    Console.WriteLine($"  - {file}");
                    _logger.Log($"  Changed: {file}", app.Name, LogLevel.Debug);
                }
                if (changedFiles.Count > 10)
                {
                    Console.WriteLine($"  ... and {changedFiles.Count - 10} more");
                }
                return true;
            }

            Console.WriteLine($"[{app.Name}] No changes in project path since last deploy.");
            _logger.Log("No changes in project path since last deploy.", app.Name);
            return false;
        }
        catch (Exception ex)
        {
            // If git diff fails (e.g., commit doesn't exist after force push), treat as needing deployment
            Console.WriteLine($"[{app.Name}] Warning: Could not compare commits ({ex.Message}). Will redeploy to be safe.");
            _logger.LogException(ex, "HasChangesForApp", app.Name);
            _logger.Log("Will redeploy to be safe.", app.Name, LogLevel.Warning);
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
        _logger.Log($"Recorded deployed commit: {_currentHeadCommit}", app.Name);
    }



    private static async Task<bool> IsLiveContainerRunning(AppConfig app)
    {
        if (!File.Exists(app.StateFile))
        {
            _logger.Log("State file does not exist - no live container", app.Name, LogLevel.Debug);
            return false;
        }

        string liveColor = await File.ReadAllTextAsync(app.StateFile);
        string containerName = $"{app.ImageName}-{liveColor}";

        _logger.Log($"Checking if container '{containerName}' is running", app.Name, LogLevel.Debug);
        string result = await RunProcessAsync("podman", $"ps --filter name={containerName} --filter status=running --format \"{{{{.ID}}}}\"", RepoPath, ignoreErrors: true, appName: app.Name, timeout: CleanupTimeout);
        bool isRunning = !string.IsNullOrWhiteSpace(result);
        _logger.Log($"Container '{containerName}' running: {isRunning}", app.Name, LogLevel.Debug);
        return isRunning;
    }

    private static async Task SendNotificationAsync(StatusLevel level, string subject, string message)
    {
        try
        {
            Console.WriteLine($"Sending {level} email: {subject}");
            _logger.Log($"Sending email notification: {subject}", level: LogLevel.Debug);
            await _emailService.SendStatusUpdateAsync(subject, message, level);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! CRITICAL: Failed to send email notification. Error: {ex.Message}");
            _logger.LogException(ex, "SendNotificationAsync");
        }
    }

    private static async Task TriggerDeploymentForApp(AppConfig app, bool isForced, CancellationToken cancellationToken = default)
    {
        var deploymentStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.Log("Starting deployment evaluation", app.Name);

            bool isLive = await IsLiveContainerRunning(app);
            bool hasChanges = await HasChangesForApp(app);

            _logger.Log($"Live container running: {isLive}, Has changes: {hasChanges}, Forced: {isForced}", app.Name);

            if (isLive && !hasChanges && !isForced)
            {
                Console.WriteLine($"[{app.Name}] Site is running and no changes detected. Skipping.");
                _logger.Log("Site is running and no changes detected. Skipping deployment.", app.Name);
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
            _logger.Log(startReason, app.Name);
            await SendNotificationAsync(StatusLevel.Info, $"[{app.Name}] Deployment Started", startReason);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Log("Cancellation requested before deployment started", app.Name, LogLevel.Warning);
                return;
            }

            string liveColor = File.Exists(app.StateFile) ? await File.ReadAllTextAsync(app.StateFile, cancellationToken) : "blue";
            string standbyColor = liveColor == "blue" ? "green" : "blue";
            int livePort = liveColor == "blue" ? app.BluePort : app.GreenPort;
            int standbyPort = liveColor == "blue" ? app.GreenPort : app.BluePort;
            string standbyContainerName = $"{app.ImageName}-{standbyColor}";
            string liveContainerName = $"{app.ImageName}-{liveColor}";

            Console.WriteLine($"[{app.Name}] Current LIVE: {liveColor} ({livePort}). Deploying to STANDBY: {standbyColor} ({standbyPort}).");
            _logger.Log($"Blue-Green state: LIVE={liveColor}:{livePort}, STANDBY={standbyColor}:{standbyPort}", app.Name);

            // Build Docker image with resource limits and reduced priority
            // nice -n 10: Lower CPU priority (range -20 to 19, higher = less priority)
            // ionice -c 2 -n 7: Best-effort I/O class with low priority (0-7, higher = less priority)
            // --memory 2g: Limit build memory to 2GB to prevent OOM
            // --cpus 1.5: Limit to 1.5 CPU cores (leaves headroom for SSH/system)
            // --jobs 2: Limit bun parallelism (passed via build-arg if Dockerfile supports it)
            _logger.LogSection("Building Container Image", app.Name);
            await RunProcessAsync(
                "nice",
                $"-n 10 ionice -c 2 -n 7 podman build --memory 1g --cpus 1 -t {app.ImageName}:latest .",
                app.ProjectPath,
                cancellationToken: cancellationToken,
                appName: app.Name,
                timeout: BuildTimeout);

            // Cleanup old standby resources
            _logger.LogSection("Cleanup Old Standby Resources", app.Name);
            Console.WriteLine($"[{app.Name}] Cleaning up old standby resources for '{standbyContainerName}' before starting...");

            await RunProcessAsync("systemctl", $"--user stop {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);
            await RunProcessAsync("systemctl", $"--user reset-failed {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);
            await RunProcessAsync("podman", $"rm -f {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);

            // Start new container
            _logger.LogSection("Starting New Container", app.Name);
            await RunProcessAsync(
                "systemd-run",
                $"--user --service-type=exec --unit={standbyContainerName} podman run --rm --name {standbyContainerName} -p {standbyPort}:80 {app.ImageName}:latest",
                RepoPath,
                cancellationToken: cancellationToken,
                appName: app.Name,
                timeout: ContainerStartTimeout);

            // Health check
            _logger.LogSection("Health Check", app.Name);
            Console.WriteLine($"[{app.Name}] Performing health check... waiting 5 seconds.");
            _logger.Log("Waiting 5 seconds before health check...", app.Name);
            await Task.Delay(5000, cancellationToken);

            _logger.Log($"Performing health check on port {standbyPort}", app.Name);
            if (!await IsHealthy(standbyPort, cancellationToken))
            {
                var failureMsg = $"Health check FAILED for container {standbyContainerName} on port {standbyPort}. Aborting deployment.";
                Console.WriteLine($"!!! [{app.Name}] {failureMsg}");
                _logger.Log(failureMsg, app.Name, LogLevel.Error);
                _logger.Log($"Deployment failed after {deploymentStopwatch.Elapsed.TotalSeconds:F2}s", app.Name, LogLevel.Error);
                await SendNotificationAsync(StatusLevel.Error, $"[{app.Name}] Deployment FAILED", failureMsg);

                // Cleanup failed container
                _logger.Log("Cleaning up failed container...", app.Name);
                await RunProcessAsync("systemctl", $"--user stop {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);
                await RunProcessAsync("systemctl", $"--user reset-failed {standbyContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);
                return;
            }
            Console.WriteLine($"[{app.Name}] Health check PASSED.");
            _logger.Log("Health check PASSED", app.Name);

            // Switch Nginx traffic
            _logger.LogSection("Switching Nginx Traffic", app.Name);
            Console.WriteLine($"[{app.Name}] Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {app.NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken, appName: app.Name, timeout: NginxTimeout);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken, appName: app.Name, timeout: NginxTimeout);

            await File.WriteAllTextAsync(app.StateFile, standbyColor, cancellationToken);
            _logger.Log($"Updated state file to: {standbyColor}", app.Name);

            // Record the commit hash that was successfully deployed
            await RecordDeployedCommit(app);

            deploymentStopwatch.Stop();
            var successMsg = $"Deployment successful! {standbyColor} is now LIVE. Commit: {_currentHeadCommit[..Math.Min(8, _currentHeadCommit.Length)]}. Duration: {deploymentStopwatch.Elapsed.TotalSeconds:F2}s";
            Console.WriteLine($"SUCCESS! [{app.Name}] {successMsg}");
            _logger.Log(successMsg, app.Name);
            await SendNotificationAsync(StatusLevel.Success, $"[{app.Name}] Deployment Successful", successMsg);

            // Stop the old live service via systemd, which will in turn stop the container.
            _logger.LogSection("Cleanup Old Live Container", app.Name);
            await RunProcessAsync("systemctl", $"--user stop {liveContainerName}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);

            Console.WriteLine($"[{app.Name}] Pruning old container images...");
            _logger.Log("Pruning old container images", app.Name);
            await RunProcessAsync("podman", "image prune -f", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken, appName: app.Name, timeout: CleanupTimeout);

            Console.WriteLine($"--- [{app.Name}] Deployment Complete ---");
            _logger.Log($"Deployment complete. Total time: {deploymentStopwatch.Elapsed}", app.Name);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{app.Name}] Deployment cancelled.");
            _logger.Log("Deployment was cancelled", app.Name, LogLevel.Warning);
            await SendNotificationAsync(StatusLevel.Warning, $"[{app.Name}] Deployment Cancelled", "The deployment process was cancelled.");
        }
        catch (TimeoutException tex)
        {
            var errorMsg = $"[{app.Name}] Deployment TIMED OUT: {tex.Message}\n\nA process hung and was terminated after exceeding its timeout.";
            Console.WriteLine($"!!! {errorMsg}");
            _logger.Log(tex.Message, app.Name, LogLevel.Error);
            _logger.Log($"Deployment timed out after {deploymentStopwatch.Elapsed.TotalSeconds:F2}s", app.Name, LogLevel.Error);
            await SendNotificationAsync(StatusLevel.Error, $"[{app.Name}] Deployment TIMED OUT", errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"An unexpected error occurred during [{app.Name}] deployment: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            Console.WriteLine($"!!! {errorMsg}");
            _logger.LogException(ex, "TriggerDeploymentForApp", app.Name);
            _logger.Log($"Deployment failed after {deploymentStopwatch.Elapsed.TotalSeconds:F2}s", app.Name, LogLevel.Error);
            await SendNotificationAsync(StatusLevel.Error, $"[{app.Name}] Deployment FAILED", errorMsg);
        }
    }

    private static async Task<bool> IsHealthy(int port, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var response = await client.GetAsync($"http://localhost:{port}", cancellationToken);
            _logger.Log($"Health check response: {(int)response.StatusCode} {response.StatusCode}", level: LogLevel.Debug);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Log($"Health check failed with exception: {ex.Message}", level: LogLevel.Warning);
            return false;
        }
    }

    private static async Task<string> RunProcessAsync(
        string command,
        string args,
        string workingDirectory,
        string? input = null,
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default,
        string? appName = null,
        TimeSpan? timeout = null)
    {
        // Default timeout of 10 minutes for most operations
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(10);

        Console.WriteLine($"> Running: {command} {args}");
        _logger.LogProcessStart(command, args, appName);
        _logger.Log($"Timeout set to: {effectiveTimeout.TotalSeconds:F0}s", appName, LogLevel.Debug);

        var processStopwatch = Stopwatch.StartNew();

        using var process = new Process
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

        // Create a combined cancellation token that includes both the user cancellation and the timeout
        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            process.Start();
            _logger.Log($"Process started with PID: {process.Id}", appName, LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to start process '{command}': {ex.Message}", appName, LogLevel.Error);
            throw new Exception($"Failed to start process '{command}'. Is it in your PATH? Error: {ex.Message}");
        }

        // Write input if provided
        if (input != null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        // Read stdout and stderr ASYNCHRONOUSLY to prevent buffer deadlock.
        // This is critical: if we wait for the process to exit before reading,
        // and the output buffer fills up, the process will block forever waiting
        // for the buffer to be drained, causing a deadlock.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            bool isTimeout = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }

            if (isTimeout)
            {
                var timeoutMsg = $"Process TIMED OUT after {effectiveTimeout.TotalSeconds:F0}s: {command} {args}";
                Console.WriteLine($"!!! {timeoutMsg}");
                _logger.Log(timeoutMsg, appName, LogLevel.Error);
                throw new TimeoutException(timeoutMsg);
            }

            _logger.Log($"Process cancelled: {command} {args}", appName, LogLevel.Warning);
            throw;
        }

        processStopwatch.Stop();

        // Now safely read the output (process has exited, buffers are complete)
        string output = await stdoutTask;
        string error = await stderrTask;

        // Log all output to the deployment log
        _logger.LogProcessOutput(command, output, appName);
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogProcessError(command, error, appName);
        }
        _logger.LogProcessComplete(command, process.ExitCode, processStopwatch.Elapsed, appName);

        if (process.ExitCode == 0)
        {
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Warning from command '{command} {args}': {error}");
            }
            return output;
        }

        if (ignoreErrors)
        {
            return string.Empty;
        }

        var fullError = new StringBuilder();
        fullError.AppendLine($"Command '{command} {args}' failed with exit code {process.ExitCode}.");
        if (!string.IsNullOrWhiteSpace(output)) fullError.AppendLine($"Output:\n{output}");
        if (!string.IsNullOrWhiteSpace(error)) fullError.AppendLine($"Error:\n{error}");
        await SendNotificationAsync(StatusLevel.Error, "Deployment FAILED", fullError.ToString());
        throw new Exception(fullError.ToString());
    }
}

