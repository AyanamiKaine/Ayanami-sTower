using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;


namespace Avalonia.Flecs.StellaLearning.Util;

/// <summary>
/// Provides methods to open a file with the default program registered in the
/// desktop eviroment. For windows we use the explorer and for linux
/// and macos we use xdg-open (KDE and GNOME respect this) and open respectively.
/// </summary>
public static class FileOpener
{
    /// <summary>
    /// Opens a file with the default program registered in the desktop environment.
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static void OpenFileWithDefaultProgram(string filePath)
    {
        // Remove double quotes only if they enclose the entire filepath
        // We are doing this because when the user copies paths they usally come with double quotes
        if (filePath.StartsWith('"') && filePath.EndsWith('"'))
        {
            filePath = filePath[1..^1];
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File:{filePath} not found", filePath);
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                // Replace backslashes with forward slashes for Windows.
                filePath = filePath.Replace(@"\\", @"\");

                // Windows: Use Process.Start with "explorer.exe" and the file path.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + filePath + "\"", // Important: Quote the path in case it contains spaces.
                });

            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Linux/macOS: Use xdg-open (Linux) or open (macOS)
                string opener = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open";

                Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = opener,
                        Arguments = "\"" + filePath + "\"", // Quote the path
                        UseShellExecute = false, // Required for redirection
                        CreateNoWindow = true, // Optional: Don't show a console window
                        RedirectStandardError = true // Capture error output for debugging
                    }
                };
                process.Start();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    // Handle errors (e.g., file not found, no application associated)
                    Console.WriteLine($"Error opening file: {error}");
                    throw new Exception($"Error opening file: {error}");
                }

            }
            else
            {
                // Handle other operating systems or throw an exception.
                throw new PlatformNotSupportedException("Operating system not supported.");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., file not found, no associated program).
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens a markdown file with Obsidian if the file is part of an Obsidian vault.
    /// We must supply the path to the Obsidian executable and the markdown file
    /// must be part of an Obsidian vault.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="obsidianPath"></param>
    public static void OpenMarkdownFileWithObsidian(string filePath, string obsidianPath)
    {
        // Remove double quotes only if they enclose the entire filepath
        // We are doing this because when the user copies paths they usally come with double quotes
        if (filePath.StartsWith('"') && filePath.EndsWith('"'))
        {
            filePath = filePath[1..^1];
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Replace backslashes with forward slashes for Windows.
            filePath = filePath.Replace(@"/", @"\");
        }

        if (filePath.EndsWith(".md") && IsFileInVault(filePath))
        {
            OpenFileInVault(filePath, obsidianPath);
        }
    }

    private static bool IsFileInVault(string path)
    {
        // Recursively check for .obsidian folder up the directory tree
        DirectoryInfo? directory = File.Exists(path) ? new FileInfo(path).Directory : new DirectoryInfo(path);
        while (directory != null && directory.Exists)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".obsidian")))
            {
                return true;
            }

            if (directory.FullName == directory.Parent?.FullName)
            {
                break; // Exit the loop if we're at the root
            }

            directory = directory.Parent;
        }
        return false;
    }

    private static void OpenFileInVault(string path, string obsidianPath)
    {
        try
        {
            Console.WriteLine($"Opening file in Obsidian: {obsidianPath} obsidian://open?path={path}");
            Process.Start(obsidianPath, $"obsidian://open?path=\"{path}\"");

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = obsidianPath,
                    Arguments = $"obsidian://open?path=\"{path}\"", // Quote the path
                    UseShellExecute = false, // Required for redirection
                    CreateNoWindow = true, // Optional: Don't show a console window
                }
            };
            process.Start();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error opening file in Obsidian: {ex.Message}");
            throw;
        }
    }
}