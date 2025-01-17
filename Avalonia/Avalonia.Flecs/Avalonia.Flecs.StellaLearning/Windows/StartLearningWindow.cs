using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.Util;
using Avalonia.Media;
using Avalonia.Threading;
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

        foreach (var item in spacedRepetitionItems)
        {
            item.PropertyChanged += ((sender, e) =>
            {
                if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                {
                    DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);
                }
            });
        }

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

        /*
        Every minute we check if a item can now be reviewed
        */

        var timer = new Timer(60000)
        {
            AutoReset = true,
            Enabled = true,
        };

        timer.Elapsed += ((object? sender, ElapsedEventArgs e) =>
        {
            Dispatcher.UIThread.Post(() =>
                        {
                            DisplayRightItem(entities, spacedRepetitionItems).ChildOf(entities["StartLearningMainContentDisplay"]);
                        });
        });
    }

    private static Entity LearnFileContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var file = (SpacedRepetitionFile)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        var layout = entities.GetEntityCreateIfNotExist("LearnFileLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

        entities.GetEntityCreateIfNotExist("learnFileQuestion")
            .ChildOf(layout)
            .Set(new TextBlock()
            { TextWrapping = Media.TextWrapping.Wrap })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetMargin(0, 20)
            .SetText(file.Question);

        entities.GetEntityCreateIfNotExist("LearnFileContent")
            .ChildOf(layout)
            .Set(new TextBlock()
            { TextWrapping = Media.TextWrapping.Wrap })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetMargin(0, 10)
            .SetText(file.FilePath);

        entities.GetEntityCreateIfNotExist("FileOpenButton")
            .Set(new Button())
            .SetContent("Open File")
            .SetMargin(0, 10)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .ChildOf(layout)
            .OnClick((sender, e) =>
            {
                Console.WriteLine("Open Clicked");
                try
                {
                    if (entities["SettingsProvider"].Has<Settings>())
                    {
                        string ObsidianPath = entities["SettingsProvider"].Get<Settings>().ObsidianPath;
                        FileOpener.OpenMarkdownFileWithObsidian(file.FilePath, ObsidianPath);
                    }
                    else
                    {
                        FileOpener.OpenFileWithDefaultProgram(file.FilePath);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine(ex.Message, ex.FileName);
                }
            });

        var reviewButtonGrid = entities.GetEntityCreateIfNotExist("FileReviewButtonGrid")
            .Set(new Grid())
            .ChildOf(layout)
            .SetColumnDefinitions("*, *, *, *")
            .SetRowDefinitions("auto");

        var easyReviewButton = entities.GetEntityCreateIfNotExist("FileEasyReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button())
            .SetContent("Easy")
            .SetMargin(10, 0)
            .SetColumn(0)
            .OnClick((_, _) => file.EasyReview());

        var goodReviewButton = entities.GetEntityCreateIfNotExist("FileGoodReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button())
            .SetContent("Good")
            .SetMargin(10, 0)
            .SetColumn(1)
            .OnClick((_, _) => file.GoodReview());

        var hardReviewButton = entities.GetEntityCreateIfNotExist("FileHardReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button())
            .SetContent("Hard")
            .SetMargin(10, 0)
            .SetColumn(2)
            .OnClick((_, _) => file.HardReview());

        var againReviewButton = entities.GetEntityCreateIfNotExist("FileAgainReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button())
            .SetContent("Again")
            .SetMargin(10, 0)
            .SetColumn(3)
            .OnClick((_, _) => file.AgainReview());

        return layout;
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
        var layout = entities.GetEntityCreateIfNotExist("LearnQuizLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

        var quiz = (SpacedRepetitionQuiz)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        entities.GetEntityCreateIfNotExist("LearnQuizContent")
            .ChildOf(layout)
            .Set(new TextBlock())
            .SetText(quiz.Name);

        entities.GetEntityCreateIfNotExist("LearnQuizQuestion")
            .ChildOf(layout)
            .Set(new TextBlock())
            .SetMargin(20)
            .SetText(quiz.Question);

        var anwserButtonGrid = entities.GetEntityCreateIfNotExist("AnwserButtonGrid")
            .Set(new Grid())
            .ChildOf(layout)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetColumnDefinitions("auto,auto")
            .SetRowDefinitions("*,*");

        var anwser1Button = entities.GetEntityCreateIfNotExist("anwser1Button")
            .ChildOf(anwserButtonGrid)
            .Set(new Button())
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetContent(quiz.Answers[0])
            .SetMargin(10, 10)
            .SetColumn(0)
            .SetRow(0)
            .OnClick(async (sender, args) =>
            {
                if (sender is Button button)
                {

                    if (quiz.CorrectAnswerIndex == 0)
                    {

                        button.Background = Brushes.LightGreen;
                        await Task.Delay(1000);
                        quiz.GoodReview();
                    }
                    else
                    {
                        button.Background = Brushes.Red;
                        await Task.Delay(1000);
                        quiz.AgainReview();
                    }
                }
            });

        var anwser2Button = entities.GetEntityCreateIfNotExist("anwser2Button")
            .ChildOf(anwserButtonGrid)
            .Set(new Button())
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetContent(quiz.Answers[1])
            .SetMargin(10, 10)
            .SetColumn(1)
            .SetRow(0)
            .OnClick(async (sender, args) =>
            {
                if (sender is Button button)
                {

                    if (quiz.CorrectAnswerIndex == 1)
                    {
                        button.Background = Brushes.LightGreen;
                        await Task.Delay(1000);
                        quiz.GoodReview();
                    }
                    else
                    {
                        button.Background = Brushes.Red;
                        await Task.Delay(1000);
                        quiz.AgainReview();
                    }
                }
            });

        var anwser3Button = entities.GetEntityCreateIfNotExist("anwser3Button")
            .ChildOf(anwserButtonGrid)
            .Set(new Button())
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetContent(quiz.Answers[2])
            .SetMargin(10, 10)
            .SetColumn(0)
            .SetRow(1)
            .OnClick(async (sender, args) =>
            {
                if (sender is Button button)
                {

                    if (quiz.CorrectAnswerIndex == 2)
                    {
                        button.Background = Brushes.LightGreen;
                        await Task.Delay(1000);
                        quiz.GoodReview();
                    }
                    else
                    {
                        button.Background = Brushes.Red;
                        await Task.Delay(1000);
                        quiz.AgainReview();
                    }
                }
            });

        var anwser4Button = entities.GetEntityCreateIfNotExist("anwser4Button")
            .ChildOf(anwserButtonGrid)
            .Set(new Button())
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetContent(quiz.Answers[3])
            .SetMargin(10, 10)
            .SetColumn(1)
            .SetRow(1)
            .OnClick(async (sender, args) =>
            {
                if (sender is Button button)
                {

                    if (quiz.CorrectAnswerIndex == 3)
                    {
                        button.Background = Brushes.LightGreen;
                        await Task.Delay(1000);
                        quiz.GoodReview();
                    }
                    else
                    {
                        button.Background = Brushes.Red;
                        await Task.Delay(1000);
                        quiz.AgainReview();
                    }
                }
            });

        return layout;
    }

    private static Entity LearnFlashcardContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var flashcard = (SpacedRepetitionFlashcard)GetNextItemToBeReviewed(spacedRepetitionItems)!;


        var layout = entities.GetEntityCreateIfNotExist("LearnFlashcardLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

        entities.GetEntityCreateIfNotExist("LearnFlashcardContent")
                    .ChildOf(layout)
                    .Set(new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap
                    })
                    .SetText(flashcard.Front);

        entities.GetEntityCreateIfNotExist("LearnFlashcardSeparatorLine")
            .ChildOf(layout)
            .Set(new Separator()
            {
                BorderThickness = new Thickness(100, 5, 100, 0),
                BorderBrush = Brushes.Black,
            });

        entities.GetEntityCreateIfNotExist("LearnFlashcardRevealButton")
            .ChildOf(layout)
            .Set(new Button())
            .SetContent("Reveal")
            .SetMargin(0, 20)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .OnClick((_, _) =>
            {
                entities["LearnFlashcardBackText"].Get<TextBlock>().IsVisible = true;
                entities["FlashcardEasyReviewButton"].Get<Button>().IsVisible = true;
                entities["FlashcardGoodReviewButton"].Get<Button>().IsVisible = true;
                entities["FlashcardHardReviewButton"].Get<Button>().IsVisible = true;
                entities["FlashcardAgainReviewButton"].Get<Button>().IsVisible = true;

                entities["LearnFlashcardRevealButton"].Get<Button>().IsVisible = false;
            });

        entities.GetEntityCreateIfNotExist("LearnFlashcardBackText")
            .ChildOf(layout)
            .Set(new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                IsVisible = false,
            })
            .SetText(flashcard.Back);

        var reviewButtonGrid = entities.GetEntityCreateIfNotExist("FlashcardReviewButtonGrid")
                    .Set(new Grid())
                    .ChildOf(layout)
                    .SetColumnDefinitions("*, *, *, *")
                    .SetRowDefinitions("auto");

        var easyReviewButton = entities.GetEntityCreateIfNotExist("FlashcardEasyReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsVisible = false
            })
            .SetContent("Easy")
            .SetMargin(10, 0)
            .SetColumn(0)
            .OnClick((_, _) => flashcard.EasyReview());

        var goodReviewButton = entities.GetEntityCreateIfNotExist("FlashcardGoodReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsVisible = false
            })
            .SetContent("Good")
            .SetMargin(10, 0)
            .SetColumn(1)
            .OnClick((_, _) => flashcard.GoodReview());

        var hardReviewButton = entities.GetEntityCreateIfNotExist("FlashcardHardReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsVisible = false
            })
            .SetContent("Hard")
            .SetMargin(10, 0)
            .SetColumn(2)
            .OnClick((_, _) => flashcard.HardReview());

        var againReviewButton = entities.GetEntityCreateIfNotExist("FlashcardAgainReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsVisible = false
            })
            .SetContent("Again")
            .SetMargin(10, 0)
            .SetColumn(3)
            .OnClick((_, _) => flashcard.AgainReview());


        return layout;
    }

    private static Entity LearnClozeContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {

        var layout = entities.GetEntityCreateIfNotExist("LearnClozeLayout")
            .Set(new StackPanel())
            .SetOrientation(Layout.Orientation.Vertical)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

        var cloze = (SpacedRepetitionCloze)GetNextItemToBeReviewed(spacedRepetitionItems)!;

        StringBuilder sb = new StringBuilder(cloze.FullText);
        foreach (string word in cloze.ClozeWords)
        {
            sb.Replace(word, "[...]");
        }

        string clozeRemovedText = sb.ToString();


        var clozeText = entities.GetEntityCreateIfNotExist("LearnClozeContent")
            .Set(new TextBlock()
            {
                TextWrapping = Media.TextWrapping.Wrap
            })
            .ChildOf(layout)
            .SetText(clozeRemovedText);

        var showMaskedClozeButton = entities.Create()
            .Set(new Button())
            .SetMargin(15)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .ChildOf(layout)
            .SetContent("Show");

        var reviewButtonGrid = entities.GetEntityCreateIfNotExist("ClozeReviewButtonGrid")
            .Set(new Grid())
            .ChildOf(layout)
            .SetColumnDefinitions("*, *, *, *")
            .SetRowDefinitions("auto");

        var easyReviewButton = entities.GetEntityCreateIfNotExist("ClozeEasyReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsEnabled = false
            })
            .SetContent("Easy")
            .SetMargin(10, 0)
            .SetColumn(0)
            .OnClick((_, _) => cloze.EasyReview());

        var goodReviewButton = entities.GetEntityCreateIfNotExist("ClozeGoodReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsEnabled = false
            })
            .SetContent("Good")
            .SetMargin(10, 0)
            .SetColumn(1)
            .OnClick((_, _) => cloze.GoodReview());

        var hardReviewButton = entities.GetEntityCreateIfNotExist("ClozeHardReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsEnabled = false
            })
            .SetContent("Hard")
            .SetMargin(10, 0)
            .SetColumn(2)
            .OnClick((_, _) => cloze.HardReview());

        var againReviewButton = entities.GetEntityCreateIfNotExist("ClozeAgainReviewButton")
            .ChildOf(reviewButtonGrid)
            .Set(new Button()
            {
                IsEnabled = false
            })
            .SetContent("Again")
            .SetMargin(10, 0)
            .SetColumn(3)
            .OnClick((_, _) => cloze.AgainReview());

        showMaskedClozeButton.OnClick((_, _) =>
            {
                clozeText.SetText(cloze.FullText);
                againReviewButton.Get<Button>().IsEnabled = true;
                hardReviewButton.Get<Button>().IsEnabled = true;
                goodReviewButton.Get<Button>().IsEnabled = true;
                easyReviewButton.Get<Button>().IsEnabled = true;

            });

        return layout;
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
            _ => NoMoreItemToBeReviewedContent(entities, spacedRepetitionItems),
        };
    }

    private static Entity NoMoreItemToBeReviewedContent(NamedEntities entities, ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        var futureItem = NextItemToBeReviewedInFuture(spacedRepetitionItems);

        string? text;
        if (futureItem is null)
        {
            text = "Currently: No Items";
        }
        else
        {
            text = $"Next Item: '{futureItem?.Name}', due: {futureItem?.NextReview}";
        }

        return entities.GetEntityCreateIfNotExist("NoMoreItemToBeReviewed")
            .Set(new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetMargin(20)
            .SetText(text);
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
                .OrderBy(item => item.Priority)     // Order by the next review date (ascending)
                .FirstOrDefault();                  // Take the first item (the nearest due date)
    }

    /// <summary>
    /// Returns the next item to be reviewed that has its due date in the future.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    private static SpacedRepetitionItem? NextItemToBeReviewedInFuture(ObservableCollection<SpacedRepetitionItem> items)
    {
        if (items == null || !items.Any())
        {
            return null;
        }

        return items
                .OrderBy(item => item.NextReview)
                .FirstOrDefault();
    }
}
