using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.UiComponents;
using Avalonia.Flecs.Util;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;
/// <summary>
/// Represents the window to add spaced repetition items of the type quiz
/// </summary>
public static class AddQuiz
{
    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var addQuizWindow = entities.GetEntityCreateIfNotExist("AddQuizWindow")
            .Set(new Window())
            .SetWindowTitle("Add Quiz")
            .SetWidth(400)
            .SetHeight(400);

        var scrollViewer = entities.GetEntityCreateIfNotExist("AddQuizScrollViewer")
            .ChildOf(addQuizWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            addQuizWindow.CloseWindow();
        });

        addQuizWindow.OnClosing((s, e) =>
        {
            // As long as the main window is visible dont 
            // close the window but hide it instead
            if (entities["MainWindow"].Get<Window>().IsVisible)
            {
                ((Window)s!).Hide();
                e.Cancel = true;
            }
        });

        DefineWindowContents(entities).ChildOf(scrollViewer);

        return addQuizWindow;
    }

    private static Entity DefineWindowContents(NamedEntities entities)
    {
        var layout = entities.GetEntityCreateIfNotExist("AddQuizLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

        var nameTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var quizQuestionTextBox = entities.Create()
                .ChildOf(layout)
                .Set(new TextBox() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap })
                .SetWatermark("Quiz Question");

        var quizAnswers = CreateAnswerLayout(entities).ChildOf(layout);

        var isAnwserCheck = (() =>
        {
            Console.WriteLine(FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 0, 0)!.IsChecked);
            Console.WriteLine(FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 1, 0)!.IsChecked);
            Console.WriteLine(FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 2, 0)!.IsChecked);
            Console.WriteLine(FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 3, 0)!.IsChecked);


            if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 0, 0)?.IsChecked ?? false)
                return true;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 1, 0)?.IsChecked ?? false)
                return true;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 2, 0)?.IsChecked ?? false)
                return true;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 3, 0)?.IsChecked ?? false)
                return true;
            else
                return false;
        });

        var findAnwserIndex = (() =>
        {
            if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 0, 0)?.IsChecked ?? false)
                return 0;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 1, 0)?.IsChecked ?? false)
                return 1;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 2, 0)?.IsChecked ?? false)
                return 2;
            else if (FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 3, 0)?.IsChecked ?? false)
                return 3;
            else
                throw new Exception("An Answer must be checked!");

        });

        var gatherAllAnwsers = (() =>
        {
            var anwsers = new List<string>
            {
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 0, 1)?.Text ?? "Anwser1",
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 1, 1)?.Text ?? "Anwser2",
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 2, 1)?.Text ?? "Anwser3",
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 3, 1)?.Text ?? "Anwser4",

            };


            return anwsers;
        });

        var createQuizButton = entities.Create()
            .Set(new Button())
            .SetContent("Create Quiz");

        (Entity priorityCompareComponent, Entity calculatedPriority) = ComparePriority.Create(entities, layout, createQuizButton);
        priorityCompareComponent.ChildOf(layout);

        createQuizButton
        .ChildOf(layout)
        .OnClick((_, _) =>
            {
                if (string.IsNullOrEmpty(nameTextBox.GetText()))
                {
                    nameTextBox.SetWatermark("Name is required");
                    return;
                }

                if (!isAnwserCheck())
                {
                    return;
                }

                entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionQuiz()
                {
                    Name = nameTextBox.GetText(),
                    Question = quizQuestionTextBox.GetText(),

                    Priority = calculatedPriority.Get<int>(),

                    CorrectAnswerIndex = findAnwserIndex(),
                    Answers = gatherAllAnwsers(),
                    SpacedRepetitionItemType = SpacedRepetitionItemType.Quiz
                });

                nameTextBox.SetText("");
                quizQuestionTextBox.SetText("");
                calculatedPriority.Set(500000000);

                FindControl<TextBox>(quizAnswers.Get<Grid>(), 0, 1)!.Text = "";
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 1, 1)!.Text = "";
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 2, 1)!.Text = "";
                FindControl<TextBox>(quizAnswers.Get<Grid>(), 3, 1)!.Text = "";

                FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 0, 0)!.IsChecked = false;
                FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 1, 0)!.IsChecked = false;
                FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 2, 0)!.IsChecked = false;
                FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 3, 0)!.IsChecked = false;

            });

        return layout;
    }

    private static Entity CreateAnswerLayout(NamedEntities entities)
    {
        var quizAnswers = entities.Create()
            .Set(new Grid())
            .SetRowDefinitions("*,*,*,*")
            .SetColumnDefinitions("Auto, *");

        foreach (int number in Enumerable.Range(0, 4))
        {
            entities.Create()
                .ChildOf(quizAnswers)
                .Set(new ToggleButton())
                .SetContent("Correct")
                .SetRow(number)
                .SetColumn(0);

            entities.Create()
                .ChildOf(quizAnswers)
                .Set(new TextBox())
                .SetWatermark("Answer")
                .SetColumn(1)
                .SetRow(number)
                .SetMargin(5);
        }
        return quizAnswers;
    }

    private static T? FindControl<T>(this Grid grid, int row, int column) where T : Control
    {
        return grid.Children
                   .OfType<T>()
                   .FirstOrDefault(control => Grid.GetRow(control) == row && Grid.GetColumn(control) == column);
    }
}