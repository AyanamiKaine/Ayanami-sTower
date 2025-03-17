using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.Util;
using Avalonia.Media;
using Avalonia.Threading;
using DesktopNotifications;
using Flecs.NET.Core;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to learn spaced repetition items
/// </summary>
public class StartLearningWindow : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private World _world;
    private Entity _contentContainer;
    private Entity _currentContent;
    private ObservableCollection<SpacedRepetitionItem> _spacedRepetitionItems;
    /// <summary>
    /// Represents the item that is or should be currently be 
    /// displayed.
    /// </summary>
    private SpacedRepetitionItem? _itemToBeLearnedField;
    private SpacedRepetitionItem? _ItemToBeLearned
    {
        get => _itemToBeLearnedField;
        set
        {
            _itemToBeLearnedField = value;

            //When the UI is not fully constructed dont update the content display
            if (_root == 0 || _contentContainer == 0 || _currentContent == 0)
            { return; }

            UpdateContentDisplay();
        }
    }

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public StartLearningWindow(World world)
    {
        _world = world;
        _spacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();
        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
        _root = world.UI<Window>((window) =>
         {
             window
                 .SetTitle("Start Learning")
                 .SetWidth(400)
                 .SetHeight(400)
                 .Child<ScrollViewer>((scrollViewer) =>
                 {
                     scrollViewer
                         .SetRow(1)
                         .SetColumnSpan(3)
                         .Child(CreateWindowContents());
                 });
             window.Show();
         });

    }
    private Entity CreateWindowContents()
    {
        _contentContainer = _world.UI<ContentControl>(container =>
        {
            container.SetVerticalAlignment(Layout.VerticalAlignment.Stretch)
                     .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch);
        });

        foreach (var item in _spacedRepetitionItems)
        {
            item.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                {
                    // Update the container content
                    UpdateContentDisplay();
                }
            };
        }

        /*
        Here we describe the logic, what should happen when an spaced repetition item changes?
        We want to recalculate what item should be displayed. 

        We also use this when the underlying item changes, but the same item would be shown,
        for example because you changed the name of it.
        */

        _spacedRepetitionItems.CollectionChanged += (sender, e) =>
            {

                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SpacedRepetitionItem newItem in e.NewItems!)
                    {
                        newItem.PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                            {
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            }
                        };
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (SpacedRepetitionItem oldItem in e.OldItems!)
                    {
                        oldItem.PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview))
                            {
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            }
                        };
                    }
                }
                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
            };
        UpdateContentDisplay();
        return _contentContainer;
    }

    private Entity LearnFileContent()
    {
        var file = (SpacedRepetitionFile)_ItemToBeLearned!;

        return _world.UI<StackPanel>((layout) =>
        {
            layout
                .SetOrientation(Layout.Orientation.Vertical)
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                .SetSpacing(10)
                .SetMargin(20);

            layout.Child<TextBlock>((question) =>
            {
                question
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetMargin(0, 20)
                    .SetText(file.Question);

                question.Get<TextBlock>().TextWrapping = Media.TextWrapping.Wrap;
            });

            layout.Child<TextBlock>((content) =>
            {
                content
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetMargin(0, 10)
                    .SetText(file.FilePath);

                content.Get<TextBlock>().TextWrapping = Media.TextWrapping.Wrap;
            });

            layout.Child<Button>((button) =>
            {
                button
                    .SetMargin(0, 10)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .OnClick((sender, e) =>
                    {
                        try
                        {
                            if (_world.Has<Settings>() && file.FilePath.EndsWith(".md"))
                            {
                                string ObsidianPath = _world.Get<Settings>().ObsidianPath;
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

                button.Child<TextBlock>((textBloc) =>
                {
                    textBloc.SetText("Browse File");
                });
            });

            layout.Child<Grid>((grid) =>
            {
                grid
                    .SetColumnDefinitions("*, *, *, *")
                    .SetRowDefinitions("auto");

                grid.Child<Button>((button) =>
                {
                    button
                        .SetMargin(10, 0)
                        .SetColumn(0)
                        .OnClick((_, _) =>
                        {
                            file.EasyReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Easy"); });
                });

                grid.Child<Button>((button) =>
                {
                    button
                        .SetMargin(10, 0)
                        .SetColumn(1)
                        .OnClick((_, _) =>
                        {
                            file.GoodReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Good"); }); ;
                });

                grid.Child<Button>((button) =>
                {
                    button
                        .SetMargin(10, 0)
                        .SetColumn(2)
                        .OnClick((_, _) =>
                        {
                            file.HardReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Hard"); }); ;
                });

                grid.Child<Button>((button) =>
                {
                    button
                        .SetMargin(10, 0)
                        .SetColumn(3)
                        .OnClick((_, _) =>
                        {
                            file.AgainReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Again"); });
                });
            });
        });

    }

    private Entity LearnQuizContent()
    {
        var quiz = (SpacedRepetitionQuiz)_ItemToBeLearned!;

        return _world.UI<StackPanel>((layout) =>
        {
            layout
                .SetOrientation(Layout.Orientation.Vertical)
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                .SetSpacing(10)
                .SetMargin(20);

            layout.Child<TextBlock>((question) =>
            {
                question
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetMargin(0, 20)
                    .SetText(quiz.Name);

                question.Get<TextBlock>().TextWrapping = Media.TextWrapping.Wrap;
            });

            layout.Child<TextBlock>((content) =>
            {
                content
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetMargin(0, 10)
                    .SetText(quiz.Question);

                content.Get<TextBlock>().TextWrapping = Media.TextWrapping.Wrap;
            });

            layout.Child<WrapPanel>((wrapPanel) =>
            {
                wrapPanel
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center);



                for (int i = 0; i < quiz.Answers.Count; i++)
                {
                    int index = i; // Capture the index for the lambda
                    wrapPanel.Child<Button>((button) =>
                    {
                        button.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetText(quiz.Answers[index])
                            .SetTextWrapping(TextWrapping.Wrap);
                        });

                        button
                        .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                        .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                        .SetMargin(10, 10)
                        .OnClick(async (sender, args) =>
                        {
                            if (sender is Button button)
                            {
                                if (quiz.CorrectAnswerIndex == index)
                                {
                                    button.Background = Brushes.LightGreen;
                                    await Task.Delay(1000);
                                    quiz.GoodReview();
                                    _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                                }
                                else
                                {
                                    button.Background = Brushes.Red;
                                    await Task.Delay(1000);
                                    quiz.AgainReview();
                                    _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                                }
                            }
                        });

                    });
                }


            });
        });
    }

    private Entity LearnFlashcardContent()
    {
        var flashcard = (SpacedRepetitionFlashcard)_ItemToBeLearned!;

        return _world.UI<StackPanel>((stackPanel) =>
                {
                    UIBuilder<TextBlock>? flashcardBackText = null;
                    UIBuilder<Button>? easyButton = null;
                    UIBuilder<Button>? goodButton = null;
                    UIBuilder<Button>? hardButton = null;
                    UIBuilder<Button>? againButton = null;

                    stackPanel
                    .SetOrientation(Layout.Orientation.Vertical)
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetSpacing(10)
                    .SetMargin(20);

                    stackPanel.Child<TextBlock>((textBlock) =>
                    {
                        textBlock
                        .SetText(flashcard.Front)
                        .SetTextWrapping(TextWrapping.Wrap);

                    });

                    stackPanel.Child<Separator>((separatorUI) =>
                    {
                        separatorUI
                            .SetBorderThickness(new Thickness(100, 5, 100, 0))
                            .SetBorderBrush(Brushes.Black);
                    });

                    stackPanel.Child<TextBlock>((textBlock) =>
                    {
                        flashcardBackText = textBlock;

                        textBlock
                        .Visible(false)
                        .SetText(flashcard.Back)
                        .SetTextWrapping(TextWrapping.Wrap);
                    });

                    stackPanel.Child<Button>((button) =>
                    {
                        button
                        .SetMargin(0, 20)
                        .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                        .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                        .OnClick((_, _) =>
                                        {
                                            flashcardBackText!.Visible();
                                            againButton!.Enable();
                                            hardButton!.Enable();
                                            goodButton!.Enable();
                                            easyButton!.Enable();
                                        });
                        button.Child<TextBlock>((textBlock) =>
                        {
                            textBlock.SetText("Reveal");
                        });
                    });



                    stackPanel.Child<Grid>((grid) =>
                    {
                        grid
                        .SetColumnDefinitions("*, *, *, *")
                        .SetRowDefinitions("auto");

                        grid.Child<Button>((button) =>
                        {
                            easyButton = button;
                            button
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(0)
                            .OnClick((_, _) =>
                            {
                                flashcard.EasyReview();
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });

                            button.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText("Easy");
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            goodButton = button;
                            button
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(1)
                            .OnClick((_, _) =>
                            {
                                flashcard.GoodReview();
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });

                            button.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText("Good");
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            hardButton = button;
                            button
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(2)
                            .OnClick((_, _) =>
                            {
                                flashcard.HardReview();
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });

                            button.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText("Hard");
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            againButton = button;
                            button
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(3)
                            .OnClick((_, _) =>
                            {
                                flashcard.AgainReview();
                                _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });

                            button.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText("Again");
                            });
                        });
                    });
                });
    }

    private Entity LearnClozeContent()
    {

        var cloze = (SpacedRepetitionCloze)_ItemToBeLearned!;
        return _world.UI<StackPanel>((stackPanel) =>
        {

            UIBuilder<TextBlock>? clozeText = null;
            UIBuilder<Button>? easyButton = null;
            UIBuilder<Button>? goodButton = null;
            UIBuilder<Button>? hardButton = null;
            UIBuilder<Button>? againButton = null;

            stackPanel
            .SetOrientation(Layout.Orientation.Vertical)
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                clozeText = textBlock;
                StringBuilder sb = new(cloze.FullText);
                foreach (string word in cloze.ClozeWords)
                {
                    sb.Replace(word, "[...]");
                }

                string clozeRemovedText = sb.ToString();

                textBlock.SetText(clozeRemovedText);
            });

            stackPanel.Child<Button>((button) =>
            {
                button
                .SetMargin(15)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .OnClick((_, _) =>
                                {
                                    clozeText!.SetText(cloze.FullText);
                                    againButton!.Enable();
                                    hardButton!.Enable();
                                    goodButton!.Enable();
                                    easyButton!.Enable();

                                });
                button.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Show");
                });
            });

            stackPanel.Child<Grid>((grid) =>
            {
                grid
                .SetColumnDefinitions("*, *, *, *")
                .SetRowDefinitions("auto");

                grid.Child<Button>((button) =>
                {
                    easyButton = button;
                    button
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(0)
                    .OnClick((_, _) =>
                    {
                        cloze.EasyReview();
                        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });

                    button.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText("Easy");
                    });
                });

                grid.Child<Button>((button) =>
                {
                    goodButton = button;
                    button
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(1)
                    .OnClick((_, _) =>
                    {
                        cloze.GoodReview();
                        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });

                    button.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText("Good");
                    });
                });

                grid.Child<Button>((button) =>
                {
                    hardButton = button;
                    button
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(2)
                    .OnClick((_, _) =>
                    {
                        cloze.HardReview();
                        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });

                    button.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText("Hard");
                    });
                });

                grid.Child<Button>((button) =>
                {
                    againButton = button;
                    button
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(3)
                    .OnClick((_, _) =>
                    {
                        cloze.AgainReview();
                        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });

                    button.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText("Again");
                    });
                });
            });
        });
    }

    private void UpdateContentDisplay()
    {
        // If we already have content, destroy it
        if (_currentContent != default)
        {
            _currentContent.Destruct();
        }
        // Generate the new content
        _currentContent = DisplayRightItem();

        if (!_contentContainer.Has<ContentControl>())
        {
            Console.WriteLine("ContentContainer is missing its content control component!");
            return;
        }

        if (!_currentContent.Has<object>())
        {
            Console.WriteLine("_currentContent is missing its object component!");
            return;
        }

        // Set it as the content of our container
        _contentContainer.Get<ContentControl>().Content = _currentContent.Get<object>();
    }

    private Entity DisplayRightItem()
    {
        try
        {
            return _ItemToBeLearned switch
            {
                SpacedRepetitionQuiz => LearnQuizContent(),
                SpacedRepetitionFlashcard => LearnFlashcardContent(),
                SpacedRepetitionFile => LearnFileContent(),
                SpacedRepetitionCloze => LearnClozeContent(),
                _ => NoMoreItemToBeReviewedContent(),
            };
        }
        catch (NotImplementedException e)
        {
            Console.WriteLine(e.Message);
            return _world.Entity();
        }
    }

    private Entity NoMoreItemToBeReviewedContent()
    {
        var futureItem = _spacedRepetitionItems.NextItemToBeReviewedInFuture();

        string? text;
        if (futureItem is null)
        {
            text = "Currently: No Items";
        }
        else
        {
            text = $"Next Item: '{futureItem?.Name}', due: {futureItem?.NextReview}";
        }

        return _world.Entity("NoMoreItemToBeReviewed")
            .Set(new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap
            })
            .SetVerticalAlignment(Layout.VerticalAlignment.Center)
            .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
            .SetMargin(20)
            .SetText(text);
    }
}
