using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type cloze
/// </summary>
public static class AddCloze
{

    /// <summary>
    /// Create the Add Cloze Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var addClozeWindow = entities.GetEntityCreateIfNotExist("AddClozeWindow")
            .Set(new Window())
            .SetWindowTitle("Add Cloze")
            .SetWidth(400)
            .SetHeight(400);

        var scrollViewer = entities.Create()
            .ChildOf(addClozeWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            addClozeWindow.CloseWindow();
        });

        addClozeWindow.OnClosing((s, e) =>
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

        return addClozeWindow;
    }

    private static Entity DefineWindowContents(NamedEntities entities)
    {
        static void AddCloze(string item, ObservableCollection<string> clozes)
        {
            var trimmedItem = item.Trim();
            if (!clozes.Contains(trimmedItem) && !string.IsNullOrWhiteSpace(trimmedItem))
            {
                clozes.Add(trimmedItem);
            }
        }

        var layout = entities.Create()
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);


        var nameTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var clozeBox = entities.Create()
                .ChildOf(layout)
                .Set(new TextBox() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap })
                .SetWatermark("Cloze Text");


        ObservableCollection<string> clozes = [];

        var markAsClozeButton = entities.Create()
            .Set(new Button())
            .SetContent("Mark as Cloze")
            .OnClick((_, _) =>
            {
                var cloze = clozeBox.Get<TextBox>().SelectedText;
                AddCloze(cloze, clozes);
            });

        object? flyout = clozeBox.Get<TextBox>().ContextFlyout = new Flyout() { Content = markAsClozeButton.Get<Button>() };

        var clozeList = entities.GetEntityCreateIfNotExist("ClozeList")
            .ChildOf(layout)
            .Set(new ItemsControl())
            .Set(clozes)
            .SetItemTemplate(DefineClozeTemplate(entities))
            .SetItemsSource(clozes);

        entities.Create()
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Create Cloze")
            .OnClick((sender, args) =>
            {
                if (clozes.Count == 0 || string.IsNullOrEmpty(nameTextBox.GetText()))
                {
                    nameTextBox.SetWatermark("Name is required");
                    return;
                }

                entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionCloze()
                {
                    Name = nameTextBox.GetText(),
                    FullText = clozeBox.GetText(),
                    ClozeWords = [.. clozes],
                    SpacedRepetitionItemType = SpacedRepetitionItemType.Cloze
                });

                nameTextBox.SetText("");
                clozeBox.SetText("");
                clozes.Clear();
            });
        return layout;
    }

    private static FuncDataTemplate<string> DefineClozeTemplate(NamedEntities entities)
    {
        return new FuncDataTemplate<string>((tag, _) =>
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 5
            };

            var nameText = new TextBlock()
            {
                Text = tag
            };

            var removeButton = new Button()
            {
                Content = "X"
            };

            removeButton.Click += ((sender, args) =>
            {
                entities["ClozeList"].Get<ObservableCollection<string>>().Remove(tag);
            });

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(removeButton);
            return stackPanel;
        });
    }
}