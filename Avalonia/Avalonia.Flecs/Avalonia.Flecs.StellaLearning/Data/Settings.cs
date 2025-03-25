using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Avalonia.Flecs.StellaLearning.Data;

/// <summary>
/// Settings for the application
/// </summary>
public class Settings
{
    /// <summary>
    /// Whether the application is allowed to send notifcations
    /// </summary>
    public bool enableNotifications = true;
    /// <summary>
    /// Whether the application is in dark mode
    /// </summary>
    public bool isDarkMode = false;
    /// <summary>
    /// Whether the application closes to a small tray icon instead. Leaving
    /// the app running in the background.
    /// </summary>
    public bool closeToTray = true;
    /// <summary>
    /// The path to the Obsidian executable
    /// </summary>
    public string ObsidianPath = string.Empty;
    /// <summary>
    /// Caching the json serializer options for performance
    /// </summary>
    public static JsonSerializerOptions jsonSerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true
    };
    /// <summary>
    /// Create a new settings object
    /// </summary>
    /// <param name="isDarkMode"></param>
    /// <param name="ObsidianPath">The obsidian path is used for markdown files, if set we will try to open the markdown file with obsidian.</param>
    /// <param name="closeToTray">When set to true, the app will close to tray instead of exiting completly.</param>
    /// <param name="enableNotifications">Notifications are used to show that new spaced repetition items can be learned or other important user notifications.</param>
    public Settings(bool isDarkMode = false, string ObsidianPath = "", bool closeToTray = true, bool enableNotifications = true)
    {
        if (ObsidianPath.Length == 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ObsidianPath = GetWindowsDefaultObsidianFolderPath();

        this.closeToTray = closeToTray;
        this.isDarkMode = isDarkMode;
        this.ObsidianPath = ObsidianPath;
        this.enableNotifications = enableNotifications;
    }

    private static string GetWindowsDefaultObsidianFolderPath()
    {
        // Get the AppData\Local folder path
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Construct the full path to the Obsidian folder
        return Path.Combine(localAppDataPath, "Programs", "obsidian", "Obsidian.exe");
    }

    /// <summary>
    /// Loads settings from disk, or returns default settings if the file doesn't exist or an error occurs.
    /// </summary>
    /// <returns>The loaded settings, or default settings if loading fails.</returns>
    public static Settings LoadFromDisk()
    {
        // Define the path for the settings file
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string settingsFolder = Path.Combine(appDataFolder, "StellaLearning");
        string settingsFile = Path.Combine(settingsFolder, "settings.json");

        // Check if the file exists
        if (File.Exists(settingsFile))
        {
            try
            {
                // Read the file content
                string jsonContent = File.ReadAllText(settingsFile);

                return JsonSerializer.Deserialize<Settings>(jsonContent, jsonSerializerOptions) ?? new Settings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        // Return default settings if file doesn't exist or there's an error
        return new Settings();
    }

    /// <summary>
    /// Saves settings to disk in JSON format.
    /// </summary>
    /// <param name="settings">The settings object to save.</param>
    public static void SaveToDisk(Settings settings)
    {
        // Define the path for the settings file
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string settingsFolder = Path.Combine(appDataFolder, "StellaLearning");
        string settingsFile = Path.Combine(settingsFolder, "settings.json");

        // Ensure directory exists
        Directory.CreateDirectory(settingsFolder);

        try
        {
            // Serialize the settings object
            string jsonContent = JsonSerializer.Serialize(settings, jsonSerializerOptions);

            // Write to the file
            File.WriteAllText(settingsFile, jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}