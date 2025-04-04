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
using Avalonia.Flecs.StellaLearning.Data;
using Avalonia.Flecs.StellaLearning.Windows;
using System.Text.Json;
using Avalonia.Flecs.StellaLearning.Converters;
using NLog;
using Avalonia.Flecs.Controls;
using System.Timers;
using Avalonia.Threading;
using DesktopNotifications;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;

namespace Avalonia.Flecs.StellaLearning.Pages;

/// <summary>
/// This class represents the Spaced Repetition Page
/// </summary>
public class SpacedRepetitionPage : IUIComponent, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private readonly CompositeDisposable _disposables = [];
    private bool _isDisposed = false;
    private readonly List<string> sortItems = ["Sort By Date", "Sort By Priority", "Sort By Name"];
    private static readonly INotificationManager _iNotificationManager = Program.NotificationManager ??
                           throw new InvalidOperationException("Missing notification manager");
    // --- Control References ---
    private UIBuilder<ListBox>? _srItemsBuilder;
    private UIBuilder<ComboBox>? _sortItemButtonBuilder;
    private UIBuilder<TextBox>? _searchTextBoxBuilder; // Keep ref to search box
    private UIBuilder<TextBlock>? _itemCountTextBlockBuilder; // Keep ref to count textblock
    // ---
    private static bool _previouslyHadItemToReview = false;
    private readonly ObservableCollection<SpacedRepetitionItem> _baseSpacedRepetitionItems; // The original, unfiltered list

    /// <summary>
    /// Creates the Spaced Repetition Page
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public SpacedRepetitionPage(World world)
    {
        _baseSpacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();

        /*
        We only add the handler once. 
        */
        _iNotificationManager.NotificationActivated += OnNotificationActivated;

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
                _searchTextBoxBuilder = textBox; // Store reference

                textBox
                .SetColumn(0)
                .SetWatermark("Search Entries")
                .OnTextChanged((sender, args) =>
                {
                    // Get current search text directly from the event sender or builder
                    string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;
                    ApplyFilterAndSort(searchText); // Apply filter and current sort
                });

                textBox.AttachToolTip(world.UI<ToolTip>((toolTip) =>
                {
                    toolTip.Child<TextBlock>((textBlock) =>
                    {
                        textBlock.SetText(
                        """
                        You can search spaced repetition items by name or by their tags.
                        """);
                    });
                }));
            });

            grid.Child<TextBlock>((textBlock) =>
            {
                _itemCountTextBlockBuilder = textBlock; // Store reference

                textBlock
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetMargin(new Thickness(10, 0))
                .SetText($"Total Items: {_baseSpacedRepetitionItems.Count}")
                .SetColumn(1);

                _baseSpacedRepetitionItems.CollectionChanged += (sender, args) => textBlock.SetText($"Total Items: {_baseSpacedRepetitionItems.Count}");
            });

            grid.Child<ComboBox>((comboBox) =>
            {
                _sortItemButtonBuilder = comboBox; // Store reference

                comboBox
                .SetPlaceholderText("Sort Items")
                .SetColumn(2)
                .SetItemsSource(sortItems);

                comboBox.OnSelectionChanged((sender, args) =>
                {
                    if (args.AddedItems.Count == 0)
                    {
                        return;
                    }
                    var selectedItem = args.AddedItems[0]!.ToString();
                    var itemsSource = (ObservableCollection<SpacedRepetitionItem>)_srItemsBuilder!.GetItemsSource();

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

                    _srItemsBuilder!.SetItemsSource(itemsSource);
                });

            });

            grid.Child<ScrollViewer>((scrollViewer) =>
            {
                scrollViewer
                    .SetRow(1)
                    .SetColumnSpan(3);

                scrollViewer.Child<ListBox>((listBox) =>
                {
                    _srItemsBuilder = listBox; // Store reference

                    var contextFlyout = world.UI<MenuFlyout>((menuFlyout) =>
                    {
                        menuFlyout.OnOpened((sender, e) =>
                        {
                            if (!listBox.HasItemSelected())
                            {
                                menuFlyout.Hide();
                            }
                        });

                        menuFlyout.Child<MenuItem>((item) =>
                        {
                            item.SetHeader("Edit")
                            .OnClick((sender, args) =>
                            {
                                var item = listBox.GetSelectedItem<SpacedRepetitionItem>();

                                object _ = item switch
                                {
                                    SpacedRepetitionCloze => new EditCloze(world, (SpacedRepetitionCloze)item),
                                    SpacedRepetitionFlashcard => new EditFlashcard(world, (SpacedRepetitionFlashcard)item),
                                    SpacedRepetitionFile => new EditFile(world, (SpacedRepetitionFile)item),
                                    SpacedRepetitionQuiz => new EditQuiz(world, (SpacedRepetitionQuiz)item),
                                    SpacedRepetitionImageCloze => new EditImageCloze(world, (SpacedRepetitionImageCloze)item),
                                    _ => throw new NotImplementedException(),
                                };
                            });
                        });

                        menuFlyout.Child<MenuItem>((item) =>
                        {
                            item.SetHeader("Delete")
                            .OnClick(async (sender, args) =>
                            {
                                var item = listBox.GetSelectedItem<SpacedRepetitionItem>();

                                var cd = new ContentDialog()
                                {
                                    Title = "Attention",
                                    Content = $"""Do you want to delete the item "{item.Name}". This cannot be undone!""",
                                    PrimaryButtonText = "Yes",
                                    SecondaryButtonText = "No",
                                    DefaultButton = ContentDialogButton.Secondary,
                                    IsSecondaryButtonEnabled = true,
                                };
                                var result = await cd.ShowAsync();

                                if (result == ContentDialogResult.Primary)
                                {
                                    _baseSpacedRepetitionItems.Remove(item);
                                }

                            });

                        });

                    });

                    _srItemsBuilder
                    .SetItemsSource(_baseSpacedRepetitionItems)
                    .SetItemTemplate(world.CreateTemplate<SpacedRepetitionItem, Grid>((grid, item) =>
                    {
                        var dateTimeConverter = new DateTimeOffsetToLocalTimeStringConverter();

                        grid
                        .SetColumnDefinitions("*, *")
                        .SetRowDefinitions("Auto, Auto")
                        .SetMargin(0, 5);

                        grid.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetTextWrapping(TextWrapping.NoWrap)
                            .SetTextTrimming(TextTrimming.CharacterEllipsis)
                            .SetFontWeight(FontWeight.Bold)
                            .SetMargin(0, 0, 5, 0)
                            .SetBinding(TextBlock.TextProperty, "Name")
                            .SetColumn(0);
                        });

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
                            TextWrapping = TextWrapping.NoWrap,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            Margin = new Thickness(0, 0, 5, 0),
                        };

                        grid.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetTextWrapping(TextWrapping.NoWrap)
                            .SetTextTrimming(TextTrimming.CharacterEllipsis)
                            .SetMargin(0, 0, 5, 0)
                            .SetBinding(TextBlock.TextProperty, "SpacedRepetitionItemType")
                            .SetColumn(0)
                            .SetRow(1);
                        });

                        var nextReviewTextBlock = new TextBlock
                        {
                            TextWrapping = TextWrapping.NoWrap,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Right,
                        };

                        grid.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetTextWrapping(TextWrapping.NoWrap)
                            .SetTextTrimming(TextTrimming.CharacterEllipsis)
                            .SetFontWeight(FontWeight.Bold)
                            .SetHorizontalAlignment(HorizontalAlignment.Right)
                            .SetBinding(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.NextReview))
                            {
                                // Assign the converter instance
                                Converter = dateTimeConverter,
                                // Optionally pass a format string as the ConverterParameter
                                // If omitted, the converter's default ("g") will be used.
                                ConverterParameter = "dd/MM/yyyy HH:mm"
                                // StringFormat is NO LONGER used here, the converter handles formatting
                            })
                            .SetColumn(1)
                            .SetRow(0);
                        });
                        /*

                        Here we want to show our tags for our items.

                        grid.Child<TextBlock>((textBlock) =>
                        {
                            textBlock
                            .SetTextWrapping(TextWrapping.NoWrap)
                            .SetTextTrimming(TextTrimming.CharacterEllipsis)
                            .SetHorizontalAlignment(HorizontalAlignment.Right)
                            .SetColumn(1)
                            .SetRow(1)
                            .SetText("YOUR TAGS HERE!");
                        });
                        */

                    }))
                        .SetSelectionMode(SelectionMode.Single)
                        .SetContextFlyout(contextFlyout.Get<MenuFlyout>());
                });
            });

            grid.Child<Button>((startLearningButton) =>
            {

                var menu = world.UI<MenuFlyout>((menu) =>
                {
                    menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Normal")
                        .OnClick((_, _) => new StartLearningWindow(world, cramMode: false))
                        .AttachToolTip(world.UI<ToolTip>((toolTip) =>
                        {
                            toolTip.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText(
                                """
                                Use spaced repetition to learn items in order of their defined priority.
                                """);
                            });
                        }));
                    });

                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Cram")
                        .OnClick((_, _) => new StartLearningWindow(world, cramMode: true))
                        .AttachToolTip(world.UI<ToolTip>((toolTip) =>
                        {
                            toolTip.Child<TextBlock>((textBlock) =>
                            {
                                textBlock.SetText(
                                """
                                Learn items based on their priority (They are slightly randomized, to avoid seeing the pattern)
                                regardless of their due date.
                                """);
                            });
                        }));
                    });
                });

                startLearningButton
                .SetMargin(0, 20, 0, 0)
                .SetColumn(0)
                .SetColumnSpan(2)
                .SetRow(2)
                .SetFlyout(menu)
                .Child<TextBlock>(t => t.SetText("Start Learning"));
            });

            grid.Child<Button>((addItemButton) =>
            {
                var menu = world.UI<MenuFlyout>((menu) =>
                {
                    menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("File")
                        .OnClick((_, _) => new AddFile(world));
                    });

                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Cloze Text")
                        .OnClick((_, _) => new AddCloze(world));
                    });

                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Image Cloze")
                        .OnClick((_, _) => new AddImageCloze(world));
                    });

                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Flashcard")
                        .OnClick((_, _) => new AddFlashcard(world));
                    });

                    menu.Child<MenuItem>((menuItem) =>
                    {
                        menuItem
                        .SetHeader("Quiz")
                        .OnClick((_, _) => new AddQuiz(world));
                    });
                });

                addItemButton
                .SetMargin(0, 20, 0, 0)
                                .SetHorizontalAlignment(HorizontalAlignment.Right)
                                .SetColumn(2)
                                .SetRow(2)
                                .SetFlyout(menu.Entity)
                                .SetText("Add Item");
            });

        })
        .Add<Page>().Entity;

        // Initial Apply Filter & Sort
        Dispatcher.UIThread.InvokeAsync(() => ApplyFilterAndSort(string.Empty));

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

        // --- Timers & Save Logic ---
        var timerEntity = world.Entity()
            .Set(CreateAutoSaveTimer(_baseSpacedRepetitionItems));
        _disposables.Add(Disposable.Create(() => timerEntity.Destruct())); // Cleanup timer entity

        var notificationTimer = world.Entity()
            .Set(CreateNotificationTimer());
        _disposables.Add(Disposable.Create(() => notificationTimer.Destruct())); // Cleanup timer entity


        // Handle Base Collection Changed (Re-apply filter/sort)
        _baseSpacedRepetitionItems.CollectionChanged += OnBaseCollectionChanged;
        _disposables.Add(Disposable.Create(() => _baseSpacedRepetitionItems.CollectionChanged -= OnBaseCollectionChanged));

        App.GetMainWindow().Closing += (_, _) => SaveSpaceRepetitionItemsToDisk(_baseSpacedRepetitionItems);
    }

    /// <summary>
    /// Applies the current search filter and sort order to the base item list
    /// and updates the ListBox ItemsSource.
    /// </summary>
    private void ApplyFilterAndSort(string searchText)
    {
        if (_srItemsBuilder?.Entity.IsAlive() != true)
        {
            Logger.Warn("Cannot apply filter/sort, ListBox builder is invalid.");
            return;
        }

        string lowerSearchText = searchText?.ToLowerInvariant() ?? string.Empty;

        // 1. Filter the base list
        IEnumerable<SpacedRepetitionItem> filteredItems;
        if (string.IsNullOrWhiteSpace(lowerSearchText))
        {
            filteredItems = _baseSpacedRepetitionItems; // No filter
        }
        else
        {
            string searchTerm = lowerSearchText;

            filteredItems = _baseSpacedRepetitionItems.Where(item =>
            {
                // Check Name (null-safe, case-insensitive)
                bool nameMatch = item.Name?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false;
                if (nameMatch) return true; // Early exit if name matches

                // Check Tags (null-safe, case-insensitive)
                bool tagMatch = item.Tags?.Any(tag => tag?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? false;
                if (tagMatch) return true; // Early exit if tag matches

                // Check Type (case-insensitive)
                // Convert Enum to string and compare
                bool typeMatch = item.SpacedRepetitionItemType.ToString().Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
                if (typeMatch) return true; // Early exit if type matches

                // No match found for this item
                return false;
            });
        }

        // 2. Apply the current sort order
        IEnumerable<SpacedRepetitionItem> sortedAndFilteredItems = ApplySorting(filteredItems);

        // 3. Update the ListBox ItemsSource on the UI thread
        var finalCollection = new ObservableCollection<SpacedRepetitionItem>(sortedAndFilteredItems);
        Dispatcher.UIThread.Post(() =>
        {
            // Re-check validity inside dispatcher
            if (_srItemsBuilder?.Entity.IsAlive() == true)
            {
                _srItemsBuilder.SetItemsSource(finalCollection);
                Logger.Trace($"ListBox updated. Filter: '{searchText}', Items: {finalCollection.Count}");
            }
        }, DispatcherPriority.Background); // Use Background priority for UI updates

        // 4. Update item count (also on UI thread)
        Dispatcher.UIThread.Post(() =>
        {
            if (_itemCountTextBlockBuilder?.Entity.IsAlive() == true)
            {
                _itemCountTextBlockBuilder.SetText($"Total Items: {_baseSpacedRepetitionItems.Count}"); // Show total count always
                                                                                                        // Or show filtered count: .SetText($"Items: {finalCollection.Count} / {_baseSpacedRepetitionItems.Count}");
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Applies sorting to a given collection based on the selected sort option.
    /// </summary>
    private IEnumerable<SpacedRepetitionItem> ApplySorting(IEnumerable<SpacedRepetitionItem> itemsToSort)
    {
        string? selectedSort = null;
        if (_sortItemButtonBuilder?.Entity.IsAlive() == true)
        {
            selectedSort = _sortItemButtonBuilder.Get<ComboBox>().SelectedItem as string;
        }

        Logger.Trace($"Applying sort: {selectedSort ?? "Default"}");

        switch (selectedSort)
        {
            case "Sort By Date":
                return itemsToSort.OrderBy(s => s.NextReview);
            case "Sort By Priority":
                // Ensure stable sort if priorities are equal (e.g., sort by Name next)
                return itemsToSort.OrderByDescending(s => s.Priority).ThenBy(s => s.Name);
            case "Sort By Name":
                return itemsToSort.OrderBy(s => s.Name);
            default:
                // Default sort order (e.g., by name if nothing selected)
                return itemsToSort.OrderBy(s => s.Name);
        }
    }

    /// <summary>
    /// Handles changes to the base collection (_baseSpacedRepetitionItems).
    /// </summary>
    private void OnBaseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // When the underlying data changes (add/remove), re-apply the current filter and sort.
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            Logger.Debug("Base collection changed, re-applying filter and sort.");
            string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;
            ApplyFilterAndSort(searchText);


            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                // Iterate through all items that were removed in this event.
                foreach (var removedItemObject in e.OldItems)
                {
                    // Safely cast the removed item to the expected type.
                    if (removedItemObject is SpacedRepetitionItem removedItem)
                    {
                        Guid itemIdToRemove = removedItem.Uid; // Get the unique ID.
                        Logger.Info($"Item removed from base collection: '{removedItem.Name ?? "N/A"}' (ID: {itemIdToRemove}). Scheduling statistics removal.");

                        try
                        {
                            // Get the singleton instance and await the removal process.
                            await StatsTracker.Instance.RemoveStatsForItemAsync(itemIdToRemove);
                            Logger.Info($"Successfully completed background statistics removal for item ID {itemIdToRemove}.");
                        }
                        catch (Exception ex)
                        {
                            // Log any exceptions during the stats removal process.
                            Logger.Error(ex, $"Error occurred during background statistics removal for item ID {itemIdToRemove}.");
                            // Consider additional error handling if needed (e.g., user notification).
                        }
                    }
                    else
                    {
                        // Log a warning if the removed item was not of the expected type.
                        Logger.Warn($"An item removed from the base collection was not of the expected type 'SpacedRepetitionItem'. Actual type: {removedItemObject?.GetType().FullName ?? "null"}");
                    }
                }
            }
        });

    }

    /// <summary>
    /// Creates an autosave timer that autosaves every 5 minutes
    /// </summary>
    /// <param name="spacedRepetitionItems"></param>
    /// <returns></returns>
    public static Timer CreateAutoSaveTimer(ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        // Create a System.Timers.Timer for auto-saving
        var autoSaveTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds) // 5 minutes
        {
            AutoReset = true, // Make the timer repeat
            Enabled = true,   // Start the timer immediately
        };

        autoSaveTimer.Elapsed += async (sender, e) =>
        {
            Console.WriteLine("Auto Saving data...");
            try
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    SaveSpaceRepetitionItemsToDisk(spacedRepetitionItems);
                    await StatsTracker.Instance.SaveStatsAsync();
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during saving
                Logger.Warn($"Error during auto-save: {ex.Message}");
            }
        };

        return autoSaveTimer;
    }

    private Timer CreateNotificationTimer()
    {
        var notificationTimer = new Timer(60000)
        {
            AutoReset = true,
            Enabled = true,
        };

        notificationTimer.Elapsed += (sender, e) =>
        {
            Dispatcher.UIThread.Post(() =>
                        {
                            if (_root == 0)
                            {
                                return;
                            }
                            if (!_root.CsWorld().Has<Settings>())
                            {
                                return;
                            }

                            var _ItemToBeLearned = _baseSpacedRepetitionItems.GetNextItemToBeReviewed();
                            bool hasItemToReview = _ItemToBeLearned != null;
                            if (hasItemToReview && !_previouslyHadItemToReview && !App.GetMainWindow().IsActive && _root.CsWorld().Get<Settings>().EnableNotifications)
                            {
                                var nf = new Notification
                                {
                                    Title = "New item can be learned",
                                    Body = "Open the learning window by clicking start learning",
                                    Buttons =
                                {
                                    ("Start Learning", "startLearning"),
                                    ("Dismiss", "dismiss")
                                }
                                };

                                _iNotificationManager.ShowNotification(nf);
                            }
                            _previouslyHadItemToReview = hasItemToReview;
                        });
        };

        return notificationTimer;
    }

    // Track the last hash of the saved data to detect changes
    private static string? _lastSavedDataHash;

    /// <summary>
    /// Saves the spaced repetition items to disk, but only if the data has changed since the last save
    /// </summary>
    /// <param name="spacedRepetitionItems"></param>
    public static void SaveSpaceRepetitionItemsToDisk(ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        if (spacedRepetitionItems == null || spacedRepetitionItems.Count == 0)
        {
            Logger.Debug("No items to save or collection is null.");
            return;
        }

        try
        {
            // Create JsonSerializerOptions and register the converter
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new SpacedRepetitionItemConverter() }
            };

            // Generate JSON string from current data
            string jsonString = JsonSerializer.Serialize(spacedRepetitionItems, options);

            // Calculate hash of current data
            string currentHash = CalculateDataHash(jsonString);

            // Check if data has changed by comparing hashes
            if (currentHash == _lastSavedDataHash)
            {
                Logger.Debug("Data hasn't changed since last save - skipping write operation.");
                return;
            }

            // Data has changed, save it
            Logger.Info("Data changed - saving space repetition data.");

            const string directoryPath = "./save";
            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, "space_repetition_items.json");

            File.WriteAllText(filePath, jsonString);

            // Update the hash after successful save
            _lastSavedDataHash = currentHash;
            Logger.Debug($"Successfully saved {spacedRepetitionItems.Count} items.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Error during space repetition data save: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates a hash of the serialized data for change detection
    /// </summary>
    private static string CalculateDataHash(string data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Define the Spaced Repetition Item Template, used to define how the
    /// SpacedRepetitionItem is displayed in the ListBox.
    /// </summary>
    /// <returns></returns>
    public static FuncDataTemplate<SpacedRepetitionItem> DefineSpacedRepetitionItemTemplate()
    {

        var dateTimeConverter = new DateTimeOffsetToLocalTimeStringConverter();

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
                        "You can even add more and more text to make it even longer.",
            };

            //Name
            var nameTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 5, 0),
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
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 5, 0),
            };

            typeTextBlock.Bind(TextBlock.TextProperty, new Binding(nameof(SpacedRepetitionItem.SpacedRepetitionItemType)));
            Grid.SetColumn(typeTextBlock, 0);
            Grid.SetRow(typeTextBlock, 1);
            grid.Children.Add(typeTextBlock);

            var nextReviewTextBlock = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var nextReviewBinding = new Binding(nameof(SpacedRepetitionItem.NextReview))
            {
                // Assign the converter instance
                Converter = dateTimeConverter,
                // Optionally pass a format string as the ConverterParameter
                // If omitted, the converter's default ("g") will be used.
                ConverterParameter = "dd/MM/yyyy HH:mm"
                // StringFormat is NO LONGER used here, the converter handles formatting
            };

            nextReviewTextBlock.Bind(TextBlock.TextProperty, nextReviewBinding);
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

    private void OnNotificationActivated(object? sender, NotificationActivatedEventArgs e)
    {
        if (e.ActionId == "startLearning")
        {
            Dispatcher.UIThread.Post(() => new StartLearningWindow(_root.CsWorld()));
        }
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Diposes flecs entities and event handlers correctly
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Logger.Debug($"Disposing SpacedRepetitionPage (Root: {_root.Id})...");
                // Unsubscribe external events first
                if (_baseSpacedRepetitionItems != null)
                {
                    _baseSpacedRepetitionItems.CollectionChanged -= OnBaseCollectionChanged;
                }
                var mainWindow = App.GetMainWindow();

                _iNotificationManager.NotificationActivated -= OnNotificationActivated; // Ensure this is removed too

                // Dispose internal disposables (includes timer entities)
                _disposables.Dispose();

                // Destroy root Flecs UI if necessary (depends on ownership model)
                // If the Page lifecycle is managed elsewhere that destroys Root, this isn't needed.
                // If this page component *owns* its UI root, destroy it.
                // Assuming it owns it for now:
                if (_root.IsValid() && _root.IsAlive()) _root.Destruct();

                Logger.Debug("SpacedRepetitionPage disposed.");
            }
            _isDisposed = true;
        }
    }
}
