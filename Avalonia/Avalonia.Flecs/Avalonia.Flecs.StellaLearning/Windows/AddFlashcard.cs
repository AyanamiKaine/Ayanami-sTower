using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public class AddFlashcard : IUIComponent, IDisposable
{
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

            var comparePriority = new ComparePriority(world);
            calculatedPriority = comparePriority.CalculatedPriorityEntity;
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
