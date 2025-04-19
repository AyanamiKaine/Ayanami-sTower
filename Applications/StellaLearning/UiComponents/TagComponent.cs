using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Flecs.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Flecs.NET.Core;
using System.Reactive.Disposables;
using NLog;
using Avalonia.Threading;
using AyanamisTower.StellaLearning.Data;
using Avalonia; // For Dispatcher.UIThread

namespace AyanamisTower.StellaLearning.UiComponents // Adjust namespace if needed
{
    /// <summary>
    /// A reusable UI component for managing a list of tags with auto-completion.
    /// Allows adding tags via text input and removing existing tags.
    /// Suggests existing tags from the main SpacedRepetitionItem collection.
    /// </summary>
    public class TagComponent : IUIComponent, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly CompositeDisposable _disposables = [];
        private bool _isDisposed = false;
        private UIBuilder<AutoCompleteBox>? _tagInputAutoCompleteBox; // Changed from TextBox
        /// <summary>
        /// The underlying collection of tags *for this specific instance* managed by this component.
        /// </summary>
        public ObservableCollection<string> Tags { get; } = [];


        /*
        TODO: We want to reuse the unique tags for everything not just spaced repetition items, so 
        we can reuse the tags for art and literature based items. Probably we can define an tags interface
        that implements a list of tags. That also ensure the list only holds unique values etc.
        */
        /// <summary>
        /// Collection holding all unique tags found across all SpacedRepetitionItems.
        /// Used as the source for AutoCompleteBox suggestions.
        /// </summary>
        private readonly ObservableCollection<string> _allUniqueTags = [];
        /// <summary>
        /// Reference to the main collection of all spaced repetition items.
        /// </summary>
        private readonly ObservableCollection<SpacedRepetitionItem>? _allSpacedRepetitionItems; // Make nullable
        private readonly ObservableCollection<LiteratureSourceItem>? _allLiteratureSourceItems; // Make nullable

        /// <inheritdoc/>
        public Entity Root { get; }
        // --- Primary Constructor (Existing) ---
        /// <summary>
        /// Creates a new Tag Management UI Component with auto-completion, initially empty.
        /// </summary>
        /// <param name="world">The Flecs world, expected to contain ObservableCollection&lt;SpacedRepetitionItem&gt;.</param>
        public TagComponent(World world) : this(world, null) // Chain to the new constructor with null initial tags
        {
            // Primary constructor logic is now mostly in the new constructor
            // Or keep the common logic here and call common init methods
        }

        /// <summary>
        /// Creates a new Tag Management UI Component with auto-completion, optionally initializing with existing tags.
        /// </summary>
        /// <param name="world">The Flecs world, expected to contain ObservableCollection&lt;SpacedRepetitionItem&gt;.</param>
        /// <param name="initialTags">An optional collection of tags to pre-populate the component.</param>
        public TagComponent(World world, IEnumerable<string>? initialTags)
        {
            // --- Get Data & Setup AutoComplete Source ---
            try
            {
                _allSpacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();
                _allLiteratureSourceItems = world.Get<ObservableCollection<LiteratureSourceItem>>();

                UpdateAllUniqueTags(); // Populate suggestions source
                _allSpacedRepetitionItems.CollectionChanged += OnAllItemsChanged;
                _disposables.Add(Disposable.Create(() =>
                {
                    if (_allSpacedRepetitionItems != null) _allSpacedRepetitionItems.CollectionChanged -= OnAllItemsChanged;
                    Logger.Debug("Unsubscribed from _allSpacedRepetitionItems.CollectionChanged");
                }));

                _allLiteratureSourceItems.CollectionChanged += OnAllItemsChanged;
                _disposables.Add(Disposable.Create(() =>
                {
                    if (_allLiteratureSourceItems != null) _allLiteratureSourceItems.CollectionChanged -= OnAllItemsChanged;
                    Logger.Debug("Unsubscribed from _allLiteratureSourceItems.CollectionChanged");
                }));
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to get SpacedRepetitionItem collection. Auto-completion source empty.");
                _allSpacedRepetitionItems = null;
            }

            // --- Initialize Component's Tags ---
            // Clear any defaults and add initial tags if provided
            Tags.Clear(); // Ensure Tags starts empty before adding initial ones
            if (initialTags != null)
            {
                foreach (var tag in initialTags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Tags.Add(tag.Trim());
                }
                Logger.Debug($"Initialized TagComponent with {Tags.Count} tags.");
            }
            // --- Define Tag Item Template (Unchanged) ---
            var tagItemTemplate = world.CreateTemplate<string, Border>((borderBuilder, tagText) =>
            {
                // ... (Identical to previous version) ...
                borderBuilder
                    .SetMargin(2)
                    .SetPadding(5, 2)
                    .SetCornerRadius(new CornerRadius(4))
                    .SetBackground(Brushes.LightGray)
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

                        stackPanel.Child<Button>(removeButton =>
                        {
                            removeButton
                                .SetText("x")
                                .SetPadding(5, 0)
                                .SetFontSize(10)
                                .SetVerticalAlignment(VerticalAlignment.Center)
                                .SetHorizontalAlignment(HorizontalAlignment.Center)
                                .SetBorderThickness(new Thickness(0))
                                .OnClick((_, _) => Tags.Remove(tagText));
                        });
                    });
            });


            // --- Build the main component UI (Same as before) ---
            Root = world.UI<StackPanel>(rootPanel =>
            {
                rootPanel.SetOrientation(Orientation.Vertical).SetSpacing(5);
                rootPanel.Child<Grid>(inputGrid =>
                {
                    inputGrid.SetColumnDefinitions("*,Auto").SetRowDefinitions("Auto");
                    inputGrid.Child<AutoCompleteBox>(autoCompleteBox =>
                    {
                        _tagInputAutoCompleteBox = autoCompleteBox;
                        autoCompleteBox.SetColumn(0).SetWatermark("Add Tag...") // Added Watermark here
                           .With(acb =>
                           {
                               acb.ItemsSource = _allUniqueTags;
                               acb.FilterMode = AutoCompleteFilterMode.StartsWith;
                           })
                           .OnKeyDown((sender, args) =>
                           {
                               if (sender is AutoCompleteBox acb && (args.Key == Key.Enter || args.Key == Key.Tab))
                               {
                                   AddTagFromInput(acb.Text);
                                   args.Handled = true;
                               }
                           });
                    });
                    inputGrid.Child<Button>(button =>
                    {
                        button.SetText("Add").SetColumn(1).SetMargin(5, 0, 0, 0)
                           .OnClick((_, _) =>
                           {
                               if (_tagInputAutoCompleteBox?.Entity.IsAlive() == true)
                               {
                                   AddTagFromInput(_tagInputAutoCompleteBox.Get<AutoCompleteBox>().Text);
                               }
                           });
                    });
                });
                rootPanel.Child<ItemsControl>(itemsControl =>
                {
                    itemsControl.SetItemsSource(Tags).SetItemTemplate(tagItemTemplate)
                       .With(ic => ic.ItemsPanel = new FuncTemplate<Panel>(() => new WrapPanel { Orientation = Orientation.Horizontal })!);
                });
            }).Entity;

            // --- Disposables & Setup ---
            _disposables.Add(Disposable.Create(() =>
            {
                if (Root.IsValid() && Root.IsAlive())
                {
                    Logger.Debug($"Disposing TagComponent Root Entity: {Root.Id}");
                    Root.Destruct();
                }
                else
                {
                    Logger.Warn("Root entity invalid or dead during TagComponent Dispose.");
                }
            }));

            Root.SetName($"TAGCOMPONENT_AC-{new Random().Next()}");
            Logger.Info($"TagComponent (AutoComplete Corrected) created with Root Entity: {Root.Id}");
        }


        /// <summary>
        /// Rebuilds the _allUniqueTags collection based on the current _allItems.
        /// Should be called on initialization and when _allItems changes.
        /// Uses Dispatcher to ensure UI thread safety for modifying _allUniqueTags.
        /// </summary>
        private async void UpdateAllUniqueTags()
        {
            if (_allSpacedRepetitionItems is null || _allLiteratureSourceItems is null)
            {
                Logger.Warn("Cannot update unique tags, _allItems collection is null.");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_allUniqueTags.Any()) _allUniqueTags.Clear();
                });
                return;
            }

            Logger.Debug("Updating unique tags list...");
            // Using HashSet for efficient unique collection, ignoring case
            var uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Iterate safely - consider locking _allItems if modifications can happen on other threads,
            // but ObservableCollection is generally safe for reads if changes happen via CollectionChanged on UI thread.
            try
            {
                foreach (var item in _allSpacedRepetitionItems) // Assuming SpacedRepetitionItem and Tags list are accessible
                {
                    if (item?.Tags != null) // Check item and Tags list for null
                    {
                        foreach (var tag in item.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                uniqueTags.Add(tag.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch potential issues during iteration, e.g., if item becomes invalid unexpectedly
                Logger.Error(ex, "Error occurred while iterating through _allItems to collect unique tags.");
                return; // Abort update if iteration fails
            }

            try
            {
                foreach (var item in _allLiteratureSourceItems) // Assuming SpacedRepetitionItem and Tags list are accessible
                {
                    if (item?.Tags != null) // Check item and Tags list for null
                    {
                        foreach (var tag in item.Tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                uniqueTags.Add(tag.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch potential issues during iteration, e.g., if item becomes invalid unexpectedly
                Logger.Error(ex, "Error occurred while iterating through _allItems to collect unique tags.");
                return; // Abort update if iteration fails
            }


            var sortedTags = uniqueTags.Order().ToList(); // Sort alphabetically

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Efficiently update the ObservableCollection on the UI thread
                    foreach (var tagToRemove in _allUniqueTags.Except(sortedTags).ToList()) _allUniqueTags.Remove(tagToRemove);

                    foreach (var tagToAdd in sortedTags.Except(_allUniqueTags).ToList()) _allUniqueTags.Add(tagToAdd); // Add new tags

                    Logger.Debug($"Unique tags list updated. Count: {_allUniqueTags.Count}");
                }
                catch (Exception uiEx)
                {
                    Logger.Error(uiEx, "Error occurred while updating _allUniqueTags collection on UI thread.");
                }
            });
        }

        /// <summary>
        /// Handles changes in the main _allItems collection to update the unique tag list.
        /// </summary>
        private void OnAllItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // It's generally safer to schedule the update via Dispatcher
            // in case the CollectionChanged event isn't raised on the UI thread.
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Logger.Debug("Main item collection changed, queuing unique tags update.");
                UpdateAllUniqueTags();
            });
        }

        /// <summary>
        /// Helper method to add a tag from the input AutoCompleteBox.
        /// </summary>
        /// <param name="tagToAdd">The raw text/tag to add.</param>
        private void AddTagFromInput(string? tagToAdd)
        {
            string cleanTag = tagToAdd?.Trim() ?? string.Empty;
            bool added = false;

            if (!string.IsNullOrWhiteSpace(cleanTag) && !Tags.Any(t => t.Equals(cleanTag, StringComparison.OrdinalIgnoreCase))) // Use LINQ Any for case-insensitive check
            {
                Tags.Add(cleanTag);
                Logger.Debug($"Tag added: {cleanTag}");
                added = true;
            }
            else if (string.IsNullOrWhiteSpace(cleanTag))
            {
                Logger.Debug("Attempted to add empty tag.");
            }
            else // Tag already exists (case-insensitive)
            {
                Logger.Debug($"Attempted to add duplicate tag: {cleanTag}");
            }

            // Clear input only if tag was successfully added
            if (added && _tagInputAutoCompleteBox?.Entity.IsAlive() == true)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_tagInputAutoCompleteBox.Entity.IsAlive())
                    { // Double check validity inside dispatcher
                        _tagInputAutoCompleteBox.Get<AutoCompleteBox>().Text = string.Empty;
                    }
                });
            }
            // Optional: Decide if you want to clear input even if adding failed (e.g., duplicate)
            // else if (!added && _tagInputAutoCompleteBox != null && _tagInputAutoCompleteBox.Entity.IsAlive()) {
            //    Dispatcher.UIThread.InvokeAsync(() => _tagInputAutoCompleteBox.Get<AutoCompleteBox>().Text = string.Empty);
            // }
        }


        /// <summary>
        /// Clears all tags *from this component instance*.
        /// </summary>
        public void ClearTags()
        {
            if (Tags.Any())
            {
                Tags.Clear();
                Logger.Debug("Component tags cleared.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Logger.Debug($"Disposing TagComponent (AutoComplete Corrected) (Root: {Root.Id})...");
                    // Unsubscribe external event first is safer
                    if (_allSpacedRepetitionItems != null)
                    {
                        _allSpacedRepetitionItems.CollectionChanged -= OnAllItemsChanged;
                    }
                    // Dispose Flecs entities and internal handlers
                    _disposables.Dispose();
                    // Clear local collections
                    Tags.Clear();
                    _allUniqueTags.Clear();
                    Logger.Debug($"TagComponent (AutoComplete Corrected) disposed (Root: {Root.Id}).");
                }
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~TagComponent() { Dispose(disposing: false); }
    }
}