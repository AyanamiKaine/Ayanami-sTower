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
using NLog;
using Avalonia.Flecs.Controls;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// Settings Page
/// </summary>
public class SettingsPage : IUIComponent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    /// <summary>
    /// Create the settings page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public SettingsPage(World world)
    {
        _root = world.UI<StackPanel>((stackPanel) =>
        {
            stackPanel.Child(new ThemeToggleSwitch(world));
            stackPanel.Child(new ObsidianPath(world));
        })
            .Add<Page>();
    }

    private class ThemeToggleSwitch : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public ThemeToggleSwitch(World world)
        {
            _root = world.UI<ToggleSwitch>((toggleSwitch) =>
            {
                toggleSwitch.Child<TextBlock>((textBlock) => textBlock.SetText("Dark Mode"));
                toggleSwitch.OnIsCheckedChanged((sender, args) =>
                {
                    var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
                    if (Application.Current is not null)
                        SetTheme(Application.Current, isDarkMode ? "Dark" : "Light");
                });
            });
        }
    }

    private class ObsidianPath : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;
        public Entity _settingsProvider;

        public ObsidianPath(World world)
        {
            _root = world.UI<TextBox>((textBox) =>
            {
                _settingsProvider = world.Entity()
                    .Set(new Settings());

                App.Entities!["SettingsProvider"] = _settingsProvider;

                textBox
                .SetWatermark("Path to Obsidian")
                .SetText(_settingsProvider.Get<Settings>().ObsidianPath)
                .SetInnerRightContent(world.UI<Button>((button) =>
                {
                    button.Child<TextBlock>((textBlock) => textBlock.SetText("Browse"));

                    button.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                    {
                        toolTip.Child<TextBlock>((textBlock) =>
                        {
                            textBlock.SetText(
                            """
                            When a obsidian path is set, the application 
                            will be able to open the obsidian vault when a
                            markdown file is part of an obsidian vault.
                            """);
                        });
                    }));

                    button.OnClick(async (e, args) =>
                    {
                        string newObsidanPath = await ObsidianFilePickerAsync();
                        if (newObsidanPath != "" && _settingsProvider.GetText()?.Length == 0)
                            _root.SetText(newObsidanPath);
                        _settingsProvider.Get<Settings>().ObsidianPath = _root.GetText();
                        Console.WriteLine("New Obsidian Path:" + _settingsProvider.Get<Settings>().ObsidianPath);
                    });
                }));
            });
        }
    }

    private static async Task<string> ObsidianFilePickerAsync()
    {
        // Create and configure the file picker options
        var options = new FilePickerOpenOptions
        {
            Title = "Select the obsidian executable",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
        };

        // Create an OpenFileDialog instance
        IReadOnlyList<IStorageFile> result = await App.Entities!["MainWindow"].Get<Window>().StorageProvider.OpenFilePickerAsync(options);

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

