using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using NLog;
using Avalonia.Flecs.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using StellaLearning.Data;
using Avalonia.Layout;
using Avalonia;

namespace StellaLearning.Pages;

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
        _root = world.UI<ScrollViewer>((scrollViewer) =>
        {
            scrollViewer.Child<StackPanel>((stackPanel) =>
            {
                stackPanel.Child<TextBlock>(header => header.SetText("Settings").SetFontSize(18).SetFontWeight(FontWeight.Bold));


                stackPanel.Child(new ThemeToggleSwitch(world));
                stackPanel.Child(new AppToTray(world));
                stackPanel.Child(new EnableAlwaysOnTop(world));
                stackPanel.Child(new EnableNotificationsToggleSwitch(world));
                stackPanel.Child(new EnableLargeLanguageModelToggleSwitch(world));
                stackPanel.Child(new EnableCloudSavesToggleSwitch(world));
                stackPanel.Child<Separator>((Separator) =>
                {
                    Separator
                    .SetMargin(0, 0, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
                });
                stackPanel.Child(new ObsidianPath(world));
                stackPanel.Child<Separator>((Separator) =>
                {
                    Separator
                    .SetMargin(0, 10, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
                });
                stackPanel.Child<Button>((button) =>
                {
                    button
                    .SetText("Reset Statistics")
                    .SetPointerOverBackground(Brushes.Firebrick)
                    .OnClick(async (_, _) =>
                    {

                        var cd = new ContentDialog()
                        {
                            Title = "Resetting Statistics",
                            Content = "Do you want to reset your statistics?",
                            PrimaryButtonText = "Confirm",
                            DefaultButton = ContentDialogButton.Secondary,
                            SecondaryButtonText = "Deny",
                            IsSecondaryButtonEnabled = true,
                        };

                        cd.PrimaryButtonClick += (s, e) => { };
                        var result = await cd.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            await StatsTracker.Instance.ResetStatsAsync();
                        }
                    });
                });

            });

        }).Add<Page>().Entity;

    }

    private class EnableAlwaysOnTop : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableAlwaysOnTop(World world)
        {
            _root =
            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {

                    textBlock.SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Enable Always on top")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().EnableAlwaysOnTop;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var enableAlwaysOnTop = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.EnableAlwaysOnTop = enableAlwaysOnTop;
                    });
                });


                dockPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        Always show Stella Learning windows on top of other open windows.
                        """);
                    });
                }));
            }).Entity;
        }
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

                    textBlock.SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Enable Notifications")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().EnableNotifications;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var enableNotifications = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.EnableNotifications = enableNotifications;
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
            }).Entity;
        }
    }

    private class EnableLargeLanguageModelToggleSwitch : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableLargeLanguageModelToggleSwitch(World world)
        {
            _root =
            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {

                    textBlock.SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Enable Large Language Features")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().EnableLargeLanguageFeatures;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var enableLargeLanguageFeatures = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.EnableLargeLanguageFeatures = enableLargeLanguageFeatures;
                    });
                });


                dockPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        When enabled automatic meta can be generated for various items like art, and literature (Automatic generation for title, summary, tags). (Paid Feature)
                        """);
                    });
                }));
            }).Entity;
        }
    }

    private class EnableCloudSavesToggleSwitch : IUIComponent
    {
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableCloudSavesToggleSwitch(World world)
        {
            _root =
            world.UI<DockPanel>((dockPanel) =>
            {
                dockPanel.Child<TextBlock>((textBlock) =>
                {

                    textBlock.SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Enable Cloud Saves")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().EnableCloudSaves;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var enableCloudSaves = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.EnableCloudSaves = enableCloudSaves;
                    });
                });


                dockPanel.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        Create backups in the cloud that can be used to restore your data. This includes all meta data, all files, etc. Your data is not stored as plain text but instead is decrypted. Should you lose your account data. You will never be able to access them again!
                        (Paid Feature) (NOT YET IMPLEMENTED)
                        """);
                    });
                }));
            }).Entity;
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
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Dark Mode")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().IsDarkMode;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.IsDarkMode = isDarkMode;
                    });
                });
            }).Entity;
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
                    textBlock.SetVerticalAlignment(VerticalAlignment.Center)
                    .SetText("Close to tray")
                    .SetTextWrapping(TextWrapping.Wrap);
                });

                dockPanel.Child<ToggleSwitch>((toggleSwitch) =>
                {
                    toggleSwitch
                    .SetDock(Dock.Right)
                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                    toggleSwitch.With((toggleSwitch) =>
                    {
                        toggleSwitch.IsChecked = world.Get<Settings>().CloseToTray;
                    });

                    toggleSwitch.OnIsCheckedChanged((sender, args) =>
                    {
                        var closeToTray = ((ToggleSwitch)sender!).IsChecked ?? false;
                        var currentSettings = world.Get<Settings>();
                        currentSettings.CloseToTray = closeToTray;
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
            }).Entity;


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
                                   textBox.SetText(newObsidanPath);
                                   var currentSettings = world.Get<Settings>();
                                   currentSettings.ObsidianPath = newObsidanPath;
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
            }).Entity;
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
}

