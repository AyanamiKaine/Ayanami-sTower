using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS; // Assuming Module and UIBuilderExtensions are here
using Avalonia.Layout;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module; // If Page tag is defined here
using Avalonia.Data;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Input; // For DragEventArgs, KeyEventArgs
using Avalonia.Interactivity; // For RoutedEventArgs
using Avalonia.Platform.Storage; // For FilePicker
using Avalonia.Threading; // For Dispatcher
using Avalonia.Flecs.StellaLearning.Data; // Where LiteratureSourceItem etc. reside
using Avalonia.Flecs.StellaLearning.Util; // For MessageDialog (assuming it's here)
using Avalonia.Flecs.StellaLearning.UiComponents; // For TagComponent (assuming it's here)
using Avalonia.Flecs.Controls; // For UIBuilderExtensions
using System.Text.Json; // For saving/loading (will need converters)
using System.Reactive.Disposables; // For CompositeDisposable
using NLog;
using FluentAvalonia.UI.Controls;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Flecs.StellaLearning.Windows; // Assuming you use NLog like in SpacedRepetitionPage

namespace Avalonia.Flecs.StellaLearning.Pages
{
    /// <summary>
    /// Represents the UI page for managing literature sources.
    /// </summary>
    public class LiteraturePage : IUIComponent, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly World _world;
        private Entity _root;
        /// <inheritdoc/>
        public Entity Root => _root;

        private bool _isDisposed = false;
        private readonly CompositeDisposable _disposables = [];


        private readonly Dictionary<Guid, PropertyChangedEventHandler> _itemPropertyChangedHandlers = [];

        // --- Data Collections ---
        // The master list of all literature items
        private readonly ObservableCollection<LiteratureSourceItem> _baseLiteratureItems;
        // The currently displayed list (after filtering/sorting) - ListBox ItemsSource will point to a filtered version
        // We don't necessarily need a separate collection field if ApplyFilterAndSort creates a new one each time.

        // --- Control References ---
        private UIBuilder<ListBox>? _literatureListBuilder;
        private UIBuilder<TextBox>? _searchTextBoxBuilder;
        private UIBuilder<TextBlock>? _itemCountTextBlockBuilder;

        // --- Constants ---
        private const string LITERATURE_FOLDER_NAME = "literature"; // Folder to store local files
        private const string LITERATURE_SAVE_FILE = "literature_items.json"; // Save file name

        /// <summary>
        /// Initializes a new instance of the LiteraturePage class.
        /// </summary>
        /// <param name="world">The Flecs world.</param>
        public LiteraturePage(World world)
        {
            _world = world;

            // Ensure literature directory exists
            EnsureLiteratureDirectory();

            // Load existing data
            _baseLiteratureItems = LoadLiteratureItemsFromDisk(); // Implement this
            SubscribeToAllItemChanges(_baseLiteratureItems);

            // Subscribe to collection changes for saving and UI updates
            _baseLiteratureItems.CollectionChanged += OnBaseCollectionChanged;
            _disposables.Add(Disposable.Create(() => _baseLiteratureItems.CollectionChanged -= OnBaseCollectionChanged));

            // Build the UI
            _root = _world.UI<Grid>(BuildLiteraturePageUI)
                          .Add<Page>() // Add the Page tag
                          .Entity;

            // Initial filter/sort application
            Dispatcher.UIThread.Post(() => ApplyFilterAndSort(string.Empty), DispatcherPriority.Background);

            // Setup auto-save timer (similar to SpacedRepetitionPage)
            // var autoSaveTimer = CreateAutoSaveTimer(_baseLiteratureItems);
            // var timerEntity = world.Entity().Set(autoSaveTimer);
            // _disposables.Add(Disposable.Create(() => timerEntity.Destruct()));

            // Save on close
            App.GetMainWindow().Closing += (_, _) => SaveLiteratureItemsToDisk(_baseLiteratureItems);
        }

        /// <summary>
        /// Ensures the directory for storing local literature files exists.
        /// </summary>
        private static void EnsureLiteratureDirectory()
        {
            try
            {
                string literaturePath = Path.Combine(Directory.GetCurrentDirectory(), LITERATURE_FOLDER_NAME);
                Directory.CreateDirectory(literaturePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error creating literature directory: {LITERATURE_FOLDER_NAME}");
                MessageDialog.ShowErrorDialog($"Error creating literature directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the UI structure for the LiteraturePage using UIBuilder.
        /// </summary>
        private void BuildLiteraturePageUI(UIBuilder<Grid> grid)
        {
            grid.SetRowDefinitions("Auto, *, Auto") // Row 0: Controls, Row 1: List, Row 2: Actions
                .SetColumnDefinitions("*, Auto, Auto"); // Col 0: Search, Col 1: Count, Col 2: Add/Sort

            // --- Row 0: Search, Count, Add/Sort ---
            grid.Child<TextBox>(textBox =>
            {
                _searchTextBoxBuilder = textBox;
                textBox.SetColumn(0)
                       .SetWatermark("Search by Name, Title, Author, Tags...")
                       .SetMargin(5)
                       .OnTextChanged((sender, args) =>
                       {
                           ApplyFilterAndSort(_searchTextBoxBuilder?.GetText() ?? string.Empty);
                       });
                textBox.AttachToolTip(_world.UI<ToolTip>(tooltip => tooltip.Child<TextBlock>(tb => tb.SetText("Search across item names, titles, authors, and tags."))));
            });

            grid.Child<TextBlock>(textBlock =>
            {
                _itemCountTextBlockBuilder = textBlock;
                textBlock.SetColumn(1)
                         .SetVerticalAlignment(VerticalAlignment.Center)
                         .SetMargin(5)
                         .SetText($"Items: {_baseLiteratureItems.Count}"); // Initial count
            });

            grid.Child<Button>(button => // Placeholder for Add/Sort
            {
                button.SetColumn(2)
                      .SetMargin(5)
                      .SetText("Add Source...")
                      .OnClick(async (s, e) => await AddSourceAsync()); // Implement AddSourceAsync
                button.AttachToolTip(_world.UI<ToolTip>(tooltip => tooltip.Child<TextBlock>(tb => tb.SetText("Add a new literature source (File or URL). You can also drag & drop files onto the list."))));

                // TODO: Add MenuFlyout for choosing File/URL if desired
            });

            // --- Row 1: Literature List ---
            grid.Child<ScrollViewer>(scrollViewer =>
            {
                scrollViewer.SetRow(1)
                            .SetColumnSpan(3) // Span all columns
                            .SetMargin(5, 0, 5, 0);

                scrollViewer.Child<ListBox>(listBox =>
                {
                    _literatureListBuilder = listBox;

                    // Context Menu Setup
                    var contextFlyout = _world.UI<MenuFlyout>(menuFlyout =>
                    {
                        menuFlyout.OnOpened((sender, e) =>
                        {
                            if (!listBox.HasItemSelected())
                            {
                                menuFlyout.Hide();
                            }
                        });

                        menuFlyout.Child<MenuItem>(item => item.SetHeader("Open").OnClick((_, _) => { }));
                        menuFlyout.Child<MenuItem>(item => item.SetHeader("Edit Details...").OnClick((_, _) =>
                        {
                            var selectedLiteratureItem = listBox.GetSelectedItem<LiteratureSourceItem>();
                            new EditLiteratureSource(_world, selectedLiteratureItem);
                        }));
                        menuFlyout.Child<MenuItem>(item => item.SetHeader("Get Citation...").OnClick((_, _) => { }));
                        menuFlyout.Child<Separator>((_) => { });
                        menuFlyout.Child<MenuItem>(item => item.SetHeader("Remove").OnClick(async (sender, e) =>
                        {

                            var selectedLiteratureItem = listBox.GetSelectedItem<LiteratureSourceItem>();

                            // Confirmation Dialog
                            var confirmDialog = new ContentDialog
                            {
                                Title = "Confirm Removal",
                                Content = $"Are you sure you want to remove '{selectedLiteratureItem.Name}' from the list?\n\nNote: This only removes the entry from the list. The associated file (if any) will NOT be deleted from your '{LITERATURE_FOLDER_NAME}' folder.",
                                PrimaryButtonText = "Remove",
                                CloseButtonText = "Cancel",
                                DefaultButton = ContentDialogButton.Close
                            };

                            var result = await confirmDialog.ShowAsync();

                            if (result == ContentDialogResult.Primary)
                            {
                                Logger.Info($"Removing item: {selectedLiteratureItem.Name}");
                                _baseLiteratureItems.Remove(selectedLiteratureItem);
                                // Selection will be cleared automatically by ListBox if the source updates correctly
                                // Optionally trigger save
                                // SaveLiteratureItemsToDisk(_baseLiteratureItems);
                            }
                            else
                            {
                                Logger.Debug("Removal cancelled by user.");
                            }
                        })); // Implement RemoveSelectedItem
                    });

                    listBox.SetItemsSource(_baseLiteratureItems) // Bind to the base list initially
                           .SetItemTemplate(CreateLiteratureItemTemplate())
                           .SetSelectionMode(SelectionMode.Single)
                           .SetContextFlyout(contextFlyout)
                           .AllowDrop() // Enable dropping files
                           .OnDragOver(HandleLiteratureListDragOver) // Handle hover effect
                           .OnDrop(HandleLiteratureListDropAsync); // Handle the actual drop
                });
            });
        }

        /// <summary>
        /// Creates the DataTemplate for displaying LiteratureSourceItem in the ListBox.
        /// </summary>
        private FuncDataTemplate<LiteratureSourceItem> CreateLiteratureItemTemplate()
        {
            return _world.CreateTemplate<LiteratureSourceItem, Grid>((grid, item) =>
            {
                if (item is null)
                    return;

                grid.SetColumnDefinitions("Auto, *, Auto") // Icon(Type), Main Info, Tags
                    .SetRowDefinitions("Auto, Auto")    // Row 0: Name/Title, Row 1: Author/Year + Tags
                    .SetMargin(5);

                // Row 0, Col 0: Icon based on SourceType (Placeholder)
                grid.Child<TextBlock>(icon =>
                {
                    // Add Tooltip for icon maybe?
                    icon.AttachToolTip(_world.UI<ToolTip>(tooltip => tooltip.Child<TextBlock>(tb => tb.SetText(item.SourceType.ToString()!))));
                });

                // Row 0, Col 1: Name / Title
                grid.Child<TextBlock>(nameTitle =>
                {
                    nameTitle.SetColumn(1).SetRow(0)
                             .SetFontWeight(FontWeight.Bold)
                             .SetTextTrimming(TextTrimming.CharacterEllipsis)
                             .SetBinding(TextBlock.TextProperty, new Binding(nameof(LiteratureSourceItem.Title)) // Bind to Title primarily
                             {
                                 // Fallback to Name if Title is null/empty
                                 FallbackValue = item.Name, // Show Name if Title is unset
                                 TargetNullValue = item.Name // Show Name if Title becomes null
                             });
                    // Tooltip showing full Title or Name
                    nameTitle.AttachToolTip(_world.UI<ToolTip>(tooltip => tooltip.Child<TextBlock>(tb => tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(LiteratureSourceItem.Title)) { FallbackValue = item.Name, TargetNullValue = item.Name }))));

                });

                // Row 1, Col 1: Author / Year
                grid.Child<TextBlock>(authorYear =>
                {
                    authorYear.SetColumn(1).SetRow(1)
                              .SetFontSize(11)
                              .SetForeground(Brushes.Gray)
                              .SetTextTrimming(TextTrimming.CharacterEllipsis);

                    // Combine Author and Year - requires a MultiBinding or custom converter,
                    // or just display Author for simplicity for now.
                    authorYear.SetBinding(TextBlock.TextProperty, new Binding(nameof(LiteratureSourceItem.Author))
                    {
                        TargetNullValue = "No Author", // Placeholder if author is null
                        FallbackValue = "No Author"
                    });
                    // TODO: Improve this display to include year, possibly using a converter.
                    authorYear.AttachToolTip(_world.UI<ToolTip>(tooltip => tooltip.Child<TextBlock>(tb => tb.SetBinding(TextBlock.TextProperty, new Binding(nameof(LiteratureSourceItem.Author)) { TargetNullValue = "No Author", FallbackValue = "No Author" }))));
                });

                // Row 1, Col 2: Tags (using ItemsControl or similar)
                grid.Child<ItemsControl>(tagsList =>
                {
                    tagsList.SetColumn(2).SetRow(1)
                            .SetHorizontalAlignment(HorizontalAlignment.Right)
                            .SetItemsSource(item.Tags) // Bind to the item's Tags collection
                                                       //.SetItemsPanel(new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = double.NaN })) // Use WrapPanel
                            .SetItemTemplate(_world.CreateTemplate<string, Border>((border, tagText) => // Simple tag template
                            {
                                border.SetBackground(Brushes.LightGray)
                                      .SetCornerRadius(3)
                                      .SetPadding(4, 1)
                                      .SetMargin(2, 0)
                                    .Child<StackPanel>(stackPanel =>
                                    {
                                        stackPanel
                                            .SetOrientation(Orientation.Horizontal)
                                            .SetSpacing(5)
                                            .SetVerticalAlignment(VerticalAlignment.Center);

                                        stackPanel.Child<TextBlock>(textBlock =>
                                        {
                                            textBlock
                                                .SetText(tagText)
                                                .SetVerticalAlignment(VerticalAlignment.Center);
                                        });


                                    });
                            }))
                            .With(ic => ic.ItemsPanel = new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal })!);
                });


            });
        }

        /// <summary>
        /// Gets a simple string icon representation for the source type.
        /// </summary>
        private string GetIconForSourceType(LiteratureSourceType type)
        {
            // Replace with actual icons (e.g., from Fluent Icons) later
            return type switch
            {
                LiteratureSourceType.Book => "\uE736", // Book icon (Fluent System Icons)
                LiteratureSourceType.JournalArticle => "\uE8A5", // Document icon
                LiteratureSourceType.Website => "\uE774", // Globe icon
                LiteratureSourceType.LocalFile => "\uE7C3", // File icon
                LiteratureSourceType.ConferencePaper => "\uE8A5", // Document icon
                LiteratureSourceType.Report => "\uE7F6", // Report icon
                LiteratureSourceType.Thesis => "\uE7BC", // Education icon
                _ => "\uE783", // Question mark icon
            };
        }

        // --- Event Handlers ---

        private void SubscribeToAllItemChanges(IEnumerable<LiteratureSourceItem>? items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                SubscribeToItemChanges(item);
            }
        }

        private void SubscribeToItemChanges(LiteratureSourceItem item)
        {
            if (item == null || _itemPropertyChangedHandlers.ContainsKey(item.Uid))
            {
                return; // Avoid double subscription or null refs
            }

            PropertyChangedEventHandler handler = HandleItemPropertyChanged;
            item.PropertyChanged += handler;
            _itemPropertyChangedHandlers[item.Uid] = handler; // Store the specific handler instance
        }
        private void HandleItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isDisposed) return; // Don't handle if page is disposed
            if (sender is not LiteratureSourceItem changedItem) return;

            // Check if the changed property is relevant for filtering or sorting
            bool needsRefresh = e.PropertyName switch
            {
                nameof(LiteratureSourceItem.Name) => true,
                nameof(LiteratureSourceItem.Title) => true,
                nameof(LiteratureSourceItem.Author) => true,
                nameof(LiteratureSourceItem.Tags) => true,
                nameof(LiteratureSourceItem.SourceType) => true,
                _ => false
            };

            if (needsRefresh)
            {
                Logger.Trace($"Relevant property '{e.PropertyName}' changed for item '{changedItem.Name}', triggering list refresh.");
                // Refresh the list on the UI thread. Use Post for better responsiveness if updates are frequent.
                Dispatcher.UIThread.Post(() =>
                {
                    if (_isDisposed) return; // Double check dispose state
                    string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;
                    ApplyFilterAndSort(searchText);
                }, DispatcherPriority.Background); // Lower priority for UI updates
            }
        }

        /// <summary>
        /// Handles the DragOver event for the literature ListBox.
        /// Allows dropping only if files are being dragged.
        /// </summary>
        private void HandleLiteratureListDragOver(object? sender, DragEventArgs e)
        {
            // Allow copy effect if data contains files.
            e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the Drop event for the literature ListBox.
        /// Processes dropped files asynchronously.
        /// </summary>
        private async void HandleLiteratureListDropAsync(object? sender, DragEventArgs e)
        {
            var storageItems = e.Data.GetFiles();
            if (storageItems?.Any() != true)
            {
                e.Handled = false;
                return;
            }

            e.DragEffects = DragDropEffects.Copy; // Indicate we're handling it
            e.Handled = true;

            Logger.Info($"Processing {storageItems.Count()} dropped items...");

            var processingTasks = new List<Task>();

            foreach (var item in storageItems)
            {
                if (item is IStorageFile file) // Process only files
                {
                    processingTasks.Add(ProcessDroppedFileAsync(file));
                }
            }

            try
            {
                await Task.WhenAll(processingTasks);
                Logger.Info("Finished processing dropped items.");
                // Optionally trigger save after processing drops
                // SaveLiteratureItemsToDisk(_baseLiteratureItems);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while processing dropped files.");
                MessageDialog.ShowWarningDialog($"An error occurred while processing dropped files: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a single dropped file: copies it, creates a LiteratureSourceItem, and adds it.
        /// </summary>
        private async Task ProcessDroppedFileAsync(IStorageFile storageFile)
        {
            string? sourcePath = storageFile.TryGetLocalPath();
            if (string.IsNullOrEmpty(sourcePath))
            {
                Logger.Warn($"Could not get local path for dropped file: {storageFile.Name}. Skipping.");
                return;
            }

            // TODO: Add check for supported file types if necessary (e.g., only PDFs)
            // string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            // if (extension != ".pdf") { Logger.Info($"Skipping non-PDF file: {sourcePath}"); return; }

            Logger.Debug($"Processing dropped file: {sourcePath}");

            try
            {
                // 1. Copy file to the literature directory
                string? newPath = await Task.Run(() => CopyFileToLiteratureFolder(sourcePath)); // Run copy operation in background
                if (string.IsNullOrEmpty(newPath))
                {
                    return;
                }

                // 2. Create LiteratureSourceItem (LocalFileSourceItem)
                // The constructor handles basic validation and sets the default name
                var newItem = new LocalFileSourceItem(newPath);
                newItem.Title = storageFile.Name;

                if (Path.GetExtension(newPath) == ".pdf")
                {
                    try
                    {
                        PdfMetadata info = PdfSharpMetadataExtractor.ExtractMetadata(newPath);
                        newItem.PageCount = info.PageCount;
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Info($"Exception was thrown {ex}");
                    }
                }
                // You might want to add default tags or prompt user later

                // 3. Add to collection on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _baseLiteratureItems.Add(newItem);
                    Logger.Info($"Added literature source from drop: {newItem.Name}");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error processing dropped file '{sourcePath}'");
                // Show error to user on UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MessageDialog.ShowErrorDialog($"Error adding file '{Path.GetFileName(sourcePath)}': {ex.Message}");
                });
            }
        }


        /// <summary>
        /// Copies a file to the application's literature folder, ensuring a unique name.
        /// </summary>
        /// <returns>The full path to the newly copied file, or null if an error occurred.</returns>
        private static string? CopyFileToLiteratureFolder(string sourcePath)
        {
            try
            {
                string literaturePath = Path.Combine(Directory.GetCurrentDirectory(), LITERATURE_FOLDER_NAME);
                string destinationFileName = Path.GetFileName(sourcePath);
                string destinationPath = Path.Combine(literaturePath, destinationFileName);

                // Handle potential naming conflicts (e.g., append number or GUID)
                int count = 1;
                string fileNameOnly = Path.GetFileNameWithoutExtension(destinationPath);
                string extension = Path.GetExtension(destinationPath);
                while (File.Exists(destinationPath))
                {
                    destinationFileName = $"{fileNameOnly}_{count}{extension}";
                    destinationPath = Path.Combine(literaturePath, destinationFileName);
                    count++;
                }

                File.Copy(sourcePath, destinationPath);
                Logger.Debug($"Copied '{sourcePath}' to '{destinationPath}'");
                return destinationPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error copying file '{sourcePath}' to literature folder.");
                // Don't show dialog here, let caller handle UI feedback
                return null;
            }
        }

        /// <summary>
        /// Handles changes to the base item collection. Updates the count and triggers save.
        /// </summary>
        private void OnBaseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isDisposed) return; // Double check dispose state
                string searchText = _searchTextBoxBuilder?.GetText() ?? string.Empty;
                ApplyFilterAndSort(searchText);

                _itemCountTextBlockBuilder?.SetText($"Items: {_baseLiteratureItems.Count}");
                // Optionally trigger save immediately on changes
                // SaveLiteratureItemsToDisk(_baseLiteratureItems);
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    foreach (LiteratureSourceItem item in e.NewItems.OfType<LiteratureSourceItem>()) SubscribeToItemChanges(item);
                }


            }, DispatcherPriority.Background);
        }


        // --- Actions (Implement Logic) ---

        private async Task AddSourceAsync()
        {
            // Placeholder: Show dialog/flyout to choose File or URL
            Logger.Debug("Add Source button clicked.");

            // For now, just implement adding a local file
            var options = new FilePickerOpenOptions
            {
                Title = "Select Literature File",
                AllowMultiple = true, // Allow adding multiple files at once
                // Add FileTypeFilter if you want to restrict (e.g., PDFs only)
                // FileTypeFilter = [ new FilePickerFileType("PDF Documents") { Patterns = ["*.pdf"] } ]
            };

            var result = await App.GetMainWindow().StorageProvider.OpenFilePickerAsync(options);

            if (result?.Any() == true)
            {
                var processingTasks = result.Select(file => ProcessDroppedFileAsync(file)).ToList();
                try
                {
                    await Task.WhenAll(processingTasks);
                    Logger.Info($"Finished adding {result.Count} file(s).");
                    // Optionally trigger save
                    // SaveLiteratureItemsToDisk(_baseLiteratureItems);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error occurred while adding selected files.");
                    MessageDialog.ShowWarningDialog($"An error occurred while adding files: {ex.Message}");
                }
            }
            else
            {
                Logger.Debug("Add Source file selection cancelled.");
            }
        }

        // --- Filtering & Sorting ---
        private void ApplyFilterAndSort(string searchText)
        {
            if (_literatureListBuilder?.Entity.IsAlive() != true)
            {
                Logger.Warn("Cannot apply filter/sort, ListBox builder is invalid.");
                return;
            }

            string lowerSearchText = searchText?.ToLowerInvariant() ?? string.Empty;

            IEnumerable<LiteratureSourceItem> filteredItems;
            if (string.IsNullOrWhiteSpace(lowerSearchText))
            {
                filteredItems = _baseLiteratureItems; // No filter
            }
            else
            {
                filteredItems = _baseLiteratureItems.Where(item =>
                {
                    // Check Name
                    if (item.Name?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false) return true;
                    // Check Title
                    if (item.Title?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false) return true;
                    // Check Author
                    if (item.Author?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false) return true;
                    // Check Tags
                    if (item.Tags?.Any(tag => tag?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? false) return true;
                    // Check FilePath (if applicable)
                    if (item is LocalFileSourceItem fileItem && (fileItem.FilePath?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false)) return true;
                    // Check URL (if applicable)
                    if (item is WebSourceItem webItem && (webItem.Url?.Contains(lowerSearchText, StringComparison.InvariantCultureIgnoreCase) ?? false)) return true;

                    return false;
                });
            }

            // TODO: Add Sorting Logic if needed (using _sortComboBoxBuilder)
            // IEnumerable<LiteratureSourceItem> sortedAndFilteredItems = ApplySorting(filteredItems);
            IEnumerable<LiteratureSourceItem> sortedAndFilteredItems = filteredItems.OrderBy(item => item.Name); // Default sort by Name

            // Update the ListBox ItemsSource on the UI thread
            var finalCollection = new ObservableCollection<LiteratureSourceItem>(sortedAndFilteredItems);
            Dispatcher.UIThread.Post(() =>
            {
                if (_literatureListBuilder?.Entity.IsAlive() == true)
                {
                    _literatureListBuilder.SetItemsSource(finalCollection);
                    Logger.Trace($"ListBox updated. Filter: '{searchText}', Items displayed: {finalCollection.Count}");
                }
            }, DispatcherPriority.Background);
        }

        // --- Persistence ---

        /// <summary>
        /// Loads literature items from the JSON save file.
        /// </summary>
        private ObservableCollection<LiteratureSourceItem> LoadLiteratureItemsFromDisk()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), LITERATURE_FOLDER_NAME, LITERATURE_SAVE_FILE);
            if (!File.Exists(filePath))
            {
                Logger.Info($"Literature save file not found ('{filePath}'). Starting with empty collection.");
                return [];
            }

            try
            {
                Logger.Info($"Loading literature items from '{filePath}'...");
                string jsonString = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Match saving format
                                          // *** IMPORTANT: Add converter for abstract base class ***
                                          //Converters = { new LiteratureSourceItemConverter() } // You need to create this converter
                };

                var items = JsonSerializer.Deserialize<ObservableCollection<LiteratureSourceItem>>(jsonString, options);
                Logger.Info($"Successfully loaded {items?.Count ?? 0} literature items.");
                return items ?? [];
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error loading literature items from '{filePath}'. Returning empty collection.");
                MessageDialog.ShowErrorDialog($"Error loading literature data: {ex.Message}\n\nA new empty list will be used.");
                return [];
            }
        }

        /// <summary>
        /// Saves the current literature items to the JSON save file.
        /// </summary>
        public static void SaveLiteratureItemsToDisk(ObservableCollection<LiteratureSourceItem> items)
        {
            if (items == null) return;

            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), LITERATURE_FOLDER_NAME);
            string filePath = Path.Combine(directoryPath, LITERATURE_SAVE_FILE);

            try
            {
                Logger.Info($"Saving {items.Count} literature items to '{filePath}'...");
                Directory.CreateDirectory(directoryPath); // Ensure directory exists

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    // *** IMPORTANT: Add converter for abstract base class ***
                    //Converters = { new LiteratureSourceItemConverter() } // You need to create this converter
                };

                string jsonString = JsonSerializer.Serialize(items, options);
                File.WriteAllText(filePath, jsonString);
                Logger.Info("Successfully saved literature items.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error saving literature items to '{filePath}'.");
                MessageDialog.ShowErrorDialog($"Error saving literature data: {ex.Message}");
            }
        }

        // --- IDisposable Implementation ---
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Disposable
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Logger.Debug($"Disposing LiteraturePage (Root: {_root.Id})...");

                    // Unsubscribe external events
                    if (_baseLiteratureItems != null)
                    {
                        _baseLiteratureItems.CollectionChanged -= OnBaseCollectionChanged;
                    }
                    var mainWindow = App.GetMainWindow();
                    if (mainWindow != null)
                    {
                        mainWindow.Closing -= (_, _) => SaveLiteratureItemsToDisk(_baseLiteratureItems!);
                    }

                    // Dispose internal disposables (includes timer entities if added)
                    _disposables.Dispose();

                    // Destroy root Flecs UI if owned by this page
                    if (_root.IsValid() && _root.IsAlive())
                    {
                        // Flecs OnRemove hooks should handle component disposal
                        _root.Destruct();
                    }

                    Logger.Debug("LiteraturePage disposed.");
                }
                _isDisposed = true;
            }
        }

        // --- IUIComponent Implementation ---
        /// <inheritdoc/>
        public void Attach(Entity parent)
        {
            if (_root.IsValid() && !_root.Has(Ecs.ChildOf, parent))
            {
                _root.ChildOf(parent);
            }
        }

        /// <inheritdoc/>
        public void Detach()
        {
            if (_root.IsValid() && _root.Parent().IsValid())
            {
                _root.Remove(Ecs.ChildOf, _root.Parent());
            }
        }
    }

    /*

    // --- TODO: Create this JsonConverter ---
    // You will need a custom System.Text.Json.Serialization.JsonConverter
    // to handle serializing and deserializing the abstract LiteratureSourceItem
    // and its derived types (LocalFileSourceItem, WebSourceItem, etc.).
    // This converter will typically look at the 'SourceType' property during
    // deserialization to decide which concrete class to instantiate.
    public class LiteratureSourceItemConverter : System.Text.Json.Serialization.JsonConverter<LiteratureSourceItem>
    {
        public override LiteratureSourceItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Implementation needed: Read JSON, determine SourceType, deserialize into the correct derived type.
            throw new NotImplementedException("Deserialization logic for LiteratureSourceItem hierarchy is required.");
        }

        public override void Write(Utf8JsonWriter writer, LiteratureSourceItem value, JsonSerializerOptions options)
        {
            // Implementation needed: Serialize the actual derived type.
            // Using JsonSerializer.Serialize(writer, (object)value, value.GetType(), options)
            // within this method often works if the derived types are simple POCOs
            // or if you remove the converter temporarily from options for the recursive call.
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options); // May need adjusted options
                                                                                       // throw new NotImplementedException("Serialization logic for LiteratureSourceItem hierarchy is required.");
        }

        // Optional: Add CanConvert if needed, though usually not required if applied directly
        // public override bool CanConvert(Type typeToConvert) => typeof(LiteratureSourceItem).IsAssignableFrom(typeToConvert);
    }
    */
}
