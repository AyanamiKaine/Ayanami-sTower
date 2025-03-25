using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using System.Collections.ObjectModel;
using FSRSPythonBridge;

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
        private ObservableCollection<DailyStats> _dailyStats = new();

        /// <summary>
        /// Records of study sessions.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<StudySession> _studySessions = new();

        /// <summary>
        /// Statistics about different card types.
        /// </summary>
        [ObservableProperty]
        private Dictionary<SpacedRepetitionItemType, ItemTypeStats> _itemTypeStats = new();

        /// <summary>
        /// Statistics about tags and their performance.
        /// </summary>
        [ObservableProperty]
        private Dictionary<string, TagStats> _tagStats = new();

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
        /// Total time spent studying in minutes.
        /// </summary>
        [ObservableProperty]
        private long _totalStudyTimeMinutes;

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
            _statsFilePath = Path.Combine(appDataPath, "learning_stats.json");

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

            // Update total study time
            TotalStudyTimeMinutes += (long)CurrentSession.Duration.TotalMinutes;

            // Add to sessions collection
            StudySessions.Add(CurrentSession);

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
                if (!ItemTypeStats.ContainsKey(item.SpacedRepetitionItemType))
                {
                    ItemTypeStats[item.SpacedRepetitionItemType] = new ItemTypeStats
                    {
                        ItemType = item.SpacedRepetitionItemType
                    };
                }

                var typeStats = ItemTypeStats[item.SpacedRepetitionItemType];
                typeStats.TotalReviews++;
                if ((int)rating >= 3) // Good or Easy
                {
                    typeStats.CorrectReviews++;
                }

                // Update tag statistics
                foreach (var tag in item.Tags)
                {
                    if (!TagStats.ContainsKey(tag))
                    {
                        TagStats[tag] = new TagStats { Tag = tag };
                    }

                    var tagStat = TagStats[tag];
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
        /// Updates the overall accuracy based on all reviews.
        /// </summary>
        private void UpdateOverallAccuracy()
        {
            var totalCorrect = StudySessions.SelectMany(s => s.Reviews).Count(r => r.Rating >= 3);
            if (TotalReviews > 0)
            {
                OverallAccuracy = (double)totalCorrect / TotalReviews * 100;
            }
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
                    if (!itemPerformance.ContainsKey(review.ItemId))
                    {
                        itemPerformance[review.ItemId] = (0, 0);
                    }

                    var current = itemPerformance[review.ItemId];
                    var newTotal = current.TotalReviews + 1;
                    var newFailed = current.FailedReviews + (review.Rating < 3 ? 1 : 0);

                    itemPerformance[review.ItemId] = (newTotal, newFailed);
                }
            }

            // Filter for items with at least 3 reviews
            return itemPerformance
                .Where(kvp => kvp.Value.TotalReviews >= 3)
                .OrderByDescending(kvp => (double)kvp.Value.FailedReviews / kvp.Value.TotalReviews)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets study time statistics by day of week.
        /// </summary>
        public Dictionary<DayOfWeek, TimeSpan> GetStudyTimeByDayOfWeek()
        {
            var result = new Dictionary<DayOfWeek, TimeSpan>();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var sessionsOnDay = StudySessions.Where(s => s.StartTime.DayOfWeek == day);
                var totalTime = sessionsOnDay.Aggregate(TimeSpan.Zero,
                    (current, session) => current + session.Duration);

                result[day] = totalTime;
            }

            return result;
        }

        /// <summary>
        /// Gets a forecast of reviews due in the upcoming days.
        /// </summary>
        public Dictionary<DateTime, int> GetReviewForecast(ObservableCollection<SpacedRepetitionItem> items, int daysAhead = 14)
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
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var statsData = new StatsData
                {
                    DailyStats = [.. DailyStats],
                    StudySessions = [.. StudySessions],
                    ItemTypeStats = ItemTypeStats,
                    TagStats = TagStats,
                    CurrentStreak = CurrentStreak,
                    LongestStreak = LongestStreak,
                    TotalStudyTimeMinutes = TotalStudyTimeMinutes,
                    TotalReviews = TotalReviews,
                    OverallAccuracy = OverallAccuracy,
                    LastUpdated = DateTime.Now
                };

                var json = JsonSerializer.Serialize(statsData, options);
                await File.WriteAllTextAsync(_statsFilePath, json);

                Logger.Debug("Saved statistics to {FilePath}", _statsFilePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save statistics");
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
                    Logger.Info("No existing statistics file found at {FilePath}", _statsFilePath);
                    return;
                }

                var json = await File.ReadAllTextAsync(_statsFilePath);
                var statsData = JsonSerializer.Deserialize<StatsData>(json);

                if (statsData != null)
                {
                    DailyStats = [.. statsData.DailyStats];
                    StudySessions = [.. statsData.StudySessions];
                    ItemTypeStats = statsData.ItemTypeStats;
                    TagStats = statsData.TagStats;
                    CurrentStreak = statsData.CurrentStreak;
                    LongestStreak = statsData.LongestStreak;
                    TotalStudyTimeMinutes = statsData.TotalStudyTimeMinutes;
                    TotalReviews = statsData.TotalReviews;
                    OverallAccuracy = statsData.OverallAccuracy;
                    LastUpdated = statsData.LastUpdated;

                    Logger.Info("Loaded statistics from {FilePath}", _statsFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load statistics");
            }
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
        /// Total time spent studying across all sessions, in minutes.
        /// </summary>
        public long TotalStudyTimeMinutes { get; set; }

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