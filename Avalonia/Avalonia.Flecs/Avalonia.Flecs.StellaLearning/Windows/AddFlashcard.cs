using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
public class AddFlashcard : IUIComponent
{

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public AddFlashcard(World world)
    {
        _root = world.UI<Window>((window) =>
                {
                    window
                    .SetTitle("Add Flashcard")
                    .SetWidth(400)
                    .SetHeight(400)
                    .Child<ScrollViewer>((scrollViewer) =>
                    {
                        scrollViewer
                        .SetRow(1)
                        .SetColumnSpan(3)
                        .Child(DefineWindowContents(world));
                    });
                    //TODO: This is the wrong way of doing it and will result in invalid memory somewhere.
                    //window.OnClosed((sender, args) => _root.Destruct());
                    window.OnClosed((sender, args) => _root.Clear());

                    window.Show();
                });
    }

    private Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            Entity calculatedPriority;
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? frontText = null;
            UIBuilder<TextBox>? backText = null;

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
                frontText = textBox;
                textBox
                .SetWatermark("Front Text")
                .SetTextWrapping(TextWrapping.Wrap);

                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                backText = textBox;
                textBox
                .SetWatermark("Back Text")
                .SetTextWrapping(TextWrapping.Wrap);

                textBox.Get<TextBox>().AcceptsReturn = true;
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
                        frontText is null ||
                        backText is null)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(nameTextBox.GetText()))
                    {
                        nameTextBox!.SetWatermark("Name is required");
                        return;
                    }

                    world.Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionFlashcard()
                    {
                        Name = nameTextBox.GetText(),
                        Front = frontText.GetText(),
                        Back = backText.GetText(),
                        Priority = calculatedPriority.Get<int>(),
                        SpacedRepetitionItemType = SpacedRepetitionItemType.Flashcard
                    });

                    calculatedPriority.Set(500000000);
                    nameTextBox.SetText("");
                    frontText.SetText("");
                    backText.SetText("");
                    tags.Clear();
                    comparePriority.Reset();
                });
            });
        });
    }
}
