namespace Avalonia.Flecs.StellaLearning.Data;

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;

/// <summary>
/// Provides extension methods for ObservableCollection of SpacedRepetitionItem objects.
/// </summary>
/// <summary>
/// Provides extension methods for ObservableCollection of SpacedRepetitionItem objects.
/// </summary>
public static class SpacedRepetitionObservableCollectionExtensions
{
    // Comments explaining randomization remain the same...
    /*
    Why are the first most priority items randomized?

    To avoid the creation of patterns. Sometimes a pattern of
    items can emerge that a closely together. Where you can anticipate
    the next item.

    Why is that bad?

    What can happen is that you associate the anwswer of spaced repetition items
    based on what items came before, making it harder or easier to do active recall.
    This is a real thing that can happen.
    That is why we need to add some randomize.
    */

    /// <summary>
    /// Returns the next spaced repetition item that is due for review, with randomization among top priority items.
    /// </summary>
    /// <param name="spacedRepetitionItems">The collection of spaced repetition items to search through.</param>
    /// <returns>The next item due for review, or null if no items are due or the collection is empty.</returns>
    public static SpacedRepetitionItem? GetNextItemToBeReviewed(this ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        if (spacedRepetitionItems?.Any() != true)
        {
            return null; // Return null if the collection is empty or null
        }

        // Use DateTimeOffset.UtcNow for accurate time comparison
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var random = new Random();

        // Filter using DateTimeOffset
        return spacedRepetitionItems
                .Where(item => item.NextReview <= now)      // Filter for items that are due (compare DateTimeOffset)
                .OrderByDescending(item => item.Priority)  // Order by priority
                .Take(25)                                  // Take top 25 priority items
                .OrderBy(item => random.Next())            // Randomize these top 25
                .FirstOrDefault();                         // Return the first (random) item
    }


    /// <summary>
    /// Returns the next item to be reviewed that has its due date in the future.
    /// </summary>
    /// <returns>The earliest item due in the future, or null if none exist.</returns>
    public static SpacedRepetitionItem? NextItemToBeReviewedInFuture(this ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        if (spacedRepetitionItems?.Any() != true)
        {
            return null;
        }

        // Order by DateTimeOffset
        return spacedRepetitionItems
                .OrderBy(item => item.NextReview)
                .FirstOrDefault();
    }
}

/// <summary>
/// Provides a globally accessible instance of the FSRS Scheduler.
/// Initialize this service once at application startup.
/// </summary>
public static class SchedulerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static FsrsSharp.Scheduler? _instance;

    /// <summary>
    /// Gets the configured Scheduler instance.
    /// Throws an InvalidOperationException if the service has not been initialized.
    /// </summary>
    public static FsrsSharp.Scheduler Instance
    {
        get
        {
            if (_instance == null)
            {
                Logger.Error("SchedulerService accessed before initialization.");
                throw new InvalidOperationException("SchedulerService has not been initialized. Call Initialize() first.");
            }
            return _instance;
        }
    }

    /// <summary>
    /// Initializes the SchedulerService with a specific Scheduler instance.
    /// This should be called once during application startup.
    /// </summary>
    /// <param name="scheduler">The Scheduler instance to use globally.</param>
    public static void Initialize(FsrsSharp.Scheduler scheduler)
    {
        if (_instance != null)
        {
            Logger.Warn("SchedulerService is being initialized more than once.");
            // Decide if re-initialization should be allowed or throw an error
            // For now, allow re-initialization but log a warning.
        }
        if (scheduler == null)
        {
            Logger.Error("Attempted to initialize SchedulerService with a null scheduler.");
            throw new ArgumentNullException(nameof(scheduler));
        }

        Logger.Info("SchedulerService initialized.");
        _instance = scheduler;
    }

    /// <summary>
    /// Resets the service, clearing the current scheduler instance.
    /// Useful for testing or specific shutdown scenarios.
    /// </summary>
    public static void Reset()
    {
        Logger.Info("SchedulerService reset.");
        _instance = null;
    }
}

/// <summary>
/// Represents the type of the spaced repetition item
/// </summary>
public enum SpacedRepetitionItemType
{
    /// <summary>
    /// Cloze
    /// </summary>
    Cloze,
    /// <summary>
    /// Image
    /// </summary>
    Image,
    /// <summary>
    /// ImageCloze
    /// </summary>
    ImageCloze,
    /// <summary>
    /// Video
    /// </summary>
    Video,
    /// <summary>
    /// Audio
    /// </summary>
    Audio,
    /// <summary>
    /// Quiz
    /// </summary>
    Quiz,
    /// <summary>
    /// Flashcard
    /// </summary>
    Flashcard,
    /// <summary>
    /// Text
    /// </summary>
    Text,
    /// <summary>
    /// Exercise
    /// </summary>
    Exercise,
    /// <summary>
    /// File
    /// </summary>
    File,
    /// <summary>
    /// PDF
    /// </summary>
    PDF,
    /// <summary>
    /// Executable
    /// </summary>
    Executable,
}

/// <summary>
/// Represents the state of the spaced repetition item, mapping to FsrsSharp.State
/// </summary>
public enum SpacedRepetitionState
{
    /// <summary>
    /// The item has not been reviewed yet.
    /// </summary>
    New = 0, // Added to match FsrsSharp.State
    /// <summary>
    /// The item is in the learning state.
    /// </summary>
    Learning = 1,
    /// <summary>
    /// The item has graduated from learning.
    /// </summary>
    Review = 2,
    /// <summary>
    /// The item was forgotten and is being relearned.
    /// </summary>
    Relearning = 3
}

///<summary>
///Defines an SpacedRepetitionItem that can be used for spaced repetition
///</summary>
public partial class SpacedRepetitionItem : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The unique identifier of the item
    /// </summary>
    public Guid Uid { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private List<string> _tags = [];

    [ObservableProperty]
    private double? _stability; // Changed to double? to match FsrsSharp.Card

    [ObservableProperty]
    private double? _difficulty; // Changed to double? to match FsrsSharp.Card

    [ObservableProperty]
    private int _priority;

    /// <summary>
    /// The card's current learning or relearning step or null if the card is New or in the Review state.
    /// </summary>
    [ObservableProperty]
    private int? _step = null; // Changed to int? to match FsrsSharp.Card

    [ObservableProperty]
    private DateTimeOffset? _lastReview; // Changed to DateTimeOffset?

    [ObservableProperty]
    private DateTimeOffset _nextReview; // Changed to DateTimeOffset

    [ObservableProperty]
    private int _numberOfTimesSeen;

    // These properties seem less directly related to FSRS state, keeping them as int
    [ObservableProperty]
    private int _elapsedDays;
    [ObservableProperty]
    private int _scheduledDays;

    [ObservableProperty]
    private SpacedRepetitionState _spacedRepetitionState = SpacedRepetitionState.New; // Default to New

    [ObservableProperty]
    private SpacedRepetitionItemType _spacedRepetitionItemType = SpacedRepetitionItemType.Text;

    // Backing field for the Card property using the native C# FSRS card
    private FsrsSharp.Card? _card;

    /// <summary>
    /// Represents a reference to the underlying FsrsSharp.Card object.
    /// Setting this property updates the related ObservableProperties of the SpacedRepetitionItem.
    /// </summary>
    private FsrsSharp.Card Card
    {
        get
        {
            // Ensure the card exists, especially after deserialization
            // If _card is null, try to recreate it from the item's properties
            if (_card == null)
            {
                Logger.Warn("FsrsSharp.Card reference was null. Recreating from SpacedRepetitionItem properties. Item ID: {Uid}", Uid);
                CreateCardFromSpacedRepetitionData();
            }
            // The non-null forgiveness operator (!) assumes CreateCardFromSpacedRepetitionData always succeeds in setting _card.
            // Consider adding null checks or alternative handling if that's not guaranteed.
            return _card!;
        }
        set
        {
            try
            {
                if (value == null)
                {
                    Logger.Warn("Attempted to set Card reference to null. Item ID: {Uid}", Uid);
                    _card = null;
                    // Optionally reset related properties or handle as needed
                    Stability = null;
                    Difficulty = null;
                    Step = null;
                    // Keep state, lastReview, nextReview as they might have been loaded
                    return;
                }

                Logger.Debug("Setting FsrsSharp.Card reference and updating properties from it. Card ID: {CardID}", value.CardId);
                _card = value;

                // Update Observable Properties from the FsrsSharp.Card
                // Note the type conversions/casting
                Stability = _card.Stability; // double? to double?
                Difficulty = _card.Difficulty; // double? to double?
                SpacedRepetitionState = (SpacedRepetitionState)_card.State; // FsrsSharp.State to SpacedRepetitionState
                LastReview = _card.LastReview; // DateTimeOffset? to DateTimeOffset?
                NextReview = _card.Due; // DateTimeOffset to DateTimeOffset
                Step = _card.Step; // int? to int?

                Logger.Debug("Updated properties from Card: State={State}, Stability={Stability}, Difficulty={Difficulty}, Step={Step}, NextReview={NextReview:O}",
                    SpacedRepetitionState, Stability, Difficulty, Step, NextReview);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error setting FsrsSharp.Card reference and updating properties for Item ID: {Uid}", Uid);
                // Decide if re-throwing is appropriate or if the application can recover
                // throw;
            }
        }
    }

    /// <summary>
    /// Default constructor for creating a new spaced repetition item.
    /// Initializes a new underlying FsrsSharp.Card.
    /// </summary>
    public SpacedRepetitionItem()
    {
        Logger.Trace("Creating new SpacedRepetitionItem with new FsrsSharp.Card. Item ID: {Uid}", Uid);
        // Create a new native C# Card (defaults to State.New)
        var newCard = new FsrsSharp.Card();
        // Set the internal card and update observable properties
        Card = newCard; // Use the property setter to update all fields
    }

    /// <summary>
    /// Constructor primarily used for deserialization.
    /// It avoids creating a new FSRS card state immediately, assuming properties
    /// will be set by the deserializer, and the FSRS card will be reconstructed later if needed.
    /// </summary>
    /// <param name="isDeserializing">Flag indicating if this constructor is called during deserialization.</param>
    public SpacedRepetitionItem(bool isDeserializing) : this() // Chain to default constructor initially
    {
        // If deserializing, we don't want the default new card state from the
        // chained constructor to overwrite deserialized values immediately.
        // The properties (Stability, Difficulty, State, LastReview, NextReview, Step)
        // will be set by the deserialization process.
        // The _card backing field will remain null initially after this constructor.
        // The Card property getter will handle recreating the _card object
        // from the deserialized properties when it's first accessed.
        if (isDeserializing)
        {
            Logger.Trace("Creating SpacedRepetitionItem instance for deserialization. Item ID: {Uid}", Uid);
            _card = null; // Ensure card is null, properties will be set by deserializer.
                          // Reset observable properties that might have been set by chained constructor,
                          // allowing deserializer to set the correct values.
            _stability = null;
            _difficulty = null;
            _step = null;
            _lastReview = null;
            _nextReview = default; // Reset to default DateTimeOffset
            _spacedRepetitionState = SpacedRepetitionState.New; // Default, will be overwritten
        }
        // If isDeserializing is false, it behaves like the default constructor.
    }

    /// <summary>
    /// Reconstructs the internal FsrsSharp.Card object from the current
    /// state of the SpacedRepetitionItem properties. Typically used after deserialization
    /// or if the internal card reference needs to be refreshed.
    /// </summary>
    public void CreateCardFromSpacedRepetitionData()
    {
        try
        {
            Logger.Trace("Reconstructing FsrsSharp.Card from SpacedRepetitionItem properties. Item ID: {Uid}", Uid);
            // Create a new card object and populate it from the item's properties
            var reconstructedCard = new FsrsSharp.Card(
                // Assuming Uid doesn't directly map to Fsrs CardId, generate new or handle mapping if needed
                // cardId: this.Uid, // Cannot directly map Guid to long CardId easily. FsrsSharp generates its own long ID.
                // If you need to persist/link based on *your* Uid, store the FsrsSharp CardId separately or handle mapping elsewhere.
                state: (FsrsSharp.State)SpacedRepetitionState,
                step: Step, // int? maps directly
                stability: Stability, // double? maps directly
                difficulty: Difficulty, // double? maps directly
                due: NextReview, // DateTimeOffset maps directly
                lastReview: LastReview // DateTimeOffset? maps directly
            );

            // Assign the reconstructed card back using the property setter
            // This ensures consistency if the setter has additional logic.
            Card = reconstructedCard;
            Logger.Debug("Successfully reconstructed FsrsSharp.Card. Card ID: {CardId}", _card?.CardId);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error reconstructing FsrsSharp.Card from SpacedRepetitionItem properties. Item ID: {Uid}", Uid);
            // Decide on error handling: throw, log, or attempt recovery?
            // For now, log the error. The _card might remain null or in an inconsistent state.
        }
    }

    // --- Review Methods ---
    // These now require a Scheduler instance to perform the rating.

    /// <summary>
    /// Reviews the item with a 'Good' rating using the globally configured FSRS scheduler.
    /// </summary>
    public void GoodReview() // Removed Scheduler parameter
    {
        RateCardInternal(FsrsSharp.Rating.Good);
    }

    /// <summary>
    /// Reviews the item with an 'Again' rating using the globally configured FSRS scheduler.
    /// </summary>
    public void AgainReview() // Removed Scheduler parameter
    {
        RateCardInternal(FsrsSharp.Rating.Again);
    }

    /// <summary>
    /// Reviews the item with an 'Easy' rating using the globally configured FSRS scheduler.
    /// </summary>
    public void EasyReview() // Removed Scheduler parameter
    {
        RateCardInternal(FsrsSharp.Rating.Easy);
    }

    /// <summary>
    /// Reviews the item with a 'Hard' rating using the globally configured FSRS scheduler.
    /// </summary>
    public void HardReview() // Removed Scheduler parameter
    {
        RateCardInternal(FsrsSharp.Rating.Hard);
    }

    /// <summary>
    /// Reviews the item with a 'Good' rating using the provided FSRS scheduler.
    /// </summary>
    /// <param name="scheduler">The FSRS scheduler instance to use for calculations.</param>
    public void GoodReview(FsrsSharp.Scheduler scheduler)
    {
        RateCardInternal(scheduler, FsrsSharp.Rating.Good);
    }

    /// <summary>
    /// Reviews the item with an 'Again' rating using the provided FSRS scheduler.
    /// </summary>
    /// <param name="scheduler">The FSRS scheduler instance to use for calculations.</param>
    public void AgainReview(FsrsSharp.Scheduler scheduler)
    {
        RateCardInternal(scheduler, FsrsSharp.Rating.Again);
    }

    /// <summary>
    /// Reviews the item with an 'Easy' rating using the provided FSRS scheduler.
    /// </summary>
    /// <param name="scheduler">The FSRS scheduler instance to use for calculations.</param>
    public void EasyReview(FsrsSharp.Scheduler scheduler)
    {
        RateCardInternal(scheduler, FsrsSharp.Rating.Easy);
    }

    /// <summary>
    /// Reviews the item with a 'Hard' rating using the provided FSRS scheduler.
    /// </summary>
    /// <param name="scheduler">The FSRS scheduler instance to use for calculations.</param>
    public void HardReview(FsrsSharp.Scheduler scheduler)
    {
        RateCardInternal(scheduler, FsrsSharp.Rating.Hard);
    }

    /// <summary>
    /// Internal helper method to rate the card using the native FSRS scheduler.
    /// </summary>
    /// <param name="scheduler">The scheduler instance.</param>
    /// <param name="rating">The FSRS rating.</param>
    private void RateCardInternal(FsrsSharp.Scheduler scheduler, FsrsSharp.Rating rating)
    {
        if (scheduler == null)
        {
            Logger.Error("Scheduler instance provided to RateCardInternal was null. Item ID: {Uid}", Uid);
            throw new ArgumentNullException(nameof(scheduler));
        }

        try
        {
            // Ensure the internal card object exists before rating
            FsrsSharp.Card currentFsrsCard = Card; // Access via property getter to ensure it's initialized

            Logger.Info("Rating card as {Rating} for item: {ItemName} (ID: {Uid}) using FsrsSharp", rating, Name, Uid);

            // Use the native C# scheduler's ReviewCard method
            // It returns a *new* card object (a clone with updated state)
            var reviewResult = scheduler.ReviewCard(currentFsrsCard, rating, DateTimeOffset.UtcNow);

            // Update the internal card reference and observable properties
            // by assigning the result back via the property setter.
            Card = reviewResult.UpdatedCard;

            NumberOfTimesSeen++;
            Logger.Debug("Card rated as {Rating}. New state: {State}, Next review: {NextReview:O}",
                rating, SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as {Rating} using FsrsSharp for Item ID: {Uid}", rating, Uid);
            // Consider if re-throwing is appropriate for the application context
            throw;
        }
    }

    /// <summary>
    /// Internal helper method to rate the card using the native FSRS scheduler obtained from the service locator.
    /// </summary>
    /// <param name="rating">The FSRS rating.</param>
    private void RateCardInternal(FsrsSharp.Rating rating) // Removed Scheduler parameter
    {
        // Get the scheduler instance from the static service
        FsrsSharp.Scheduler scheduler;
        try
        {
            scheduler = SchedulerService.Instance; // Retrieve the global scheduler instance
        }
        catch (InvalidOperationException ex)
        {
            Logger.Error(ex, "Failed to rate card because SchedulerService is not initialized. Item ID: {Uid}", Uid);
            // Decide how to handle this - maybe throw a more specific exception or return early?
            throw new InvalidOperationException("Cannot review item because the FSRS Scheduler has not been initialized.", ex);
        }
        catch (Exception ex) // Catch other potential exceptions during retrieval
        {
            Logger.Error(ex, "An unexpected error occurred while retrieving the scheduler instance. Item ID: {Uid}", Uid);
            throw; // Re-throw other exceptions
        }


        try
        {
            FsrsSharp.Card currentFsrsCard = Card;

            Logger.Info("Rating card as {Rating} for item: {ItemName} (ID: {Uid}) using FsrsSharp", rating, Name, Uid);

            var reviewResult = scheduler.ReviewCard(currentFsrsCard, rating, DateTimeOffset.UtcNow);

            Card = reviewResult.UpdatedCard; // Assign back using property setter

            NumberOfTimesSeen++;
            Logger.Debug("Card rated as {Rating}. New state: {State}, Next review: {NextReview:O}",
                rating, SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as {Rating} using FsrsSharp for Item ID: {Uid}", rating, Uid);
            throw;
        }
    }
}

// --- Other Spaced Repetition Item Types (Cloze, Flashcard, etc.) ---
// These classes inherit from SpacedRepetitionItem and don't need changes
// unless they directly interacted with the FSRS Python bridge themselves.

/// <summary>
/// Represents a quiz item for spaced repetition
/// </summary>
public class SpacedRepetitionQuiz : SpacedRepetitionItem
{
    /// <summary>
    /// Question
    /// </summary>
    public string Question { get; set; } = "Lorem Ispusm";
    /// <summary>
    /// Answers you can select
    /// </summary>
    public List<string> Answers { get; set; } = ["Lorem Ispusm", "Lorem Ispusmiusm Dorema", "Anwser3", "Anwser4"];
    /// <summary>
    /// Index number for the answer list
    /// </summary>
    public int CorrectAnswerIndex { get; set; } = 0;
}

/// <summary>
/// Represents a cloze item used for spaced repetition
/// </summary>
public class SpacedRepetitionCloze : SpacedRepetitionItem
{
    /// <summary>
    /// Full text
    /// </summary>
    public string FullText = "Lorem Ispusm";
    /// <summary>
    /// Clozes
    /// </summary>
    public List<string> ClozeWords { get; set; } = [];
}

/// <summary>
/// Represents a flashcard item for spaced repetition
/// </summary>
public class SpacedRepetitionFlashcard : SpacedRepetitionItem
{
    /// <summary>
    /// Front text
    /// </summary>
    public string Front { get; set; } = "Front";
    /// <summary>
    /// Back text
    /// </summary>
    public string Back { get; set; } = "Back";
}

/// <summary>
/// Represents a video item for spaced repetition
/// </summary>
public class SpacedRepetitionVideo : SpacedRepetitionItem
{
    /// <summary>
    /// Video URL
    /// </summary>
    public string VideoUrl { get; set; } = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
}

/// <summary>
/// Represents a file item for spaced repetition
/// </summary>
public class SpacedRepetitionFile : SpacedRepetitionItem
{
    /// <summary>
    /// Question
    /// </summary>
    public string Question { get; set; } = "Lorum Ipsum";
    /// <summary>
    /// Filepath
    /// </summary>
    public string FilePath { get; set; } = "C:/Users/YOUR_USERNAME/Documents/EXAMPLE_FILE.txt";
}
/// <summary>
/// Represents an exercise item for spaced repetition
/// </summary>
public class SpacedRepetitionExercise : SpacedRepetitionItem
{
    /// <summary>
    /// Problem
    /// </summary>
    public string Problem { get; set; } = "Lorem Ipsum";
    /// <summary>
    /// Solution
    /// </summary>
    public string Solution { get; set; } = "Lorem Ipsum";
}

/// <summary>
/// Represents an image cloze item for spaced repetition where specific areas of an image need to be identified
/// </summary>
public class SpacedRepetitionImageCloze : SpacedRepetitionItem
{
    /// <summary>
    /// Image path used to find the image when to be displayed.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
    /// <summary>
    /// List of areas to be covered.
    /// </summary>
    public List<ImageClozeArea> ClozeAreas { get; set; } = [];
}

/// <summary>
/// Represents a specific area on an image that needs to be identified
/// </summary>
public class ImageClozeArea
{
    /// <summary>
    /// X Position
    /// </summary>
    public double X { get; set; }
    /// <summary>
    /// Y Position
    /// </summary>
    public double Y { get; set; }
    /// <summary>
    /// Width
    /// </summary>
    public double Width { get; set; }
    /// <summary>
    /// Height
    /// </summary>
    public double Height { get; set; }
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Area Color
    /// </summary>
    public SolidColorBrush FillColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 221, 176, 55));
}