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
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AyanamisTower.StellaLearning.Data;
using AyanamisTower.StellaLearning.Extensions;
using AyanamisTower.StellaLearning.Util.NoteHandler;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace AyanamisTower.StellaLearning.Pages;

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
        var ObsidianSettings = new List<IUIComponent>()
        {
            new ImportObsidianVault(world),
            new ObsidianPath(world),
        };
        _root = world
            .UI<ScrollViewer>(
                (scrollViewer) =>
                {
                    scrollViewer.SetMargin(50, 7, 0, 20);
                    scrollViewer.Child<StackPanel>(
                        (stackPanel) =>
                        {
                            stackPanel.SetMargin(0, 0, 20, 0).SetSpacing(10);
                            stackPanel.Child<TextBlock>(header =>
                                header
                                    .SetText("Settings")
                                    .SetFontSize(18)
                                    .SetFontWeight(FontWeight.Bold)
                            );
                            stackPanel.Child<SettingsExpander>(list =>
                            {
                                list.SetHeader("Application Settings")
                                    .SetItemTemplate(
                                        world.CreateTemplate<IUIComponent, Panel>(
                                            (panel, item) =>
                                            {
                                                panel.Child(item);
                                                // Because we want that those ui components stay forever we dont destroy them
                                                // when they are not visually attached anymore. This should not result in a
                                                // memory leak. If we cleaned them up we would instead get a crash in flecs.
                                            },
                                            shouldBeCleanedUp: false
                                        )
                                    )
                                    .SetItemsSource(
                                        new List<IUIComponent>()
                                        {
                                            new AppToTray(world),
                                            new EnableAlwaysOnTop(world),
                                            new EnableNotificationsToggleSwitch(world),
                                        }
                                    );
                            });

                            stackPanel.Child<SettingsExpander>(list =>
                            {
                                list.SetHeader("Themes/Styling")
                                    .SetItemTemplate(
                                        world.CreateTemplate<IUIComponent, Panel>(
                                            (panel, item) =>
                                            {
                                                panel.Child(item);
                                            },
                                            shouldBeCleanedUp: false
                                        )
                                    )
                                    .SetItemsSource(
                                        new List<IUIComponent>() { new ThemeToggleSwitch(world) }
                                    );
                            });

                            stackPanel.Child<SettingsExpander>(list =>
                            {
                                list.SetHeader("Obsidian")
                                    .SetItemTemplate(
                                        world.CreateTemplate<IUIComponent, Panel>(
                                            (panel, item) =>
                                            {
                                                panel.Child(item);
                                            },
                                            shouldBeCleanedUp: false
                                        )
                                    )
                                    .SetItemsSource(ObsidianSettings);
                            });

                            stackPanel.Child<SettingsExpander>(list =>
                            {
                                list.SetHeader("Additional Features")
                                    .SetItemTemplate(
                                        world.CreateTemplate<IUIComponent, Panel>(
                                            (panel, item) =>
                                            {
                                                panel.Child(item);
                                            },
                                            shouldBeCleanedUp: false
                                        )
                                    )
                                    .SetItemsSource(
                                        new List<IUIComponent>()
                                        {
                                            new EnableLargeLanguageModelToggleSwitch(world),
                                            new EnableCloudSavesToggleSwitch(world),
                                        }
                                    );
                            });

                            stackPanel.Child<SettingsExpander>(list =>
                            {
                                list.SetHeader("Dangerous Options")
                                    .SetItemTemplate(
                                        world.CreateTemplate<IUIComponent, Panel>(
                                            (panel, item) =>
                                            {
                                                panel.Child(item);
                                            },
                                            shouldBeCleanedUp: false
                                        )
                                    )
                                    .SetItemsSource(
                                        new List<IUIComponent>() { new ResetStats(world) }
                                    );
                            });
                        }
                    );
                }
            )
            .Add<Page>()
            .Entity;
    }

    private class ImportObsidianVault : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public ImportObsidianVault(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Import obsidian vault")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<Button>(
                            (button) =>
                            {
                                button
                                    .SetText("Select Vault")
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right)
                                    .OnClick(
                                        async (_, _) =>
                                        {
                                            // Create and configure the file picker options
                                            var options = new FolderPickerOpenOptions
                                            {
                                                Title = "Select the root of the obsidian folder",
                                                AllowMultiple = false, // Set to true if you want to allow multiple file selections
                                            };

                                            // Create an OpenFileDialog instance
                                            IReadOnlyList<IStorageFolder> result =
                                                await App.GetMainWindow()
                                                    .StorageProvider.OpenFolderPickerAsync(options);
                                            // Get the selected folder (since AllowMultiple is false, there's at most one)
                                            IStorageFolder selectedFolder = result[0]; // The result is an IStorageFolder

                                            if (
                                                selectedFolder.Path is Uri uri
                                                && uri.IsAbsoluteUri
                                                && uri.IsFile
                                            )
                                            {
                                                var literature = world.Get<
                                                    ObservableCollection<LiteratureSourceItem>
                                                >();

                                                foreach (
                                                    var item in ObsidianHandler.ParseVault(
                                                        selectedFolder.Path.AbsolutePath,
                                                        true
                                                    )
                                                )
                                                {
                                                    literature.Add(item);
                                                }

                                                literature.RemoveDuplicateFilePaths();
                                            }
                                        }
                                    );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                Imports all markdown and pdf files to the literature list.
                                                (This does not copy the obsidian files in the literature folder)

                                                When an obsidian path is set it also opens the markdown file directly using obsidian.
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class ResetStats : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public ResetStats(World world)
        {
            _root = world
                .UI<Button>(
                    (button) =>
                    {
                        button
                            .SetText("Reset Statistics")
                            .SetPointerOverBackground(Brushes.Firebrick)
                            .OnClick(
                                async (_, _) =>
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
                                }
                            );
                    }
                )
                .Entity;
        }
    }

    private class EnableAlwaysOnTop : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableAlwaysOnTop(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Enable Always on top")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world
                                            .Get<Settings>()
                                            .EnableAlwaysOnTop;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var enableAlwaysOnTop =
                                            ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.EnableAlwaysOnTop = enableAlwaysOnTop;
                                    }
                                );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                Always show Stella Learning windows on top of other open windows.
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class EnableNotificationsToggleSwitch : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableNotificationsToggleSwitch(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Enable Notifications")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world
                                            .Get<Settings>()
                                            .EnableNotifications;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var enableNotifications =
                                            ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.EnableNotifications = enableNotifications;
                                    }
                                );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                When enabled the app will send desktop notifications where appropriate.
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class EnableLargeLanguageModelToggleSwitch : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableLargeLanguageModelToggleSwitch(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Enable Large Language Features")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world
                                            .Get<Settings>()
                                            .EnableLargeLanguageFeatures;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var enableLargeLanguageFeatures =
                                            ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.EnableLargeLanguageFeatures =
                                            enableLargeLanguageFeatures;
                                    }
                                );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                When enabled automatic meta can be generated for various items like art, and literature (Automatic generation for title, summary, tags). (Paid Feature)
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class EnableCloudSavesToggleSwitch : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public EnableCloudSavesToggleSwitch(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Enable Cloud Saves")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world
                                            .Get<Settings>()
                                            .EnableCloudSaves;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var enableCloudSaves =
                                            ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.EnableCloudSaves = enableCloudSaves;
                                    }
                                );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                Create backups in the cloud that can be used to restore your data. This includes all meta data, all files, etc. Your data is not stored as plain text but instead is decrypted. Should you lose your account data. You will never be able to access them again!
                                                (Paid Feature) (NOT YET IMPLEMENTED)
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class ThemeToggleSwitch : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public ThemeToggleSwitch(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Dark Mode")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world.Get<Settings>().IsDarkMode;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var isDarkMode = ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.IsDarkMode = isDarkMode;
                                    }
                                );
                            }
                        );
                    }
                )
                .Entity;
        }
    }

    private class AppToTray : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public AppToTray(World world)
        {
            _root = world
                .UI<DockPanel>(
                    (dockPanel) =>
                    {
                        dockPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock
                                    .SetVerticalAlignment(VerticalAlignment.Center)
                                    .SetText("Close to tray")
                                    .SetTextWrapping(TextWrapping.Wrap);
                            }
                        );

                        dockPanel.Child<ToggleSwitch>(
                            (toggleSwitch) =>
                            {
                                toggleSwitch
                                    .SetDock(Dock.Right)
                                    .SetHorizontalAlignment(HorizontalAlignment.Right);

                                toggleSwitch.With(
                                    (toggleSwitch) =>
                                    {
                                        toggleSwitch.IsChecked = world.Get<Settings>().CloseToTray;
                                    }
                                );

                                toggleSwitch.OnIsCheckedChanged(
                                    (sender, args) =>
                                    {
                                        var closeToTray =
                                            ((ToggleSwitch)sender!).IsChecked ?? false;
                                        var currentSettings = world.Get<Settings>();
                                        currentSettings.CloseToTray = closeToTray;
                                    }
                                );
                            }
                        );

                        dockPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                When toggled on, the app instead of closing will minimize into a tray icon.
                                                                    
                                                The app will run in the background, so you can desktop notifications for example when a new item can be learned.
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
        }
    }

    private class ObsidianPath : IUIComponent
    {
        private Entity _root;

        /// <inheritdoc/>
        public Entity Root => _root;

        public ObsidianPath(World world)
        {
            _root = world
                .UI<StackPanel>(
                    (stackPanel) =>
                    {
                        stackPanel.Child<TextBlock>(
                            (textBlock) =>
                            {
                                textBlock.SetText("Obsidian Path:");
                                textBlock.SetMargin(0, 0, 0, 10);
                            }
                        );
                        stackPanel.Child<TextBox>(
                            (textBox) =>
                            {
                                textBox
                                    .SetWatermark("Path to Obsidian")
                                    .SetText(world.Get<Settings>().ObsidianPath)
                                    .SetInnerRightContent(
                                        world.UI<Button>(
                                            (button) =>
                                            {
                                                button.Child<TextBlock>(
                                                    (textBlock) => textBlock.SetText("Browse")
                                                );

                                                button.OnClick(
                                                    async (e, args) =>
                                                    {
                                                        string newObsidanPath =
                                                            await ObsidianFilePickerAsync();
                                                        if (newObsidanPath != "")
                                                        {
                                                            textBox.SetText(newObsidanPath);
                                                            var currentSettings =
                                                                world.Get<Settings>();
                                                            currentSettings.ObsidianPath =
                                                                newObsidanPath;
                                                        }
                                                    }
                                                );
                                            }
                                        )
                                    );
                            }
                        );

                        stackPanel.AttachToolTip(
                            world.UI<ToolTip>(
                                (toolTip) =>
                                {
                                    toolTip.Child<TextBlock>(
                                        (textBlock) =>
                                        {
                                            textBlock.SetText(
                                                """
                                                When a obsidian path is set, the application 
                                                will be able to open the obsidian vault when a
                                                markdown file is part of an obsidian vault.
                                                """
                                            );
                                        }
                                    );
                                }
                            )
                        );
                    }
                )
                .Entity;
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
        IReadOnlyList<IStorageFile> result = await App.GetMainWindow()
            .StorageProvider.OpenFilePickerAsync(options);

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
