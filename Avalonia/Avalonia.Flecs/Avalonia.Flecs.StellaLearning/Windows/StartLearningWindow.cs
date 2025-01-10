using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
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


        var scrollViewer = entities.GetEntityCreateIfNotExist("StartLearningMainContentDisplay")
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

        DefineWindowContents(entities);

        return startLearningWindow;
    }
    private static void DefineWindowContents(NamedEntities entities)
    {



        var spacedRepetitionItems =
                                    entities["SpacedRepetitionItems"]
                                    .Get<ObservableCollection<SpacedRepetitionItem>>();

        DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);

        /*
        Here we describe the logic, what should happen when an spaced repetition item changes?
        We want to recalculate what item should be displayed. 

        We also use this when the underlying item changes, but the same item would be shown,
        for example because you changed the name of it.
        */

        spacedRepetitionItems.CollectionChanged += ((sender, e) =>
            {

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SpacedRepetitionItem newItem in e.NewItems!)
                    {
                        newItem.PropertyChanged += ((sender, e) =>
                        {
                            if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                            {
                                DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);
                            }
                        });
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (SpacedRepetitionItem oldItem in e.OldItems!)
                    {
                        oldItem.PropertyChanged += ((sender, e) =>
                        {
                            if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                            {
                                DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);
                            }
                        });
                    }
                }

                DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);
            });
    }

    private static Entity LearnFileContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var file = (SpacedRepetitionFile)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnFileContent")
            .Set(new TextBlock())
            .SetText(file.FilePath);
    }

    private static Entity LearnVideoContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var video = (SpacedRepetitionVideo)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnVideoContent")
            .Set(new TextBlock())
            .SetText(video.VideoUrl);
    }

    private static Entity LearnExerciseContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var exercise = (SpacedRepetitionExercise)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnExerciseContent")
            .Set(new TextBlock())
            .SetText(exercise.Problem);
    }

    private static Entity LearnQuizContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var quiz = (SpacedRepetitionQuiz)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnQuizContent")
            .Set(new TextBlock())
            .SetText(quiz.Question);
    }

    private static Entity LearnFlashcardContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var flashcard = (SpacedRepetitionFlashcard)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnFlashcardContent")
            .Set(new TextBlock())
            .SetText(flashcard.Front);
    }

    private static Entity LearnClozeContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var cloze = (SpacedRepetitionCloze)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        return entities.GetEntityCreateIfNotExist("LearnClozeContent")
            .Set(new TextBlock())
            .SetText(cloze.FullText);
    }

    private static Entity DisplayRightItem(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {

        return GetNextItemToBeReviewed(spacedRepetitionItems) switch
        {
            SpacedRepetitionFile => LearnFileContent(entities, spacedRepetitionItems),
            SpacedRepetitionCloze => LearnClozeContent(entities, spacedRepetitionItems),
            SpacedRepetitionFlashcard => LearnFlashcardContent(entities, spacedRepetitionItems),
            SpacedRepetitionQuiz => LearnQuizContent(entities, spacedRepetitionItems),
            SpacedRepetitionVideo => LearnVideoContent(entities, spacedRepetitionItems),
            SpacedRepetitionExercise => LearnExerciseContent(entities, spacedRepetitionItems),
            _ => NoMoreItemToBeReviewedContent(entities),
        };
    }

    private static Entity NoMoreItemToBeReviewedContent(NamedEntities entities)
    {
        return entities.GetEntityCreateIfNotExist("NoMoreItemToBeReviewed")
            .Set(new TextBlock())
            .SetText("No More Items To be reviewd");
    }

    private static SpacedRepetitionItem? GetNextItemToBeReviewed(ObservableCollection<SpacedRepetitionItem> items)
    {
        if (items == null || !items.Any())
        {
            return null; // Return null if the collection is empty or null
        }

        DateTime now = DateTime.Now;

        return items
                .Where(item => item.NextReview <= now) // Filter for items that are due
                .OrderBy(item => item.NextReview)     // Order by the next review date (ascending)
                .FirstOrDefault();                  // Take the first item (the nearest due date)
    }
}
