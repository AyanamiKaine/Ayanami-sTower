using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class EditFile : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionFile spacedRepetitionFile;
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

            /* NOTE:
            You might see high memory usage and no memory reclaim when the window closes and the entity is destroyed.
            Why might that be? Because the GC didnt run yet, the GC reclaims the memory only when it thinks its a
            good moment to do so.
            */
            window.OnClosed((sender, args) => _root.Destruct());

            window.Show();
        });
    }

    private Entity DefineWindowContents(World world)
    {

        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBlock>? validationTextBlock = null;
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? filePath = null;
            UIBuilder<TextBox>? questionTextBox = null;


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

            stackPanel.Child<TextBox>((textBox) =>
            {
                nameTextBox = textBox;
                textBox.SetWatermark("Name")
                .SetText(spacedRepetitionFile.Name);
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Question");
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                questionTextBox = textBox;
                textBox.SetWatermark("Question")
                .SetText(spacedRepetitionFile.Question);
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("FilePath");
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

                textBox.SetText(spacedRepetitionFile.FilePath);

                textBox.With((textBox) => textBox.TextChanged += (sender, args) => ValidateFilePath(textBox.Text!));

            });

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                validationTextBlock = textBlock;
                textBlock.SetText("");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
            });

            stackPanel.Child<Button>((button) =>
            {
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Save");
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


                        spacedRepetitionFile.Name = nameTextBox.GetText();
                        spacedRepetitionFile.Question = questionTextBox.GetText();
                        spacedRepetitionFile.FilePath = filePath.GetText();

                        // Clearing an entity results in all components, relationships etc to be removed.
                        // this also results in invoking the remove hooks that are used on components for 
                        // cleanup. For example removing a window component results in closing it.
                        _root.Clear();
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
}