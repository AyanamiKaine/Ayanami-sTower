using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
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

        entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        entities.Create()
                .ChildOf(layout)
                .Set(new TextBox())
                .SetWatermark("Quiz Question");

        CreateAnswerLayout(entities).ChildOf(layout);

        entities.Create()
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Create Quiz");

        return layout;
    }

    private static Entity CreateAnswerLayout(NamedEntities entities)
    {
        var quizAnswers = entities.Create()
            .Set(new Grid())
            .SetRowDefinitions(new RowDefinitions("*,*,*,*"))
            .SetColumnDefinitions(new ColumnDefinitions("Auto, *"));


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


        Console.WriteLine(FindControl<ToggleButton>(quizAnswers.Get<Grid>(), 0, 0)!.IsChecked);
        return quizAnswers;
    }

    private static T? FindControl<T>(this Grid grid, int row, int column) where T : Control
    {
        return grid.Children
                   .OfType<T>()
                   .FirstOrDefault(control => Grid.GetRow(control) == row && Grid.GetColumn(control) == column);
    }
}