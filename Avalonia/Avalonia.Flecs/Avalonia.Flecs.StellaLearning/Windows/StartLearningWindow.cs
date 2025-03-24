using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.Util;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.StellaLearning.Windows;

/// <summary>
/// Represents the window to learn spaced repetition items
/// </summary>
public class StartLearningWindow : IUIComponent, IDisposable
{
    private bool isDisposed = false;
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

            window.OnOpened(async (_, _) =>
            {
                await StatsTracker.Instance.StartStudySession();
            });

            window.OnClosing(async (_, _) =>
            {
                Dispose();
                await StatsTracker.Instance.EndStudySession();
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

        // Single event handler per item that handles all property change needs
        foreach (var item in _spacedRepetitionItems)
        {
            AttachItemEventHandler(item);
        }

        // Only attach collection changed event handler once
        _spacedRepetitionItems.CollectionChanged += OnSpacedRepetitionItemsChanged;

        UpdateContentDisplay();
        return _contentContainer;
    }

    private Entity LearnFileContent()
    {
        var file = (SpacedRepetitionFile)_ItemToBeLearned!;

        return _world.UI<StackPanel>((layout) =>
        {

            UIBuilder<Button>? easyButton = null;
            UIBuilder<Button>? goodButton = null;
            UIBuilder<Button>? hardButton = null;
            UIBuilder<Button>? againButton = null;


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
                            easyButton!.Enable();
                            hardButton!.Enable();
                            againButton!.Enable();
                            goodButton!.Enable();

                            /*
                            When the user tries to open an executable file instead of data that gets openend with
                            a program like a .png. We want to warn
                            him that he is currently tries to execute
                            a program.
                            */
                            if (ExecutableDetector.IsExecutable(file.FilePath))
                            {
                                var cd = new ContentDialog()
                                {
                                    Title = "Opening an Executable?",
                                    Content = "You are currently trying to run an executable program. Do you wish to continue?",
                                    PrimaryButtonText = "Confirm",
                                    SecondaryButtonText = "Deny",
                                    IsPrimaryButtonEnabled = true,
                                    IsSecondaryButtonEnabled = true,
                                };
                                cd.PrimaryButtonClick += (_, _) => FileOpener.OpenFileWithDefaultProgram(file.FilePath);

                                cd.ShowAsync();
                            }
                            else if (_world.Has<Settings>() && file.FilePath.EndsWith(".md"))
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
                    textBloc.SetText("Open File");
                });
            });

            layout.Child<Grid>((grid) =>
            {
                grid
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetColumnDefinitions("*, *, *, *")
                    .SetRowDefinitions("auto");

                grid.Child<Button>((button) =>
                {
                    easyButton = button;
                    button
                        .SetMargin(10, 0)
                        .SetColumn(0)
                        .OnClick((_, _) =>
                        {
                            file.EasyReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Easy"); });


                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    goodButton = button;
                    button
                        .SetMargin(10, 0)
                        .SetColumn(1)
                        .OnClick((_, _) =>
                        {
                            file.GoodReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Good"); });


                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    hardButton = button;
                    button
                        .SetMargin(10, 0)
                        .SetColumn(2)
                        .OnClick((_, _) =>
                        {
                            file.HardReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Hard"); });


                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    againButton = button;
                    button
                        .SetMargin(10, 0)
                        .SetColumn(3)
                        .OnClick((_, _) =>
                        {
                            file.AgainReview();
                            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        })
                        .Child<TextBlock>(textBlock => { textBlock.SetText("Again"); });


                    button.Disable();
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
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center)
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

                textBlock
                .SetText(clozeRemovedText)
                .SetTextWrapping(TextWrapping.Wrap);
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
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center)
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

    private Entity LearnImageClozeContent()
    {
        var imageCloze = (SpacedRepetitionImageCloze)_ItemToBeLearned!;

        return _world.UI<StackPanel>((stackPanel) =>
        {
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
                    .SetText(imageCloze.Name)
                    .SetTextWrapping(TextWrapping.Wrap)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetFontWeight(FontWeight.Bold)
                    .SetMargin(0, 0, 0, 10);
            });

            // Container for the image and cloze areas
            stackPanel.Child<Grid>((grid) =>
            {
                grid.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                grid.SetVerticalAlignment(VerticalAlignment.Stretch);
                // Set a reasonable size constraint
                //grid.SetMaxWidth(600);
                //grid.SetMaxHeight(400);


                // Add a Viewbox to contain and scale the image properly
                grid.Child<Viewbox>((viewbox) =>
                {
                    //viewbox.SetStretch(Stretch.Uniform);
                    viewbox.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    viewbox.SetVerticalAlignment(VerticalAlignment.Stretch);

                    // Add a Canvas inside the Viewbox for positioning elements
                    viewbox.Child<Canvas>((canvas) =>
                    {
                        //viewbox.With((w) => { w.Child = canvas.Get<Canvas>(); });
                        // Add the image to the canvas
                        canvas.Child<Image>((image) =>
                        {

                            if (imageCloze.ImagePath.Length != 0)
                            {

                                var bitmap = new Bitmap(File.OpenRead(imageCloze.ImagePath));
                                image.SetSource(bitmap);

                                // Set the canvas size to match the image's natural size
                                canvas.SetWidth(bitmap.Size.Width);
                                canvas.SetHeight(bitmap.Size.Height);
                            }
                        });

                        // Create rectangles for each cloze area
                        foreach (var area in imageCloze.ClozeAreas)
                        {
                            canvas.Child<Rectangle>((rect) =>
                            {
                                rect.SetWidth(area.Width);
                                rect.SetHeight(area.Height);
                                rect.SetFill(new SolidColorBrush(Color.FromArgb(
                                    a: 255,
                                    r: 221,
                                    g: 176,
                                    b: 55)));

                                // Set the position
                                Canvas.SetLeft(rect.Get<Rectangle>(), area.X);
                                Canvas.SetTop(rect.Get<Rectangle>(), area.Y);

                                var menu = _world.UI<MenuFlyout>((menu) =>
                                    {
                                        menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                                        menu.Child<MenuItem>((menuItem) =>
                                        {
                                            menuItem
                                            .SetHeader("Reveal")
                                            .OnClick((_, _) =>
                                            {
                                                rect.SetFill(new SolidColorBrush(Color.FromArgb(
                                                  a: 55,
                                                  r: 221,
                                                  g: 176,
                                                  b: 55)));
                                            });
                                        });
                                    });

                                rect.SetContextFlyout(menu);
                            });
                        }
                    });
                });
            });

            // Rating buttons
            stackPanel.Child<Grid>((grid) =>
            {
                grid
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetColumnDefinitions("*, *, *, *")
                    .SetRowDefinitions("auto");

                grid.Child<Button>((button) =>
                {
                    easyButton = button;
                    button
                        .SetMargin(10, 0)
                        .SetColumn(0)
                        .OnClick((_, _) =>
                        {
                            imageCloze.EasyReview();
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
                        .SetMargin(10, 0)
                        .SetColumn(1)
                        .OnClick((_, _) =>
                        {
                            imageCloze.GoodReview();
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
                        .SetMargin(10, 0)
                        .SetColumn(2)
                        .OnClick((_, _) =>
                        {
                            imageCloze.HardReview();
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
                        .SetMargin(10, 0)
                        .SetColumn(3)
                        .OnClick((_, _) =>
                        {
                            imageCloze.AgainReview();
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

    // Centralized method to attach event handler
    private void AttachItemEventHandler(SpacedRepetitionItem item)
    {
        item.PropertyChanged += OnItemPropertyChanged;
    }

    // Centralized method to detach event handler
    private void DetachItemEventHandler(SpacedRepetitionItem item)
    {
        item.PropertyChanged -= OnItemPropertyChanged;
    }

    // Event handler for item property changes
    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SpacedRepetitionItem.NextReview) || e == null)
        {
            _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
        }
    }

    // Event handler for collection changes
    private void OnSpacedRepetitionItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (SpacedRepetitionItem newItem in e.NewItems!)
            {
                AttachItemEventHandler(newItem);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (SpacedRepetitionItem oldItem in e.OldItems!)
            {
                DetachItemEventHandler(oldItem);
            }
        }
        _ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
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
                SpacedRepetitionImageCloze => LearnImageClozeContent(),
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
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetHorizontalAlignment(HorizontalAlignment.Center)
            .SetMargin(20)
            .SetText(text);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; 
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                // Clean up events

                if (_spacedRepetitionItems is not null)
                {
                    foreach (var item in _spacedRepetitionItems)
                    {
                        DetachItemEventHandler(item);
                    }
                    _spacedRepetitionItems.CollectionChanged -= OnSpacedRepetitionItemsChanged;
                }

                // Clean up other resources
                // Consider calling destruct if needed
                if (_root.IsValid())
                {
                    _root.Get<Window>().Content = null;
                    _root.Destruct();
                }
            }

            isDisposed = true;
        }
    }
}
