/*
Stella Learning is a modern learning app.
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NLog;

namespace AyanamisTower.StellaLearning.Util;

/// <summary>
/// Provides methods to open a file with the default program registered in the
/// desktop eviroment. For windows we use the explorer and for linux
/// and macos we use xdg-open (KDE and GNOME respect this) and open respectively.
/// </summary>
public static class FileOpener
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Opens a file with the default program registered in the desktop environment.
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static void OpenFileWithDefaultProgram(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Logger.Error("Attempted to open file with null or empty path.");
            throw new ArgumentNullException(nameof(filePath));
        }

        // 1. Trim surrounding quotes if present
        if (filePath.Length > 1 && filePath.StartsWith('"') && filePath.EndsWith('"'))
        {
            filePath = filePath[1..^1];
            Logger.Trace($"Trimmed quotes, path is now: {filePath}");
        }

        // 2. Normalize the path (resolves relative paths, cleans up slashes)
        try
        {
            // Important: GetFullPath might require appropriate file system permissions itself.
            filePath = Path.GetFullPath(filePath);
            Logger.Trace($"Normalized path: {filePath}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error normalizing path: {filePath}");
            throw new ArgumentException($"Invalid file path: {filePath}", nameof(filePath), ex);
        }

        // 3. Check if file exists (using the normalized path)
        if (!File.Exists(filePath))
        {
            Logger.Error($"File not found: {filePath}");
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        Logger.Info($"Attempting to open file: {filePath}");

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use ProcessStartInfo with UseShellExecute = true.
                // This is the standard way to ask Windows to open a file with its default app.
                // It uses the file path directly as the target.
                ProcessStartInfo startInfo = new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true,
                };
                Logger.Debug(
                    $"Windows: Starting process with UseShellExecute=true, FileName='{startInfo.FileName}'"
                );
                Process.Start(startInfo);
                // NOTE: Success here only means the request was sent to the OS shell.
                // Failure in the target app often won't cause an exception here.
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Use xdg-open
                const string opener = "xdg-open";
                Logger.Debug($"Linux: Starting process: {opener} \"{filePath}\"");
                StartProcessUnix(opener, filePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: Use open
                const string opener = "open";
                Logger.Debug($"macOS: Starting process: {opener} \"{filePath}\"");
                StartProcessUnix(opener, filePath);
            }
            else
            {
                Logger.Error(
                    $"Operating system not supported for opening files: {RuntimeInformation.OSDescription}"
                );
                throw new PlatformNotSupportedException(
                    "Operating system not supported for opening files."
                );
            }
            Logger.Info($"Successfully initiated opening file: {filePath}");
        }
        // Catch exceptions specifically from Process.Start if it fails immediately
        // (e.g., Win32Exception if UseShellExecute=true fails, or other issues)
        catch (System.ComponentModel.Win32Exception winEx)
        {
            Logger.Error(
                winEx,
                $"Win32 Error opening file '{filePath}'. ErrorCode: {winEx.ErrorCode}, NativeErrorCode: {winEx.NativeErrorCode}"
            );
            // Consider showing a user-friendly message here
            MessageDialog.ShowErrorDialog(
                $"Failed to open file '{Path.GetFileName(filePath)}'.\nReason: {winEx.Message}\nCheck file associations and permissions."
            );
            // Re-throw or handle as needed
            throw new Exception($"Failed to open file '{filePath}'.", winEx);
        }
        catch (Exception ex)
        {
            // Catch other potential exceptions during the process start.
            Logger.Error(ex, $"An unexpected error occurred trying to open file: {filePath}");
            MessageDialog.ShowErrorDialog(
                $"An unexpected error occurred while trying to open '{Path.GetFileName(filePath)}':\n{ex.Message}"
            );
            // Re-throw the original exception or a new one wrapping it
            throw;
        }
    }

    // Helper for Linux/macOS process start with error checking
    private static void StartProcessUnix(string command, string filePathArgument)
    {
        // Use ProcessStartInfo to ensure arguments are handled correctly, especially quoting
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = $"\"{filePathArgument}\"", // Quote the path
            UseShellExecute = false, // Required for redirection
            CreateNoWindow = true, // Optional: Don't show a console window
            RedirectStandardError = true, // Capture error output
            RedirectStandardOutput =
                true // Capture standard output as well (optional)
            ,
        };

        using Process process = new Process { StartInfo = startInfo };
        process.Start();

        // Read output/error asynchronously is generally safer to avoid deadlocks,
        // but for quick commands like open/xdg-open, synchronous might be okay.
        string error = process.StandardError.ReadToEnd();
        string output = process.StandardOutput.ReadToEnd(); // Capture standard output too

        process.WaitForExit();

        // Log output/error regardless of exit code for debugging
        if (!string.IsNullOrWhiteSpace(output))
        {
            Logger.Debug($"{command} stdout: {output}");
        }
        if (!string.IsNullOrWhiteSpace(error))
        {
            Logger.Warn($"{command} stderr: {error}"); // Log error as warning
        }

        // Check exit code AFTER reading streams
        if (process.ExitCode != 0)
        {
            string errorMessage = string.IsNullOrWhiteSpace(error)
                ? $"Command '{command}' failed with exit code {process.ExitCode} but no error message."
                : $"Command '{command}' failed with exit code {process.ExitCode}. Error: {error}";

            Logger.Error(errorMessage);
            // Provide error message back to the user
            MessageDialog.ShowErrorDialog(
                $"Failed to open file '{Path.GetFileName(filePathArgument)}' using {command}.\nDetails: {error}"
            );
            throw new Exception(errorMessage);
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
        DirectoryInfo? directory = File.Exists(path)
            ? new FileInfo(path).Directory
            : new DirectoryInfo(path);
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
            Console.WriteLine(
                $"Opening file in Obsidian: {obsidianPath} obsidian://open?path={path}"
            );
            Process.Start(obsidianPath, $"obsidian://open?path=\"{path}\"");

            Process process =
                new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = obsidianPath,
                        Arguments = $"obsidian://open?path=\"{path}\"", // Quote the path
                        UseShellExecute = false, // Required for redirection
                        CreateNoWindow = true, // Optional: Don't show a console window
                    },
                };
            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening file in Obsidian: {ex.Message}");
            throw;
        }
    }
}
