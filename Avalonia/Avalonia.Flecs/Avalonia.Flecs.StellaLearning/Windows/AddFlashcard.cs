using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class AddFlashcard : IUIComponent, IDisposable
{
    /// <summary>
    /// Collection to track all disposables
    /// </summary>
    private readonly CompositeDisposable _disposables = [];
    private readonly ComparePriority comparePriority;
    private UIBuilder<Button>? createButton = null;
    private Entity calculatedPriority;
    private UIBuilder<TextBox>? nameTextBox = null;
    private UIBuilder<TextBox>? frontText = null;
    private UIBuilder<TextBox>? backText = null;
    private bool isDisposed = false;
    private EventHandler<RoutedEventArgs>? createButtonClickedHandler;

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


        comparePriority = new ComparePriority(world);
        /*
        Disposables should be there defined where we inital created
        the object that should get disposed.
        1. Creation Logic
        2. Dispose Logic

        Its better to have them together so we know when we forget 
        to create the dispose logic. It will be immediatly clear to 
        us.
        */
        _disposables.Add(Disposable.Create(() => comparePriority.Dispose()));
        calculatedPriority = comparePriority.CalculatedPriorityEntity;

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
                    window.OnClosed((sender, args) => Dispose());

                    window.Show();
                });

        _disposables.Add(Disposable.Create(() =>
        {
            if (_root.IsValid())
            {
                _root.Get<Window>().Content = null;
                _root.Destruct();
            }
        }));
    }

    private Entity DefineWindowContents(World world)
    {
        ObservableCollection<Tag> tags = [];

        return world.UI<StackPanel>((stackPanel) =>
        {
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

            stackPanel.Child<Separator>((separator) =>
            {
                separator
                    .SetMargin(0, 0, 0, 10)
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
            });

            stackPanel.Child(comparePriority);

            stackPanel.Child<Button>((button) =>
            {
                createButton = button;

                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Create Item");
                });

                createButtonClickedHandler = (sender, args) =>
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
                };
                button.With((b) => b.Click += createButtonClickedHandler);
                _disposables.Add(Disposable.Create(() => createButton?.With((b) => b.Click -= createButtonClickedHandler)));
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
                // Dispose all tracked disposables
                _disposables.Dispose();
            }

            isDisposed = true;
        }
    }
}
