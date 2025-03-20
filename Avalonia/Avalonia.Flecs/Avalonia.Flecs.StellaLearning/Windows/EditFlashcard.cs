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
public class EditFlashcard : IUIComponent
{

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionFlashcard flashcard;

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="flashcard"></param>
    /// <returns></returns>
    public EditFlashcard(World world, SpacedRepetitionFlashcard flashcard)
    {
        this.flashcard = flashcard;
        _root = world.UI<Window>((window) =>
                {
                    window
                    .SetTitle($"Edit Flashcard: {flashcard.Name}")
                    .SetWidth(400)
                    .SetHeight(400)
                    .Child<ScrollViewer>((scrollViewer) =>
                    {
                        scrollViewer
                        .SetRow(1)
                        .SetColumnSpan(3)
                        .Child(DefineWindowContents(world));
                    });
                    window.OnClosed((sender, args) => _root.Destruct());

                    window.Show();
                });
    }

    private Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
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
                textBox.SetWatermark("Name").SetText(flashcard.Name);
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                frontText = textBox;
                textBox
                .SetWatermark("Front Text")
                .SetTextWrapping(TextWrapping.Wrap).SetText(flashcard.Front);

                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                backText = textBox;
                textBox
                .SetWatermark("Back Text")
                .SetTextWrapping(TextWrapping.Wrap).SetText(flashcard.Back);

                textBox.Get<TextBox>().AcceptsReturn = true;
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

                    flashcard.Name = nameTextBox.GetText();
                    flashcard.Front = frontText.GetText();
                    flashcard.Back = backText.GetText();

                    // Clearing an entity results in all components, relationships etc to be removed.
                    // this also results in invoking the remove hooks that are used on components for 
                    // cleanup. For example removing a window component results in closing it.
                    _root.Clear();
                });
            });
        });
    }
}
