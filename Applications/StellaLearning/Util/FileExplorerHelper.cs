using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaLearning.Util;

/// <summary>
/// File explorer helper to use its common features like 
/// opening a folder.
/// </summary>
public static class FileExplorerHelper
{
    /// <summary>
    /// Opens the folder containing the specified file in the native file explorer.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    public static void OpenContainingFolder(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("Error: File path cannot be null or empty.");
            // Or throw new ArgumentNullException(nameof(filePath));
            return;
        }

        try
        {
            // Check if the file itself exists. This is optional but good practice.
            // If you only want to open the directory regardless of whether the file exists,
            // you can skip this File.Exists check.
            if (!File.Exists(filePath))
            {
                // Try to get the directory anyway, in case the file is just a placeholder
                // for a directory path that might exist.
                string directoryPathOnly = Path.GetDirectoryName(filePath) ?? "";
                if (string.IsNullOrEmpty(directoryPathOnly))
                {
                    Console.WriteLine($"Error: Could not determine directory from path: {filePath}");
                    return;
                }
                if (!Directory.Exists(directoryPathOnly))
                {
                    Console.WriteLine($"Error: The directory '{directoryPathOnly}' does not exist.");
                    return;
                }
                Console.WriteLine($"Warning: File '{filePath}' does not exist, but attempting to open its directory: '{directoryPathOnly}'.");
                OpenDirectory(directoryPathOnly);
                return;
            }

            // Get the directory path from the file path.
            string directoryPath = Path.GetDirectoryName(filePath) ?? "";

            if (string.IsNullOrEmpty(directoryPath))
            {
                Console.WriteLine($"Error: Could not determine the directory for the file: {filePath}");
                return;
            }

            // Open the directory in the native file explorer.
            OpenDirectory(directoryPath);

        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Error: The file path '{filePath}' is invalid. {ex.Message}");
        }
        catch (PathTooLongException ex)
        {
            Console.WriteLine($"Error: The file path '{filePath}' is too long. {ex.Message}");
        }
        catch (Exception ex) // Catch other potential exceptions
        {
            Console.WriteLine($"An unexpected error occurred while trying to open the folder for '{filePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the folder containing the specified file in the native file explorer
    /// and attempts to select the file. Works on Windows, macOS, and Linux.
    /// </summary>
    /// <param name="filePath">The full path to the file.</param>
    public static void OpenFolderAndSelectFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("Error: File path cannot be null or empty.");
            return;
        }

        string fullPath = Path.GetFullPath(filePath); // Ensure absolute path

        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"Error: The file '{fullPath}' does not exist.");
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenFolderAndSelectFileWindows(fullPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OpenFolderAndSelectFileMacOs(fullPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OpenFolderAndSelectFileLinux(fullPath);
            }
            else
            {
                Console.WriteLine($"Error: Unsupported operating system: {RuntimeInformation.OSDescription}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }

    private static void OpenFolderAndSelectFileWindows(string filePath)
    {
        string normalizedPath = filePath.Replace('/', '\\'); // Ensure backslashes
        string arguments = $"/select,\"{normalizedPath}\"";

        ProcessStartInfo psi = new ProcessStartInfo("explorer.exe", arguments)
        {
            UseShellExecute = true // explorer.exe works well with UseShellExecute true
        };
        StartProcess(psi, filePath);
    }

    private static void OpenFolderAndSelectFileMacOs(string filePath)
    {
        // The -R flag reveals the file in Finder. filePath can contain spaces,
        // ProcessStartInfo will handle quoting if UseShellExecute is false,
        // but open command itself is simple enough.
        // Arguments should be "-R" and "filePath" separately if not using shell.
        // For simplicity and robustness with paths containing spaces, let shell handle it or quote carefully.

        // Option 1: More direct, ensure filePath is quoted if passing as a single argument string part
        // string arguments = $"-R \"{filePath}\""; // This might not work if filePath itself has quotes
        // ProcessStartInfo psi = new ProcessStartInfo("open", arguments) { UseShellExecute = false };

        // Option 2: Safer if UseShellExecute = false, pass arguments properly.
        // However, 'open' is a shell utility and often works better with UseShellExecute = true
        // or by invoking through /bin/sh -c "open -R ..."
        // The simplest that usually works for `open` is letting the shell figure it out,
        // or using it directly if the arguments are simple.

        // For 'open -R <path>', it's often more reliable to let the arguments be separate
        // if UseShellExecute = false, or ensure perfect shell escaping.
        // Let's try a robust way for ProcessStartInfo:
        ProcessStartInfo psi = new ProcessStartInfo("open")
        {
            ArgumentList = { "-R", filePath }, // Use ArgumentList for clean separation
            UseShellExecute = false // More control
        };
        if (!StartProcess(psi, filePath))
        {
            // Fallback for macOS if -R fails for some reason, just open the directory
            Console.WriteLine($"Fallback: Could not select file on macOS. Opening directory instead for {filePath}.");
            string directory = Path.GetDirectoryName(filePath) ?? "";
            ProcessStartInfo psiDir = new ProcessStartInfo("open", $"\"{directory}\"") // Quote directory path
            {
                UseShellExecute = true // 'open <dir>' is fine with shell execute
            };
            StartProcess(psiDir, directory);
        }
    }

    private static void OpenFolderAndSelectFileLinux(string filePath)
    {
        // Primary attempt: Use D-Bus via dbus-send (Freedesktop standard)
        string fileUri = new Uri(filePath).AbsoluteUri; // Converts to file:///... and handles basic escaping

        // Ensure the fileUri is quoted for the dbus-send command line
        string dbusArguments = $"--session --dest=org.freedesktop.FileManager1 --type=method_call /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"{fileUri}\" string:\"\"";

        ProcessStartInfo psiDbus = new ProcessStartInfo("dbus-send", dbusArguments)
        {
            UseShellExecute = false, // Important for precise argument passing
            RedirectStandardOutput = true, // Optional: capture output
            RedirectStandardError = true   // Optional: capture errors
        };

        Console.WriteLine($"Attempting Linux D-Bus: dbus-send {dbusArguments}");
        if (StartProcess(psiDbus, filePath, true)) // Check exit code
        {
            Console.WriteLine($"Linux D-Bus command executed for: {filePath}");
            return; // Success
        }

        // Fallback 1: Try gio (common on GTK-based systems)
        // gio needs URI correctly formatted.
        Console.WriteLine($"D-Bus failed or not available. Fallback 1: Trying 'gio open --select' for {filePath}");
        ProcessStartInfo psiGio = new ProcessStartInfo("gio")
        {
            ArgumentList = { "open", "--select", fileUri },
            UseShellExecute = false
        };
        if (StartProcess(psiGio, filePath, true))
        {
            Console.WriteLine($"Linux 'gio open --select' command executed for: {filePath}");
            return; // Success
        }

        // Fallback 2: Try specific file managers (less ideal, but can work)
        // Example: Nautilus (GNOME). Add others like dolphin if needed.
        Console.WriteLine($"gio failed. Fallback 2: Trying 'nautilus --select' for {filePath}");
        ProcessStartInfo psiNautilus = new ProcessStartInfo("nautilus")
        {
            ArgumentList = { "--select", filePath },
            UseShellExecute = false
        };
        if (StartProcess(psiNautilus, filePath, true))
        {
            Console.WriteLine($"Linux 'nautilus --select' command executed for: {filePath}");
            return; // Success
        }

        // Last Resort Fallback for Linux: Open the containing directory
        Console.WriteLine($"All select methods failed for Linux. Fallback: Opening containing directory for {filePath} using xdg-open.");
        string directory = Path.GetDirectoryName(filePath) ?? "";
        if (!string.IsNullOrEmpty(directory))
        {
            // xdg-open is usually fine with UseShellExecute = true, as it's a utility script
            // that figures out the default application.
            ProcessStartInfo psiDir = new ProcessStartInfo("xdg-open", $"\"{directory}\"") // Quote directory path
            {
                UseShellExecute = true // xdg-open often works better this way
            };
            StartProcess(psiDir, directory);
        }
        else
        {
            Console.WriteLine($"Error: Could not determine directory for Linux fallback: {filePath}");
        }
    }

    private static bool StartProcess(ProcessStartInfo psi, string pathForLogging, bool checkExitCode = false)
    {
        try
        {
            using Process? process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine($"Error: Failed to start process for {psi.FileName} with args '{psi.Arguments}' for path: {pathForLogging}. Process object is null.");
                return false;
            }

            if (checkExitCode)
            {
                // Optionally, capture output for debugging
                string output = string.Empty;
                string error = string.Empty;
                if (psi.RedirectStandardOutput) output = process.StandardOutput.ReadToEnd();
                if (psi.RedirectStandardError) error = process.StandardError.ReadToEnd();

                process.WaitForExit(5000); // Wait for 5 seconds
                if (!process.HasExited)
                {
                    process.Kill(); // Kill if it hangs
                    Console.WriteLine($"Process {psi.FileName} for {pathForLogging} timed out and was killed.");
                    return false;
                }
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Error: Process {psi.FileName} for {pathForLogging} exited with code {process.ExitCode}.");
                    if (!string.IsNullOrEmpty(output)) Console.WriteLine($"Output: {output}");
                    if (!string.IsNullOrEmpty(error)) Console.WriteLine($"Error output: {error}");
                    return false;
                }
            }
            Console.WriteLine($"Successfully requested action for: {pathForLogging} using {psi.FileName}.");
            return true;
        }
        catch (Win32Exception ex) // Catches "file not found" for the executable, "access denied", etc.
        {
            Console.WriteLine($"Error starting process {psi.FileName} for '{pathForLogging}': {ex.Message} (Win32Exception). Check if '{psi.FileName}' is installed and in PATH.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while starting process {psi.FileName} for '{pathForLogging}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Opens the specified directory path in the native file explorer.
    /// </summary>
    /// <param name="directoryPath">The full path to the directory.</param>
    public static void OpenDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            Console.WriteLine("Error: Directory path cannot be null or empty.");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Error: The directory '{directoryPath}' does not exist.");
            return;
        }

        try
        {
            // Using Process.Start with "explorer.exe" and the directory path as an argument
            // is a common way to open a folder on Windows.
            // For cross-platform compatibility, using ProcessStartInfo with UseShellExecute = true
            // is generally preferred as it lets the OS decide how to open the directory.

            ProcessStartInfo startInfo = new()
            {
                Arguments = directoryPath,
                FileName = "explorer.exe" // For Windows.
                                          // On other OS, you might need different commands or rely solely on UseShellExecute
            };

            // A more cross-platform friendly way (relies on OS to handle directory opening)
            // ProcessStartInfo startInfo = new ProcessStartInfo
            // {
            //     FileName = directoryPath,
            //     UseShellExecute = true,
            //     Verb = "open"
            // };


            // On Windows, you can also use "explorer.exe /select, \"filePath\"" to open the folder
            // and select the specific file. If you only want to open the folder, providing
            // just the directory path to explorer.exe is sufficient.

            Process.Start(startInfo);

            Console.WriteLine($"Successfully requested to open directory: {directoryPath}");
        }
        catch (Win32Exception winEx)
        {
            // This exception can occur if explorer.exe is not found or if there's an issue starting the process.
            Console.WriteLine($"Error opening directory '{directoryPath}': {winEx.Message}. Make sure a file explorer is available.");
        }
        catch (FileNotFoundException)
        {
            // This can happen if 'explorer.exe' (or the specified FileName) isn't found.
            Console.WriteLine($"Error: The file explorer executable was not found. Cannot open directory '{directoryPath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while trying to open the directory '{directoryPath}': {ex.Message}");
        }
    }
}
