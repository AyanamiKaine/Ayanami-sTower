using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AyanamisTower.PersonalWebsite;

static class DeploymentService
{
    // --- Configuration ---
    private const string RepoPath = "/home/ayanami/Ayanami-sTower/";
    private const string ImageName = "my-personal-website";
    private const string StateFile = "/home/ayanami/deployment.state";
    private const string ProjectPath = "/home/ayanami/Ayanami-sTower/PersonalWebsite/";
    private const string NginxUpstreamConfig = "/etc/nginx/astro_upstream.conf";
    private const int BluePort = 8080;
    private const int GreenPort = 8081;

    private static Timer? _debounceTimer;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    private static bool _deploymentInProgress = false;
    private static readonly object _deploymentLock = new();

    public static async Task Main(string[] _)
    {
        Console.WriteLine("Starting Website Deployment Service...");

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nShutdown requested...");
            _cancellationTokenSource.Cancel();
        };

        try
        {
            // Trigger initial deployment first
            await TriggerDeployment(_cancellationTokenSource.Token);

            // Only watch the source code directory, not the entire repo
            // This prevents git operations from triggering new deployments
            using var watcher = new FileSystemWatcher(ProjectPath) // Watch only the project directory
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Filter out temporary files and build artifacts
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileChanged;

            Console.WriteLine($"Watching for changes in: {ProjectPath}");
            Console.WriteLine("Service is running. Press Ctrl+C to exit.");

            // Wait until cancellation is requested
            await Task.Delay(Timeout.Infinite, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Service shutting down gracefully...");
        }
        finally
        {
            _debounceTimer?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return;

        // Filter out files that shouldn't trigger deployments
        if (ShouldIgnoreFile(e.Name))
        {
            Console.WriteLine($"Ignoring file change: {e.Name}");
            return;
        }

        // Check if deployment is already in progress
        lock (_deploymentLock)
        {
            if (_deploymentInProgress)
            {
                Console.WriteLine($"Deployment in progress, ignoring change: {e.ChangeType} - {e.Name}");
                return;
            }
        }

        Console.WriteLine($"Change detected: {e.ChangeType} - {e.Name}");

        // Reset the debounce timer
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            _ = Task.Run(async () => await TriggerDeployment(_cancellationTokenSource.Token));
        }, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }

    private static bool ShouldIgnoreFile(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return true;

        var name = fileName.ToLowerInvariant();

        // Ignore temporary files, build artifacts, and system files
        return name.EndsWith(".tmp") ||
               name.EndsWith(".swp") ||
               name.EndsWith("~") ||
               name.StartsWith(".") ||
               name.Contains("node_modules") ||
               name.Contains("dist") ||
               name.Contains("build") ||
               name.Contains(".git") ||
               name.EndsWith(".log");
    }

    private static async Task TriggerDeployment(CancellationToken cancellationToken = default)
    {
        // Prevent concurrent deployments
        lock (_deploymentLock)
        {
            if (_deploymentInProgress)
            {
                Console.WriteLine("Deployment already in progress, skipping...");
                return;
            }
            _deploymentInProgress = true;
        }

        try
        {
            Console.WriteLine($"\n--- {DateTime.Now}: Starting deployment. ---");

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
                Console.WriteLine("!!! Health check FAILED. Aborting deployment.");
                return;
            }
            Console.WriteLine("Health check PASSED.");

            // Step 6: Switch Nginx traffic.
            Console.WriteLine("Switching Nginx traffic...");
            await RunProcessAsync("sudo", $"tee {NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};", cancellationToken: cancellationToken);
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath, cancellationToken: cancellationToken);

            // Step 7: Update state and clean up.
            await File.WriteAllTextAsync(StateFile, standbyColor, cancellationToken);
            Console.WriteLine($"SUCCESS! {standbyColor} is now LIVE.");

            await RunProcessAsync("podman", $"stop astro-site-{liveColor}", workingDirectory: RepoPath, ignoreErrors: true, cancellationToken: cancellationToken);
            Console.WriteLine("--- Deployment Complete ---\n");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Deployment cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! An error occurred during deployment: {ex.Message}");
        }
        finally
        {
            lock (_deploymentLock)
            {
                _deploymentInProgress = false;
            }
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