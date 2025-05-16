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
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AyanamisTower.StellaLearning.Data;

/// <summary>
/// Settings for the application
/// </summary>
public partial class Settings : ObservableObject
{
    /// <summary>
    /// Whether the application is allowed to send notifcations
    /// </summary>
    [ObservableProperty]
    private bool _enableNotifications = true;

    /// <summary>
    /// Whether the the windows are always on top
    /// </summary>
    [ObservableProperty]
    private bool _enableAlwaysOnTop = false;

    /// <summary>
    /// Whether the application is allowed to send notifcations
    /// </summary>
    [ObservableProperty]
    private bool _enableCloudSaves = true;

    //TODO: I want to implement a more fine grain control so the user can say, yea titles are fine but not descriptions.
    /// <summary>
    /// Whether we are allowed to use large language models to genereate various meta data for
    /// different things.
    /// </summary>
    [ObservableProperty]
    private bool _enableLargeLanguageFeatures = true;

    /// <summary>
    /// Whether the application is in dark mode
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode = false;

    /// <summary>
    /// Whether the application closes to a small tray icon instead. Leaving
    /// the app running in the background.
    /// </summary>
    [ObservableProperty]
    private bool _closeToTray = true;

    /// <summary>
    /// The path to the Obsidian executable
    /// </summary>
    [ObservableProperty]
    private string _obsidianPath = string.Empty;

    /// <summary>
    /// Caching the json serializer options for performance
    /// </summary>
    public static JsonSerializerOptions jsonSerializerOptions =
        new() { IncludeFields = true, WriteIndented = true };

    /// <summary>
    /// Create a new settings object
    /// </summary>
    public Settings()
    {
        // Set initial safe defaults. The deserializer will overwrite these
        // with values from the JSON if they exist.
        _enableNotifications = true;
        _isDarkMode = false; // Default to light mode unless specified in JSON
        _closeToTray = true;
        _obsidianPath = string.Empty; // Default path handled later or in CreateDefaultSettings
        _enableLargeLanguageFeatures = true;
        _enableCloudSaves = true;

        if (_isDarkMode)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }
        // Optionally set OS-specific defaults here if they apply universally to new instances
        if (
            string.IsNullOrEmpty(_obsidianPath)
            && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        )
        {
            _obsidianPath = GetWindowsDefaultObsidianFolderPath();
        }
    }

    private static string GetWindowsDefaultObsidianFolderPath()
    {
        // Get the AppData\Local folder path
        string localAppDataPath = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );

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

                return JsonSerializer.Deserialize<Settings>(jsonContent, jsonSerializerOptions)
                    ?? new Settings();
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

    /// <summary>
    /// Sets the application theme based on the provided theme name.
    /// </summary>
    /// <param name="app">The application instance to apply the theme to.</param>
    /// <param name="theme">The theme name to set. Use "dark" for dark mode, any other value for light mode.</param>
    public static void SetTheme(Application app, string theme)
    {
        // Get the current app theme variant
        var currentThemeVariant = app.ActualThemeVariant;

        // Determine the new theme variant based on the input string

        var newThemeVariant = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        // Only update the theme if it has changed
        if (currentThemeVariant != newThemeVariant)
        {
            app.RequestedThemeVariant = newThemeVariant;
        }
    }
}
