using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Media;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;
/// <summary>
/// Represents the window to add spaced repetition items of the type quiz
/// </summary>
public class EditQuiz : IUIComponent, IDisposable
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionQuiz _spacedRepetitionQuiz;
    private readonly CompositeDisposable _disposables = new(); // For managing disposables
    private bool _isDisposed = false; // For IDisposable pattern
    /// <summary>
    /// Create the Add Quiz Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="spacedRepetitionQuiz"></param>
    /// <returns></returns>
    public EditQuiz(World world, SpacedRepetitionQuiz spacedRepetitionQuiz)
    {
        _spacedRepetitionQuiz = spacedRepetitionQuiz;
        _root = world.UI<Window>((window) =>
        {
            window
            .SetTitle($"Edit Quiz: {_spacedRepetitionQuiz.Name}")
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
        return world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBox>? nameTextBox = null;
            UIBuilder<TextBox>? quizQuestionTextBox = null;
            UIBuilder<Grid>? quizAnswers = null;

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
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
                .SetText(_spacedRepetitionQuiz.Name);
            });

            stackPanel.Child<TextBlock>((t) =>
            {
                t.SetText("Quiz Question");
            });

            quizQuestionTextBox = stackPanel.Child<TextBox>((textBox) =>
            {
                textBox
                .SetWatermark("Quiz Question")
                .SetTextWrapping(TextWrapping.Wrap)
                .SetText(_spacedRepetitionQuiz.Question);

                textBox.Get<TextBox>().AcceptsReturn = true;
            });

            quizAnswers = stackPanel.Child<Grid>((grid) =>
            {
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

                        if (_spacedRepetitionQuiz.CorrectAnswerIndex == number)
                        {
                            toggleButton.Check();
                        }
                    });

                    grid.Child<TextBox>((textBox) =>
                    {
                        textBox.SetWatermark("Answer")
                        .SetColumn(1)
                        .SetRow(row)
                        .SetMargin(5)
                        .SetText(_spacedRepetitionQuiz.Answers[number]);
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

            var tagManager = new TagComponent(world, _spacedRepetitionQuiz.Tags);
            stackPanel.Child(tagManager); // Add the tag manager UI
            _disposables.Add(tagManager); // Add tagManager to window's disposables

            stackPanel.Child<Button>((button) =>
            {
                button
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch);
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Save Changes");
                });

                button.OnClick((_, _) =>
                {
                    if (nameTextBox is null ||
                        quizQuestionTextBox is null ||
                        quizAnswers is null)
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

                    Grid grid = quizAnswers.Get<Grid>();

                    bool isAnswerCheck()
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (FindControl<ToggleButton>(grid, i, 0)?.IsChecked ?? false)
                                return true;
                        }
                        return false;
                    }

                    if (!isAnswerCheck())
                    {

                        var cd = new ContentDialog()
                        {
                            Title = "Missing Answer",
                            Content = "Now anwser for your quiz was selected please select at least one",
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
                            if (FindControl<ToggleButton>(grid, i, 0)?.IsChecked ?? false)
                                return i;
                        }
                        throw new Exception("An Answer must be checked!");
                    }

                    List<string> gatherAllAnswers()
                    {
                        var answers = new List<string>();
                        for (int i = 0; i < 4; i++)
                        {
                            answers.Add(FindControl<TextBox>(grid, i, 1)?.Text ?? $"Answer{i + 1}");
                        }
                        return answers;
                    }


                    _spacedRepetitionQuiz.Name = nameTextBox.GetText();
                    _spacedRepetitionQuiz.Question = quizQuestionTextBox.GetText();
                    _spacedRepetitionQuiz.CorrectAnswerIndex = findAnswerIndex();
                    _spacedRepetitionQuiz.Answers = gatherAllAnswers();
                    _spacedRepetitionQuiz.Tags = [.. tagManager.Tags];

                    // Clearing an entity results in all components, relationships etc to be removed.
                    // this also results in invoking the remove hooks that are used on components for 
                    // cleanup. For example removing a window component results in closing it.
                    _root.Clear();
                });
            });
        }).Entity;
    }

    private static T? FindControl<T>(Grid grid, int row, int column) where T : Control
    {
        return grid.Children
                   .OfType<T>()
                   .FirstOrDefault(control => Grid.GetRow(control) == row && Grid.GetColumn(control) == column);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes entities and event handlers
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
    ~EditQuiz() { Dispose(disposing: false); }

}