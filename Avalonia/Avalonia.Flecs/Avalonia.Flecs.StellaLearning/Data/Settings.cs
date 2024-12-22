using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Avalonia.Flecs.StellaLearning.Data;

/// <summary>
/// Settings for the application
/// </summary>
public class Settings
{
    /// <summary>
    /// Whether the application is in dark mode
    /// </summary>
    public bool isDarkMode;
    /// <summary>
    /// The path to the Obsidian executable
    /// </summary>
    public string ObsidianPath;

    /// <summary>
    /// Create a new settings object
    /// </summary>
    /// <param name="isDarkMode"></param>
    /// <param name="ObsidianPath"></param>
    public Settings(bool isDarkMode = false, string ObsidianPath = "")
    {
        if (ObsidianPath.Length == 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ObsidianPath = GetWindowsDefaultObsidianFolderPath();

        this.isDarkMode = isDarkMode;
        this.ObsidianPath = ObsidianPath;
    }

    private static string GetWindowsDefaultObsidianFolderPath()
    {
        // Get the AppData\Local folder path
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Construct the full path to the Obsidian folder
        return Path.Combine(localAppDataPath, "Programs", "obsidian", "Obsidian.exe");
    }
}