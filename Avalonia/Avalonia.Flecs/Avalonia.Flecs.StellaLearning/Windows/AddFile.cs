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
public class AddFile : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;

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

            window.OnClosed((sender, args) => _root.Destruct());

            window.Show();
        });
    }

    private static Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
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
}