using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// UI component for editing literature sources.
/// </summary>
public class EditLiteratureSource : IUIComponent, IDisposable
{

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private LiteratureSourceItem _literatureSourceItem;
    private readonly CompositeDisposable _disposables = []; // For managing disposables
    private bool _isDisposed = false; // For IDisposable pattern
    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="literatureSourceItem"></param>
    /// <returns></returns>
    public EditLiteratureSource(World world, LiteratureSourceItem literatureSourceItem)
    {

        _literatureSourceItem = literatureSourceItem;
        _root = world.UI<Window>((window) =>
                {
                    window
                    .SetTitle($"Edit Flashcard: {literatureSourceItem.Name}")
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
        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? titleTextBox = null;
            UIBuilder<TextBox>? authorTextBox = null;

            var tagManager = new TagComponent(world, [.. _literatureSourceItem.Tags]);

            void SaveData()
            {
                if (titleTextBox is null ||
                    authorTextBox is null)
                {
                    return;
                }
                if (string.IsNullOrEmpty(titleTextBox.GetText()))
                {
                    titleTextBox.SetWatermark("Title is required");
                    var cd = new ContentDialog()
                    {
                        Title = "Missing Title",
                        Content = "You must define a title",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                        IsSecondaryButtonEnabled = true,
                    };
                    cd.ShowAsync();
                    return;
                }

                if (string.IsNullOrEmpty(authorTextBox.GetText()))
                {
                    authorTextBox.SetWatermark("Author is required");
                    var cd = new ContentDialog()
                    {
                        Title = "Missing Author",
                        Content = "You must define an Author",
                        PrimaryButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Primary,
                        IsSecondaryButtonEnabled = true,
                    };
                    cd.ShowAsync();
                    return;
                }


                _literatureSourceItem.Title = titleTextBox.GetText();
                _literatureSourceItem.Author = authorTextBox.GetText();
                _literatureSourceItem.Tags = [.. tagManager.Tags];

                // Clearing an entity results in all components, relationships etc to be removed.
                // this also results in invoking the remove hooks that are used on components for 
                // cleanup. For example removing a window component results in closing it.
                _root.Clear();
            }

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Title");
            });

            titleTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Title")
                .SetText(_literatureSourceItem.Title)
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
                t.SetText("Author");
            });

            authorTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Author")
                .SetText(_literatureSourceItem.Author)
                .OnKeyDown((sender, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        SaveData();
                    }
                });
            });

            stackPanel.Child(tagManager); // Add the tag manager UI

            stackPanel.Child<Button>((button) =>
            {
                button
                .SetText("Save Changes")
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch);

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
    ~EditLiteratureSource() { Dispose(disposing: false); }
}
