using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AyanamisTower.PersonalWebsite;

static class DeploymentService
{
    // --- Configuration ---
    // All paths are absolute to avoid ambiguity when run as a service.
    private const string RepoPath = "/home/ayanami/Ayanami-sTower/";
    private const string ImageName = "my-personal-website";
    private const string StateFile = "/home/ayanami/deployment.state";
    private const string ProjectPath = "/home/ayanami/Ayanami-sTower/PersonalWebsite/";

    private const string NginxUpstreamConfig = "/etc/nginx/astro_upstream.conf";

    private const int BluePort = 8080;
    private const int GreenPort = 8081;

    // A timer to "debounce" file system events. This prevents multiple rapid-fire
    // deployments if many files change at once (like during a 'git pull').
    private static Timer? _debounceTimer;

    // The entry point of our application.
    public static async Task Main(string[] _)
    {
        Console.WriteLine("Starting Website Deployment Service...");

        TriggerDeployment();

        using var watcher = new FileSystemWatcher(RepoPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileChanged;

        Console.WriteLine($"Watching for changes in: {RepoPath}");
        Console.WriteLine("Service is running. Press Ctrl+C to exit.");

        // Keep the application running indefinitely.
        await Task.Delay(Timeout.Infinite);
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Change detected: {e.ChangeType} - {e.Name}");

        // Reset the debounce timer every time a change is detected.
        // The deployment will only trigger after things have been quiet for 5 seconds.
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => TriggerDeployment(), null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }

    private static async void TriggerDeployment()
    {
        Console.WriteLine($"\n--- {DateTime.Now}: Debounce timer elapsed. Starting deployment check. ---");

        try
        {
            // Step 2: Determine live and standby environments.
            string liveColor = File.Exists(StateFile) ? await File.ReadAllTextAsync(StateFile) : "blue";
            string standbyColor = liveColor == "blue" ? "green" : "blue";
            int livePort = liveColor == "blue" ? BluePort : GreenPort;
            int standbyPort = liveColor == "blue" ? GreenPort : BluePort;

            Console.WriteLine($"Current LIVE: {liveColor} ({livePort}). Deploying to STANDBY: {standbyColor} ({standbyPort}).");

            // Step 3: Pull changes and build the new image.
            await RunProcessAsync("git", "pull", RepoPath);
            await RunProcessAsync("podman", $"build -t {ImageName}:latest .", ProjectPath);
            // Step 4: Stop any old standby container and start the new one.
            await RunProcessAsync("podman", $"rm -f astro-site-{standbyColor}", workingDirectory: RepoPath, ignoreErrors: true);
            await RunProcessAsync("podman", $"run -d --name astro-site-{standbyColor} -p {standbyPort}:80 {ImageName}:latest", RepoPath);

            // Step 5: Health Check.
            Console.WriteLine("Performing health check...");
            await Task.Delay(5000); // Give the container time to start.
            if (!await IsHealthy(standbyPort))
            {
                Console.WriteLine("!!! Health check FAILED. Aborting deployment.");
                // In a real scenario, you might log the container output here.
                return;
            }
            Console.WriteLine("Health check PASSED.");

            // Step 6: Switch Nginx traffic.
            Console.WriteLine("Switching Nginx traffic...");
            // We need to run 'tee' with sudo, which requires setting up sudoers.
            await RunProcessAsync("sudo", $"tee {NginxUpstreamConfig}", workingDirectory: RepoPath, input: $"server 127.0.0.1:{standbyPort};");
            await RunProcessAsync("sudo", "systemctl reload nginx", RepoPath);

            // Step 7: Update state and clean up.
            await File.WriteAllTextAsync(StateFile, standbyColor);
            Console.WriteLine($"SUCCESS! {standbyColor} is now LIVE.");

            await RunProcessAsync("podman", $"stop astro-site-{liveColor}", workingDirectory: RepoPath, ignoreErrors: true);
            Console.WriteLine("--- Deployment Complete ---\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"!!! An error occurred during deployment: {ex.Message}");
        }
    }

    // Helper method to check for new commits.
    private static async Task<bool> HasNewCommits()
    {
        await RunProcessAsync("git", "fetch", RepoPath);
        var local = await RunProcessAsync("git", "rev-parse @", RepoPath);
        var remote = await RunProcessAsync("git", "rev-parse @{u}", RepoPath);
        return local.Trim() != remote.Trim();
    }

    // Helper method to check the health of a container.
    private static async Task<bool> IsHealthy(int port)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var response = await client.GetAsync($"http://localhost:{port}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // A robust helper method to run external processes like 'git' or 'podman'.
    private static Task<string> RunProcessAsync(string command, string args, string workingDirectory, string? input = null, bool ignoreErrors = false)
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
        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) =>
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (process.ExitCode == 0)
            {
                tcs.SetResult(output);
            }
            else
            {
                if (ignoreErrors)
                {
                    tcs.SetResult(string.Empty);
                }
                else
                {
                    tcs.SetException(new Exception($"Command '{command} {args}' failed with exit code {process.ExitCode}.\nError: {error}"));
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