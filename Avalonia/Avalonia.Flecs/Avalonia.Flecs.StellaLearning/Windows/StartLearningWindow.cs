using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.Util;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using FsrsSharp;

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
    /// <summary>
    /// Cram mode determines if we simply learn all items based on the priority and ignore review dates
    /// </summary>
    private bool _cramMode;
    private UIBuilder<Control>? _contentContainer;
    private UIBuilder<Control>? _currentContent;
    private readonly ObservableCollection<SpacedRepetitionItem> _spacedRepetitionItems;
    /// <summary>
    /// Temporary list holding items for the current cram session pass.
    /// It's a shallow copy of references, shuffled. Items are removed as they are shown.
    /// </summary>
    private List<SpacedRepetitionItem>? _cramSessionItems;
    /// <summary>
    /// Random number generator for shuffling the cram session list.
    /// </summary>
    private readonly Random _random = new();
    /// <summary>
    /// Represents the item that is or should be currently be 
    /// displayed.
    /// </summary>
    private SpacedRepetitionItem? _itemToBeLearnedField;
    private SpacedRepetitionItem? ItemToBeLearned
    {
        get => _itemToBeLearnedField;
        set
        {
            _itemToBeLearnedField = value;

            //When the UI is not fully constructed dont update the content display
            if (_root == 0 || _contentContainer is null || _currentContent is null)
            { return; }

            UpdateContentDisplay();
        }
    }

    /// <summary>
    /// Create the Add File Window
    /// </summary>
    /// <param name="world"></param>
    /// <param name="cramMode">When set to true we learn items regardless of time and do not update their spaced repetition values</param>
    /// <returns></returns>
    public StartLearningWindow(World world, bool cramMode = false)
    {
        _cramMode = cramMode;
        _world = world;
        _spacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();
        if (_cramMode)
        {
            InitializeCramSession(); // Set up the temporary list
            ItemToBeLearned = GetNextCramItem(); // Get the first item for cramming
        }
        else
        {
            // Original logic: Get the next item due for review based on SR schedule
            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
        }
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
                if (ItemToBeLearned is not null && !cramMode)
                    await StatsTracker.Instance.StartStudySession();
            });

            window.OnClosing(async (_, _) =>
            {
                Dispose();
                await StatsTracker.Instance.EndStudySession();
            });
            window.Show();
        }).Entity;
    }
    private UIBuilder<Control> CreateWindowContents()
    {
        _contentContainer = _world.UI<ContentControl>(container =>
        {
            container.SetVerticalAlignment(VerticalAlignment.Stretch)
                     .SetHorizontalAlignment(HorizontalAlignment.Stretch);
        }).AsBaseBuilder<Control, ContentControl>();

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

    private UIBuilder<StackPanel> LearnFileContent()
    {
        var file = (SpacedRepetitionFile)ItemToBeLearned!;

        return _world.UI<StackPanel>((layout) =>
        {

            UIBuilder<Button>? easyButton = null;
            UIBuilder<Button>? goodButton = null;
            UIBuilder<Button>? hardButton = null;
            UIBuilder<Button>? againButton = null;


            layout
                .SetOrientation(Orientation.Vertical)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetSpacing(10)
                .SetMargin(20);

            layout.Child<TextBlock>((textBlock) =>
            {
                textBlock
                .SetText(file.Name)
                .SetTextWrapping(TextWrapping.Wrap)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center);
            });

            layout.Child<Separator>((separatorUI) =>
            {
                separatorUI
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
            });

            layout.Child<TextBlock>((question) =>
            {
                question
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetMargin(0, 20)
                    .SetText(file.Question);

                question.Get<TextBlock>().TextWrapping = TextWrapping.Wrap;
            });

            layout.Child<TextBlock>((content) =>
            {
                content
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetMargin(0, 10)
                    .SetText(file.FilePath);

                content.Get<TextBlock>().TextWrapping = TextWrapping.Wrap;
            });

            layout.Child<Button>((button) =>
            {
                button
                    .SetText("Open File")
                    .SetMargin(0, 10)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
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
                        .SetText("Easy")
                        .SetMargin(10, 0)
                        .SetColumn(0)
                        .OnClick(async (_, _) =>
                        {
                            // When we are in cram mode we simply want to get the next item.
                            if (_cramMode)
                            {
                                ItemToBeLearned = GetNextCramItem();
                            }

                            await StatsTracker.Instance.RecordReview(file, Rating.Easy);
                            file.EasyReview();
                            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                        });

                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    goodButton = button;
                    button
                        .SetText("Good")
                        .SetMargin(10, 0)
                        .SetColumn(1)
                        .OnClick(async (_, _) =>
                        {

                            if (_cramMode)
                            {
                                ItemToBeLearned = GetNextCramItem();
                                return;
                            }

                            await StatsTracker.Instance.RecordReview(file, Rating.Good);
                            file.GoodReview();
                            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                        });

                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    hardButton = button;
                    button
                        .SetText("Hard")
                        .SetMargin(10, 0)
                        .SetColumn(2)
                        .OnClick(async (_, _) =>
                        {

                            if (_cramMode)
                            {
                                ItemToBeLearned = GetNextCramItem();
                                return;
                            }

                            await StatsTracker.Instance.RecordReview(file, Rating.Hard);
                            file.HardReview();
                            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                        });

                    button.Disable();
                });

                grid.Child<Button>((button) =>
                {
                    againButton = button;
                    button
                        .SetText("Again")
                        .SetMargin(10, 0)
                        .SetColumn(3)
                        .OnClick(async (_, _) =>
                        {

                            if (_cramMode)
                            {
                                ItemToBeLearned = GetNextCramItem();
                                return;
                            }

                            await StatsTracker.Instance.RecordReview(file, Rating.Again);
                            file.AgainReview();
                            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();

                        });

                    button.Disable();
                });
            });
        });

    }

    private UIBuilder<StackPanel> LearnQuizContent()
    {
        var quiz = (SpacedRepetitionQuiz)ItemToBeLearned!;
        var buttons = new List<UIBuilder<Button>>();
        return _world.UI<StackPanel>((layout) =>
        {
            layout
                .SetOrientation(Orientation.Vertical)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetSpacing(10)
                .SetMargin(20);

            layout.Child<TextBlock>((question) =>
            {
                question
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetMargin(0, 20)
                    .SetText(quiz.Name)
                    .SetTextWrapping(TextWrapping.Wrap);
            });

            layout.Child<TextBlock>((content) =>
            {
                content
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetMargin(0, 10)
                    .SetText(quiz.Question)
                    .SetTextWrapping(TextWrapping.Wrap);
            });

            layout.Child<WrapPanel>((wrapPanel) =>
            {
                wrapPanel
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center);



                for (int i = 0; i < quiz.Answers.Count; i++)
                {
                    int index = i; // Capture the index for the lambda
                    var button = wrapPanel.Child<Button>((button) =>
                    {
                        button.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetText(quiz.Answers[index])
                            .SetTextWrapping(TextWrapping.Wrap);
                        });

                        button
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetHorizontalAlignment(HorizontalAlignment.Center)
                        .SetMargin(10, 10)
                        .OnClick(async (sender, args) =>
                        {
                            if (quiz.CorrectAnswerIndex == index)
                            {

                                buttons.RemoveAt(index);
                                foreach (var button in buttons)
                                {
                                    button.SetBackground(Brushes.Red);
                                }

                                button.SetButtonPointerOverBackground(Brushes.Green);

                                await StatsTracker.Instance.RecordReview(quiz, Rating.Good);
                                await Task.Delay(3000);

                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                quiz.GoodReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            }
                            else
                            {
                                button.SetButtonPointerOverBackground(Brushes.Red);

                                foreach (var button in buttons)
                                {
                                    button.SetBackground(Brushes.Red);
                                }

                                buttons[quiz.CorrectAnswerIndex].SetBackground(Brushes.Green);

                                await StatsTracker.Instance.RecordReview(quiz, Rating.Again);
                                await Task.Delay(3000);

                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                quiz.AgainReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            }

                        });

                    });
                    buttons.Add(button);
                }


            });
        });
    }

    private UIBuilder<StackPanel> LearnFlashcardContent()
    {
        var flashcard = (SpacedRepetitionFlashcard)ItemToBeLearned!;

        return _world.UI<StackPanel>((stackPanel) =>
                {
                    UIBuilder<TextBlock>? flashcardBackText = null;
                    UIBuilder<Button>? easyButton = null;
                    UIBuilder<Button>? goodButton = null;
                    UIBuilder<Button>? hardButton = null;
                    UIBuilder<Button>? againButton = null;

                    stackPanel
                    .SetOrientation(Orientation.Vertical)
                    .SetVerticalAlignment(VerticalAlignment.Center)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetSpacing(10)
                    .SetMargin(20);

                    stackPanel.Child<TextBlock>((textBlock) =>
                    {
                        textBlock
                        .SetText(flashcard.Name)
                        .SetTextWrapping(TextWrapping.Wrap)
                        .SetHorizontalAlignment(HorizontalAlignment.Center)
                        .SetVerticalAlignment(VerticalAlignment.Center);
                    });

                    stackPanel.Child<Separator>((separatorUI) =>
                    {
                        separatorUI
                        .SetBorderThickness(new Thickness(100, 5, 100, 0))
                        .SetBorderBrush(Brushes.Black);
                    });

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
                        flashcardBackText = textBlock
                        .Visible(false)
                        .SetText(flashcard.Back)
                        .SetTextWrapping(TextWrapping.Wrap);
                    });

                    stackPanel.Child<Button>((button) =>
                    {
                        button
                        .SetText("Reveal")
                        .SetMargin(0, 20)
                        .SetVerticalAlignment(VerticalAlignment.Center)
                        .SetHorizontalAlignment(HorizontalAlignment.Center)
                        .OnClick((_, _) =>
                                        {
                                            flashcardBackText!.Visible();
                                            againButton!.Enable();
                                            hardButton!.Enable();
                                            goodButton!.Enable();
                                            easyButton!.Enable();
                                        });
                    });



                    stackPanel.Child<Grid>((grid) =>
                    {
                        grid
                        .SetColumnDefinitions("*, *, *, *")
                        .SetRowDefinitions("auto");

                        grid.Child<Button>((button) =>
                        {
                            easyButton = button
                            .SetText("Easy")
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(0)
                            .OnClick(async (_, _) =>
                            {
                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                await StatsTracker.Instance.RecordReview(flashcard, Rating.Easy);
                                flashcard.EasyReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });

                            button.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText("Easy");
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            goodButton = button
                            .SetText("Good")
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(1)
                            .OnClick(async (_, _) =>
                            {
                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                await StatsTracker.Instance.RecordReview(flashcard, Rating.Good);
                                flashcard.GoodReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            hardButton = button
                            .SetText("Hard")
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(2)
                            .OnClick(async (_, _) =>
                            {
                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                await StatsTracker.Instance.RecordReview(flashcard, Rating.Hard);
                                flashcard.HardReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });
                        });

                        grid.Child<Button>((button) =>
                        {
                            againButton = button
                            .SetText("Again")
                            .Disable()
                            .SetMargin(10, 0)
                            .SetColumn(3)
                            .OnClick(async (_, _) =>
                            {
                                if (_cramMode)
                                {
                                    ItemToBeLearned = GetNextCramItem();
                                    return;
                                }

                                await StatsTracker.Instance.RecordReview(flashcard, Rating.Again);
                                flashcard.AgainReview();
                                ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                            });
                        });
                    });
                });
    }

    private UIBuilder<StackPanel> LearnClozeContent()
    {
        var cloze = (SpacedRepetitionCloze)ItemToBeLearned!;
        return _world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<TextBlock> clozeText;
            UIBuilder<Button>? easyButton = null;
            UIBuilder<Button>? goodButton = null;
            UIBuilder<Button>? hardButton = null;
            UIBuilder<Button>? againButton = null;

            stackPanel
            .SetOrientation(Orientation.Vertical)
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetHorizontalAlignment(HorizontalAlignment.Center)
            .SetSpacing(10)
            .SetMargin(20);

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock
                .SetText(cloze.Name)
                .SetTextWrapping(TextWrapping.Wrap)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center);
            });

            stackPanel.Child<Separator>((separatorUI) =>
            {
                separatorUI
                    .SetBorderThickness(new Thickness(100, 5, 100, 0))
                    .SetBorderBrush(Brushes.Black);
            });

            clozeText = stackPanel.Child<TextBlock>((textBlock) =>
            {
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
                .SetText("Show")
                .SetMargin(15)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .OnClick((_, _) =>
                                {
                                    // Disable the show button after it revealed its clozes
                                    button.Disable();
                                    clozeText.SetText(cloze.FullText);
                                    againButton!.Enable();
                                    hardButton!.Enable();
                                    goodButton!.Enable();
                                    easyButton!.Enable();

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
                    easyButton = button
                    .SetText("Easy")
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(0)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(cloze, Rating.Easy);
                        cloze.EasyReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });

                grid.Child<Button>((button) =>
                {
                    goodButton = button
                    .SetText("Good")
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(1)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(cloze, Rating.Good);
                        cloze.GoodReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });

                grid.Child<Button>((button) =>
                {
                    hardButton = button
                    .SetText("Hard")
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(2)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(cloze, Rating.Hard);
                        cloze.HardReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });

                });

                grid.Child<Button>((button) =>
                {
                    againButton = button
                    .SetText("Again")
                    .Disable()
                    .SetMargin(10, 0)
                    .SetColumn(3)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(cloze, Rating.Again);
                        cloze.AgainReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });
            });
        });
    }

    private UIBuilder<StackPanel> LearnImageClozeContent()
    {
        var imageCloze = (SpacedRepetitionImageCloze)ItemToBeLearned!;

        return _world.UI<StackPanel>((stackPanel) =>
        {
            UIBuilder<Button>? easyButton = null;
            UIBuilder<Button>? goodButton = null;
            UIBuilder<Button>? hardButton = null;
            UIBuilder<Button>? againButton = null;

            stackPanel
                .SetOrientation(Orientation.Vertical)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
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
                                try
                                {
                                    var bitmap = new Bitmap(File.OpenRead(imageCloze.ImagePath));
                                    image.SetSource(bitmap);

                                    // Set the canvas size to match the image's natural size
                                    canvas.SetWidth(bitmap.Size.Width);
                                    canvas.SetHeight(bitmap.Size.Height);
                                }
                                catch (FileNotFoundException ex)
                                {
                                    var cd = new ContentDialog()
                                    {
                                        Title = "Picture not found",
                                        Content = $"The picture couldn't not be found at path: {ex.FileName}",
                                        PrimaryButtonText = "Ok",
                                        DefaultButton = ContentDialogButton.Primary,
                                        IsSecondaryButtonEnabled = true,
                                    };
                                    cd.ShowAsync();
                                }
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
                    easyButton = button
                    .SetText("Easy")
                    .SetMargin(10, 0)
                    .SetColumn(0)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(imageCloze, Rating.Easy);
                        imageCloze.EasyReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });

                grid.Child<Button>((button) =>
                {
                    goodButton = button
                    .SetText("Good")
                    .SetMargin(10, 0)
                    .SetColumn(1)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(imageCloze, Rating.Good);
                        imageCloze.GoodReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });

                grid.Child<Button>((button) =>
                {
                    hardButton = button
                    .SetText("Hard")
                    .SetMargin(10, 0)
                    .SetColumn(2)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(imageCloze, Rating.Hard);
                        imageCloze.HardReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
                    });
                });

                grid.Child<Button>((button) =>
                {
                    againButton = button
                    .SetText("Again")
                    .SetMargin(10, 0)
                    .SetColumn(3)
                    .OnClick(async (_, _) =>
                    {
                        if (_cramMode)
                        {
                            ItemToBeLearned = GetNextCramItem();
                            return;
                        }

                        await StatsTracker.Instance.RecordReview(imageCloze, Rating.Again);
                        imageCloze.AgainReview();
                        ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
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
            _currentContent.Entity.Destruct();
        }
        // Generate the new content
        _currentContent = DisplayRightItem();

        // Set it as the content of our container
        _contentContainer!.Get<ContentControl>().Content = _currentContent.Get<object>();
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
            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
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
        if (!_cramMode)
            ItemToBeLearned = _spacedRepetitionItems.GetNextItemToBeReviewed();
        else
            ItemToBeLearned = GetNextCramItem();
    }

    /// <summary>
    /// Initializes the cram session list by sorting items primarily by Priority (descending),
    /// applying relative random noise to prevent strictly deterministic ordering, suitable for large priority values.
    /// </summary>
    private void InitializeCramSession()
    {
        // 1. Create a new list containing references to the items in the main collection.
        _cramSessionItems = _spacedRepetitionItems
                                .Where(item => item != null) // Filter out nulls
                                .ToList();

        // 2. *** Sort by Priority Scaled by (1 + Random Noise Factor) ***
        // This approach ensures noise is proportional to the priority magnitude.

        // Adjust this factor: It represents the maximum *percentage* of noise added.
        // e.g., 0.01 means up to 1% noise, 0.05 means up to 5% noise.
        // Smaller values result in an order closer to strict priority.
        // Choose based on how much deviation from strict priority you want.
        const double relativeNoiseFactor = 0.05; // <-- TUNABLE PARAMETER (e.g., 5% relative noise)

        // Sort using the scaled priority.
        _cramSessionItems = _cramSessionItems
            .OrderByDescending(item =>
            {
                // Convert Priority to double for calculation.
                // Use Math.Max(1.0, ...) to handle potential non-positive priorities gracefully.
                // If priority can be zero or negative, multiplying by (1 + noise) could lead to
                // zero or negative scores, affecting sorting. Treating minimum base as 1 avoids this.
                // If your priorities are guaranteed positive, you can simplify this.
                double basePriority = Math.Max(1.0, item.Priority);

                // Calculate the random multiplier between 1.0 and (1.0 + relativeNoiseFactor)
                double noiseMultiplier = 1.0 + (_random.NextDouble() * relativeNoiseFactor);

                // Calculate the final score for sorting
                double noisyPriorityScore = basePriority * noiseMultiplier;

                return noisyPriorityScore;
            })
            .ToList(); // Create the final sorted list

        //Console.WriteLine($"Cram session list initialized/reset with {_cramSessionItems.Count} items (Sorted by Priority * (1 + Noise%)).");

        // Optional: Log the first few items to verify the semi-randomized priority order
        /*
        Console.WriteLine("Top 5 items in cram session queue:");
        foreach(var item in _cramSessionItems.Take(5))
        {
            // Recalculate an approximate score for logging if needed (won't be exact due to randomness)
            double basePrio = Math.Max(1.0, (double)item.Priority);
            Console.WriteLine($"- Name: {item.Name}, Prio: {item.Priority}, Approx. Score Range: {basePrio * 1.0:F0} - {basePrio * (1.0 + relativeNoiseFactor):F0}");
        }
        */
    }

    private SpacedRepetitionItem? GetNextCramItem()
    {
        // Ensure the session list exists (should be initialized in constructor or reset)
        if (_cramSessionItems == null)
        {
            //Console.WriteLine("Error: Cram session item list is null. Re-initializing."); // Debugging
            InitializeCramSession(); // Attempt recovery
            if (_cramSessionItems == null) return null; // If still null, something is wrong
        }

        // If the temporary list is empty, it means we've gone through all items in this pass.
        // Reset and reshuffle the list to allow continuous cramming.
        if (_cramSessionItems.Count == 0)
        {
            // Check if the original list is also empty
            if (!_spacedRepetitionItems.Any())
            {
                //Console.WriteLine("No items available in the main list to cram.");
                return null; // Truly no items left
            }

            //Console.WriteLine("Cram session pass complete. Re-initializing and shuffling for another pass."); // Debugging
            InitializeCramSession();

            // If after re-initializing it's still empty, means the source was empty
            if (_cramSessionItems.Count == 0)
            {
                //Console.WriteLine("Source list is empty, cannot get next cram item.");
                return null;
            }
        }

        // Get the next item (the first one in the shuffled list)
        var nextItem = _cramSessionItems[0];
        // IMPORTANT: Remove the item from the temporary list so it's not immediately repeated in this pass.
        _cramSessionItems.RemoveAt(0);

        //Console.WriteLine($"Next cram item: {nextItem?.Name ?? "N/A"}. Items remaining in this pass: {_cramSessionItems.Count}"); // Debugging
        return nextItem;
    }

    private UIBuilder<Control> DisplayRightItem()
    {
        try
        {
            return ItemToBeLearned switch
            {

                /*
                Note regarding the AsBaseBuilder method. The problem we face is the following, we want that our
                display right item simply returns a avalonia control class that gets used as a children. But
                our various content displayers, return UIBuilder<MoreSpecificType> like a stack panel. The caller
                shouldnt care for what more specific type gets returned only the base control type matters.

                Calling AsBaseBuilder uses the same underlying entity and fields with the only difference being 
                that the UIBuilder gets converted from the type UIBuilder<StackPanel> => UIBuilder<Control>.

                For better clarity you can imagine this as a simple type cast.
                */

                SpacedRepetitionQuiz quizItem => LearnQuizContent().AsBaseBuilder<Control, StackPanel>(),
                SpacedRepetitionFlashcard => LearnFlashcardContent().AsBaseBuilder<Control, StackPanel>(),
                SpacedRepetitionFile => LearnFileContent().AsBaseBuilder<Control, StackPanel>(),
                SpacedRepetitionCloze => LearnClozeContent().AsBaseBuilder<Control, StackPanel>(),
                SpacedRepetitionImageCloze => LearnImageClozeContent().AsBaseBuilder<Control, StackPanel>(),
                _ => NoMoreItemToBeReviewedContent().AsBaseBuilder<Control, TextBlock>(),
            };
        }
        catch (NotImplementedException)
        {
            //Console.WriteLine(e.Message);
            return _world.UI<TextBox>((t) => t.SetText("Somethin Went Wrong!")).AsBaseBuilder<Control, TextBox>();
        }
    }

    private UIBuilder<TextBlock> NoMoreItemToBeReviewedContent()
    {
        string text;

        if (!_spacedRepetitionItems.Any()) // Check if the main list is empty
        {
            text = "There are no items added yet. Add some items to start learning!";
        }
        else if (_cramMode)
        {
            // In cram mode, if GetNextCramItem returned null, it means the main list was empty after trying to re-initialize.
            // (Because GetNextCramItem loops otherwise)
            text = "Cram session complete or no items to cram."; // Should ideally only show if main list is empty.
        }
        else
        {
            // In normal mode, check for items due in the future
            var futureItem = _spacedRepetitionItems.NextItemToBeReviewedInFuture();
            if (futureItem is null)
            {
                // This case means there are items, but none have a future review date set (e.g., all new)
                // Or, more likely, the GetNextItemToBeReviewed already handles new items.
                // Let's assume it means genuinely nothing due now or later according to the SR logic.
                text = "Congratulations! No items due for review right now.\n\nCheck back later or add new items.";
            }
            else
            {
                // Found an item due later
                text = $"No items due right now.\nNext review: '{futureItem.Name}'\nDue: {futureItem.NextReview.ToLocalTime():dd/MM/yyyy HH:mm}";
            }
        }


        return _world.UI<TextBlock>((textBlock) =>
        {
            textBlock
                .SetTextWrapping(TextWrapping.Wrap)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                .SetTextAlignment(TextAlignment.Center) // Center align text
                .SetMargin(20)
                .SetText(text);
        });
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
