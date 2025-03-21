using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type cloze
/// </summary>
public class AddCloze : IUIComponent, IDisposable
{
    private ComparePriority comparePriority;
    private UIBuilder<Button>? createButton = null;
    private UIBuilder<TextBox>? nameTextBox = null;
    private UIBuilder<TextBox>? clozeBox = null;
    private UIBuilder<ItemsControl>? clozeList = null;
    private Entity calculatedPriority;
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private readonly ObservableCollection<string> clozes = [];
    private bool isDisposed = false;

    // When a new cloze gets added we can remove the warning text
    // if a cloze gets removed and no clozes exist anymore we will show
    // it again
    private NotifyCollectionChangedEventHandler? collectionChangedHandler;
    private EventHandler<RoutedEventArgs>? createButtonClickedHandler;
    /// <summary>
    /// Create the Add Cloze Window
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public AddCloze(World world)
    {
        comparePriority = new ComparePriority(world);
        calculatedPriority = comparePriority.CalculatedPriorityEntity;

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

            window.OnClosed((sender, args) => Dispose());
            window.Show();
        });
        calculatedPriority.ChildOf(_root);
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

        return world.UI<StackPanel>((stackPanel) =>
        {

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBox>((textBox) =>
            {
                nameTextBox = textBox;
                textBox.With(t => t.Watermark = "Name");
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                clozeBox = textBox;
                textBox
                .SetWatermark("Cloze Text")
                .SetTextWrapping(TextWrapping.Wrap)
                .With((textBox) => textBox.AcceptsReturn = true);

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

                menu.ChildOf(textBox.Entity);

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

                        sp.Child<TextBlock>((tb) =>
                        {
                            tb
                            .SetText(tag)
                            .SetVerticalAlignment(Layout.VerticalAlignment.Center);
                        });
                        sp.Child<Button>((btn) =>
                        {
                            btn.Child<TextBlock>(textBlock => textBlock.SetText("X"));
                            btn
                            .SetPadding(6, 2, 6, 2)
                            .OnClick((_, _) => clozes.Remove(tag));


                            btn.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                            {
                                toolTip.Child<TextBlock>((textBlock) =>
                                {
                                    textBlock.SetText("Removes the cloze word");
                                });
                            }));

                        });
                    }));
            }).SetItemsSource(clozes);

            var warningText = stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock.SetText("Please create at least one cloze word")
                    .SetForeground(Brushes.Red)
                    .SetFontWeight(FontWeight.Bold)
                    .SetFontSize(12)
                    .SetMargin(new Thickness(0, 5, 0, 5));

                // Set initial visibility based on collection status
                textBlock.Visible(clozes.Count == 0);
            });

            // Subscribe to collection changes to toggle warning visibility
            collectionChangedHandler = (_, _) =>
            {
                warningText.Get<TextBlock>().IsVisible = clozes.Count == 0;
            };

            clozes.CollectionChanged += collectionChangedHandler;

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

            stackPanel.Child(comparePriority);

            // Create button
            stackPanel.Child<Button>((button) =>
            {
                createButton = button;

                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Create Cloze");
                });

                createButtonClickedHandler = (sender, args) =>
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
                    if (_root.IsValid())
                    {
                        world.Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionCloze()
                        {
                            Name = nameTextBox.GetText(),
                            Priority = calculatedPriority.Get<int>(),
                            FullText = clozeBox.GetText(),
                            ClozeWords = [.. clozes],
                            SpacedRepetitionItemType = SpacedRepetitionItemType.Cloze
                        });

                        nameTextBox.SetText("");
                        clozeBox.SetText("");
                        clozes.Clear();
                        calculatedPriority.Set(500000000);
                        comparePriority.Reset();
                    }
                };

                button.With((b) => b.Click += createButtonClickedHandler);
            });
        });
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
                comparePriority?.Dispose();

                // Unsubscribe from events
                if (clozes != null && collectionChangedHandler != null)
                {
                    clozes.CollectionChanged -= collectionChangedHandler;
                }

                if (createButton is not null && createButtonClickedHandler is not null)
                {
                    createButton.With((b) => b.Click -= createButtonClickedHandler);
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