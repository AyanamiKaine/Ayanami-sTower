using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;
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

        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle("Add Quiz")
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
        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? quizQuestionTextBox = null;
            UIBuilder<Grid>? quizAnswers = null;

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
                quizQuestionTextBox = textBox;
                textBox.SetWatermark("Quiz Question")
                .SetTextWrapping(TextWrapping.Wrap);
                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            stackPanel.Child<Grid>((grid) =>
            {
                quizAnswers = grid;
                grid.SetRowDefinitions("*,*,*,*")
                .SetColumnDefinitions("Auto, *");

                // Create answer rows
                for (int number = 0; number < 4; number++)
                {
                    int row = number; // Capture for closure

                    grid.Child<ToggleButton>((toggleButton) =>
                    {
                        toggleButton
                        .SetRow(row)
                        .SetColumn(0)
                        .Child<TextBlock>((textBock) =>
                        {
                            textBock.SetText("Correct");
                        });
                    });

                    grid.Child<TextBox>((textBox) =>
                    {
                        textBox.SetWatermark("Answer")
                        .SetColumn(1)
                        .SetRow(row)
                        .SetMargin(5);
                    });
                }
            });
            /*
            //TODO: Implement a textblock that shows when the user didnt select an option as anwser
            stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock.SetText("You must select at least one answer");
                textBlock.SetFontSize(12);
                textBlock.SetMargin(new Thickness(0, -5, 0, 0));
            });
            */

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
                // We only want to enable the button when a anwser is selected.
                //button.Disable();

                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Create Quiz");
                });

                createButtonClickedHandler = (_, _) =>
                {
                    if (nameTextBox is null ||
                        quizQuestionTextBox is null ||
                        quizAnswers is null)
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
                            IsSecondaryButtonEnabled = true,
                        };
                        cd.ShowAsync();
                        return;

                    }

                    Grid grid = quizAnswers.Get<Grid>();

                    var isAnswerCheck = () =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (FindControl<ToggleButton>(grid, i, 0)?.IsChecked ?? false)
                                return true;
                        }
                        return false;
                    };

                    if (!isAnswerCheck())
                    {

                        var cd = new ContentDialog()
                        {
                            Title = "Missing Answer",
                            Content = "Now anwser for your quiz was selected please select at least one",
                            PrimaryButtonText = "Ok",
                            IsSecondaryButtonEnabled = true,
                        };
                        cd.ShowAsync();
                        return;
                    }

                    var findAnswerIndex = () =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (FindControl<ToggleButton>(grid, i, 0)?.IsChecked ?? false)
                                return i;
                        }
                        throw new Exception("An Answer must be checked!");
                    };

                    var gatherAllAnswers = () =>
                    {
                        var answers = new List<string>();
                        for (int i = 0; i < 4; i++)
                        {
                            answers.Add(FindControl<TextBox>(grid, i, 1)?.Text ?? $"Answer{i + 1}");
                        }
                        return answers;
                    };

                    world.Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionQuiz()
                    {
                        Name = nameTextBox.GetText(),
                        Question = quizQuestionTextBox.GetText(),
                        Priority = calculatedPriority.Get<int>(),
                        CorrectAnswerIndex = findAnswerIndex(),
                        Answers = gatherAllAnswers(),
                        SpacedRepetitionItemType = SpacedRepetitionItemType.Quiz
                    });

                    // Reset form
                    nameTextBox.SetText("");
                    quizQuestionTextBox.SetText("");
                    calculatedPriority.Set(500000000);
                    comparePriority.Reset();

                    // Clear answer text boxes and toggle buttons
                    for (int i = 0; i < 4; i++)
                    {
                        FindControl<TextBox>(grid, i, 1)!.Text = "";
                        FindControl<ToggleButton>(grid, i, 0)!.IsChecked = false;
                    }
                };

                button.With((b) => b.Click += createButtonClickedHandler);

            });
        });
    }

    private static T? FindControl<T>(Grid grid, int row, int column) where T : Control
    {
        return grid.Children
                   .OfType<T>()
                   .FirstOrDefault(control => Grid.GetRow(control) == row && Grid.GetColumn(control) == column);
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