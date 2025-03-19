using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to edit spaced repetition items of the type cloze
/// </summary>
public class EditCloze : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionCloze cloze;
    /// <summary>
    /// Create the edit Cloze Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="cloze"></param>
    /// <returns></returns>
    public EditCloze(World world, SpacedRepetitionCloze cloze)
    {
        this.cloze = cloze;
        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle($"Edit Cloze : {cloze.Name}")
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

            window.Show();
        });
    }

    private Entity DefineWindowContents(World world)
    {
        static void AddCloze(string item, ObservableCollection<string> clozes)
        {
            var trimmedItem = item.Trim();
            if (!clozes.Contains(trimmedItem) && !string.IsNullOrWhiteSpace(trimmedItem))
            {
                clozes.Add(trimmedItem);
            }
        }

        ObservableCollection<string> clozes = [.. cloze.ClozeWords];

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
                textBox.SetText(cloze.Name);
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                clozeBox = textBox;
                textBox
                .SetWatermark("Cloze Text")
                .SetTextWrapping(TextWrapping.Wrap)
                .With((textBox) => textBox.AcceptsReturn = true)
                .SetText(cloze.FullText);

                var menu = world.UI<MenuFlyout>((menu) =>
                      {
                          menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                          menu.Child<MenuItem>((menuItem) =>
                          {
                              menuItem
                              .SetHeader("Mark as Cloze")
                              .OnClick((_, _) =>
                              {
                                  var cloze = textBox.Get<TextBox>().SelectedText;
                                  AddCloze(cloze, clozes);
                              });
                          });
                      });

                textBox.SetContextFlyout(menu);
            });

            // Create cloze list with items control
            stackPanel.Child<ItemsControl>((itemsControl) =>
            {
                clozeList = itemsControl
                .SetItemTemplate(
                    world.CreateTemplate<string, StackPanel>(
                    (sp, tag) =>
                    {
                        sp
                        .SetOrientation(Layout.Orientation.Horizontal)
                        .SetSpacing(5);

                        sp.Child<TextBlock>(tb => tb.SetText(tag));
                        sp.Child<Button>((btn) =>
                        {
                            btn.Child<TextBlock>(textBlock => textBlock.SetText("X"));
                            btn.OnClick((_, _) => clozes.Remove(tag));
                        });

                    })
                    );
            }).SetItemsSource(clozes);

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock.SetText("Select a word and right click to mark it as a cloze");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0)); // Tighten spacing
            });

            stackPanel.Child<Separator>((separator) =>
            {
                separator
                    .SetMargin(0, 0, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
            });

            // Create button
            stackPanel.Child<Button>((button) =>
            {
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Save");
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

                    cloze.Name = nameTextBox.GetText();
                    cloze.FullText = clozeBox.GetText();
                    cloze.ClozeWords = [.. clozes];

                    // Clearing an entity results in all components, relationships etc to be removed.
                    // this also results in invoking the remove hooks that are used on components for 
                    // cleanup. For example removing a window component results in closing it.
                    _root.Clear();
                });
            });
        });
    }
}