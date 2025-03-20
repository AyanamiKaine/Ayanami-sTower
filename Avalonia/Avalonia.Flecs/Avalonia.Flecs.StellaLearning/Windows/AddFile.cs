using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Flecs.Util;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class AddFile : IUIComponent, IDisposable
{


    private UIBuilder<TextBlock>? validationTextBlock = null;
    private UIBuilder<TextBox>? nameTextBox = null;
    private UIBuilder<TextBox>? filePath = null;
    private UIBuilder<TextBox>? questionTextBox = null;
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

        //DefineWindowContents(world).ChildOf(scrollViewer);

        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle("Add File")
            .SetWidth(400)
            .SetHeight(400)
            .Child<ScrollViewer>((scrollViwer) =>
            {
                scrollViwer
                .SetRow(1)
                .SetColumnSpan(3)
                .Child(DefineWindowContents(world));
            });

            /* NOTE:
            You might see high memory usage and no memory reclaim when the window closes and the entity is destroyed.
            Why might that be? Because the GC didnt run yet, the GC reclaims the memory only when it thinks its a
            good moment to do so.
            */
            window.OnClosed((sender, args) => Dispose());

            window.Show();
        });
    }

    private Entity DefineWindowContents(World world)
    {

        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
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

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBox>((textBox) =>
            {
                nameTextBox = textBox;
                textBox.SetWatermark("Name");
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                questionTextBox = textBox;
                textBox.SetWatermark("Question");
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                filePath = textBox;
                textBox
                .SetWatermark("FilePath")
                .SetInnerRightContent(FilePickerButton(world).OnClick(async (e, args) =>
                {
                    var path = await FilePickerAsync();
                    textBox.SetText(path);
                    ValidateFilePath(path);
                }));

                filePathHasChangedHandler = (_, _) => ValidateFilePath(textBox.GetText());

                textBox.With((textBox) => textBox.TextChanged += filePathHasChangedHandler);
            });

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                validationTextBlock = textBlock;
                textBlock.SetText("");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
            });

            var comparePriority = new ComparePriority(world);
            calculatedPriority = comparePriority.CalculatedPriorityEntity;
            stackPanel.Child(comparePriority);

            stackPanel.Child<Button>((button) =>
            {
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Create Item");
                });

                button.OnClick((sender, args) =>
                    {
                        if (nameTextBox is null ||
                            questionTextBox is null ||
                            filePath is null)
                        {
                            return;
                        }

                        if (string.IsNullOrEmpty(nameTextBox.GetText()) || string.IsNullOrEmpty(filePath.GetText()) || string.IsNullOrEmpty(questionTextBox.GetText()))
                        {
                            nameTextBox!.SetWatermark("Name is required");
                            questionTextBox!.SetWatermark("Question is required");
                            filePath!.SetWatermark("File path is required");
                            return;
                        }

                        if (!System.IO.File.Exists(filePath!.GetText()))
                        {
                            filePath!.SetWatermark("File at defined path does not exist");
                            return;
                        }

                        world.Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionFile()
                        {
                            Name = nameTextBox.GetText(),
                            Priority = calculatedPriority.Get<int>(),
                            Question = questionTextBox.GetText(),
                            FilePath = filePath.GetText(),
                            SpacedRepetitionItemType = SpacedRepetitionItemType.File
                        });

                        calculatedPriority.Set(500000000);
                        nameTextBox.SetText("");
                        questionTextBox.SetText("");
                        filePath.SetText("");
                        tags.Clear();
                        comparePriority.Reset();
                    });

            });
        });
    }



    private Entity FilePickerButton(World world)
    {
        return world.UI<Button>((button) =>
        {
            button.Child<TextBlock>((t) => t.SetText("Browse"));
        });
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

                // Clean up other resources
                // Consider calling destruct if needed
                if (_root.IsValid())
                {
                    _root.Destruct();
                }
            }

            isDisposed = true;
        }
    }
}