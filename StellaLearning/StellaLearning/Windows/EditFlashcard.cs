using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using StellaLearning.Data;
using StellaLearning.UiComponents;

namespace StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class EditFlashcard : IUIComponent, IDisposable
{

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionFlashcard flashcard;
    private readonly CompositeDisposable _disposables = []; // For managing disposables
    private bool _isDisposed = false; // For IDisposable pattern
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
                    .AlwaysOnTop(world.Get<Settings>().EnableAlwaysOnTop)
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
                    window.OnClosed((sender, args) => Dispose());

                    window.Show();
                }).Entity;
    }

    private Entity DefineWindowContents(World world)
    {
        var comparePriority = new ComparePriority(world);
        _disposables.Add(Disposable.Create(() => comparePriority.Dispose()));
        var calculatedPriority = comparePriority.CalculatedPriorityEntity;
        // Here we set the inital priority
        calculatedPriority.Set(flashcard.Priority);

        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? frontText = null;
            UIBuilder<TextBox>? backText = null;
            var tagManager = new TagComponent(world, flashcard.Tags);

            void SaveData()
            {
                if (nameTextBox is null ||
                    frontText is null ||
                    backText is null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(nameTextBox.GetText()))
                {
                    nameTextBox.SetWatermark("Name is required");
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

                if (string.IsNullOrEmpty(frontText.GetText()))
                {
                    frontText.SetWatermark("A front text is required");
                    var cd = new ContentDialog()
                    {
                        Title = "Missing Front Text",
                        Content = "You must define a front text",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                        IsSecondaryButtonEnabled = true,
                    };
                    cd.ShowAsync();
                    return;
                }

                if (string.IsNullOrEmpty(backText.GetText()))
                {
                    backText.SetWatermark("A back text is required");
                    var cd = new ContentDialog()
                    {
                        Title = "Missing Back Text",
                        Content = "You must define a back text",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                        IsSecondaryButtonEnabled = true,
                    };
                    cd.ShowAsync();
                    return;
                }

                flashcard.Name = nameTextBox.GetText();
                flashcard.Front = frontText.GetText();
                flashcard.Back = backText.GetText();
                flashcard.Priority = calculatedPriority.Get<int>();
                flashcard.Tags = [.. tagManager.Tags];

                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await StatsTracker.Instance.UpdateTagsForItemAsync(flashcard.Uid, flashcard.Tags);
                });


                // Clearing an entity results in all components, relationships etc to be removed.
                // this also results in invoking the remove hooks that are used on components for 
                // cleanup. For example removing a window component results in closing it.
                _root.Clear();
            }

            stackPanel
            .SetOrientation(Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Name");
            });

            nameTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Name")
                .SetText(flashcard.Name)
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        SaveData();
                    }
                });
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Front Text");
            });


            frontText = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Front Text")
                .SetTextWrapping(TextWrapping.Wrap).SetText(flashcard.Front);

                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Back Text");
            });

            backText = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Back Text")
                .SetTextWrapping(TextWrapping.Wrap).SetText(flashcard.Back);

                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            stackPanel.Child(tagManager); // Add the tag manager UI
            stackPanel.Child(comparePriority);
            stackPanel.Child<Button>((button) =>
            {
                button
                .SetText("Save Changes")
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Stretch);

                button.OnClick((sender, args) =>
                {
                    SaveData();
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
    ~EditFlashcard() { Dispose(disposing: false); }
}
