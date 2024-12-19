using System;
using System.Runtime.InteropServices;
using System.Diagnostics;


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
    public static void OpenFileWithDefaultProgram(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use Process.Start with "explorer.exe" and the file path.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + filePath + "\"" // Important: Quote the path in case it contains spaces.
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
}