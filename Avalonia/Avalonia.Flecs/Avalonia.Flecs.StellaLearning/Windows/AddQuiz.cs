using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Input;
using Avalonia.Platform.Storage;
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

        var quizNameTextBox = entities.GetEntityCreateIfNotExist("QuizNameTextBox")
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var quizQuestionTextBox = entities.GetEntityCreateIfNotExist("QuizQuestionTextBox")
                .ChildOf(layout)
                .Set(new TextBox())
                .SetWatermark("Quiz Question");

        var quizAnswers = entities.GetEntityCreateIfNotExist("QuizAnswers")
            .ChildOf(layout)
            .Set(new Grid())
            .SetRowDefinitions(new RowDefinitions("Auto"))
            .SetColumnDefinitions(new ColumnDefinitions("Auto, *, Auto"));

        var toggle = entities.GetEntityCreateIfNotExist("QuizAnswerToggle")
            .ChildOf(quizAnswers)
            .Set(new ToggleButton())
            .SetContent("Correct")
            .SetColumn(0);

        var quizAnswer = entities.GetEntityCreateIfNotExist("QuizAnswerTextBox")
            .ChildOf(quizAnswers)
            .Set(new TextBox())
            .SetWatermark("Answer")
            .SetColumn(1)
            .SetMargin(5);

        entities.Create()
            .ChildOf(quizAnswers)
            .Set(new Button())
            .SetContent("Delete")
            .SetColumn(2);

        entities.Create()
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Add Answer");

        entities.Create()
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Create Quiz");

        return layout;
    }
}