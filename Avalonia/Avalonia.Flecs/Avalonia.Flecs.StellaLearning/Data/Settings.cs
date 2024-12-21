using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Avalonia.Flecs.StellaLearning.Data;

public class Settings
{
    public bool isDarkMode;
    public string ObsidianPath;

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