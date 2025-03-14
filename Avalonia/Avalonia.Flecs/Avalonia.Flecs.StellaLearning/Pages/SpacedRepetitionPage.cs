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
using NLog;
using Avalonia.Flecs.Controls;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// This class represents the Spaced Repetition Page
/// </summary>
public class SpacedRepetitionPage : IUIComponent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;

    private readonly List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];

    private readonly List<string> itemTypes = ["File", "Quiz", "Cloze"];
    private UIBuilder<ListBox>? srItems;
    private UIBuilder<ComboBox>? sortItemButton;
    /// <summary>
    /// Creates the Spaced Repetition Page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public SpacedRepetitionPage(World world)
    {
        ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems = LoadSpaceRepetitionItemsFromDisk();
        world.Set(spacedRepetitionItems);

        _root = world.UI<Grid>((grid) =>
        {
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
            grid
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");

            grid.Child<TextBox>((textBox) =>
            {
                textBox
                .SetColumn(0)
                .SetWatermark("Search Entries")
                .OnTextChanged((sender, args) =>
                {
                    string searchText = textBox.GetText().ToLower();
                    var filteredItems = spacedRepetitionItems.Where(item => item.Name.ToLower().Contains(searchText));
                    srItems!.SetItemsSource(new ObservableCollection<SpacedRepetitionItem>(filteredItems)); ;
                });
            });

            grid.Child<TextBlock>((textBlock) =>
            {
                textBlock
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetMargin(new Thickness(10, 0))
                .SetText($"Total Items: {spacedRepetitionItems.Count}")
                .SetColumn(1);

                spacedRepetitionItems.CollectionChanged += (sender, args) => textBlock.SetText($"Total Items: {spacedRepetitionItems.Count}");
            });

            grid.Child<ComboBox>((comboBox) =>
            {
                var myFlyout = new Flyout()
                {
                    Content = new TextBlock() { Text = "Hello World" },
                    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
                };

                sortItemButton = comboBox;

                comboBox
                .SetPlaceholderText("Sort Items")
                .SetColumn(2)
                .SetItemsSource(sortItems)
                .SetContextFlyout(myFlyout);

                comboBox.OnSelectionChanged((sender, args) =>
                {
                    if (args.AddedItems.Count == 0)
                    {
                        return;
                    }
                    var selectedItem = args.AddedItems[0]!.ToString();
                    var itemsSource = (ObservableCollection<SpacedRepetitionItem>)srItems!.GetItemsSource();

                    if (selectedItem == "Sort By Date")
                    {
                        // Sort by NextReview date (ascending - soonest due date first)
                        itemsSource = [.. itemsSource.OrderBy(s => s.NextReview)];
                    }
                    else if (selectedItem == "Sort By Priority")
                    {
                        // Sort by Priority (descending - highest priority first)
                        itemsSource = [.. itemsSource.OrderByDescending(s => s.Priority)];
                    }
                    else if (selectedItem == "Sort By Name")
                    {
                        // Sort by Name (ascending - alphabetical order)
                        itemsSource = [.. itemsSource.OrderBy(s => s.Name)];
                    }
                    // You might want to add more sorting options, e.g., by type, difficulty, stability, etc.

                    srItems!.SetItemsSource(itemsSource);
                });

            });

            grid.Child<ScrollViewer>((scrollViewer) =>
            {
                scrollViewer
                    .SetRow(1)
                    .SetColumnSpan(3);

                scrollViewer.Child<ListBox>((listBox) =>
                {
                    srItems = listBox;

                    var contextFlyout = world.UI<MenuFlyout>((menuFlyout) =>
                    {
                        menuFlyout.OnOpened((object? sender, EventArgs e) =>
                        {
                            if (!listBox.HasItemSelected())
                            {
                                menuFlyout.Hide();
                            }
                        });

                        menuFlyout.Child<MenuItem>((item) =>
                        {

                            item.SetHeader("Delete")
                            .OnClick((sender, args) =>
                            {
                                var item = listBox.GetSelectedItem<SpacedRepetitionItem>();
                                spacedRepetitionItems.Remove(item);
                            });

                        });
                    });

                    srItems
                    .SetItemsSource(spacedRepetitionItems)
                    .SetItemTemplate(DefineSpacedRepetitionItemTemplate())
                    .SetSelectionMode(SelectionMode.Single)
                    .SetContextFlyout(contextFlyout.Get<MenuFlyout>());
                });
            });

            grid.Child<Button>((startLearningButton) =>
            {
                startLearningButton
                .SetMargin(0, 20, 0, 0)
                .SetColumn(0)
                .SetColumnSpan(2)
                .SetRow(2)
                .Child<TextBlock>(t => t.SetText("Start Learning"));
            });

            grid.Child<Button>((addItemButton) =>
            {
                var stackPanel = world.UI<StackPanel>((stackPanel) =>
                {
                    stackPanel
                    .SetOrientation(Orientation.Vertical)
                    .SetSpacing(5);

                    stackPanel.Child<Button>((addFileButton) =>
                    {
                        addFileButton
                        .OnClick((sender, args) => AddFile.Create(world).ShowWindow())
                        .Child<TextBlock>(t =>
                            t.SetText("File")
                            .SetFontWeight(FontWeight.Normal)
                        );
                    });

                    stackPanel.Child<Button>((addClozeButton) =>
                    {
                        addClozeButton
                        .OnClick((sender, args) => AddFile.Create(world).ShowWindow())
                        .Child<TextBlock>(t =>
                            t.SetText("Cloze")
                            .SetFontWeight(FontWeight.Normal)
                        );
                    });

                    stackPanel.Child<Button>((addQuizButton) =>
                    {
                        addQuizButton
                        .OnClick((sender, args) => AddFile.Create(world).ShowWindow())
                        .Child<TextBlock>(t =>
                            t.SetText("Quiz")
                            .SetFontWeight(FontWeight.Normal)
                        );
                    });

                    stackPanel.Child<Button>((addFlashcardButton) =>
                    {
                        addFlashcardButton
                        .OnClick((sender, args) => AddFile.Create(world).ShowWindow())
                        .Child<TextBlock>(t =>
                            t.SetText("Flashcard")
                            .SetFontWeight(FontWeight.Normal)
                        );
                    });
                });

                var addItemsFlyout = new Flyout()
                {
                    Content = stackPanel.Get<object>(),
                    ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway
                };

                addItemButton
                .SetMargin(0, 20, 0, 0)
                .SetHorizontalAlignment(HorizontalAlignment.Right)
                .SetColumn(2)
                .SetRow(2)
                .SetFlyout(addItemsFlyout)
                .Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Add Item");
                });
            });

        })
        .Add<Page>();

        _root.AddDefaultStyling((spacedRepetitionPage) =>
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

        //ToolTip.SetTip(sortItemsButton.Get<ComboBox>(), myToolTip);

        /*
        I believe that entites should not know the exact control type but
        all other entities should only care for the base classes like
        Control, Panel, ItemsControl, TemplatedControl, Etc. They should
        always take the lowest common denominator.

        No need to depend on things that we dont care for 
        */

        //App.Entities!["SpacedRepetitionItems"] = world.Entity()
        //    .Set(dummyItems);

        var timerEntity = world.Entity()
            .Set(CreateAutoSaveTimer(spacedRepetitionItems));


        /*
        When the spaced reptition items change we want to refresh the shown list, this mostly occurs when new items are added, updated or removed.
        */
        spacedRepetitionItems.CollectionChanged += ((_, _) =>
                {
                    if (sortItemButton!.Get<ComboBox>().SelectedItem is null)
                    {
                        return;
                    }
                    var selectedItem = sortItemButton.Get<ComboBox>().SelectedItem!.ToString();
                    var itemsSource = spacedRepetitionItems;

                    if (selectedItem == "Sort By Date")
                    {
                        // Sort by NextReview date (ascending - soonest due date first)
                        itemsSource = [.. itemsSource.OrderBy(s => s.NextReview)];
                    }
                    else if (selectedItem == "Sort By Priority")
                    {
                        // Sort by Priority (descending - highest priority first)
                        itemsSource = [.. itemsSource.OrderByDescending(s => s.Priority)];
                    }
                    else if (selectedItem == "Sort By Name")
                    {
                        // Sort by Name (ascending - alphabetical order)
                        itemsSource = [.. itemsSource.OrderBy(s => s.Name)];
                    }
                    // You might want to add more sorting options, e.g., by type, difficulty, stability, etc.

                    srItems!.SetItemsSource(itemsSource);
                });

        App.GetMainWindow().Closing += (_, _) => SaveSpaceRepetitionItemsToDisk(spacedRepetitionItems);
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
            Logger.Info($"{DateTime.Now}: Auto-saving data..."); // Debug output
            try
            {
                SaveSpaceRepetitionItemsToDisk((ObservableCollection<SpacedRepetitionItem>)state!);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during saving
                // Log the error, display an error message to the user, etc.
                Logger.Warn($"Error during auto-save: {ex.Message}");
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
        Logger.Info("Trying to save space repetition data."); // Debug output

        try
        {
            const string directoryPath = "./save";
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
        catch (Exception ex)
        {
            Logger.Warn($"Error during auto-save: {ex.Message}");
        }
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

            return items ?? [];
        }
        else
        {
            return [];
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
