using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Layout;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Data;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.StellaLearning.Util;
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.Util;
using Avalonia.Flecs.StellaLearning.Windows;
using System.Text.Json;
using System.Threading;
using Avalonia.Flecs.StellaLearning.Converters;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// This class represents the Spaced Repetition Page
/// </summary>
public static class SpacedRepetitionPage
{
    /// <summary>
    /// Creates the Spaced Repetition Page
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public static Entity Create(NamedEntities entities)
    {
        var spacedRepetitionPage = entities.GetEntityCreateIfNotExist("SpacedRepetitionPage")
        .Add<Page>()
        .Set(new Grid())
        /*
        *: This represents a "star" column. 
        It means this column will take up as much available space as 
        possible after any fixed-size or Auto columns have been accounted for. 
        Think of it as flexible or "greedy". 
        In this case, the first column will grab most of the grid's width.

        Auto: This means the column's width will adjust automatically to 
        fit the content within it. If you place a button in this column, 
        the column will be just wide enough to accommodate the button's size.
        */
        .SetColumnDefinitions("*, Auto, Auto")
        .SetRowDefinitions("Auto, *, Auto");

        spacedRepetitionPage.AddDefaultStyling((spacedRepetitionPage) =>
        {
            if (spacedRepetitionPage.Parent() != 0 &&
                spacedRepetitionPage.Parent().Has<NavigationView>())
            {
                switch (spacedRepetitionPage.Parent().Get<NavigationView>().DisplayMode)
                {
                    case NavigationViewDisplayMode.Minimal:
                        spacedRepetitionPage.SetMargin(50, 10, 20, 20);
                        break;
                    default:
                        spacedRepetitionPage.SetMargin(20, 10, 20, 20);
                        break;
                }
            }
        });

        var listSearchSpacedRepetition = entities.GetEntityCreateIfNotExist("ListSearchSpacedRepetition")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBox())
            .SetColumn(0)
            .SetWatermark("Search Entries");

        var totalItems = entities.GetEntityCreateIfNotExist("TotalSpacedRepetitionItems")
            .ChildOf(spacedRepetitionPage)
            .Set(new TextBlock())
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetMargin(new Thickness(10, 0))
            .SetText("Total Items: 0")
            .SetColumn(1);

        List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];

        var myFlyout = new Flyout()
        {
            Content = new TextBlock() { Text = "Hello World" },
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
        };

        var sortItemsButton = entities.GetEntityCreateIfNotExist("SpacedRepetitionSortItemsButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new ComboBox())
            .SetPlaceholderText("Sort Items")
            .SetColumn(2)
            .SetItemsSource(sortItems)
            .SetContextFlyout(myFlyout);

        //ToolTip.SetTip(sortItemsButton.Get<ComboBox>(), myToolTip);

        /*
        I believe that entites should not know the exact control type but
        all other entities should only care for the base classes like
        Control, Panel, ItemsControl, TemplatedControl, Etc. They should
        always take the lowest common denominator.

        No need to depend on things that we dont care for 
        */

        List<string> itemTypes = ["File", "Quiz", "Cloze"];


        var stackPanel = entities.GetEntityCreateIfNotExist("AddSpacedRepetitionItemStackPanel")
            .Set(new StackPanel())
            .SetOrientation(Orientation.Vertical)
            .SetSpacing(5);

        var addFileButton = entities.GetEntityCreateIfNotExist("AddFileButton")
            .ChildOf(stackPanel)
            .Set(new Button())
            .SetContent("File")
            .SetFontWeight(FontWeight.Normal)
            .OnClick((sender, args) =>
            {
                if (entities.Contains("AddFileWindow"))
                    entities["AddFileWindow"].ShowWindow();
                else
                    AddFile.Create(entities).ShowWindow();
            });

        var addClozeButton = entities.GetEntityCreateIfNotExist("AddClozeButton")
               .ChildOf(stackPanel)
               .Set(new Button())
               .SetContent("Cloze")
               .SetFontWeight(FontWeight.Normal)
               .OnClick((_, _) =>
               {
                   if (entities.Contains("AddClozeWindow"))
                       entities["AddClozeWindow"].ShowWindow();
                   else
                       AddCloze.Create(entities).ShowWindow();
               });

        var addQuizButton = entities.GetEntityCreateIfNotExist("AddQuizButton")
            .ChildOf(stackPanel)
            .Set(new Button())
            .SetContent("Quiz")
            .SetFontWeight(FontWeight.Normal)
            .OnClick((_, _) =>
            {
                if (entities.Contains("AddQuizWindow"))
                    entities["AddQuizWindow"].ShowWindow();
                else
                    AddQuiz.Create(entities).ShowWindow();
            });

        var addFlashcardButton = entities.Create()
            .ChildOf(stackPanel)
            .Set(new Button())
            .SetContent("Flashcard")
            .SetFontWeight(FontWeight.Normal)
            .OnClick((_, _) =>
            {
                if (entities.Contains("AddFlashcardWindow"))
                    entities["AddFlashcardWindow"].ShowWindow();
                else
                    AddFlashcard.Create(entities).ShowWindow();
            });


        var addAudioButton = entities.GetEntityCreateIfNotExist("AddAudioButton")
           .ChildOf(stackPanel)
           .Set(new Button())
           .SetContent("Audio")
           .SetFontWeight(FontWeight.Normal)
            .OnClick((_, _) =>
            {
                if (entities.Contains("AddAudioWindow"))
                    entities["AddAudioWindow"].ShowWindow();
                else
                    AddAudio.Create(entities).ShowWindow();
            });


        var addVideoButton = entities.GetEntityCreateIfNotExist("AddVideoButton")
           .ChildOf(stackPanel)
           .Set(new Button())
           .SetContent("Video")
           .SetFontWeight(FontWeight.Normal)
           .OnClick((_, _) =>
           {
               if (entities.Contains("AddVideoWindow"))
                   entities["AddVideoWindow"].ShowWindow();
               else
                   AddVideo.Create(entities).ShowWindow();
           });
        var addItemsFlyout = new Flyout()
        {
            Content = stackPanel.Get<object>(),
            ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
        };

        var startLearningButton = entities.Create()
            .ChildOf(spacedRepetitionPage)
            .Set(new Button())
            .SetContent("Start Learning")
            .SetMargin(0, 20, 0, 0)
            .SetColumn(0)
            .SetColumnSpan(2)
            .SetRow(2)
            .OnClick((_, _) =>
            {
                if (entities.Contains("StartLearningWindow"))
                    entities["StartLearningWindow"].ShowWindow();
                else
                    StartLearningWindow.Create(entities).ShowWindow();
            });


        var addItemButton = entities.GetEntityCreateIfNotExist("AddSpacedRepetitionItemButton")
            .ChildOf(spacedRepetitionPage)
            .Set(new Button() { Flyout = addItemsFlyout })
            .SetMargin(0, 20, 0, 0)
            .SetHorizontalAlignment(HorizontalAlignment.Right)
            .SetContent(new TextBlock() { Text = "+", FontWeight = FontWeight.Bold, FontSize = 16 })
            .SetColumn(2)
            .SetRow(2);

        var scrollViewer = entities.GetEntityCreateIfNotExist("SpacedRepetitionScrollViewer")
            .ChildOf(spacedRepetitionPage)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(3);

        ObservableCollection<SpacedRepetitionItem> dummyItems = LoadSpaceRepetitionItemsFromDisk();



        var spacedRepetitionItems = entities.GetEntityCreateIfNotExist("SpacedRepetitionItems")
            .Set(dummyItems);



        var timerEntity = entities.GetEntityCreateIfNotExist("AutoSave")
            .Set(CreateAutoSaveTimer(dummyItems));

        var srItems = entities.GetEntityCreateIfNotExist("SpaceRepetitionList")
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(dummyItems)
            .SetItemTemplate(DefineSpacedRepetitionItemTemplate())
            .SetSelectionMode(SelectionMode.Single);

        listSearchSpacedRepetition.OnTextChanged((sender, args) =>
        {
            string searchText = listSearchSpacedRepetition.Get<TextBox>().Text!.ToLower();
            var filteredItems = dummyItems.Where(item => item.Name.ToLower().Contains(searchText));
            srItems.Get<ListBox>().ItemsSource = new ObservableCollection<SpacedRepetitionItem>(filteredItems);
            srItems.SetItemsSource(new ObservableCollection<SpacedRepetitionItem>(filteredItems));
        });

        //Use MenuFlyout to create a context menu
        //contextMenu is used for legacy WPF apps
        var contextFlyout = entities.GetEntityCreateIfNotExist("SpacedRepetitionContextFlyout")
            .ChildOf(srItems)
            .Set(new MenuFlyout());

        contextFlyout.Get<MenuFlyout>().Opened += (object? sender, EventArgs e) =>
        {
            if (!srItems.HasItemSelected())
            {
                contextFlyout.Get<MenuFlyout>().Hide();
            }
        };


        var goodReview = entities.GetEntityCreateIfNotExist("SpacedRepetitiongoodReviewMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("GoodReview")
            .OnClick((sender, args) =>
            {
                var item = srItems.GetSelectedItem<SpacedRepetitionItem>();
                item.GoodReview();
            });

        var editMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionEditMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("Edit")
            .OnClick((sender, args) => Console.WriteLine("Edit Clicked"));

        var deleteMenuItem = entities.GetEntityCreateIfNotExist("SpacedRepetitionDeleteMenuItem")
            .ChildOf(contextFlyout)
            .Set(new MenuItem())
            .SetHeader("Delete")
            .OnClick((sender, args) =>
            {
                var item = srItems.GetSelectedItem<SpacedRepetitionItem>();
                dummyItems.Remove(item);
            });
        _ = sortItemsButton.OnSelectionChanged((sender, args) =>
        {
            if (args.AddedItems.Count == 0)
            {
                return;
            }
            var selectedItem = args.AddedItems[0]!.ToString();
            var itemsSource = (ObservableCollection<SpacedRepetitionItem>)srItems.GetItemsSource()!;

            if (selectedItem == "Sort By Date")
            {
                // Sort by NextReview date (ascending - soonest due date first)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderBy(s => s.NextReview));
            }
            else if (selectedItem == "Sort By Priority")
            {
                // Sort by Priority (descending - highest priority first)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderByDescending(s => s.Priority));
            }
            else if (selectedItem == "Sort By Name")
            {
                // Sort by Name (ascending - alphabetical order)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderBy(s => s.Name));
            }
            // You might want to add more sorting options, e.g., by type, difficulty, stability, etc.

            srItems.SetItemsSource(itemsSource);
        });

        dummyItems.CollectionChanged += ((_, _) =>
        {
            if (sortItemsButton.Get<ComboBox>().SelectedItem is null)
            {
                return;
            }
            var selectedItem = sortItemsButton.Get<ComboBox>().SelectedItem!.ToString();
            var itemsSource = dummyItems;

            if (selectedItem == "Sort By Date")
            {
                // Sort by NextReview date (ascending - soonest due date first)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderBy(s => s.NextReview));
            }
            else if (selectedItem == "Sort By Priority")
            {
                // Sort by Priority (descending - highest priority first)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderByDescending(s => s.Priority));
            }
            else if (selectedItem == "Sort By Name")
            {
                // Sort by Name (ascending - alphabetical order)
                itemsSource = new ObservableCollection<SpacedRepetitionItem>(itemsSource.OrderBy(s => s.Name));
            }
            // You might want to add more sorting options, e.g., by type, difficulty, stability, etc.

            srItems.SetItemsSource(itemsSource);
        });

        entities["MainWindow"].OnClosed((_, _) => SaveSpaceRepetitionItemsToDisk(dummyItems));

        return spacedRepetitionPage;
    }

    /// <summary>
    /// Creates an autosave timer that autosaves every 5 minutes
    /// </summary>
    /// <param name="spacedRepetitionItems"></param>
    /// <returns></returns>
    public static Timer CreateAutoSaveTimer(ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        // Create a Timer object
        var autoSaveTimer = new Timer(state =>
        {
            Console.WriteLine($"{DateTime.Now}: Auto-saving data..."); // Debug output
            try
            {
                SaveSpaceRepetitionItemsToDisk((ObservableCollection<SpacedRepetitionItem>)state!);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during saving
                // Log the error, display an error message to the user, etc.
                Console.WriteLine($"Error during auto-save: {ex.Message}");
            }
        }, spacedRepetitionItems, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)); // Pass spacedRepetitionItems as state

        return autoSaveTimer;
    }

    /// <summary>
    /// Saves the spaced repetition items to disk
    /// </summary>
    /// <param name="spacedRepetitionItems"></param>
    public static void SaveSpaceRepetitionItemsToDisk(ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        string directoryPath = "./save";
        Directory.CreateDirectory(directoryPath);
        string filePath = Path.Combine(directoryPath, "space_repetition_items.json");

        // Create JsonSerializerOptions and register the converter
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // For pretty-printing the JSON
            Converters = { new SpacedRepetitionItemConverter() } // Register the custom converter
        };

        string jsonString = JsonSerializer.Serialize(spacedRepetitionItems, options);

        File.WriteAllText(filePath, jsonString);
    }

    /// <summary>
    /// Loads the spaced repetition items from disk
    /// </summary>
    /// <returns></returns>
    public static ObservableCollection<SpacedRepetitionItem> LoadSpaceRepetitionItemsFromDisk()
    {
        string filePath = Path.Combine("./save", "space_repetition_items.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);

            // Create JsonSerializerOptions and register the converter
            var options = new JsonSerializerOptions
            {
                Converters = { new SpacedRepetitionItemConverter() }
            };

            ObservableCollection<SpacedRepetitionItem>? items = JsonSerializer.Deserialize<ObservableCollection<SpacedRepetitionItem>>(jsonString, options);

            foreach (var item in items!)
            {
                item.CreateCardFromSpacedRepetitionData();
            }

            return items ?? new ObservableCollection<SpacedRepetitionItem>();
        }
        else
        {
            return new ObservableCollection<SpacedRepetitionItem>();
        }
    }

    /// <summary>
    /// Define the Spaced Repetition Item Template, used to define how the
    /// SpacedRepetitionItem is displayed in the ListBox.
    /// </summary>
    /// <returns></returns>
    public static FuncDataTemplate<SpacedRepetitionItem> DefineSpacedRepetitionItemTemplate()
    {
        return new FuncDataTemplate<SpacedRepetitionItem>((item, nameScope) =>
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*, *"), // Name, Description, Type
                RowDefinitions = new RowDefinitions("Auto, Auto"),
                Margin = new Thickness(0, 5)
            };

            // *** Create a TextBlock for the multi-line tooltip ***
            var tooltipTextBlock = new TextBlock
            {
                FontWeight = FontWeight.Normal,
                TextWrapping = TextWrapping.Wrap, // Enable text wrapping
                MaxWidth = 200, // Set a maximum width for wrapping
                Text = "This is a very long tooltip text that spans multiple lines. " +
                        "It provides more detailed information about the content item. " +
                        "You can even add more and more text to make it even longer."
            };

            //Name
            var nameTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 5, 0)
            };
            nameTextBlock.Bind(TextBlock.TextProperty, new Binding("Name"));
            Grid.SetColumn(nameTextBlock, 0);
            grid.Children.Add(nameTextBlock);

            /*
            For now only when we hover over the name the long description is shown
            what we want is that it is also shown when we hover over the short description

            To do this we can easily use a stack panel on which we add the name and short description
            that extends to two rows and on that stack panel then we attach the tooltip.
            */
            //ToolTip.SetTip(nameTextBlock, tooltipTextBlock);
            //tooltipTextBlock.Bind(TextBlock.TextProperty, new Binding("LongDescription"));

            //Type (ENUM)
            var typeTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 5, 0)
            };

            typeTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.SpacedRepetitionItemType)));
            Grid.SetColumn(typeTextBlock, 0);
            Grid.SetRow(typeTextBlock, 1);
            grid.Children.Add(typeTextBlock);

            var nextReviewTextBlock = new TextBlock
            {
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            nextReviewTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.NextReview)));
            Grid.SetRow(nextReviewTextBlock, 0);
            Grid.SetColumn(nextReviewTextBlock, 1);
            grid.Children.Add(nextReviewTextBlock);

            //Priority
            /*
            var priorityTextBlock = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            priorityTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.Priority)) { StringFormat = "Priority: {0}" });
            Grid.SetRow(priorityTextBlock, 1);
            Grid.SetColumn(priorityTextBlock, 1);
            grid.Children.Add(priorityTextBlock);
           
            // *** Create a TextBlock for the multi-line tooltip ***
            var priorityTooltipTextBlock = new TextBlock
            {
                FontWeight = FontWeight.Normal,
                TextWrapping = TextWrapping.Wrap, // Enable text wrapping
                MaxWidth = 200, // Set a maximum width for wrapping
                Text = "Priority shows the importance, it determines in which order items will be learned."
            };

            //ToolTip.SetTip(priorityTextBlock, priorityTooltipTextBlock);
            */
            return grid;
        });
    }
}

