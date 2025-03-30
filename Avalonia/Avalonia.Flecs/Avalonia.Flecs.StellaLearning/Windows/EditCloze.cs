using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Utilities;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to edit spaced repetition items of the type cloze
/// </summary>
public class EditCloze : IUIComponent, IDisposable
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionCloze cloze;
    private readonly CompositeDisposable _disposables = []; // For managing disposables
    private bool _isDisposed = false; // For IDisposable pattern
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
            window.OnClosed((sender, args) => Dispose());

            window.Show();
        }).Entity;
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

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Name");
            });

            stackPanel.Child<TextBox>((textBox) =>
            {
                nameTextBox = textBox;
                textBox.SetWatermark("Name");
                textBox.SetText(cloze.Name);
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Cloze Text");
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

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Clozes");
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

            var tagManager = new TagComponent(world, cloze.Tags);
            _disposables.Add(tagManager);
            stackPanel.Child(tagManager); // Add the tag manager UI

            // Create button
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
                    if (nameTextBox is null || clozeBox is null)
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(nameTextBox.GetText()))
                    {
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


                    if (clozes.Count == 0)
                    {
                        var cd = new ContentDialog()
                        {
                            Title = "Clozes Missing",
                            Content = "Your test currently has not cloze words defined you must atleast define one",
                            PrimaryButtonText = "Ok",
                            DefaultButton = ContentDialogButton.Primary,
                            IsSecondaryButtonEnabled = true,
                        };
                        cd.ShowAsync();
                        return;
                    }

                    cloze.Name = nameTextBox.GetText();
                    cloze.FullText = clozeBox.GetText();
                    cloze.ClozeWords = [.. clozes];
                    cloze.Tags = [.. tagManager.Tags];

                    // Clearing an entity results in all components, relationships etc to be removed.
                    // this also results in invoking the remove hooks that are used on components for 
                    // cleanup. For example removing a window component results in closing it.
                    _root.Clear();
                });
            });
        }).Entity;
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
    ~EditCloze() { Dispose(disposing: false); }
}