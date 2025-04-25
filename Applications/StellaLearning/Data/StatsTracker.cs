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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FsrsSharp;
using NLog;

namespace AyanamisTower.StellaLearning.Data;

/// <summary>
/// Tracks and stores statistics about learning activities in the application.
/// </summary>
public partial class StatsTracker : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static StatsTracker? _instance;
    private readonly string _statsFilePath;
    private readonly TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(5);
    private bool _isInitialized = false;

    /// <summary>
    /// Gets the singleton instance of the StatsTracker.
    /// </summary>
    public static StatsTracker Instance
    {
        get
        {
            _instance ??= new StatsTracker();
            return _instance;
        }
    }

    /// <summary>
    /// Gets an object representing the current state of all statistics,
    /// suitable for serialization by the SaveDataManager.
    /// This creates a snapshot of the current data.
    /// </summary>
    public StatsData Stats
    {
        get
        {
            // Create a new StatsData instance populated with the current tracker state.
            // Use ToList() or create new Dictionaries to capture a snapshot,
            // preventing modification issues if the tracker state changes during serialization.
            return new StatsData
            {
                // Snapshot collections and dictionaries
                DailyStats = [.. DailyStats],
                StudySessions = [.. StudySessions],
                ItemTypeStats = new Dictionary<SpacedRepetitionItemType, ItemTypeStats>(
                    ItemTypeStats
                ),
                TagStats = new Dictionary<string, TagStats>(TagStats),

                // Copy current scalar values
                CurrentStreak = CurrentStreak,
                LongestStreak = LongestStreak,
                TotalStudyTimeSeconds = TotalStudyTimeSeconds,
                TotalReviews = TotalReviews,
                OverallAccuracy = OverallAccuracy,

                // Update LastUpdated timestamp when data is requested for saving
                LastUpdated = DateTime.Now,
            };
        }
    }

    /// <summary>
    /// Records of daily study activities.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DailyStats> _dailyStats = [];

    /// <summary>
    /// Records of study sessions.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<StudySession> _studySessions = [];

    /// <summary>
    /// Statistics about different card types.
    /// </summary>
    [ObservableProperty]
    private Dictionary<SpacedRepetitionItemType, ItemTypeStats> _itemTypeStats = [];

    /// <summary>
    /// Statistics about tags and their performance.
    /// </summary>
    [ObservableProperty]
    private Dictionary<string, TagStats> _tagStats = [];

    /// <summary>
    /// Current active study session or null if not studying.
    /// </summary>
    [ObservableProperty]
    private StudySession? _currentSession;

    /// <summary>
    /// Current streak of consecutive study days.
    /// </summary>
    [ObservableProperty]
    private int _currentStreak;

    /// <summary>
    /// Longest streak of consecutive study days achieved.
    /// </summary>
    [ObservableProperty]
    private int _longestStreak;

    /// <summary>
    /// Total time spent studying across all sessions, stored in seconds.
    /// </summary>
    [ObservableProperty]
    private double _totalStudyTimeSeconds;

    /// <summary>
    /// Total number of reviews made.
    /// </summary>
    [ObservableProperty]
    private int _totalReviews;

    /// <summary>
    /// Average accuracy rate across all reviews (0-100).
    /// </summary>
    [ObservableProperty]
    private double _overallAccuracy;

    /// <summary>
    /// Date when statistics were last updated.
    /// </summary>
    [ObservableProperty]
    private DateTime _lastUpdated;

    private StatsTracker()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StellaLearning"
        );

        Directory.CreateDirectory(appDataPath);

#if DEBUG
        // Use a specific filename for Debug builds
        const string statsFilename = "learning_stats_debug.json";
        Logger.Info("Running in DEBUG mode. Using stats file: {Filename}", statsFilename);
#else
        // Use the standard filename for Release builds
        const string statsFilename = "learning_stats.json";
        Logger.Info("Running in RELEASE mode. Using stats file: {Filename}", statsFilename);
#endif
        _statsFilePath = Path.Combine(appDataPath, statsFilename);

        // Start auto-save timer
        Task.Run(AutoSaveLoop);
    }

    /// <summary>
    /// Initializes the stats tracker, loads existing statistics,
    /// and filters them based on currently existing item IDs.
    /// </summary>
    /// <param name="existingItemIds">A HashSet containing the Guids of all SpacedRepetitionItems currently loaded in the main application.</param>
    public async Task InitializeAsync(HashSet<Guid> existingItemIds)
    {
        if (_isInitialized)
            return;

        try
        {
            // Pass the existing IDs down to LoadStatsAsync for filtering
            await LoadStatsAsync(existingItemIds);
            _isInitialized = true;
            Logger.Info(
                "StatsTracker initialized successfully. Stats filtered for existing items."
            );
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize StatsTracker");
            // ResetObservablePropertiesToDefault might be called within LoadStatsAsync catches,
            // but call again here ensure clean state if InitializeAsync itself fails early.
            ResetObservablePropertiesToDefault();
            _isInitialized = false; // Ensure it's marked as not initialized
            // Consider re-throwing or specific error handling if initialization is critical
        }
        // Start auto-save timer regardless of initial load success? Or only if successful?
        // Current constructor starts it immediately. Let's keep it there for simplicity unless specific need arises.
        // Task.Run(AutoSaveLoop); // Already started in constructor
    }

    /// <summary>
    /// Starts a new study session.
    /// </summary>
    public async Task StartStudySession()
    {
        if (CurrentSession != null)
        {
            Logger.Warn("Attempting to start a study session while another is active");
            await EndStudySession();
        }

        CurrentSession = new StudySession { StartTime = DateTime.Now, SessionId = Guid.NewGuid() };

        Logger.Info("Started new study session: {SessionId}", CurrentSession.SessionId);
    }

    /// <summary>
    /// Ends the current study session and saves the statistics.
    /// </summary>
    public async Task EndStudySession()
    {
        if (CurrentSession == null)
        {
            Logger.Warn("Attempting to end a study session when none is active");
            return;
        }

        CurrentSession.EndTime = DateTime.Now;
        CurrentSession.Duration = CurrentSession.EndTime - CurrentSession.StartTime;

        // Update total study time by adding the total seconds from the session duration.
        double secondsToAdd = CurrentSession.Duration.TotalSeconds;
        TotalStudyTimeSeconds += secondsToAdd; // Add seconds directly

        // Add to sessions collection (use Dispatcher if accessed from non-UI thread)
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (CurrentSession is not null) // Check again inside dispatcher in case it changed
            {
                StudySessions.Add(CurrentSession);
            }
        });

        // Update daily stats
        UpdateDailyStats(CurrentSession);
        UpdateOverallAccuracy();
        RecalculateTotalReviews([.. StudySessions]); // Recalc TotalReviews first
        RecalculateTotalStudyTime([.. StudySessions]); // Recalc TotalStudyTimeSeconds
        RebuildItemAndTagStats([.. StudySessions]); // Rebuild ItemTypeStats, TagStats
        RebuildDailyStatsAndStreaks([.. StudySessions]); // Rebuild DailyStats, CurrentStreak, LongestStreak

        // Clear current session
        CurrentSession = null;

        await SaveStatsAsync();
        Logger.Info("Ended study session and saved statistics");
    }

    /// <summary>
    /// Records a card review event.
    /// </summary>
    /// <param name="item">The item being reviewed</param>
    /// <param name="rating">The rating given (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    public async Task RecordReview(SpacedRepetitionItem item, Rating rating)
    {
        try
        {
            // Ensure a session is active
            if (CurrentSession == null)
            {
                await StartStudySession();
            }

            // Create review record
            var review = new ReviewRecord
            {
                ItemId = item.Uid,
                ItemName = item.Name,
                ReviewTime = DateTime.Now,
                Rating = (int)rating,
                State = item.SpacedRepetitionState,
                ItemType = item.SpacedRepetitionItemType,
                Tags = [.. item.Tags],
            };

            // Add to current session
            CurrentSession!.Reviews.Add(review);

            // Update item type statistics
            if (
                !ItemTypeStats.TryGetValue(
                    item.SpacedRepetitionItemType,
                    out ItemTypeStats? typeStats
                )
            )
            {
                typeStats = new ItemTypeStats { ItemType = item.SpacedRepetitionItemType };
                ItemTypeStats[item.SpacedRepetitionItemType] = typeStats;
            }

            typeStats.TotalReviews++;
            if ((int)rating >= 3) // Good or Easy
            {
                typeStats.CorrectReviews++;
            }

            // Update tag statistics
            foreach (var tag in item.Tags)
            {
                if (!TagStats.TryGetValue(tag, out TagStats? tagStat))
                {
                    tagStat = new TagStats { Tag = tag };
                    TagStats[tag] = tagStat;
                }

                tagStat.TotalReviews++;
                if ((int)rating >= 3) // Good or Easy
                {
                    tagStat.CorrectReviews++;
                }
            }

            // Update global stats
            TotalReviews++;

            // Recalculate overall accuracy
            UpdateOverallAccuracy();

            LastUpdated = DateTime.Now;

            Logger.Debug(
                "Recorded review for item {ItemName} with rating {Rating}",
                item.Name,
                rating
            );
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error recording review");
        }
    }

    /// <summary>
    /// Updates daily stats with information from a completed study session.
    /// </summary>
    private void UpdateDailyStats(StudySession session)
    {
        var today = DateTime.Today;
        var dailyStat = DailyStats.FirstOrDefault(s => s.Date.Date == today);

        if (dailyStat == null)
        {
            dailyStat = new DailyStats { Date = today };
            DailyStats.Add(dailyStat);

            // Check if we're maintaining a streak
            var yesterday = today.AddDays(-1);
            var hasYesterday = DailyStats.Any(s => s.Date.Date == yesterday);

            if (hasYesterday)
            {
                CurrentStreak++;
                if (CurrentStreak > LongestStreak)
                {
                    LongestStreak = CurrentStreak;
                }
            }
            else
            {
                CurrentStreak = 1; // Reset streak but count today
            }
        }

        // Update the daily stats
        dailyStat.TotalReviews += session.Reviews.Count;
        dailyStat.CorrectReviews += session.Reviews.Count(r => r.Rating >= 3);
        dailyStat.TotalTimeMinutes += (int)session.Duration.TotalMinutes;

        // Update unique items count
        var uniqueItemIds = session.Reviews.Select(r => r.ItemId).Distinct();
        dailyStat.UniqueItemsReviewed += uniqueItemIds.Count(id =>
            !dailyStat.ReviewedItemIds.Contains(id)
        );

        // Add the unique item IDs
        foreach (var id in uniqueItemIds)
        {
            if (!dailyStat.ReviewedItemIds.Contains(id))
            {
                dailyStat.ReviewedItemIds.Add(id);
            }
        }

        // Calculate accuracy
        if (dailyStat.TotalReviews > 0)
        {
            dailyStat.Accuracy = (double)dailyStat.CorrectReviews / dailyStat.TotalReviews * 100;
        }
    }

    /// <summary>
    /// Updates the overall accuracy based on all reviews, ensuring the value stays within the 0-100 range.
    /// </summary>
    private void UpdateOverallAccuracy()
    {
        // Calculate the total number of correct reviews across all sessions recorded so far.
        var totalCorrect = StudySessions.SelectMany(s => s.Reviews).Count(r => r.Rating >= 3);

        double calculatedAccuracy; // Use a temporary variable for the raw calculation

        if (TotalReviews > 0)
        {
            // Calculate the accuracy percentage.
            calculatedAccuracy = (double)totalCorrect / TotalReviews * 100;

            // --- Optional but Recommended: Log if the raw calculation is out of bounds ---
            // This helps identify if the underlying bug (like double counting) is still present
            // or if some other data inconsistency has occurred.
            if (calculatedAccuracy > 100.0 || calculatedAccuracy < 0.0)
            {
                Logger.Warn(
                    $"Raw calculated accuracy ({calculatedAccuracy:F2}%) was outside the valid 0-100 range before clamping. This indicates an ongoing calculation issue or data inconsistency. TotalCorrect={totalCorrect}, TotalReviews={TotalReviews}."
                );
            }
        }
        else
        {
            // If there are no reviews at all, the accuracy is defined as 0%.
            calculatedAccuracy = 0.0;
        }

        // --- Add the Clamp Here ---
        // Ensure the final OverallAccuracy value is strictly between 0.0 and 100.0.
        // Math.Clamp(value, min, max)
        OverallAccuracy = Math.Clamp(calculatedAccuracy, 0.0, 100.0);

        // The OverallAccuracy property will now never be assigned a value outside the 0-100 range.
        // If calculatedAccuracy was 133.33, OverallAccuracy will be set to 100.0.
        // If calculatedAccuracy was -10.0 (somehow), OverallAccuracy will be set to 0.0.
    }

    /// <summary>
    /// Gets the accuracy rate for a specific time range.
    /// </summary>
    public double GetAccuracyForTimeRange(DateTime start, DateTime end)
    {
        var relevantSessions = StudySessions
            .Where(s => s.StartTime >= start && s.EndTime <= end)
            .ToList();

        var reviews = relevantSessions.SelectMany(s => s.Reviews).ToList();

        if (reviews.Count == 0)
            return 0;

        var correctReviews = reviews.Count(r => r.Rating >= 3);
        return (double)correctReviews / reviews.Count * 100;
    }

    /// <summary>
    /// Gets the most difficult cards based on review history.
    /// </summary>
    public List<Guid> GetMostDifficultItems(int count = 10)
    {
        var itemPerformance = new Dictionary<Guid, (int TotalReviews, int FailedReviews)>();

        foreach (var session in StudySessions)
        {
            foreach (var review in session.Reviews)
            {
                if (
                    !itemPerformance.TryGetValue(
                        review.ItemId,
                        out (int TotalReviews, int FailedReviews) current
                    )
                )
                {
                    current = (0, 0);
                    itemPerformance[review.ItemId] = current;
                }

                var newTotal = current.TotalReviews + 1;
                var newFailed = current.FailedReviews + (review.Rating < 3 ? 1 : 0);

                itemPerformance[review.ItemId] = (newTotal, newFailed);
            }
        }

        // Filter for items with at least 3 reviews
        return
        [
            .. itemPerformance
                .Where(kvp => kvp.Value.TotalReviews >= 3)
                .OrderByDescending(kvp => (double)kvp.Value.FailedReviews / kvp.Value.TotalReviews)
                .Take(count)
                .Select(kvp => kvp.Key),
        ];
    }

    /// <summary>
    /// Gets study time statistics by day of week.
    /// </summary>
    public Dictionary<DayOfWeek, TimeSpan> GetStudyTimeByDayOfWeek()
    {
        var result = new Dictionary<DayOfWeek, TimeSpan>();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var sessionsOnDay = StudySessions.Where(s => s.StartTime.DayOfWeek == day);
            result[day] = sessionsOnDay.Aggregate(
                TimeSpan.Zero,
                (current, session) => current + session.Duration
            );
        }

        return result;
    }

    /// <summary>
    /// Gets a forecast of reviews due in the upcoming days.
    /// </summary>
    public static Dictionary<DateTime, int> GetReviewForecast(
        ObservableCollection<SpacedRepetitionItem> items,
        int daysAhead = 14
    )
    {
        var result = new Dictionary<DateTime, int>();
        var today = DateTime.Today;

        // Initialize all days with zero reviews
        for (int i = 0; i < daysAhead; i++)
        {
            result[today.AddDays(i)] = 0;
        }

        // Count reviews due on each day
        foreach (var item in items)
        {
            var dueDate = item.NextReview.Date;
            if (dueDate >= today && dueDate < today.AddDays(daysAhead))
            {
                result[dueDate]++;
            }
        }

        return result;
    }

    private async Task AutoSaveLoop()
    {
        while (true)
        {
            await Task.Delay(_autoSaveInterval);
            if (_isInitialized)
            {
                await SaveStatsAsync();
            }
        }
    }

    /// <summary>
    /// Saves the current statistics to disk.
    /// </summary>
    public async Task SaveStatsAsync()
    {
        // Prevent saving if not initialized to avoid writing default/empty data over existing file
        if (!_isInitialized && File.Exists(_statsFilePath))
        {
            Logger.Warn(
                "Attempted to save stats before initialization, but an existing file was found. Skipping save to prevent data loss."
            );
            return;
        }
        // Allow saving if not initialized *but no file exists* (first time save)
        if (!_isInitialized && !File.Exists(_statsFilePath))
        {
            Logger.Info("Performing initial save of potentially empty stats as no file exists.");
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Add any necessary converters if complex types are involved
                // Converters = { ... }
            };

            // Create data object on the fly to ensure current values are captured
            var statsData = new StatsData
            {
                // Use ToList() to capture a snapshot of the collections at save time
                DailyStats = [.. DailyStats],
                StudySessions = [.. StudySessions],
                ItemTypeStats = ItemTypeStats, // Dictionaries are usually saved correctly
                TagStats = TagStats,
                CurrentStreak = CurrentStreak,
                LongestStreak = LongestStreak,
                TotalStudyTimeSeconds = TotalStudyTimeSeconds, // Save seconds
                TotalReviews = TotalReviews,
                OverallAccuracy = OverallAccuracy,
                LastUpdated =
                    DateTime.Now // Update LastUpdated time on save
                ,
            };

            var json = JsonSerializer.Serialize(statsData, options);
            try
            {
                // Try up to 3 times with a short delay between attempts
                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    try
                    {
                        await File.WriteAllTextAsync(_statsFilePath, json);
                        // If successful, break out of the retry loop
                        break;
                    }
                    catch (IOException ioEx) when (attempt < 3)
                    {
                        // Log the failure but continue to retry
                        Logger.Warn(
                            ioEx,
                            $"Failed to save statistics on attempt {attempt}/3: File access error. Retrying after delay..."
                        );
                        await Task.Delay(250 * attempt); // Increasing delay between retries
                    }
                }

                // Update the LastUpdated property *after* successful save
                LastUpdated = statsData.LastUpdated; // Triggers OnPropertyChanged
            }
            catch (IOException ioEx)
            {
                // After all retries failed, log the error but don't crash
                Logger.Error(
                    ioEx,
                    "Failed to save statistics after multiple attempts: File access error (file may be locked by another process)"
                );
                // Consider notifying user of save failure if critical
            }

            Logger.Debug("Saved statistics to {FilePath}", _statsFilePath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save statistics");
            // Consider notifying the user of the save failure
        }
    }

    /// <summary>
    /// Loads statistics from disk, filtering reviews for existing items
    /// and recalculating all aggregate statistics.
    /// Marked internal as InitializeAsync is the intended public entry point.
    /// </summary>
    /// <param name="existingItemIds">A HashSet containing the Guids of currently existing SpacedRepetitionItems.</param>
    internal async Task LoadStatsAsync(HashSet<Guid> existingItemIds)
    {
        StatsData? loadedStatsData = null;

        // Step 1: Attempt to load the StatsData object from the file
        try
        {
            if (!File.Exists(_statsFilePath))
            {
                Logger.Info(
                    "No existing statistics file found at {FilePath}. Initializing defaults.",
                    _statsFilePath
                );
                // Fallthrough to initialize defaults
            }
            else
            {
                var json = await File.ReadAllTextAsync(_statsFilePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    loadedStatsData = JsonSerializer.Deserialize<StatsData>(json);
                }
                else
                {
                    Logger.Warn(
                        "Statistics file at {FilePath} is empty. Initializing defaults.",
                        _statsFilePath
                    );
                }
            }
        }
        catch (JsonException jsonEx)
        {
            Logger.Error(
                jsonEx,
                "JSON Error loading statistics from {FilePath}. File might be corrupted. Initializing defaults.",
                _statsFilePath
            );
        }
        catch (Exception ex)
        {
            Logger.Error(
                ex,
                "Failed to load statistics from {FilePath}. Initializing defaults.",
                _statsFilePath
            );
        }

        // Step 2: Populate the tracker state
        if (loadedStatsData != null)
        {
            Logger.Debug(
                "Deserialized StatsData successfully. Populating tracker and filtering history..."
            );

            // Filter the loaded sessions based on existing item IDs BEFORE assigning
            List<StudySession> filteredSessions = FilterSessions(
                loadedStatsData.StudySessions,
                existingItemIds
            );

            // Populate the tracker's properties from the loaded (and filtered) data
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StudySessions = new ObservableCollection<StudySession>(filteredSessions);
                // Load dictionaries - recalculation below will ensure accuracy
                ItemTypeStats =
                    loadedStatsData.ItemTypeStats
                    ?? new Dictionary<SpacedRepetitionItemType, ItemTypeStats>();
                TagStats = loadedStatsData.TagStats ?? new Dictionary<string, TagStats>();
                // Clear DailyStats - it will be fully rebuilt
                DailyStats.Clear();
            });

            // Load scalar values that represent historical maximums or timestamps
            LongestStreak = loadedStatsData.LongestStreak; // Preserve historical max
            LastUpdated = loadedStatsData.LastUpdated; // Preserve last saved time

            // Clear aggregates that need full recalculation from filtered history
            TotalReviews = 0;
            TotalStudyTimeSeconds = 0;
            CurrentStreak = 0;
            OverallAccuracy = 0;

            // Recalculate aggregates based on the filtered StudySessions now in the tracker
            var orderedSessions = StudySessions.OrderBy(s => s.StartTime).ToList();
            RecalculateTotalReviews(orderedSessions);
            RecalculateTotalStudyTime(orderedSessions);
            RebuildItemAndTagStats(orderedSessions); // Rebuild Item/Tag stats from filtered reviews
            RebuildDailyStatsAndStreaks(orderedSessions); // Rebuild Daily and Streaks from filtered sessions
            UpdateOverallAccuracy(); // Final accuracy calc

            Logger.Info(
                "Loaded and rebuilt statistics from {FilePath}. Stats filtered.",
                _statsFilePath
            );
        }
        else
        {
            // Loading failed or no file existed, reset to defaults
            ResetObservablePropertiesToDefault();
            Logger.Info("Initialized StatsTracker with default values.");
        }

        // Notify potential UI listeners about changes
        OnPropertyChanged(nameof(ItemTypeStats));
        OnPropertyChanged(nameof(TagStats));
        OnPropertyChanged(nameof(DailyStats));
        // Scalar properties have their own notifications via [ObservableProperty]
    }

    // Helper method to filter sessions (keep only those with reviews for existing items)
    private List<StudySession> FilterSessions(
        List<StudySession>? sessionsToFilter,
        HashSet<Guid> existingItemIds
    )
    {
        List<StudySession> filteredSessions = [];
        if (sessionsToFilter == null)
            return filteredSessions;

        int originalReviewCount = sessionsToFilter.SelectMany(s => s?.Reviews ?? []).Count();
        int keptReviewCount = 0;

        foreach (var session in sessionsToFilter)
        {
            if (session?.Reviews == null || !session.Reviews.Any())
                continue;

            var reviewsToKeep = session
                .Reviews.Where(review => existingItemIds.Contains(review.ItemId))
                .ToList();

            keptReviewCount += reviewsToKeep.Count;

            if (reviewsToKeep.Any())
            {
                // Add a session DTO (or modify original if safe) with only the kept reviews
                filteredSessions.Add(
                    new StudySession
                    { // Create new DTO to avoid modifying original list if needed elsewhere
                        SessionId = session.SessionId,
                        StartTime = session.StartTime,
                        EndTime = session.EndTime,
                        Duration = session.Duration,
                        Reviews =
                            reviewsToKeep // Assign filtered list
                        ,
                    }
                );
            }
        }
        Logger.Info(
            $"Filtered review history: Kept {keptReviewCount} reviews out of {originalReviewCount} based on {existingItemIds.Count} existing items."
        );
        return filteredSessions;
    }

    /// <summary>
    /// Resets observable properties to their default values. Used when loading fails.
    /// </summary>
    private void ResetObservablePropertiesToDefault()
    {
        // Use Dispatcher for collections
        Dispatcher
            .UIThread.InvokeAsync(() =>
            {
                DailyStats.Clear();
                StudySessions.Clear();
                ItemTypeStats.Clear();
                TagStats.Clear();
            })
            .Wait(); // Consider if waiting is necessary

        CurrentStreak = 0;
        LongestStreak = 0;
        TotalStudyTimeSeconds = 0.0;
        TotalReviews = 0;
        OverallAccuracy = 0;
        LastUpdated = DateTime.MinValue; // Indicate no valid data loaded

        // Manually notify changes for collections/dictionaries
        OnPropertyChanged(nameof(DailyStats));
        OnPropertyChanged(nameof(StudySessions));
        OnPropertyChanged(nameof(ItemTypeStats));
        OnPropertyChanged(nameof(TagStats));
    }

    /// <summary>
    /// Resets all tracked statistics to their initial state and saves immediately.
    /// </summary>
    public async Task ResetStatsAsync()
    {
        Logger.Warn("Resetting all learning statistics!");

        // Use Dispatcher for collection clearing if accessed from non-UI thread
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DailyStats.Clear();
            StudySessions.Clear();
            ItemTypeStats.Clear();
            TagStats.Clear();
        });

        // Reset scalar properties - these automatically trigger PropertyChanged
        CurrentStreak = 0;
        LongestStreak = 0;
        TotalStudyTimeSeconds = 0.0;
        TotalReviews = 0;
        OverallAccuracy = 0;
        LastUpdated = DateTime.Now; // Set last updated time to now

        // Manually notify changes for collections/dictionaries as Clear() might not be sufficient
        // depending on how the UI bindings are set up.
        OnPropertyChanged(nameof(DailyStats));
        OnPropertyChanged(nameof(StudySessions));
        OnPropertyChanged(nameof(ItemTypeStats));
        OnPropertyChanged(nameof(TagStats));

        // Ensure current session is also cleared if one was active
        if (CurrentSession != null)
        {
            Logger.Warn(
                "Resetting stats while a study session ({SessionId}) was active. Ending the session without saving its final state.",
                CurrentSession.SessionId
            );
            CurrentSession = null; // Clears the session without saving its potentially partial data
        }

        // Save the reset state immediately
        await SaveStatsAsync();
        Logger.Info("Learning statistics have been reset and saved.");
    }

    /// <summary>
    /// Removes all statistical records associated with a specific item ID
    /// and recalculates aggregate statistics.
    /// </summary>
    /// <param name="itemId">The Guid of the item that has been deleted.</param>
    public async Task RemoveStatsForItemAsync(Guid itemId)
    {
        Logger.Info($"Attempting to remove statistics for deleted item: {itemId}");

        // --- Early Exit Check ---
        // Verify if any reviews for this specific item ID exist in the tracker.
        // This avoids unnecessary processing if the item had no recorded stats.
        bool itemHasReviews = StudySessions // Access the current list of sessions
            .SelectMany(session => session.Reviews) // Flatten into a single sequence of all reviews
            .Any(review => review.ItemId == itemId); // Check if any review matches the ID

        if (!itemHasReviews)
        {
            // If no reviews match the item ID, log it and exit the method.
            Logger.Info(
                $"No review records found for item ID {itemId}. No statistics removal needed."
            );
            return; // Stop execution here
        }
        // --- End Early Exit Check ---

        // If the code reaches here, it means at least one review for the item exists.
        Logger.Debug(
            $"Reviews found for item {itemId}. Proceeding with stats removal and recalculation..."
        );

        Logger.Info($"Attempting to remove statistics for deleted item: {itemId}");
        bool statsChanged = false;

        // --- 1. Filter Review Records ---
        // Create a new list to hold sessions that still have relevant reviews
        List<StudySession> sessionsToKeep = [];
        int removedReviewsCount = 0;

        // Iterate over a copy of the sessions to avoid modification issues during iteration
        var currentSessionsSnapshot = StudySessions.ToList();
        StudySessions.Clear(); // Clear original collection, will rebuild or repopulate

        foreach (var session in currentSessionsSnapshot)
        {
            int reviewsBefore = session.Reviews.Count;
            // Keep only reviews NOT matching the deleted item ID
            session.Reviews.RemoveAll(review => review.ItemId == itemId);
            int reviewsAfter = session.Reviews.Count;

            if (reviewsAfter < reviewsBefore)
            {
                statsChanged = true; // Mark that we need to recalculate
                removedReviewsCount += (reviewsBefore - reviewsAfter);
            }

            // Only keep the session if it still contains any reviews after filtering
            if (session.Reviews.Count != 0)
            {
                sessionsToKeep.Add(session);
            }
            else
            {
                // If session becomes empty, we might need to adjust total study time later if we sum durations
                statsChanged = true; // Removing an empty session also triggers recalc
            }
        }

        // If no stats were changed/removed, we can potentially stop early
        if (!statsChanged)
        {
            Logger.Info($"No review records found for item {itemId}. No statistics were changed.");
            // Re-add the original sessions if we cleared the main list
            foreach (var session in currentSessionsSnapshot)
            {
                StudySessions.Add(session);
            }
            return;
        }

        Logger.Info(
            $"Removed {removedReviewsCount} review records for item {itemId}. Recalculating all aggregate statistics..."
        );

        // --- 2. Reset Aggregates ---
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StudySessions.Clear(); // Clear original collection
            foreach (var session in sessionsToKeep)
            {
                StudySessions.Add(session);
            } // Repopulate

            ItemTypeStats.Clear();
            TagStats.Clear();
            DailyStats.Clear();
        });

        TotalReviews = 0;
        TotalStudyTimeSeconds = 0;
        CurrentStreak = 0;
        LongestStreak = 0;
        OverallAccuracy = 0;

        // --- 3. Recalculate Aggregates from Filtered Data ---
        var orderedSessions = sessionsToKeep.OrderBy(s => s.StartTime).ToList();

        RecalculateTotalReviews(orderedSessions); // Recalc TotalReviews first
        RecalculateTotalStudyTime(orderedSessions); // Recalc TotalStudyTimeSeconds
        RebuildItemAndTagStats(orderedSessions); // Rebuild ItemTypeStats, TagStats
        RebuildDailyStatsAndStreaks(orderedSessions); // Rebuild DailyStats, CurrentStreak, LongestStreak

        // Final accuracy calculation based on rebuilt totals
        UpdateOverallAccuracy(); // Uses recalculated TotalReviews and scans current StudySessions

        // --- 4. Save Updated Stats ---
        LastUpdated = DateTime.Now;
        await SaveStatsAsync();

        // PropertyChanged notifications for collections were handled internally or by caller's InvokeAsync.
        // Scalar properties updated trigger their own notifications via [ObservableProperty].

        Logger.Info($"Statistics recalculation complete after removing item {itemId}.");
    }

    /// <summary>
    /// Updates the 'Tags' list in all historical ReviewRecords for a specific item
    /// and recalculates TagStats.
    /// WARNING: This modifies historical data. It assumes that a tag change should
    /// apply retroactively to all past reviews of the item.
    /// </summary>
    /// <param name="itemId">The Guid of the item whose tags have changed.</param>
    /// <param name="newTags">The new list of tags for the item.</param>
    public async Task UpdateTagsForItemAsync(Guid itemId, List<string> newTags)
    {
        Logger.Info(
            $"Updating historical tags for item {itemId} to: [{string.Join(", ", newTags ?? [])}]"
        );

        bool tagsActuallyChanged = false;

        // Create a defensive copy of the new tags list
        var newTagsCopy = newTags == null ? new List<string>() : [.. newTags];

        // Iterate through all sessions and reviews
        // No need for Dispatcher here if just reading/modifying ReviewRecord content,
        // but the rebuild step might need it if called from background.
        foreach (var session in StudySessions)
        {
            foreach (var review in session.Reviews)
            {
                if (review.ItemId == itemId)
                {
                    // Check if the tags are actually different before updating
                    // Simple comparison assuming order doesn't matter for equality check here
                    var currentTagsSet = review.Tags?.ToHashSet() ?? [];
                    var newTagsSet = newTagsCopy.ToHashSet();

                    if (!currentTagsSet.SetEquals(newTagsSet))
                    {
                        review.Tags = newTagsCopy; // Update the review record's tags
                        tagsActuallyChanged = true;
                        // Logger.Debug($"Updated tags for review at {review.ReviewTime} for item {itemId}");
                    }
                }
            }
        }

        // If any review records were modified, rebuild TagStats and save
        if (tagsActuallyChanged)
        {
            Logger.Info($"Historical tags updated for item {itemId}. Rebuilding TagStats...");

            // Rebuild TagStats based on the modified review records
            // Ensure this happens on the correct thread if necessary
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // ItemTypeStats are unaffected by tag changes, only rebuild TagStats
                RebuildTagStatsOnly(StudySessions.ToList()); // Use a helper focused only on tags
            });

            // Save the changes
            LastUpdated = DateTime.Now;
            await SaveStatsAsync();

            Logger.Info(
                $"TagStats rebuilt and statistics saved after updating tags for item {itemId}."
            );
        }
        else
        {
            Logger.Info(
                $"No historical tag changes detected for item {itemId}. TagStats remain unchanged."
            );
        }
    }

    #region Recalculation Helper Methods for Item Deletion

    /// <summary>
    /// Rebuilds ONLY the TagStats dictionary from a list of sessions.
    /// Assumes TagStats dictionary has been cleared beforehand.
    /// Requires ReviewRecord.Tags to be populated. Used after tag updates.
    /// </summary>
    private void RebuildTagStatsOnly(List<StudySession> sessions)
    {
        // Ensure dictionary is clear
        TagStats.Clear();
        var tempTagStats = new Dictionary<string, TagStats>();

        foreach (var review in sessions.SelectMany(s => s.Reviews))
        {
            if (review.Tags != null && review.Tags.Any())
            {
                foreach (var tag in review.Tags)
                {
                    var normalizedTag = tag.Trim();
                    if (string.IsNullOrEmpty(normalizedTag))
                        continue;

                    if (!tempTagStats.TryGetValue(normalizedTag, out TagStats? tagStat))
                    {
                        tagStat = new TagStats { Tag = normalizedTag };
                        tempTagStats[normalizedTag] = tagStat;
                    }
                    tagStat.TotalReviews++;
                    if (review.Rating >= (int)Rating.Good)
                        tagStat.CorrectReviews++;
                }
            }
        }
        // Update the observable dictionary
        TagStats = new Dictionary<string, TagStats>(tempTagStats);
        Logger.Debug($"Rebuilt TagStats ONLY (Count: {TagStats.Count})");
        OnPropertyChanged(nameof(TagStats)); // Notify observers
    }

    /// <summary>
    /// Recalculates the TotalReviews count based on a list of sessions.
    /// </summary>
    /// <param name="sessions">The list of sessions to count reviews from.</param>
    private void RecalculateTotalReviews(List<StudySession> sessions)
    {
        TotalReviews = sessions.Sum(s => s.Reviews.Count);
        Logger.Debug($"Recalculated TotalReviews: {TotalReviews}");
    }

    /// <summary>
    /// Recalculates the TotalStudyTimeSeconds based on the duration of sessions.
    /// </summary>
    /// <param name="sessions">The list of sessions to sum durations from.</param>
    private void RecalculateTotalStudyTime(List<StudySession> sessions)
    {
        TotalStudyTimeSeconds = sessions.Sum(s => s.Duration.TotalSeconds);
        Logger.Debug($"Recalculated TotalStudyTimeSeconds: {TotalStudyTimeSeconds}");
    }

    /// <summary>
    /// Rebuilds the ItemTypeStats and TagStats dictionaries from a list of sessions.
    /// Assumes ItemTypeStats and TagStats dictionaries have been cleared beforehand.
    /// Requires ReviewRecord.Tags to be populated for TagStats accuracy.
    /// </summary>
    /// <param name="sessions">The list of sessions containing the reviews to process.</param>
    private void RebuildItemAndTagStats(List<StudySession> sessions)
    {
        // Ensure dictionaries are clear (should be done by caller, but belt-and-suspenders)
        ItemTypeStats.Clear();
        TagStats.Clear();

        foreach (var review in sessions.SelectMany(s => s.Reviews))
        {
            // --- Rebuild ItemTypeStats ---
            if (!ItemTypeStats.TryGetValue(review.ItemType, out ItemTypeStats? typeStats))
            {
                typeStats = new ItemTypeStats { ItemType = review.ItemType };
                ItemTypeStats[review.ItemType] = typeStats;
            }
            typeStats.TotalReviews++;
            if (review.Rating >= 3)
                typeStats.CorrectReviews++;

            // --- Rebuild TagStats ---
            // CRITICAL: This assumes ReviewRecord has a 'Tags' property populated at review time.
            if (review.Tags != null && review.Tags.Count != 0)
            {
                foreach (var tag in review.Tags)
                {
                    if (!TagStats.TryGetValue(tag, out TagStats? tagStat))
                    {
                        tagStat = new TagStats { Tag = tag };
                        TagStats[tag] = tagStat;
                    }
                    tagStat.TotalReviews++;
                    if (review.Rating >= 3)
                        tagStat.CorrectReviews++;
                }
            }
            // else { // Optional: Log if a review record is missing tags if you expect them
            //    Logger.Warn($"Review record for item {review.ItemId} at {review.ReviewTime} is missing Tags for TagStats rebuild.");
            // }
        }
        Logger.Debug(
            $"Rebuilt ItemTypeStats (Count: {ItemTypeStats.Count}) and TagStats (Count: {TagStats.Count})"
        );

        // Notify observers that the dictionaries have been potentially repopulated
        // This might be redundant if the caller notifies, but safer to include.
        OnPropertyChanged(nameof(ItemTypeStats));
        OnPropertyChanged(nameof(TagStats));
    }

    /// <summary>
    /// Rebuilds the DailyStats collection and recalculates CurrentStreak and LongestStreak.
    /// Assumes DailyStats collection has been cleared beforehand.
    /// </summary>
    /// <param name="orderedSessions">A list of sessions, PRE-SORTED chronologically by StartTime.</param>
    private void RebuildDailyStatsAndStreaks(List<StudySession> orderedSessions)
    {
        // Ensure DailyStats is clear (should be done by caller)
        DailyStats.Clear();

        if (!orderedSessions.Any())
        {
            Logger.Debug("No sessions remaining, DailyStats and Streaks remain at 0.");
            CurrentStreak = 0;
            LongestStreak = 0;
            OnPropertyChanged(nameof(DailyStats)); // Notify UI it's empty
            return;
        }

        // --- Rebuild DailyStats Collection ---
        foreach (var session in orderedSessions)
        {
            UpdateDailyStatsForRebuild(session);
        }
        Logger.Debug($"Rebuilt DailyStats collection (Count: {DailyStats.Count})");

        // --- Recalculate Streaks ---
        // This needs the fully rebuilt DailyStats collection
        RecalculateStreaks(); // Calculates and sets CurrentStreak, LongestStreak

        OnPropertyChanged(nameof(DailyStats)); // Notify UI about the rebuilt collection
    }

    /// <summary>
    /// Helper to populate a single day's stats from a session during a rebuild.
    /// Adds or updates an entry in the DailyStats collection.
    /// Does NOT handle streak calculation itself.
    /// </summary>
    /// <param name="session">The study session to process.</param>
    private void UpdateDailyStatsForRebuild(StudySession session)
    {
        var sessionDate = session.StartTime.Date;
        var dailyStat = DailyStats.FirstOrDefault(s => s.Date == sessionDate);

        if (dailyStat == null)
        {
            dailyStat = new DailyStats { Date = sessionDate };
            // This modification happens within the context that already cleared DailyStats,
            // so direct add should be okay unless called concurrently.
            // If called from multiple threads potentially, use Dispatcher here.
            DailyStats.Add(dailyStat);
        }

        // Accumulate stats for the day
        dailyStat.TotalReviews += session.Reviews.Count;
        dailyStat.CorrectReviews += session.Reviews.Count(r => r.Rating >= 3);
        dailyStat.TotalTimeMinutes += (int)session.Duration.TotalMinutes; // Assumes Duration is accurate for the session

        // Update unique items reviewed for the day
        var uniqueItemIdsInSession = session.Reviews.Select(r => r.ItemId).Distinct();
        foreach (var id in uniqueItemIdsInSession)
        {
            if (!dailyStat.ReviewedItemIds.Contains(id))
            {
                dailyStat.ReviewedItemIds.Add(id);
                // Only increment UniqueItemsReviewed if it was actually added as unique *for that day*
                dailyStat.UniqueItemsReviewed++;
            }
        }

        // Recalculate accuracy for the day
        if (dailyStat.TotalReviews > 0)
        {
            dailyStat.Accuracy = (double)dailyStat.CorrectReviews / dailyStat.TotalReviews * 100;
        }
        else
        {
            dailyStat.Accuracy = 0;
        }
    }

    /// <summary>
    /// Recalculates CurrentStreak and LongestStreak based *only* on the current state
    /// of the DailyStats collection. Assumes DailyStats is populated and sorted.
    /// </summary>
    private void RecalculateStreaks()
    {
        CurrentStreak = 0; // Reset before calculation
        LongestStreak = 0; // Reset before calculation
        int currentTempStreak = 0;

        if (!DailyStats.Any())
        {
            Logger.Debug("DailyStats is empty, streaks are 0.");
            return; // No stats, no streaks
        }

        // Sort daily stats by date to ensure correct streak calculation
        var orderedDailyStats = DailyStats.OrderBy(ds => ds.Date).ToList();

        // Calculate longest streak
        for (int i = 0; i < orderedDailyStats.Count; i++)
        {
            if (i == 0 || orderedDailyStats[i].Date != orderedDailyStats[i - 1].Date.AddDays(1))
            {
                currentTempStreak = 1; // Start new streak (first day or gap)
            }
            else
            {
                currentTempStreak++; // Continue streak
            }

            if (currentTempStreak > LongestStreak)
            {
                LongestStreak = currentTempStreak; // Update longest streak found
            }
        }

        // Calculate current streak (relative to today)
        DateTime today = DateTime.Today;
        var lastStudyDay = orderedDailyStats.Last().Date;

        if (lastStudyDay == today || lastStudyDay == today.AddDays(-1))
        {
            // If the last study session was today or yesterday, the current streak is the one ending on that day.
            // We recalculate the streak ending on the last study day.
            currentTempStreak = 0;
            for (int i = orderedDailyStats.Count - 1; i >= 0; i--)
            {
                if (
                    i == orderedDailyStats.Count - 1
                    || orderedDailyStats[i].Date == orderedDailyStats[i + 1].Date.AddDays(-1)
                )
                {
                    currentTempStreak++;
                }
                else
                {
                    break; // Gap found, streak ended before this point
                }
            }
            // Assign only if the streak is contiguous up to yesterday or today
            if (lastStudyDay >= today.AddDays(-1))
            {
                CurrentStreak = currentTempStreak;
            }
            else
            {
                CurrentStreak = 0; // Should not happen based on outer if, but defensive check
            }
        }
        else
        {
            // If the last study day was before yesterday, the current streak is 0.
            CurrentStreak = 0;
        }

        // Basic sanity checks
        if (LongestStreak < 0)
            LongestStreak = 0;
        if (CurrentStreak < 0)
            CurrentStreak = 0;
        if (CurrentStreak > LongestStreak)
            LongestStreak = CurrentStreak; // Current can't exceed longest

        Logger.Debug($"Streaks recalculated: Current={CurrentStreak}, Longest={LongestStreak}");

        // PropertyChanged notifications for streaks are handled by their setters (if using [ObservableProperty])
        // If not using [ObservableProperty], manually call OnPropertyChanged here.
        // OnPropertyChanged(nameof(CurrentStreak));
        // OnPropertyChanged(nameof(LongestStreak));
    }

    #endregion // Recalculation Helper Methods
}

/// <summary>
/// Container for serializing all statistics data.
/// </summary>
public class StatsData
{
    /// <summary>
    /// Collection of statistics for each day that study occurred.
    /// </summary>
    public List<DailyStats> DailyStats { get; set; } = [];

    /// <summary>
    /// Collection of all individual study sessions.
    /// </summary>
    public List<StudySession> StudySessions { get; set; } = [];

    /// <summary>
    /// Performance statistics organized by item type.
    /// </summary>
    public Dictionary<SpacedRepetitionItemType, ItemTypeStats> ItemTypeStats { get; set; } = [];

    /// <summary>
    /// Performance statistics organized by tag.
    /// </summary>
    public Dictionary<string, TagStats> TagStats { get; set; } = [];

    /// <summary>
    /// The current streak of consecutive days studied.
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// The longest streak of consecutive days studied ever achieved.
    /// </summary>
    public int LongestStreak { get; set; }

    /// <summary>
    /// Total time spent studying across all sessions, stored in seconds.
    /// </summary>
    public double TotalStudyTimeSeconds { get; set; }

    /// <summary>
    /// Total number of card reviews performed across all sessions.
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Overall accuracy rate as a percentage (0-100).
    /// </summary>
    public double OverallAccuracy { get; set; }

    /// <summary>
    /// The date and time when statistics were last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Represents statistics for a single day of study.
/// </summary>
public class DailyStats
{
    /// <summary>
    /// The calendar date for these statistics.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total number of card reviews performed on this day.
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Number of reviews rated as "Good" or "Easy" on this day.
    /// </summary>
    public int CorrectReviews { get; set; }

    /// <summary>
    /// Percentage of correct reviews on this day (0-100).
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Total time spent studying on this day, in minutes.
    /// </summary>
    public int TotalTimeMinutes { get; set; }

    /// <summary>
    /// Number of unique items reviewed on this day.
    /// </summary>
    public int UniqueItemsReviewed { get; set; }

    /// <summary>
    /// List of unique item IDs that were reviewed on this day.
    /// </summary>
    public List<Guid> ReviewedItemIds { get; set; } = [];
}

/// <summary>
/// Represents a single study session.
/// </summary>
public class StudySession
{
    /// <summary>
    /// Unique identifier for this study session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Date and time when this study session began.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Date and time when this study session ended.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration of this study session.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Collection of all reviews performed during this session.
    /// </summary>
    public List<ReviewRecord> Reviews { get; set; } = [];
}

/// <summary>
/// Represents a single review event.
/// </summary>
public class ReviewRecord
{
    /// <summary>
    /// Unique identifier of the item that was reviewed.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Display name of the item that was reviewed.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Exact date and time when this review occurred.
    /// </summary>
    public DateTime ReviewTime { get; set; }

    /// <summary>
    /// User rating for this review (1=Again, 2=Hard, 3=Good, 4=Easy).
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// The state of the spaced repetition item at the time of review.
    /// </summary>
    public SpacedRepetitionState State { get; set; }

    /// <summary>
    /// The type of the spaced repetition item that was reviewed.
    /// </summary>
    public SpacedRepetitionItemType ItemType { get; set; }

    /// <summary>
    /// Tags associated with the item at the time of review.
    /// </summary>
    public List<string> Tags { get; set; } = []; // Initialize to empty list
}

/// <summary>
/// Statistics for a specific item type.
/// </summary>
public class ItemTypeStats
{
    /// <summary>
    /// The type of item these statistics represent.
    /// </summary>
    public SpacedRepetitionItemType ItemType { get; set; }

    /// <summary>
    /// Total number of reviews for this item type.
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Number of reviews rated as "Good" or "Easy" for this item type.
    /// </summary>
    public int CorrectReviews { get; set; }

    /// <summary>
    /// The percentage of correct reviews for this item type (0-100).
    /// </summary>
    public double AccuracyRate =>
        TotalReviews > 0 ? (double)CorrectReviews / TotalReviews * 100 : 0;
}

/// <summary>
/// Statistics for a specific tag.
/// </summary>
public class TagStats
{
    /// <summary>
    /// The tag name these statistics represent.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Total number of reviews for items with this tag.
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Number of reviews rated as "Good" or "Easy" for items with this tag.
    /// </summary>
    public int CorrectReviews { get; set; }

    /// <summary>
    /// The percentage of correct reviews for items with this tag (0-100).
    /// </summary>
    public double AccuracyRate =>
        TotalReviews > 0 ? (double)CorrectReviews / TotalReviews * 100 : 0;
}
