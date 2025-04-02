using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using System.Collections.ObjectModel;
using FsrsSharp;
using Avalonia.Threading;

namespace Avalonia.Flecs.StellaLearning.Data
{
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
                "StellaLearning");

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
        /// Initializes the stats tracker and loads existing statistics.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                await LoadStatsAsync();
                _isInitialized = true;
                Logger.Info("StatsTracker initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize StatsTracker");
            }
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

            CurrentSession = new StudySession
            {
                StartTime = DateTime.Now,
                SessionId = Guid.NewGuid()
            };

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
                    ItemType = item.SpacedRepetitionItemType
                };

                // Add to current session
                CurrentSession!.Reviews.Add(review);

                // Update item type statistics
                if (!ItemTypeStats.TryGetValue(item.SpacedRepetitionItemType, out ItemTypeStats? typeStats))
                {
                    typeStats = new ItemTypeStats
                    {
                        ItemType = item.SpacedRepetitionItemType
                    };
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

                Logger.Debug("Recorded review for item {ItemName} with rating {Rating}", item.Name, rating);
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
                !dailyStat.ReviewedItemIds.Contains(id));

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
                    Logger.Warn($"Raw calculated accuracy ({calculatedAccuracy:F2}%) was outside the valid 0-100 range before clamping. This indicates an ongoing calculation issue or data inconsistency. TotalCorrect={totalCorrect}, TotalReviews={TotalReviews}.");
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

            if (reviews.Count == 0) return 0;

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
                    if (!itemPerformance.TryGetValue(review.ItemId, out (int TotalReviews, int FailedReviews) current))
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
            return [.. itemPerformance
                .Where(kvp => kvp.Value.TotalReviews >= 3)
                .OrderByDescending(kvp => (double)kvp.Value.FailedReviews / kvp.Value.TotalReviews)
                .Take(count)
                .Select(kvp => kvp.Key)];
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
                result[day] = sessionsOnDay.Aggregate(TimeSpan.Zero,
                    (current, session) => current + session.Duration);
            }

            return result;
        }

        /// <summary>
        /// Gets a forecast of reviews due in the upcoming days.
        /// </summary>
        public static Dictionary<DateTime, int> GetReviewForecast(ObservableCollection<SpacedRepetitionItem> items, int daysAhead = 14)
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
                Logger.Warn("Attempted to save stats before initialization, but an existing file was found. Skipping save to prevent data loss.");
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
                    LastUpdated = DateTime.Now // Update LastUpdated time on save
                };

                var json = JsonSerializer.Serialize(statsData, options);
                await File.WriteAllTextAsync(_statsFilePath, json);

                // Update the LastUpdated property *after* successful save
                LastUpdated = statsData.LastUpdated; // Triggers OnPropertyChanged

                Logger.Debug("Saved statistics to {FilePath}", _statsFilePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save statistics");
                // Consider notifying the user of the save failure
            }
        }

        /// <summary>
        /// Loads statistics from disk.
        /// </summary>
        public async Task LoadStatsAsync()
        {
            try
            {
                if (!File.Exists(_statsFilePath))
                {
                    Logger.Info("No existing statistics file found at {FilePath}. Starting with default stats.", _statsFilePath);
                    // Ensure default values are set if loading fails or file doesn't exist
                    ResetObservablePropertiesToDefault();
                    return;
                }

                var json = await File.ReadAllTextAsync(_statsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warn("Statistics file at {FilePath} is empty. Starting with default stats.", _statsFilePath);
                    ResetObservablePropertiesToDefault();
                    return;
                }

                var statsData = JsonSerializer.Deserialize<StatsData>(json);

                if (statsData != null)
                {
                    // Use Dispatcher for collections if necessary
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        DailyStats = new ObservableCollection<DailyStats>(statsData.DailyStats ?? []);
                        StudySessions = new ObservableCollection<StudySession>(statsData.StudySessions ?? []);
                        // Dictionaries might not need dispatcher if replaced entirely
                        ItemTypeStats = statsData.ItemTypeStats ?? [];
                        TagStats = statsData.TagStats ?? [];
                    });


                    // Update scalar properties - triggers PropertyChanged
                    CurrentStreak = statsData.CurrentStreak;
                    LongestStreak = statsData.LongestStreak;
                    TotalStudyTimeSeconds = statsData.TotalStudyTimeSeconds;
                    TotalReviews = statsData.TotalReviews;
                    OverallAccuracy = statsData.OverallAccuracy;
                    LastUpdated = statsData.LastUpdated;

                    Logger.Info("Loaded statistics from {FilePath}", _statsFilePath);
                }
                else
                {
                    Logger.Error("Failed to deserialize statistics data from {FilePath}. File might be corrupted. Starting with default stats.", _statsFilePath);
                    ResetObservablePropertiesToDefault();
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.Error(jsonEx, "JSON Error loading statistics from {FilePath}. File might be corrupted. Starting with default stats.", _statsFilePath);
                ResetObservablePropertiesToDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load statistics from {FilePath}. Starting with default stats.", _statsFilePath);
                ResetObservablePropertiesToDefault();
            }
        }

        /// <summary>
        /// Resets observable properties to their default values. Used when loading fails.
        /// </summary>
        private void ResetObservablePropertiesToDefault()
        {
            // Use Dispatcher for collections
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DailyStats.Clear();
                StudySessions.Clear();
                ItemTypeStats.Clear();
                TagStats.Clear();
            }).Wait(); // Consider if waiting is necessary

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
                Logger.Warn("Resetting stats while a study session ({SessionId}) was active. Ending the session without saving its final state.", CurrentSession.SessionId);
                CurrentSession = null; // Clears the session without saving its potentially partial data
            }


            // Save the reset state immediately
            await SaveStatsAsync();
            Logger.Info("Learning statistics have been reset and saved.");
        }
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
        public double AccuracyRate => TotalReviews > 0
            ? (double)CorrectReviews / TotalReviews * 100
            : 0;
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
        public double AccuracyRate => TotalReviews > 0
            ? (double)CorrectReviews / TotalReviews * 100
            : 0;
    }
}