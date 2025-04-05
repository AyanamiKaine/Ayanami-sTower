using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class EditFile : IUIComponent, IDisposable
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionFile spacedRepetitionFile;
    private readonly CompositeDisposable _disposables = []; // For managing disposables
    private bool _isDisposed = false; // For IDisposable pattern
    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <returns></returns>
    public EditFile(World world, SpacedRepetitionFile spacedRepetitionFile)
    {
        this.spacedRepetitionFile = spacedRepetitionFile;
        //DefineWindowContents(world).ChildOf(scrollViewer);

        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle($"Edit File: {spacedRepetitionFile.Name}")
            .SetWidth(400)
            .SetHeight(400)
            .Child<ScrollViewer>((scrollViwer) =>
            {
                scrollViwer
                .SetRow(1)
                .SetColumnSpan(3)
                .Child(DefineWindowContents(world));
            });


            window.OnClosed((sender, args) => Dispose());

            window.Show();
        }).Entity;
    }

    private Entity DefineWindowContents(World world)
    {

        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBlock> validationTextBlock = world.UI<TextBlock>((textBlock) =>
            {
                textBlock.SetText("");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
            });
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? filePath = null;
            UIBuilder<TextBox>? questionTextBox = null;

            var comparePriority = new ComparePriority(world);
            _disposables.Add(Disposable.Create(() => comparePriority.Dispose()));
            var calculatedPriority = comparePriority.CalculatedPriorityEntity;
            // Here we set the inital priority
            calculatedPriority.Set(spacedRepetitionFile.Priority);

            var tagManager = new TagComponent(world, spacedRepetitionFile.Tags);


            void SaveData()
            {
                if (nameTextBox is null ||
                    questionTextBox is null ||
                    filePath is null)
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
                    filePath!.SetWatermark("File at defined path does not exist");
                    return;
                }


                spacedRepetitionFile.Name = nameTextBox.GetText();
                spacedRepetitionFile.Question = questionTextBox.GetText();
                spacedRepetitionFile.FilePath = filePath.GetText();
                spacedRepetitionFile.Tags = [.. tagManager.Tags];
                spacedRepetitionFile.Priority = calculatedPriority.Get<int>();

                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await StatsTracker.Instance.UpdateTagsForItemAsync(spacedRepetitionFile.Uid, spacedRepetitionFile.Tags);
                });
                // Clearing an entity results in all components, relationships etc to be removed.
                // this also results in invoking the remove hooks that are used on components for 
                // cleanup. For example removing a window component results in closing it.
                _root.Clear();
            }


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

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Name");
            });

            nameTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Name")
                .SetText(spacedRepetitionFile.Name)
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        SaveData();
                    }
                });
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Question");
            });

            questionTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox.SetWatermark("Question")
                .SetText(spacedRepetitionFile.Question)
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        SaveData();
                    }
                });
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("FilePath");
            });

            filePath = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("FilePath")
                .SetInnerRightContent(
                    world.UI<Button>((button) =>
                    {
                        button.Child<TextBlock>((t) => t.SetText("Browse"));

                        button.OnClick(async (e, args) =>
                        {
                            var path = await FilePickerAsync();
                            textBox.SetText(path);
                            ValidateFilePath(path);
                        });
                    }))
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        SaveData();
                    }
                });

                textBox.SetText(spacedRepetitionFile.FilePath);

                textBox.With((textBox) => textBox.TextChanged += (sender, args) => ValidateFilePath(textBox.Text!));
            });

            stackPanel.Child(validationTextBlock);

            stackPanel.Child<TextBlock>(t => t.SetText("Tags").SetMargin(0, 10, 0, 0)); // Label for tags

            stackPanel.Child(tagManager); // Add the tag manager UI
            stackPanel.Child(comparePriority);

            stackPanel.Child<Button>((button) =>
            {
                button
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch);
                button.Child<TextBlock>((textBlock) =>
                                {
                                    textBlock.SetText("Save Changes");
                                });

                button.OnClick((sender, args) =>
                    {
                        SaveData();
                    });

            });
        }).Entity;
    }


    private async Task<string> FilePickerAsync()
    {
        // Create and configure the file picker options
        var options = new FilePickerOpenOptions
        {
            Title = "Select the obsidian executable",
            AllowMultiple = false, // Set to true if you want to allow multiple file selections
        };

        // Create an OpenFileDialog instance
        IReadOnlyList<IStorageFile> result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

        if (result?.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            return file.TryGetLocalPath()!;
        }
        return string.Empty;
    }
    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Diposer
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (like TagComponent instance in _disposables)
                _disposables.Dispose(); // Disposes tagManager

                // Destroy the root entity *last*
                if (_root.IsValid() && _root.IsAlive())
                {
                    // Clearing triggers component remove hooks (like Window Closing)
                    // _root.Clear();
                    // Explicit destruction might be needed if Clear doesn't close window
                    _root.Destruct();
                }
            }
            _isDisposed = true;
        }
    }
    /// <summary>
    /// Destructor
    /// </summary>
    ~EditFile() { Dispose(disposing: false); }

}