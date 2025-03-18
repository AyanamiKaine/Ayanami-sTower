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
using Avalonia.Media;

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
        world.Component<Settings>("Settings").OnSet((Entity e, ref Settings settings) =>
        {
            if (Application.Current is not null)
                SetTheme(Application.Current, settings.isDarkMode ? "Dark" : "Light");

            Settings.SaveToDisk(settings);
        });

        world.Set(Settings.LoadFromDisk());

        _root = world.UI<ScrollViewer>((scrollViewer) =>
        {
            scrollViewer.Child<StackPanel>((stackPanel) =>
            {
                stackPanel.Child(new ThemeToggleSwitch(world));
                stackPanel.Child(new AppToTray(world));
                stackPanel.Child(new EnableNotificationsToggleSwitch(world));
                stackPanel.Child<Separator>((Separator) =>
                {
                    Separator
                    .SetMargin(0, 0, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
                });
                stackPanel.Child(new ObsidianPath(world));

            });

        }).Add<Page>();

    }

    private class EnableNotificationsToggleSwitch : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableNotificationsToggleSwitch(World world)
        {
            _root =
            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {

                    textBlock.SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetText("Enable Notifications")
                    .SetTextWrapping(Media.TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().enableNotifications;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var enableNotifications = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        world.Set<Settings>(new(
                            isDarkMode: currentSettings.isDarkMode,
                            ObsidianPath: currentSettings.ObsidianPath,
                            closeToTray: currentSettings.closeToTray,
                            enableNotifications: enableNotifications));
                    });
                });


                dockPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        When enabled the app will send desktop notifications where appropriate.
                        """);
                    });
                }));
            });
        }
    }

    private class ThemeToggleSwitch : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public ThemeToggleSwitch(World world)
        {
            _root =

            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {
                    textBlock
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetText("Dark Mode")
                    .SetTextWrapping(Media.TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().isDarkMode;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        world.Set<Settings>(new(
                            isDarkMode: isDarkMode,
                            ObsidianPath: currentSettings.ObsidianPath,
                            closeToTray: currentSettings.closeToTray,
                            enableNotifications: currentSettings.enableNotifications));
                    });
                });
            });
        }
    }

    private class AppToTray : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public AppToTray(World world)
        {
            _root =

            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetText("Close to tray")
                    .SetTextWrapping(Media.TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().closeToTray;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var closeToTray = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        world.Set<Settings>(new(
                            isDarkMode: currentSettings.isDarkMode,
                            ObsidianPath: currentSettings.ObsidianPath,
                            closeToTray: closeToTray,
                            enableNotifications: currentSettings.enableNotifications));
                    });
                });

                dockPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        When toggled on, the app instead of closing will minimize into a tray icon.
                                            
                        The app will run in the background, so you can desktop notifications for example when a new item can be learned.
                        """);
                    });
                }));
            });


        }
    }

    private class ObsidianPath : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public ObsidianPath(World world)
        {
            _root = world.UI<StackPanel>((stackPanel) =>
            {
                stackPanel.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Obsidian Path:");
                    textBlock.SetMargin(0, 0, 0, 10);
                });
                stackPanel.Child<TextBox>((textBox) =>
                   {
                       textBox
                       .SetWatermark("Path to Obsidian")
                       .SetText(world.Get<Settings>().ObsidianPath)
                       .SetInnerRightContent(world.UI<Button>((button) =>
                       {
                           button.Child<TextBlock>((textBlock) => textBlock.SetText("Browse"));

                           button.OnClick(async (e, args) =>
                           {
                               string newObsidanPath = await ObsidianFilePickerAsync();
                               if (newObsidanPath != "")
                               {
                                   _root.SetText(newObsidanPath);
                                   var currentSettings = world.Get<Settings>();
                                   world.Set<Settings>(new(
                                       isDarkMode: currentSettings.isDarkMode,
                                       ObsidianPath: newObsidanPath,
                                       closeToTray: currentSettings.closeToTray,
                                       enableNotifications: currentSettings.enableNotifications));
                               }
                           });
                       }));
                   });

                stackPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
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
        IReadOnlyList<IStorageFile> result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

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

