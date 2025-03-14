using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public static class AddFile
{

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <returns></returns>
    public static Entity Create(World world)
    {


        //DefineWindowContents(world).ChildOf(scrollViewer);

        return world.UI<Window>((window) =>
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
        });
    }

    private static Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        var layout = world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? filePath = null;
            UIBuilder<TextBox>? questionTextBox = null;
            Entity calculatedPriority;
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
                .SetInnerRightContent(FilePickerButton(world).OnClick(async (e, args) => textBox.SetText(await FilePickerAsync())));
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                textBox.SetWatermark("Tags")
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        if (string.IsNullOrEmpty(textBox.GetText()))
                        {
                            return;
                        }

                        tags.Add(new(textBox.GetText()));
                        textBox.SetText("");
                    }
                });
            });

            stackPanel.Child<ItemsControl>((itemsControl) =>
            {
                itemsControl
                    .SetItemsSource(tags)
                    .SetItemTemplate(DefineTagTemplate());
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

        /*
        (Entity priorityCompareComponent, Entity calculatedPriority) = ComparePriority.Create(layout, createFileButton);
        priorityCompareComponent.ChildOf(layout);
        */
        return layout;
    }

    private static Entity FilePickerButton(World world)
    {
        var browseForFileButton = world.Entity()
            .Set(new Button());

        var browseForFileButtonContent = world.Entity()
            .ChildOf(browseForFileButton)
            .Set(new TextBlock())
            .SetText("Browse");

        return browseForFileButton;
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
        IReadOnlyList<IStorageFile> result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

        if (result != null && result.Count > 0)
        {
            // Get the selected file
            IStorageFile file = result[0];

            return file.TryGetLocalPath()!;
        }
        return string.Empty;
    }

    private static FuncDataTemplate<Tag> DefineTagTemplate()
    {
        return new FuncDataTemplate<Tag>((tag, _) =>
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 5
            };

            var nameText = new TextBlock()
            {
                Text = tag.Name
            };

            var removeButton = new Button()
            {
                Content = "X"
            };

            removeButton.Click += ((sender, args) =>
            {
                //entities["tagsList"].Get<ObservableCollection<Tag>>().Remove(tag);
            });
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(removeButton);
            return stackPanel;
        });
    }
}