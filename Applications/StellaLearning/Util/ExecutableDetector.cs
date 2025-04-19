using System;
using System.IO;
using Mono.Unix;
namespace AyanamisTower.StellaLearning.Util;
/// <summary>
/// Provides utilities for detecting whether files are executable.
/// </summary>
public static class ExecutableDetector
{

    /*
    Design Note:
    This was created because of the need to distinguish files that are to be read like data (for example a png)
    and files that are programs and can be executed.
    */

    /// <summary>
    /// Determines whether the specified file is executable.
    /// </summary>
    /// <param name="filePath">The path to the file to check.</param>
    /// <returns>True if the file exists and is executable; otherwise, false.</returns>
    public static bool IsExecutable(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        // Check extension for Windows executables
        if (Path.GetExtension(filePath).ToLowerInvariant() is string ext &&
            (ext == ".exe" || ext == ".bat" || ext == ".cmd" || ext == ".com"))
        {
            return true;
        }

        // For Unix systems (Linux/macOS), check execute permissions
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                // Use FileInfo to check Unix permissions
                var fileInfo = new FileInfo(filePath);
                var unixFileInfo = UnixFileInfo.GetFileSystemEntry(filePath);
                return unixFileInfo.FileAccessPermissions.HasFlag(FileAccessPermissions.UserExecute) ||
                       unixFileInfo.FileAccessPermissions.HasFlag(FileAccessPermissions.GroupExecute) ||
                       unixFileInfo.FileAccessPermissions.HasFlag(FileAccessPermissions.OtherExecute);
            }
            catch
            {
                // Fallback if Mono.Unix isn't available
                return false;
            }
        }

        return false;
    }
}