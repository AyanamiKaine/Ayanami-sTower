using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to add spaced repetition items of the type file
/// </summary>
public static class AddFlashcard
{
    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var addFlashcardWindow = entities.GetEntityCreateIfNotExist("AddFlashcardWindow")
            .Set(new Window())
            .SetWindowTitle("Add Flashcard")
            .SetWidth(400)
            .SetHeight(400);


        var scrollViewer = entities.Create()
            .ChildOf(addFlashcardWindow)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        entities["MainWindow"].OnClosed((_, _) =>
        {
            //When the main window is closed close the add file window as well
            addFlashcardWindow.CloseWindow();
        });

        addFlashcardWindow.OnClosing((s, e) =>
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

        return addFlashcardWindow;
    }

    private static Entity DefineWindowContents(NamedEntities entities)
    {
        var layout = entities.Create()
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetSpacing(10)
            .SetMargin(20);

        var nameTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Name");

        var frontText = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox() { AcceptsReturn = true })
            .SetTextWrapping(TextWrapping.Wrap)
            .SetWatermark("Front Text");

        var backText = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox() { AcceptsReturn = true })
            .SetTextWrapping(TextWrapping.Wrap)
            .SetWatermark("Back Text");


        ObservableCollection<Tag> tags = [];


        var tagsTextBox = entities.Create()
            .ChildOf(layout)
            .Set(new TextBox())
            .SetWatermark("Tags");

        tagsTextBox.OnKeyDown((sender, args) =>
        {
            if (args.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(tagsTextBox.GetText()))
                {
                    return;
                }

                tags.Add(new(tagsTextBox.GetText()));
                tagsTextBox.SetText("");
            }
        });

        var flashCardTagsList = entities.GetEntityCreateIfNotExist("FlashCardTagsList")
            .ChildOf(layout)
            .Set(new ItemsControl())
            .Set(tags)
            .SetItemTemplate(DefineTagTemplate(entities))
            .SetItemsSource(tags);

        var createFlashcardButton = entities.Create()
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Create Item")
            .OnClick((sender, args) =>
            {
                if (string.IsNullOrEmpty(nameTextBox.GetText()))
                {
                    nameTextBox.SetWatermark("Name is required");
                    return;
                }

                entities["SpacedRepetitionItems"].Get<ObservableCollection<SpacedRepetitionItem>>().Add(new SpacedRepetitionFlashcard()
                {
                    Name = nameTextBox.GetText(),
                    Front = frontText.GetText(),
                    Back = backText.GetText(),
                    SpacedRepetitionItemType = SpacedRepetitionItemType.Flashcard
                });

                nameTextBox.SetText("");
                frontText.SetText("");
                backText.SetText("");
                tags.Clear();
            });

        return layout;
    }

    private static FuncDataTemplate<Tag> DefineTagTemplate(NamedEntities entities)
    {
        return new FuncDataTemplate<Tag>((tag, _) =>
        {
            var stackPanel = new StackPanel()
            {
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 5
            };

            var nameText = new TextBlock()
            {
                Text = tag.Name
            };

            var removeButton = new Button()
            {
                Content = "X"
            };

            removeButton.Click += ((sender, args) =>
            {
                entities["FlashCardTagsList"].Get<ObservableCollection<Tag>>().Remove(tag);
            });
            /*
            removeButton.OnClick((sender, args) =>
            {
                entities["Tags"].Get<ObservableCollection<Tag>>().Remove(tag);
            });
            */

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(removeButton);
            return stackPanel;
        });
    }
}
