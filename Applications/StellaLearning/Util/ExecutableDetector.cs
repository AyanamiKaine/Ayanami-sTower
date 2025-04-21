/*
<one line to give the program's name and a brief idea of what it does.>
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