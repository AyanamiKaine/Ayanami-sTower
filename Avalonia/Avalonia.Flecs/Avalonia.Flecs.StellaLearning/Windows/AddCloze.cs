using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type cloze
/// </summary>
public class AddCloze : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;

    /// <summary>
    /// Create the Add Cloze Window
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public AddCloze(World world)
    {
        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle("Add Cloze")
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

    private static Entity DefineWindowContents(World world)
    {
        static void AddCloze(string item, ObservableCollection<string> clozes)
        {
            var trimmedItem = item.Trim();
            if (!clozes.Contains(trimmedItem) && !string.IsNullOrWhiteSpace(trimmedItem))
            {
                clozes.Add(trimmedItem);
            }
        }

        ObservableCollection<string> clozes = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? clozeBox = null;
            UIBuilder<ItemsControl>? clozeList = null;

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
                clozeBox = textBox;
                textBox.SetWatermark("Cloze Text")
                .SetTextWrapping(TextWrapping.Wrap);
                textBox.Get<TextBox>().AcceptsReturn = true;

                // Create the mark as cloze button for the context flyout
                var markAsClozeButton = world.UI<Button>((button) =>
                {
                    button.Child<TextBlock>((textBlock) => textBlock.SetText("Mark as Cloze"));
                    button.OnClick((_, _) =>
                    {
                        var cloze = textBox.Get<TextBox>().SelectedText;
                        AddCloze(cloze, clozes);
                    });
                });

                // Set up the context flyout
                textBox.Get<TextBox>().ContextFlyout = new Flyout() { Content = markAsClozeButton.Get<Button>() };
            });

            // Create cloze list with items control
            stackPanel.Child<ItemsControl>((itemsControl) =>
            {
                clozeList = itemsControl
                .SetItemTemplate(DefineTagTemplate(world, clozes))
                .SetItemsSource(clozes);
            });

            // Create button
            stackPanel.Child<Button>((button) =>
            {
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Create Cloze");
                });

                button.OnClick((sender, args) =>
                {
                    if (nameTextBox is null || clozeBox is null)
                    {
                        return;
                    }

                    if (clozes.Count == 0 || string.IsNullOrEmpty(nameTextBox.GetText()))
                    {
                        nameTextBox!.SetWatermark("Name is required");
                        return;
                    }

                    world.Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionCloze()
                    {
                        Name = nameTextBox.GetText(),
                        FullText = clozeBox.GetText(),
                        ClozeWords = [.. clozes],
                        SpacedRepetitionItemType = SpacedRepetitionItemType.Cloze
                    });

                    nameTextBox.SetText("");
                    clozeBox.SetText("");
                    clozes.Clear();
                });
            });
        });
    }
    private static FuncDataTemplate<string> DefineTagTemplate(World world, ObservableCollection<string> clozes)
    {
        return new FuncDataTemplate<string>((tag, _) =>
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 5
            };

            var nameText = new TextBlock()
            {
                Text = tag
            };

            var removeButton = new Button()
            {
                Content = "X"
            };

            removeButton.Click += ((sender, args) =>
            {
                clozes.Remove(tag);
            });

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(removeButton);
            return stackPanel;
        });
    }
}