using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to learn spaced repetition items
/// </summary>
public static class StartLearningWindow
{

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var startLearningWindow = entities.GetEntityCreateIfNotExist("StartLearningWindow")
            .Set(new Window())
            .SetWindowTitle("Start Learning")
            .SetWidth(400)
            .SetHeight(400);


        var scrollViewer = entities.Create()
            .ChildOf(startLearningWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            startLearningWindow.CloseWindow();
        });

        startLearningWindow.OnClosing((s, e) =>
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

        return startLearningWindow;
    }
    private static Entity DefineWindowContents(NamedEntities entities)
    {
        var layout = entities.Create()
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);














        return layout;
    }

}
