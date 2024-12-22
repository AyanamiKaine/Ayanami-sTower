using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// Settings Page
/// </summary>
public static class SettingsPage
{
    /// <summary>
    /// Create the settings page
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var settingsProvider = entities.GetEntityCreateIfNotExist("SettingsProvider")
            .Set(new Settings());

        var settingsPage = entities.GetEntityCreateIfNotExist("SettingsPage")
            .Add<Page>()
            .Set(new StackPanel());

        ThemeToggleSwitch(settingsPage, entities);
        ObsidianPath(settingsPage, entities, settingsProvider);
        return settingsPage;
    }
    private static Entity ThemeToggleSwitch(Entity childOfEntity, NamedEntities entities)
    {
        var themeToggleSwitch = entities.GetEntityCreateIfNotExist("ThemeToggleSwitch")
            .ChildOf(childOfEntity)
            .Set(new ToggleSwitch())
            .SetContent("Dark Mode")
            .OnIsCheckedChanged((sender, args) =>
            {
                var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
                if (Application.Current is not null)
                    SetTheme(Application.Current, isDarkMode ? "Dark" : "Light");
            });

        return themeToggleSwitch;
    }

    private static Entity ObsidianPath(Entity childOfEntity, NamedEntities entities, Entity settingsProvider)
    {
        var browseForObsidianButton = entities.GetEntityCreateIfNotExist("BrowseForObsidianButton")
            .Set(new Button());

        var browseForObsidianButtonContent = entities.GetEntityCreateIfNotExist("BrowseForObsidianButtonContent")
            .ChildOf(browseForObsidianButton)
            .Set(new TextBlock())
            .SetText("Browse");


        var toolTipTextBlock = entities.GetEntityCreateIfNotExist("ToolTipTextBlock")
            .Set(new TextBlock())
            .SetText(
                """
                When a obsidian path is set, the application 
                will be able to open the obsidian vault when a
                markdown file is part of an obsidian vault.
                """);

        var obsidianPathTooltip = entities.GetEntityCreateIfNotExist("ObsidianPathTooltip")
            .Set(new ToolTip())
            .SetContent(toolTipTextBlock);

        var obsidianPath = entities.GetEntityCreateIfNotExist("ObsidianPath")
            .ChildOf(childOfEntity)
            .Set(new TextBox())
            .SetText(settingsProvider.Get<Settings>().ObsidianPath)
            .SetWatermark("Path to Obsidian")
            .SetInnerRightContent(browseForObsidianButton)
            .AttachToolTip(obsidianPathTooltip);

        browseForObsidianButton
            .ChildOf(obsidianPath)
            .OnClick(async (e, args) =>
            {
                string newObsidanPath = await ObsidianFilePickerAsync(entities);
                if (newObsidanPath != "" && obsidianPath.GetText() == "")
                    obsidianPath.SetText(newObsidanPath);
                settingsProvider.Get<Settings>().ObsidianPath = obsidianPath.GetText();
                Console.WriteLine("New Obsidian Path:" + settingsProvider.Get<Settings>().ObsidianPath);
            });

        return obsidianPath;
    }

    private static async Task<string> ObsidianFilePickerAsync(NamedEntities entities)
    {
        // Create and configure the file picker options
        var options = new FilePickerOpenOptions
        {
            Title = "Select the obsidian executable",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
        };

        // Create an OpenFileDialog instance
        IReadOnlyList<IStorageFile> result = await entities["MainWindow"].Get<Window>().StorageProvider.OpenFilePickerAsync(options);

        if (result != null && result?.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            // Do something with the file path, e.g., display it in a TextBox
            return file.Path.AbsolutePath;
        }
        return "";
    }

    private static void SetTheme(Application app, string theme)
    {
        // Get the current app theme variant
        var currentThemeVariant = app.ActualThemeVariant;

        // Determine the new theme variant based on the input string

        var newThemeVariant = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase) ? ThemeVariant.Dark : ThemeVariant.Light;

        // Only update the theme if it has changed
        if (currentThemeVariant != newThemeVariant)
        {
            app.RequestedThemeVariant = newThemeVariant;
        }
    }
}

