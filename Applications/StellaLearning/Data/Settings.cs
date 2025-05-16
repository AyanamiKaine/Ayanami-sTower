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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using AyanamisTower.StellaLearning.Util.NoteHandler;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AyanamisTower.StellaLearning.Data;

/// <summary>
/// Settings for the application
/// </summary>
public partial class Settings : ObservableObject
{
    /// <summary>
    /// Event that is raised when the dark mode setting changes.
    /// The boolean payload indicates whether dark mode is enabled (true) or disabled (false).
    /// </summary>
    public event EventHandler<bool>? DarkModeChanged;

    /// <summary>
    /// Whether the application is allowed to send notifcations
    /// </summary>
    [ObservableProperty]
    private bool _enableNotifications = true;


    /*
    TODO:
    Should we support multiple note apps, we maybe want to created an 
    interface for such apps. Something like a filepath to the app executable
    a filepath to the contents (if it exists), methods that make it easier to
    get, like start app, etc. I would have to think more about what contract 
    such an interface should adhere to.
    */

    /// <summary>
    /// The list of all imported obsidian vaults
    /// </summary>
    [ObservableProperty]
    private List<string> _obsidianVaultsFilePath = [];

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
    /// Whether the application is in dark mode.
    /// Setting this property will automatically update the application's theme
    /// and raise the DarkModeChanged event.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLightMode))] // Ensures IsLightMode also notifies changes
    private bool _isDarkMode = false;

    /// <summary>
    /// Gets a value indicating whether the application is in light mode.
    /// This is the inverse of IsDarkMode.
    /// </summary>
    public bool IsLightMode => !IsDarkMode;

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
    /// All created obsidian file watchers.
    /// </summary>
    public readonly List<ObsidianVaultWatcher> ObsidianVaultWatchers = [];

    [ObservableProperty]
    private bool _syncObsidianVaults = true;

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
        _syncObsidianVaults = true;

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
    /// Partial method called by the setter of the IsDarkMode property (generated by ObservableProperty).
    /// This is where we hook in to apply the theme and raise the event.
    /// </summary>
    /// <param name="oldValue">The previous value of IsDarkMode.</param>
    /// <param name="newValue">The new value of IsDarkMode.</param>
    partial void OnIsDarkModeChanged(bool oldValue, bool newValue)
    {
        // Apply the theme change to the application
        ApplyTheme(newValue);

        // Raise the DarkModeChanged event
        DarkModeChanged?.Invoke(this, newValue);
    }

    /// <summary>
    /// Applies the theme (Dark/Light) to the current Avalonia application.
    /// </summary>
    /// <param name="enableDarkMode">True to enable dark mode, false for light mode.</param>
    private static void ApplyTheme(bool enableDarkMode)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = enableDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
        else
        {
            // This might happen if settings are accessed very early or outside a UI thread
            // with an active Application instance.
            Console.WriteLine("[Settings] Warning: Application.Current is null. Theme cannot be applied at this moment.");
        }
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
}
