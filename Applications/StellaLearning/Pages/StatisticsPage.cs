/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using AyanamisTower.StellaLearning.Data; // Required for StatsTracker, SpacedRepetitionItem, etc.
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Flecs.NET.Core;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia;

namespace AyanamisTower.StellaLearning.Pages; // Adjust namespace if needed

/// <summary>
/// Represents the Statistics Page displaying learning metrics.
/// </summary>
public class StatisticsPage : IUIComponent, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly CompositeDisposable _disposables = [];
    private bool _isDisposed = false;

    private readonly StatsTracker _statsTracker;
    private readonly ObservableCollection<SpacedRepetitionItem>? _allItems; // Needed for forecast/difficult items

    /// <inheritdoc/>
    public Entity Root { get; }

    // --- Control References for Updates ---
    private UIBuilder<TextBlock>? _totalStudyTimeTextBlock;
    private UIBuilder<TextBlock>? _totalReviewsTextBlock;
    private UIBuilder<TextBlock>? _overallAccuracyTextBlock;
    private UIBuilder<TextBlock>? _currentStreakTextBlock;
    private UIBuilder<TextBlock>? _longestStreakTextBlock;
    private UIBuilder<TextBlock>? _lastUpdatedTextBlock;
    private UIBuilder<ItemsControl>? _itemTypeStatsItemsControl;
    private UIBuilder<ItemsControl>? _tagStatsItemsControl;
    // Add references for Forecast and Difficult Items if implemented

    /// <summary>
    /// Creates the Statistics Page.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    public StatisticsPage(World world)
    {
        _statsTracker = StatsTracker.Instance;

        // Try to get the main item list (needed for some stats functions)
        try
        {
            _allItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();
            Logger.Info("Successfully retrieved SpacedRepetitionItem collection for Stats Page.");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to get SpacedRepetitionItem collection in Stats Page. Some stats might be unavailable.");
            _allItems = null;
        }


        // --- Define Data Templates ---

        // Template for Daily Stats
        var dailyStatsTemplate = world.CreateTemplate<DailyStats, Border>((borderBuilder, dailyStat) =>
        {
            borderBuilder
                .SetPadding(5)
                .SetMargin(0, 2)
                .SetBorderBrush(Brushes.LightGray) // Default
                .SetBorderThickness(new Thickness(0, 0, 0, 1)) // Bottom border
                .Child<Grid>(grid =>
                {
                    grid.SetColumnDefinitions("Auto, *, Auto, Auto") // Date, Spacer, Reviews, Accuracy, Time
                        .SetRowDefinitions("Auto");

                    // Date
                    grid.Child<TextBlock>(tb => tb.SetColumn(0).SetText($"{dailyStat.Date:yyyy-MM-dd}").SetVerticalAlignment(VerticalAlignment.Center));

                    // Reviews
                    grid.Child<TextBlock>(tb => tb.SetColumn(2).SetText($"Reviews: {dailyStat.TotalReviews}").SetMargin(10, 0).SetVerticalAlignment(VerticalAlignment.Center));

                    // Accuracy
                    grid.Child<TextBlock>(tb => tb.SetColumn(3).SetText($"Accuracy: {dailyStat.Accuracy:F1}%").SetMargin(10, 0).SetVerticalAlignment(VerticalAlignment.Center));

                    // Time
                    grid.Child<TextBlock>(tb => tb.SetColumn(4).SetText($"Time: {dailyStat.TotalTimeMinutes} min").SetMargin(10, 0).SetVerticalAlignment(VerticalAlignment.Center));
                });
        });

        // Template for Item Type Stats (using KeyValuePair)
        var itemTypeStatsTemplate = world.CreateTemplate<KeyValuePair<SpacedRepetitionItemType, ItemTypeStats>, Grid>((gridBuilder, kvp) =>
        {
            var itemType = kvp.Key;
            var stats = kvp.Value;

            gridBuilder
                .SetColumnDefinitions("*, Auto, Auto") // Type, Reviews, Accuracy
                .SetMargin(0, 1);

            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(0).SetText(itemType.ToString()).SetFontWeight(FontWeight.Bold));
            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(1).SetText($"Reviews: {stats.TotalReviews}").SetMargin(10, 0));
            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(2).SetText($"Accuracy: {stats.AccuracyRate:F1}%").SetHorizontalAlignment(HorizontalAlignment.Right));
        });

        // Template for Tag Stats (using KeyValuePair)
        var tagStatsTemplate = world.CreateTemplate<KeyValuePair<string, TagStats>, Grid>((gridBuilder, kvp) =>
        {
            var tag = kvp.Key;
            var stats = kvp.Value;

            gridBuilder
                .SetColumnDefinitions("*, Auto, Auto") // Tag, Reviews, Accuracy
                .SetMargin(0, 1);

            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(0).SetText(tag).SetFontWeight(FontWeight.Bold));
            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(1).SetText($"Reviews: {stats.TotalReviews}").SetMargin(10, 0));
            gridBuilder.Child<TextBlock>(tb => tb.SetColumn(2).SetText($"Accuracy: {stats.AccuracyRate:F1}%").SetHorizontalAlignment(HorizontalAlignment.Right));
        });


        // --- Build the UI ---
        Root = world.UI<ScrollViewer>(scrollViewer =>
        {
            scrollViewer.Child<StackPanel>(mainPanel =>
            {
                mainPanel
                .SetSpacing(15) // Add some padding around the page content
                .SetMargin(5, 5, 10, 5);

                // --- Overall Stats Section ---
                mainPanel.Child<TextBlock>(header => header.SetText("Overall Statistics").SetFontSize(18).SetFontWeight(FontWeight.Bold));
                mainPanel.Child<Border>(border => // Wrap in border for visual separation
                {
                    var settings = world.Get<Settings>();

                    if (settings.IsDarkMode)
                    {
                        border.SetBackground(new SolidColorBrush(new Color(255, 45, 45, 45)));
                    }
                    else
                    {
                        border.SetBackground(Brushes.WhiteSmoke);
                    }

                    border.SetPadding(10).SetCornerRadius(5); // Light background
                    border.Child<StackPanel>(overallStatsPanel =>
                    {
                        overallStatsPanel.SetSpacing(5);

                        _totalStudyTimeTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Total Study Time: {FormatTime((long)(_statsTracker.TotalStudyTimeSeconds / 60))}"));
                        _totalReviewsTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Total Reviews: {_statsTracker.TotalReviews}"));
                        _overallAccuracyTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Overall Accuracy: {_statsTracker.OverallAccuracy:F1}%"));
                        _currentStreakTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Current Streak: {_statsTracker.CurrentStreak} days"));
                        _longestStreakTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Longest Streak: {_statsTracker.LongestStreak} days"));
                        _lastUpdatedTextBlock = overallStatsPanel.Child<TextBlock>(tb => tb.SetText($"Last Updated: {_statsTracker.LastUpdated:g}").SetFontSize(10).SetForeground(Brushes.Gray).SetMargin(0, 5, 0, 0));
                    });

                    settings.PropertyChanged += (sender, e) =>
                    {
                        if (e.PropertyName == nameof(Settings.IsDarkMode))
                        {
                            if (settings.IsDarkMode)
                            {
                                border.SetBackground(new SolidColorBrush(new Color(255, 45, 45, 45)));
                            }
                            else
                            {
                                border.SetBackground(Brushes.WhiteSmoke);
                            }
                        }
                    };
                });

                // --- Item Type Stats Section ---
                mainPanel.Child<TextBlock>(header => header.SetText("Performance by Type").SetFontSize(16).SetFontWeight(FontWeight.Bold).SetMargin(0, 15, 0, 5));
                _itemTypeStatsItemsControl = mainPanel.Child<ItemsControl>(ic =>
                {
                    // Bind to the dictionary directly. The template handles KeyValuePair.
                    ic.SetItemsSource(_statsTracker.ItemTypeStats.OrderBy(kvp => kvp.Key.ToString()))
                      .SetItemTemplate(itemTypeStatsTemplate);
                });

                // --- Tag Stats Section ---
                mainPanel.Child<TextBlock>(header => header.SetText("Performance by Tag").SetFontSize(16).SetFontWeight(FontWeight.Bold).SetMargin(0, 15, 0, 5));
                _tagStatsItemsControl = mainPanel.Child<ItemsControl>(ic =>
                {
                    // Bind to the dictionary directly.
                    ic.SetItemsSource(_statsTracker.TagStats.OrderBy(kvp => kvp.Key))
                      .SetItemTemplate(tagStatsTemplate);
                    // Optional: Set MaxHeight if the list can get very long
                    // ic.SetMaxHeight(400);
                });

                // --- Placeholder for Forecast Section ---
                // TODO: Implement if needed, requires _allItems
                // mainPanel.Child<TextBlock>(header => header.SetText("Review Forecast (Next 14 Days)").SetFontSize(16).SetFontWeight(FontWeight.Bold).SetMargin(0, 15, 0, 5));
                // ... Add ItemsControl for forecast ...

                // --- Placeholder for Difficult Items Section ---
                // TODO: Implement if needed, requires _allItems
                // mainPanel.Child<TextBlock>(header => header.SetText("Most Difficult Items").SetFontSize(16).SetFontWeight(FontWeight.Bold).SetMargin(0, 15, 0, 5));
                // ... Add ItemsControl for difficult items ...

            });
        })
        .Add<Page>() // Add Page tag if necessary for navigation/styling
        .Entity;

        // --- Subscribe to StatsTracker Property Changes ---
        _statsTracker.PropertyChanged += StatsTracker_PropertyChanged;
        _disposables.Add(Disposable.Create(() => _statsTracker.PropertyChanged -= StatsTracker_PropertyChanged));

        // --- Add Default Styling (Optional, based on SpacedRepetitionPage example) ---
        //Root.AddDefaultStyling((statsPage) => { /* Add styling adjustments if needed */ });

        Logger.Info("StatisticsPage created successfully.");
    }

    /// <summary>
    /// Handles property changes in the StatsTracker to update the UI.
    /// </summary>
    private void StatsTracker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Ensure updates happen on the UI thread
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isDisposed) return; // Don't update if disposed

            Logger.Trace($"StatsTracker property changed: {e.PropertyName}");

            try
            {
                switch (e.PropertyName)
                {
                    case nameof(StatsTracker.TotalStudyTimeSeconds):
                        _totalStudyTimeTextBlock?.SetText($"Total Study Time: {FormatTime((long)(_statsTracker.TotalStudyTimeSeconds / 60))}");
                        break;
                    case nameof(StatsTracker.TotalReviews):
                        _totalReviewsTextBlock?.SetText($"Total Reviews: {_statsTracker.TotalReviews}");
                        break;
                    case nameof(StatsTracker.OverallAccuracy):
                        _overallAccuracyTextBlock?.SetText($"Overall Accuracy: {_statsTracker.OverallAccuracy:F1}%");
                        break;
                    case nameof(StatsTracker.CurrentStreak):
                        _currentStreakTextBlock?.SetText($"Current Streak: {_statsTracker.CurrentStreak} days");
                        break;
                    case nameof(StatsTracker.LongestStreak):
                        _longestStreakTextBlock?.SetText($"Longest Streak: {_statsTracker.LongestStreak} days");
                        break;
                    case nameof(StatsTracker.LastUpdated):
                        _lastUpdatedTextBlock?.SetText($"Last Updated: {_statsTracker.LastUpdated:g}");
                        break;

                    // For collections/dictionaries, ObservableCollection/Dictionary changes might
                    // trigger updates in ItemsControl automatically IF the reference doesn't change.
                    // If the entire collection/dictionary object is replaced in StatsTracker,
                    // we need to explicitly update the ItemsSource here.
                    case nameof(StatsTracker.DailyStats):
                        // Assuming DailyStats is an ObservableCollection and the reference doesn't change often.
                        // If the reference *can* change, uncomment the line below:
                        // _dailyStatsItemsControl?.SetItemsSource(_statsTracker.DailyStats.OrderByDescending(d => d.Date));
                        // For now, assume ObservableCollection handles internal changes. If items don't appear/disappear, update ItemsSource.
                        break;
                    case nameof(StatsTracker.ItemTypeStats):
                        // Dictionaries don't raise CollectionChanged. If items are added/removed,
                        // the ItemsSource needs to be reset.
                        _itemTypeStatsItemsControl?.SetItemsSource(_statsTracker.ItemTypeStats.OrderBy(kvp => kvp.Key.ToString()));
                        break;
                    case nameof(StatsTracker.TagStats):
                        // Dictionaries don't raise CollectionChanged.
                        _tagStatsItemsControl?.SetItemsSource(_statsTracker.TagStats.OrderBy(kvp => kvp.Key));
                        break;

                        // Add cases for Forecast/Difficult Items if implemented
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error updating UI for property change: {e.PropertyName}");
                // Avoid crashing the UI thread if an update fails.
            }
        });
    }

    /// <summary>
    /// Formats total minutes into a more readable string (e.g., "1h 30m", "45m").
    /// </summary>
    private static string FormatTime(long totalMinutes)
    {
        if (totalMinutes < 0) return "N/A";
        if (totalMinutes == 0) return "0m";

        var timeSpan = TimeSpan.FromMinutes(totalMinutes);
        int hours = (int)timeSpan.TotalHours;
        int minutes = timeSpan.Minutes;

        if (hours > 0 && minutes > 0)
        {
            return $"{hours}h {minutes}m";
        }
        else if (hours > 0)
        {
            return $"{hours}h";
        }
        else
        {
            return $"{minutes}m";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cleans up resources used by the page.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Logger.Debug($"Disposing StatisticsPage (Root: {Root.Id})...");

                // Dispose managed state (event handlers, Flecs entities)
                _disposables.Dispose(); // This handles PropertyChanged unsubscribe

                // Destroy the root Flecs UI entity if this component owns it
                if (Root.IsValid() && Root.IsAlive())
                {
                    Logger.Debug($"Destroying StatisticsPage Root Entity: {Root.Id}");
                    Root.Destruct();
                }
                else
                {
                    Logger.Warn("Root entity invalid or dead during StatisticsPage Dispose.");
                }

                Logger.Debug("StatisticsPage disposed.");
            }
            _isDisposed = true;
        }
    }

    /// <summary>
    /// Destructor
    /// </summary>
    ~StatisticsPage()
    {
        Dispose(disposing: false);
    }
}

