using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.StellaLearning.Data;
using Flecs.NET.Core;
using System.Reactive.Disposables;
using NLog;
using Avalonia.Layout; // Added for Layout enums
using Avalonia.Threading;
using System.Collections.Generic; // Added for Dispatcher

namespace Avalonia.Flecs.StellaLearning.UiComponents
{
    /// <summary>
    /// UI component to determine a new item's priority via binary comparison.
    /// </summary>
    public class ComparePriority : IUIComponent, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ObservableCollection<SpacedRepetitionItem> _spacedRepetitionItems;
        private readonly Random _rng = new();
        private readonly CompositeDisposable _disposables = [];
        private bool _isDisposed = false;

        private const int MAX_COMPARISONS = 3; // Number of comparisons to perform
        private const long MAX_PRIORITY_VALUE = 999_999_999L; // Consistent max value
        private const long MIN_PRIORITY_VALUE = 0L;          // Consistent min value

        // State for the comparison process
        private int _timesCompared = 0;
        private long _internalSmallestPrio; // Use long internally for calculations
        private long _internalHighestPrio;  // Use long internally
        private SpacedRepetitionItem? _currentItemToCompare = null;
        private readonly List<Guid> _comparedItemIdsThisRound = []; // Assuming SpacedRepetitionItem has a long Id

        // UI Element Builders References (optional but good practice)
        private UIBuilder<Button>? _morePriorityButton;
        private UIBuilder<Button>? _lessPriorityButton;
        private UIBuilder<TextBlock>? _itemToCompareToTextBlock;
        private UIBuilder<TextBlock>? _statusTextBlock; // Optional: To show status like "Comparison 1 of 3"

        /// <summary>
        /// Returns the entity that holds the final calculated priority (as an int).
        /// </summary>
        public Entity CalculatedPriorityEntity { get; }

        /// <inheritdoc/>
        public Entity Root { get; }

        /// <summary>
        /// Creates a ComparePriority component.
        /// </summary>
        /// <param name="world">The Flecs world.</param>
        public ComparePriority(World world)
        {
            try
            {
                _spacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get ObservableCollection<SpacedRepetitionItem> from world.");
                // Handle error state - perhaps disable component?
                _spacedRepetitionItems = new ObservableCollection<SpacedRepetitionItem>(); // Use empty list to prevent crashes
            }

            // Entity to store the final calculated priority (as int)
            CalculatedPriorityEntity = world.Entity().Set(0); // Default value

            Root = world.UI<Grid>((grid) =>
            {
                grid
                    .SetColumnDefinitions("*,*")
                    // Add extra row for status text
                    .SetRowDefinitions("Auto, Auto, *, Auto"); // Title, Status, Item, Buttons

                // Title TextBlock
                grid.Child<TextBlock>(tb => tb
                    .SetTextWrapping(Media.TextWrapping.Wrap)
                    .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                    .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                    .SetRow(0).SetColumnSpan(2).SetMargin(5)
                    .SetText("Is the new item more or less important than this one?")
                );

                // Status TextBlock (e.g., "Comparison 1 of 3")
                grid.Child<TextBlock>(tb =>
                {
                    _statusTextBlock = tb;
                    tb.SetRow(1).SetColumnSpan(2)
                      .SetHorizontalAlignment(HorizontalAlignment.Center)
                      .SetFontSize(11)
                      .SetOpacity(0.8);
                    // Text set dynamically in SelectNextItemToCompare
                });


                // Item to Compare TextBlock
                grid.Child<TextBlock>(text =>
                {
                    _itemToCompareToTextBlock = text;
                    text.SetVerticalAlignment(Layout.VerticalAlignment.Center)
                        .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                        .SetMargin(10)
                        .SetRow(2) // Moved to row 2
                        .SetColumnSpan(2)
                        .SetTextWrapping(Media.TextWrapping.Wrap)
                        .SetFontWeight(Media.FontWeight.Bold);
                    // Initial text set by Reset -> SelectNextItemToCompare
                });

                // Less Priority Button
                grid.Child<Button>(button =>
                {
                    _lessPriorityButton = button;
                    button.SetText("Less Important") // More descriptive
                          .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch) // Stretch
                          .SetMargin(10, 5, 5, 10) // Margins
                          .SetColumn(0).SetRow(3); // Moved to row 3
                    button.OnClick((_, _) => HandleLessPriorityClick());
                });

                // More Priority Button
                grid.Child<Button>(button =>
                {
                    _morePriorityButton = button;
                    button.SetText("More Important") // More descriptive
                          .SetHorizontalAlignment(Layout.HorizontalAlignment.Stretch) // Stretch
                          .SetMargin(5, 5, 10, 10) // Margins
                          .SetColumn(1).SetRow(3); // Moved to row 3
                    button.OnClick((_, _) => HandleMorePriorityClick());
                });
            }).Entity;

            // --- Initial Setup & Event Subscriptions ---
            Reset(); // Set initial state and select first item

            _spacedRepetitionItems.CollectionChanged += OnCollectionChanged;
            _disposables.Add(Disposable.Create(() =>
            {
                // Check if collection still exists before unsubscribing
                var items = world.Get<ObservableCollection<SpacedRepetitionItem>>();
                if (items != null) items.CollectionChanged -= OnCollectionChanged;
                Logger.Debug("Unsubscribed from _spacedRepetitionItems.CollectionChanged");
            }));

            _disposables.Add(Disposable.Create(() =>
            {
                if (CalculatedPriorityEntity.IsAlive()) CalculatedPriorityEntity.Destruct(); // Clean up entity
                if (Root.IsValid() && Root.IsAlive()) Root.Destruct(); // Clean up UI
            }));

            Root.SetName($"COMPAREPRIORITY-{_rng.Next()}");
        }

        /// <summary>
        /// Resets the priority comparison state and selects the first item.
        /// </summary>
        public void Reset()
        {
            if (_isDisposed) return; // Don't reset if disposed

            _timesCompared = 0;
            _internalSmallestPrio = MIN_PRIORITY_VALUE;
            _internalHighestPrio = MAX_PRIORITY_VALUE;
            _currentItemToCompare = null; // Clear current item

            Logger.Debug("ComparePriority reset.");
            // Select the first item (this handles UI updates like enabling buttons/setting text)
            SelectNextItemToCompare();
        }

        /// <summary>
        /// Selects the next item to compare against using the revised random strategy.
        /// Updates the UI and handles completion/error states.
        /// </summary>
        private void SelectNextItemToCompare()
        {
            if (_isDisposed) return;
            if (!AreUIElementsValid()) return; // Check UI elements first

            // Update Status Text
            if (_timesCompared < MAX_COMPARISONS)
            {
                _statusTextBlock?.SetText($"Comparison {_timesCompared + 1} of {MAX_COMPARISONS}");
            }
            else
            {
                _statusTextBlock?.SetText("Comparison Complete");
            }

            // 1. Check for Completion Conditions
            if (_timesCompared >= MAX_COMPARISONS)
            {
                FinalizeComparison("Comparison limit reached.");
                return;
            }
            // Only check range collapse *after* attempting selection if needed
            // if (_internalSmallestPrio >= _internalHighestPrio - 1) ...

            if (_spacedRepetitionItems == null || _spacedRepetitionItems.Count == 0)
            {
                FinalizeComparison("No items available to compare.");
                _currentItemToCompare = null;
                return;
            }

            // 2. --- Revised Item Selection Logic ---
            SpacedRepetitionItem? candidateItem = null;
            List<SpacedRepetitionItem> availableItems;

            if (_timesCompared == 0) // First comparison: Select randomly from ALL available items
            {
                availableItems = _spacedRepetitionItems
                    .Where(i => i != null) // Basic null check
                                           // No need to check _comparedItemIdsThisRound here as it's empty
                    .ToList();
                Logger.Trace($"Selecting initial random item from {availableItems.Count} total items.");
            }
            else // Subsequent comparisons: Select randomly from items WITHIN range, not yet compared this round
            {
                // Find items STRICTLY between the current bounds
                availableItems = _spacedRepetitionItems
                    .Where(i => i != null &&
                                i.Priority > _internalSmallestPrio &&
                                i.Priority < _internalHighestPrio &&
                                !_comparedItemIdsThisRound.Contains(i.Uid)) // Exclude already shown items
                    .ToList();
                Logger.Trace($"Selecting subsequent random item from {availableItems.Count} items in range ({_internalSmallestPrio} - {_internalHighestPrio}).");
            }

            // 3. Choose Randomly from Available Candidates
            if (availableItems.Count > 0)
            {
                candidateItem = availableItems[_rng.Next(availableItems.Count)];
            }
            else
            {
                // No suitable items found (either list empty initially, or range empty/all shown)
                if (_timesCompared == 0)
                {
                    FinalizeComparison("No items available to compare.");
                }
                else
                {
                    Logger.Warn($"No *new* items found strictly within range ({_internalSmallestPrio} - {_internalHighestPrio}). Finalizing early.");
                    FinalizeComparison("No suitable items left for comparison in range.");
                }
                return; // Exit selection process
            }

            // 4. Process Selected Item
            _currentItemToCompare = candidateItem;
            _comparedItemIdsThisRound.Add(_currentItemToCompare.Uid); // Add to history for this round

            // 5. Update UI
            string currentItemName = _currentItemToCompare.Name ?? "Unnamed Item";
            _itemToCompareToTextBlock!.SetText(currentItemName);
            _morePriorityButton!.Enable();
            _lessPriorityButton!.Enable();
            Logger.Debug($"Next comparison item selected: {currentItemName} (Prio: {_currentItemToCompare.Priority}, ID: {_currentItemToCompare.Uid}). Comparison #{_timesCompared + 1}");
        }

        private void HandleLessPriorityClick()
        {
            if (_isDisposed || _currentItemToCompare == null) { /* ... Log/Recover ... */ return; }
            _internalHighestPrio = _currentItemToCompare.Priority;
            _timesCompared++;
            Logger.Debug($"Less Priority clicked. New range: [{_internalSmallestPrio} - {_internalHighestPrio}]. Comparisons: {_timesCompared}");
            SelectNextItemToCompare();
        }

        private void HandleMorePriorityClick()
        {
            if (_isDisposed || _currentItemToCompare == null) { /* ... Log/Recover ... */ return; }
            _internalSmallestPrio = _currentItemToCompare.Priority;
            _timesCompared++;
            Logger.Debug($"More Priority clicked. New range: [{_internalSmallestPrio} - {_internalHighestPrio}]. Comparisons: {_timesCompared}");
            SelectNextItemToCompare();
        }

        // --- FinalizeComparison (No changes needed from previous version) ---
        // Calculates midpoint of final range + offset + clamp + collision check
        private void FinalizeComparison(string reason)
        {
            if (_isDisposed) return;
            Logger.Info($"Finalizing priority comparison. Reason: {reason}");

            // 1. Check if range is valid before calculating midpoint
            if (_internalSmallestPrio > _internalHighestPrio)
            {
                Logger.Error($"Invalid priority range detected in FinalizeComparison: Small={_internalSmallestPrio}, High={_internalHighestPrio}. Resetting to default midpoint.");
                _internalSmallestPrio = MIN_PRIORITY_VALUE; // Reset to avoid nonsensical calculation
                _internalHighestPrio = MAX_PRIORITY_VALUE;
            }
            else if (_internalSmallestPrio == _internalHighestPrio)
            {
                // If range collapsed to a single value, nudge slightly to allow insertion
                _internalHighestPrio++; // Allow a tiny range
            }


            // 2. Calculate Final Priority (Midpoint + Random Offset)
            long finalPriorityLong = (_internalSmallestPrio + _internalHighestPrio) / 2;
            int offset = _rng.Next(-50, 51); // Range -50 to +50 inclusive
            finalPriorityLong += offset;

            // 3. Clamp to Valid Range
            finalPriorityLong = Math.Clamp(finalPriorityLong, MIN_PRIORITY_VALUE, MAX_PRIORITY_VALUE);

            // 4. Prevent Exact Collision with Boundaries
            if (finalPriorityLong <= _internalSmallestPrio && _internalSmallestPrio < _internalHighestPrio) finalPriorityLong = _internalSmallestPrio + 1;
            else if (finalPriorityLong >= _internalHighestPrio && _internalHighestPrio > _internalSmallestPrio) finalPriorityLong = _internalHighestPrio - 1;

            // 5. Re-Clamp and Cast to int
            int finalPriorityInt = (int)Math.Clamp(finalPriorityLong, MIN_PRIORITY_VALUE, MAX_PRIORITY_VALUE);

            // 6. Set the Entity Value
            if (CalculatedPriorityEntity.IsAlive())
            {
                CalculatedPriorityEntity.Set(finalPriorityInt);
                Logger.Info($"Final calculated priority set to: {finalPriorityInt} (Range: [{_internalSmallestPrio} - {_internalHighestPrio}], BaseMid: {(_internalSmallestPrio + _internalHighestPrio) / 2}, Offset: {offset})");
            }
            else
            {
                Logger.Error("CalculatedPriorityEntity is not alive! Cannot set final priority.");
            }

            // 7. Update UI to Final State
            if (AreUIElementsValid())
            {
                _itemToCompareToTextBlock!.SetText(reason);
                _lessPriorityButton!.Disable();
                _morePriorityButton!.Disable();
                _statusTextBlock?.SetText("Comparison Complete");
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isDisposed) return;
            Logger.Info("Item collection changed, resetting ComparePriority state.");
            Dispatcher.UIThread.InvokeAsync(Reset);
        }



        /// <summary>
        /// Checks if essential UI element builders are still valid.
        /// </summary>
        private bool AreUIElementsValid()
        {
            bool valid = _lessPriorityButton?.Entity.IsAlive() == true &&
                         _morePriorityButton?.Entity.IsAlive() == true &&
                         _itemToCompareToTextBlock?.Entity.IsAlive() == true &&
                         _statusTextBlock?.Entity.IsAlive() == true; // Check status block too
            if (!valid) Logger.Error("One or more ComparePriority UI elements are not valid/alive.");
            return valid;
        }


        #region IDisposable Implementation
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Diposing managed ressources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Logger.Debug($"Disposing ComparePriority (Root: {Root.Id})...");
                    // Dispose managed state (managed objects).
                    _disposables.Dispose(); // Unsubscribes events, destroys entities added to it
                                            // Set fields to null to aid GC? Optional.
                    _currentItemToCompare = null;
                    _lessPriorityButton = null;
                    _morePriorityButton = null;
                    _itemToCompareToTextBlock = null;
                    _statusTextBlock = null;
                }
                // Free unmanaged resources (unmanaged objects) and override finalizer
                _isDisposed = true;
                Logger.Debug("ComparePriority disposed.");
            }
        }

        // No finalizer needed as we only have managed resources and Flecs entities (handled by _disposables)
        // ~ComparePriority() { Dispose(disposing: false); }
        #endregion
    }
}