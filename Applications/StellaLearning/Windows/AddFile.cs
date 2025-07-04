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
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AyanamisTower.StellaLearning.Data;
using AyanamisTower.StellaLearning.UiComponents;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace AyanamisTower.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class AddFile : IUIComponent, IDisposable
{
    /// <summary>
    /// Collection to track all disposables
    /// </summary>
    private readonly CompositeDisposable _disposables = [];
    private UIBuilder<Button>? createButton;
    private EventHandler<RoutedEventArgs>? createButtonClickedHandler;
    private UIBuilder<TextBlock>? validationTextBlock;
    private UIBuilder<TextBox>? nameTextBox;
    private UIBuilder<TextBox>? filePath;
    private UIBuilder<TextBox>? questionTextBox;
    private Entity calculatedPriority;
    private Entity _root;

    /// <inheritdoc/>
    public Entity Root => _root;
    private bool isDisposed = false;
    private EventHandler<TextChangedEventArgs>? filePathHasChangedHandler;

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <returns></returns>
    public AddFile(World world)
    {
        _root = world
            .UI<Window>(
                (window) =>
                {
                    window
                        .AlwaysOnTop(world.Get<Settings>().EnableAlwaysOnTop)
                        .SetTitle("Add File")
                        .SetWidth(400)
                        .SetHeight(400)
                        .Child<ScrollViewer>(
                            (scrollViwer) =>
                            {
                                scrollViwer
                                    .SetRow(1)
                                    .SetColumnSpan(3)
                                    .Child(DefineWindowContents(world));
                            }
                        );

                    /* NOTE:
                    You might see high memory usage and no memory reclaim when the window closes and the entity is destroyed.
                    Why might that be? Because the GC didnt run yet, the GC reclaims the memory only when it thinks its a good moment to do so.
                    */
                    window.OnClosed((sender, args) => Dispose());

                    window.Show();
                }
            )
            .Entity;
    }

    private Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        return world
            .UI<StackPanel>(
                (stackPanel) =>
                {
                    void ValidateFilePath(string path)
                    {
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            validationTextBlock!.SetText("You must define a file path");
                            validationTextBlock!.SetForeground(new SolidColorBrush(Colors.Red));
                            return;
                        }

                        bool isValid = System.IO.File.Exists(path);

                        if (isValid)
                        {
                            validationTextBlock!.SetText("✓ File exists");
                            validationTextBlock!.SetForeground(new SolidColorBrush(Colors.Green));
                        }
                        else
                        {
                            validationTextBlock!.SetText("✗ File doesn't exist");
                            validationTextBlock!.SetForeground(new SolidColorBrush(Colors.Red));
                        }
                    }

                    stackPanel.SetOrientation(Orientation.Vertical).SetSpacing(10).SetMargin(20);

                    nameTextBox = stackPanel.Child<TextBox>(
                        (textBox) =>
                        {
                            textBox.SetWatermark("Name");
                        }
                    );

                    questionTextBox = stackPanel.Child<TextBox>(
                        (textBox) =>
                        {
                            textBox.SetWatermark("Question");
                        }
                    );

                    filePath = stackPanel.Child<TextBox>(
                        (textBox) =>
                        {
                            textBox
                                .SetWatermark("FilePath")
                                .SetInnerRightContent(
                                    world.UI<Button>(
                                        (button) =>
                                        {
                                            button.Child<TextBlock>((t) => t.SetText("Browse"));

                                            button.OnClick(
                                                async (e, args) =>
                                                {
                                                    var path = await FilePickerAsync();
                                                    textBox.SetText(path);
                                                    ValidateFilePath(path);
                                                }
                                            );
                                        }
                                    )
                                );

                            filePathHasChangedHandler = (_, _) =>
                                ValidateFilePath(textBox.GetText());

                            textBox.With(
                                (textBox) => textBox.TextChanged += filePathHasChangedHandler
                            );
                        }
                    );

                    validationTextBlock = stackPanel.Child<TextBlock>(
                        (textBlock) =>
                        {
                            textBlock
                                .SetText("")
                                .SetFontSize(12)
                                .SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
                        }
                    );

                    var tagManager = new TagComponent(world);
                    stackPanel.Child(tagManager);

                    stackPanel.Child<Separator>(
                        (separator) =>
                        {
                            separator
                                .SetMargin(0, 0, 0, 10)
                                .SetBorderThickness(new Thickness(100, 5, 100, 0))
                                .SetBorderBrush(Brushes.Black);
                        }
                    );

                    var comparePriority = new ComparePriority(world);
                    calculatedPriority = comparePriority.CalculatedPriorityEntity;
                    stackPanel.Child(comparePriority);

                    createButton = stackPanel.Child<Button>(
                        (button) =>
                        {
                            button
                                .SetVerticalAlignment(VerticalAlignment.Center)
                                .SetHorizontalAlignment(HorizontalAlignment.Center);

                            button.Child<TextBlock>(
                                (textBlock) =>
                                {
                                    textBlock.SetText("Create Item");
                                }
                            );

                            createButtonClickedHandler = (sender, args) =>
                            {
                                if (
                                    nameTextBox is null
                                    || questionTextBox is null
                                    || filePath is null
                                )
                                {
                                    return;
                                }

                                if (string.IsNullOrEmpty(nameTextBox.GetText()))
                                {
                                    nameTextBox.SetWatermark("Name is required");
                                    var cd = new ContentDialog()
                                    {
                                        Title = "Missing Name",
                                        Content = "You must define a name",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                    return;
                                }

                                if (string.IsNullOrEmpty(filePath.GetText()))
                                {
                                    filePath.SetWatermark("File path is required");

                                    var cd = new ContentDialog()
                                    {
                                        Title = "Missing Filepath",
                                        Content = "You must define a valid filepath",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                    return;
                                }

                                if (string.IsNullOrEmpty(questionTextBox.GetText()))
                                {
                                    questionTextBox.SetWatermark("Question is required");

                                    var cd = new ContentDialog()
                                    {
                                        Title = "Missing Question",
                                        Content = "You must define a question",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                    return;
                                }

                                if (!System.IO.File.Exists(filePath!.GetText()))
                                {
                                    filePath.SetWatermark("File at defined path does not exist");
                                    return;
                                }

                                world
                                    .Get<ObservableCollection<SpacedRepetitionItem>>()
                                    .Add(
                                        new SpacedRepetitionFile()
                                        {
                                            Name = nameTextBox.GetText(),
                                            Priority = calculatedPriority.Get<int>(),
                                            Question = questionTextBox.GetText(),
                                            FilePath = filePath.GetText(),
                                            Tags = [.. tagManager.Tags],
                                            SpacedRepetitionItemType =
                                                SpacedRepetitionItemType.File,
                                        }
                                    );

                                calculatedPriority.Set(500000000);
                                nameTextBox.SetText("");
                                questionTextBox.SetText("");
                                filePath.SetText("");
                                tags.Clear();

                                tagManager.ClearTags();
                                comparePriority.Reset();
                            };
                            button.With((b) => b.Click += createButtonClickedHandler);
                        }
                    );
                }
            )
            .Entity;
    }

    private static async Task<string> FilePickerAsync()
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

        if (result?.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            return file.TryGetLocalPath()!;
        }
        return string.Empty;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                // Unsubscribe from events

                if (filePath is not null && filePathHasChangedHandler is not null)
                {
                    filePath.With((textbox) => textbox.TextChanged -= filePathHasChangedHandler);
                }

                if (createButton is not null && createButtonClickedHandler is not null)
                {
                    createButton.With((b) => b.Click -= createButtonClickedHandler);
                }

                // Clean up other resources
                // Consider calling destruct if needed
                if (_root.IsValid())
                {
                    _root.Get<Window>().Content = null;
                    _root.Destruct();
                }
                // Dispose all tracked disposables
                _disposables.Dispose();
            }

            isDisposed = true;
        }
    }
}
