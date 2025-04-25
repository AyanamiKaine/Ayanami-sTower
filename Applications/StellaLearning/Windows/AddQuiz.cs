/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AyanamisTower.StellaLearning.Data;
using AyanamisTower.StellaLearning.UiComponents;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type quiz
/// </summary>
public class AddQuiz : IUIComponent, IDisposable
{
    /// <summary>
    /// Collection to track all disposables
    /// </summary>
    private readonly CompositeDisposable _disposables = [];
    private EventHandler<RoutedEventArgs>? createButtonClickedHandler;
    private bool isDisposed = false;
    private UIBuilder<Button>? createButton = null;
    private Entity _root;

    /// <inheritdoc/>
    public Entity Root => _root;
    private Entity calculatedPriority;
    private readonly ComparePriority comparePriority;

    /// <summary>
    /// Create the Add Quiz Window
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public AddQuiz(World world)
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

        _root = world
            .UI<Window>(
                (window) =>
                {
                    window
                        .AlwaysOnTop(world.Get<Settings>().EnableAlwaysOnTop)
                        .SetTitle("Add Quiz")
                        .SetWidth(400)
                        .SetHeight(400)
                        .Child<ScrollViewer>(
                            (scrollViewer) =>
                            {
                                scrollViewer
                                    .SetRow(1)
                                    .SetColumnSpan(3)
                                    .Child(DefineWindowContents(world));
                            }
                        );
                    window.OnClosed((sender, args) => Dispose());

                    window.Show();
                }
            )
            .Entity;
    }

    private Entity DefineWindowContents(World world)
    {
        return world
            .UI<StackPanel>(
                (stackPanel) =>
                {
                    UIBuilder<TextBox>? nameTextBox = null;
                    UIBuilder<TextBox>? quizQuestionTextBox = null;
                    UIBuilder<Grid>? quizAnswers = null;

                    stackPanel.SetOrientation(Orientation.Vertical).SetSpacing(10).SetMargin(20);

                    stackPanel.Child<TextBox>(
                        (textBox) =>
                        {
                            nameTextBox = textBox;
                            textBox.SetWatermark("Name");
                        }
                    );

                    stackPanel.Child<TextBox>(
                        (textBox) =>
                        {
                            quizQuestionTextBox = textBox;
                            textBox
                                .SetWatermark("Quiz Question")
                                .SetTextWrapping(TextWrapping.Wrap);
                            textBox.Get<TextBox>().AcceptsReturn = true;
                        }
                    );

                    stackPanel.Child<Grid>(
                        (grid) =>
                        {
                            quizAnswers = grid;
                            grid.SetRowDefinitions("*,*,*,*").SetColumnDefinitions("Auto, *");

                            // Create answer rows
                            for (int number = 0; number < 4; number++)
                            {
                                int row = number; // Capture for closure

                                grid.Child<ToggleButton>(
                                    (toggleButton) =>
                                    {
                                        toggleButton
                                            .SetRow(row)
                                            .SetColumn(0)
                                            .Child<TextBlock>(
                                                (textBock) =>
                                                {
                                                    textBock.SetText("Correct");
                                                }
                                            );
                                    }
                                );

                                grid.Child<TextBox>(
                                    (textBox) =>
                                    {
                                        textBox
                                            .SetWatermark("Answer")
                                            .SetColumn(1)
                                            .SetRow(row)
                                            .SetMargin(5);
                                    }
                                );
                            }
                        }
                    );

                    var tagManager = new TagComponent(world);
                    stackPanel.Child(tagManager);

                    stackPanel.Child<Separator>(
                        (separator) =>
                        {
                            separator
                                .SetMargin(0, 0, 0, 10)
                                .SetBorderThickness(new Thickness(100, 5, 100, 0))
                                .SetBorderBrush(Brushes.Black);
                        }
                    );

                    stackPanel.Child(comparePriority);

                    stackPanel.Child<Button>(
                        (button) =>
                        {
                            // We only want to enable the button when a anwser is selected.
                            //button.Disable();
                            button
                                .SetVerticalAlignment(VerticalAlignment.Center)
                                .SetHorizontalAlignment(HorizontalAlignment.Center);
                            button.Child<TextBlock>(
                                (textBlock) =>
                                {
                                    textBlock.SetText("Create Quiz");
                                }
                            );

                            createButtonClickedHandler = (_, _) =>
                            {
                                if (
                                    nameTextBox is null
                                    || quizQuestionTextBox is null
                                    || quizAnswers is null
                                )
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

                                Grid grid = quizAnswers.Get<Grid>();

                                bool isAnswerCheck()
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (
                                            FindControl<ToggleButton>(grid, i, 0)?.IsChecked
                                            ?? false
                                        )
                                            return true;
                                    }
                                    return false;
                                }

                                if (!isAnswerCheck())
                                {
                                    var cd = new ContentDialog()
                                    {
                                        Title = "Missing Answer",
                                        Content =
                                            "No anwser for your quiz was selected please select at least one",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                    return;
                                }

                                int findAnswerIndex()
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        if (
                                            FindControl<ToggleButton>(grid, i, 0)?.IsChecked
                                            ?? false
                                        )
                                            return i;
                                    }
                                    throw new Exception("An Answer must be checked!");
                                }

                                List<string> gatherAllAnswers()
                                {
                                    var answers = new List<string>();
                                    for (int i = 0; i < 4; i++)
                                    {
                                        answers.Add(
                                            FindControl<TextBox>(grid, i, 1)?.Text
                                                ?? $"Answer{i + 1}"
                                        );
                                    }
                                    return answers;
                                }

                                world
                                    .Get<ObservableCollection<SpacedRepetitionItem>>()
                                    .Add(
                                        new SpacedRepetitionQuiz()
                                        {
                                            Name = nameTextBox.GetText(),
                                            Question = quizQuestionTextBox.GetText(),
                                            Priority = calculatedPriority.Get<int>(),
                                            CorrectAnswerIndex = findAnswerIndex(),
                                            Answers = gatherAllAnswers(),
                                            Tags = [.. tagManager.Tags],
                                            SpacedRepetitionItemType =
                                                SpacedRepetitionItemType.Quiz,
                                        }
                                    );

                                // Reset form
                                nameTextBox.SetText("");
                                quizQuestionTextBox.SetText("");
                                calculatedPriority.Set(500000000);
                                comparePriority.Reset();
                                tagManager.ClearTags();

                                // Clear answer text boxes and toggle buttons
                                for (int i = 0; i < 4; i++)
                                {
                                    FindControl<TextBox>(grid, i, 1)!.Text = "";
                                    FindControl<ToggleButton>(grid, i, 0)!.IsChecked = false;
                                }
                            };

                            button.With((b) => b.Click += createButtonClickedHandler);
                        }
                    );
                }
            )
            .Entity;
    }

    private static T? FindControl<T>(Grid grid, int row, int column)
        where T : Control
    {
        return grid
            .Children.OfType<T>()
            .FirstOrDefault(control =>
                Grid.GetRow(control) == row && Grid.GetColumn(control) == column
            );
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
                    _root.Get<Window>().Content = null;
                    _root.Destruct();
                }
                // Dispose all tracked disposables
                _disposables.Dispose();
            }

            isDisposed = true;
        }
    }
}
