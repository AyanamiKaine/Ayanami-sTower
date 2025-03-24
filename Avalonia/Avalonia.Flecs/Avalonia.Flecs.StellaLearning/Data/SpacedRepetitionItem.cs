namespace Avalonia.Flecs.StellaLearning.Data;

using System;
using System.Collections.Generic;
using FSRSPythonBridge;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;

/// <summary>
/// Provides extension methods for ObservableCollection of SpacedRepetitionItem objects.
/// </summary>
public static class SpacedRepetitionObservableCollectionExtensions
{
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

        DateTime now = DateTime.Now;
        var random = new Random();
        
        // Get all due items, take top 5 by priority, then randomize their order
        return spacedRepetitionItems
                .Where(item => item.NextReview <= now)      // Filter for items that are due
                .OrderByDescending(item => item.Priority)   // Order by priority
                .Take(5)                                    // Take top 5 priority items
                .OrderBy(item => random.Next())             // Randomize these top 5
                .FirstOrDefault();                          // Return the first (random) item
    }


    /// <summary>
    /// Returns the next item to be reviewed that has its due date in the future.
    /// </summary>
    /// <returns></returns>
    public static SpacedRepetitionItem? NextItemToBeReviewedInFuture(this ObservableCollection<SpacedRepetitionItem> spacedRepetitionItems)
    {
        if (spacedRepetitionItems?.Any() != true)
        {
            return null;
        }

        return spacedRepetitionItems
                .OrderBy(item => item.NextReview)
                .FirstOrDefault();
    }
}

/// <summary>
/// Represents the type of the spaced repetition item
/// </summary>
public enum SpacedRepetitionItemType
{
    /// <summary>
    /// The item is a cloze
    /// </summary>
    Cloze,

    /// <summary>
    /// The item is an image
    /// </summary>
    Image,
    /// <summary>
    /// The item is an image cloze
    /// </summary>
    ImageCloze,
    /// <summary>
    /// The item is a video
    /// </summary>
    Video,
    /// <summary>
    /// The item is only a audio
    /// </summary>
    Audio,
    /// <summary>
    /// The item is a quiz
    /// </summary>
    Quiz,
    /// <summary>
    /// The item is a flashcard
    /// </summary>
    Flashcard,
    /// <summary>
    /// The item is a text
    /// </summary>
    Text,
    /// <summary>
    /// The item is an exercise
    /// </summary>
    Exercise,
    /// <summary>
    /// The item is a file
    /// </summary>
    File,
    /// <summary>
    /// The item is a PDF
    /// </summary>
    PDF,
    /// <summary>
    /// The item is an executable
    /// </summary>
    Executable,
}
/// <summary>
/// Represents the state of the spaced repetition item
/// </summary>
public enum SpacedRepetitionState
{
    /// <summary>
    /// The card is in the learning state
    /// </summary>
    Learning = 1,
    /// <summary>
    /// The card is in the review state
    /// </summary>
    Review = 2,
    /// <summary>
    /// The card is in the relearning state
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
    private string _name = "Lorem Ipsum";
    [ObservableProperty]
    private List<string> _tags = ["Lorem", "Ipsum"];
    [ObservableProperty]
    private float? _stability;
    [ObservableProperty]
    private float? _difficulty;
    [ObservableProperty]
    private int _priority;

    /// <summary>
    /// The card's current learning or relearning step or None if the card is in the Review state.
    /// </summary>
    [ObservableProperty]
    private long? _step = null;

    [ObservableProperty]
    private DateTime? _lastReview;

    [ObservableProperty]
    private DateTime _nextReview;
    [ObservableProperty]
    private int _numberOfTimesSeen;
    [ObservableProperty]
    private int _elapsedDays;
    [ObservableProperty]
    private int _scheduledDays;
    [ObservableProperty]
    private SpacedRepetitionState _spacedRepetitionState = SpacedRepetitionState.Learning;
    [ObservableProperty]
    private SpacedRepetitionItemType _spacedRepetitionItemType = SpacedRepetitionItemType.Text;

    // Backing field for the Card property
    private Card? _card;

    // Represents a refrence to the underlying representation of the card
    // Here we use a python library to create a card object using FSRS
    private Card Card
    {
        get
        {
            Logger.Trace("Getting Card reference");
            return _card!;
        }
        set
        {
            try
            {
                Logger.Debug("Setting Card reference and updating properties from it. Card ID: {CardID}", value.ID);
                _card = value;
                Stability = Card.Stability;
                Difficulty = Card.Difficulty;
                SpacedRepetitionState = (SpacedRepetitionState)Card.State;
                LastReview = Card.LastReview;
                NextReview = Card.Due;
                Step = Card.Step;
                Logger.Debug("Updated properties from Card: State={State}, Stability={Stability}, Difficulty={Difficulty}",
                    SpacedRepetitionState, Stability, Difficulty);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error setting Card reference and updating properties");
                throw;
            }
        }
    }
    /// <summary>
    /// The name of the item
    /// </summary>
    public SpacedRepetitionItem()
    {
        Card = FSRS.CreateCard();
        Stability = Card.Stability;
        Difficulty = Card.Difficulty;
        SpacedRepetitionState = (SpacedRepetitionState)Card.State;
        LastReview = Card.LastReview;
        NextReview = Card.Due;
        Step = Card.Step;
    }

    /// <summary>
    /// Used for desirialisation for already created and used spaced repetition items
    /// otherwise we would reset the card state to the initial card state that would mean
    /// the spaced repetition would look like it was just created and not already learned
    /// a certain amount of time.
    /// </summary>
    /// <param name="alreadySavedItem"></param>
    public SpacedRepetitionItem(bool alreadySavedItem = true)
    {
        if (!alreadySavedItem)
        {
            Card = FSRS.CreateCard();
            Stability = Card.Stability;
            Difficulty = Card.Difficulty;
            SpacedRepetitionState = (SpacedRepetitionState)Card.State;
            LastReview = Card.LastReview;
            NextReview = Card.Due;
            Step = Card.Step;
        }
    }
    /// <summary>
    /// Converts the data from the spaced repetition item to a FSRS Card
    /// </summary>
    public void CreateCardFromSpacedRepetitionData()
    {
        var initalCard = FSRS.CreateCard();
        initalCard.Stability = Stability;
        initalCard.Difficulty = Difficulty;
        initalCard.State = (CardState)SpacedRepetitionState;
        initalCard.LastReview = LastReview;
        initalCard.Due = NextReview;
        initalCard.Step = Step;
        Card = initalCard;
    }

    /// <summary>
    /// Remembered the card after a hesitation
    /// </summary>
    public void GoodReview()
    {
        try
        {
            Logger.Info("Rating card as Good for item: {ItemName} (ID: {Uid})", Name, Uid);
            Card = FSRS.RateCard(Card, Rating.Good);
            NumberOfTimesSeen++;
            Logger.Debug("Card rated as Good. New state: {State}, Next review: {NextReview}",
                SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as Good");
            throw;
        }
    }

    /// <summary>
    /// Forgot the cardw
    /// </summary>
    public void AgainReview()
    {
        try
        {
            Logger.Info("Rating card as Again for item: {ItemName} (ID: {Uid})", Name, Uid);
            Card = FSRS.RateCard(Card, Rating.Again);
            NumberOfTimesSeen++;
            Logger.Debug("Card rated as Again. New state: {State}, Next review: {NextReview}",
                SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as Again");
            throw;
        }
    }

    /// <summary>
    /// Remembered the card easily
    /// </summary>
    public void EasyReview()
    {
        try
        {
            Logger.Info("Rating card as Easy for item: {ItemName} (ID: {Uid})", Name, Uid);
            Card = FSRS.RateCard(Card, Rating.Easy);
            NumberOfTimesSeen++;
            Logger.Debug("Card rated as Easy. New state: {State}, Next review: {NextReview}",
                SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as Easy");
            throw;
        }
    }

    /// <summary>
    /// Remembered the card with serious difficulty
    /// </summary>
    public void HardReview()
    {
        try
        {
            Logger.Info("Rating card as Hard for item: {ItemName} (ID: {Uid})", Name, Uid);
            Card = FSRS.RateCard(Card, Rating.Hard);
            NumberOfTimesSeen++;
            Logger.Debug("Card rated as Hard. New state: {State}, Next review: {NextReview}",
                SpacedRepetitionState, NextReview);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error rating card as Hard");
            throw;
        }
    }
}

/// <summary>
/// Represents a quiz item for spaced repetition
/// </summary>
public class SpacedRepetitionQuiz : SpacedRepetitionItem
{
    /// <summary>
    /// The question of the quiz
    /// </summary>
    public string Question { get; set; } = "Lorem Ispusm";
    /// <summary>
    /// The answers of the quiz
    /// </summary>
    public List<string> Answers { get; set; } = ["Lorem Ispusm", "Lorem Ispusmiusm Dorema", "Anwser3", "Anwser4"];
    /// <summary>
    /// The index of the correct answer
    /// </summary>
    public int CorrectAnswerIndex { get; set; } = 0;
}

/// <summary>
/// Represents a cloze item used for spaced repetition
/// </summary>
public class SpacedRepetitionCloze : SpacedRepetitionItem
{
    /// <summary>
    /// The Full Text where certain words are hidden based if they appear in
    /// in the ClozeWords list
    /// </summary>
    public string FullText = "Lorem Ispusm";

    /// <summary>
    /// A list of words to be hidden and asked to correctly fill out.
    /// </summary>
    public List<string> ClozeWords { get; set; } = [];
}

/// <summary>
/// Represents a flashcard item for spaced repetition
/// </summary>
public class SpacedRepetitionFlashcard : SpacedRepetitionItem
{
    /// <summary>
    /// The front and back of the flashcard
    /// </summary>
    public string Front { get; set; } = "Front";
    /// <summary>
    /// The back of the flashcard
    /// </summary>
    public string Back { get; set; } = "Back";
}

/// <summary>
/// Represents a text item for spaced repetition
/// </summary>
public class SpacedRepetitionVideo : SpacedRepetitionItem
{
    /// <summary>
    /// The URL of the video
    /// </summary>
    public string VideoUrl { get; set; } = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
}
/// <summary>
/// Represents a text item for spaced repetition
/// </summary>
public class SpacedRepetitionFile : SpacedRepetitionItem
{
    /// <summary>
    /// Question that is shown.
    /// </summary>
    public string Question { get; set; } = "Lorum Ipsum";
    /// <summary>
    /// The path to the file
    /// </summary>
    public string FilePath { get; set; } = "C:/Users/YOUR_USERNAME/Documents/EXAMPLE_FILE.txt";
}
/// <summary>
/// Represents a text item for spaced repetition
/// </summary>
public class SpacedRepetitionExercise : SpacedRepetitionItem
{
    /// <summary>
    /// The exercise problem
    /// </summary>
    public string Problem { get; set; } = "Lorem Ipsum";
    /// <summary>
    /// The exercise solution
    /// </summary>
    public string Solution { get; set; } = "Lorem Ipsum";
}

/// <summary>
/// Represents an image cloze item for spaced repetition where specific areas of an image need to be identified
/// </summary>
public class SpacedRepetitionImageCloze : SpacedRepetitionItem
{
    /// <summary>
    /// Path to the image file
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Collection of cloze areas on the image
    /// </summary>
    public List<ImageClozeArea> ClozeAreas { get; set; } = [];
}

/// <summary>
/// Represents a specific area on an image that needs to be identified
/// </summary>
public class ImageClozeArea
{
    /// <summary>
    /// X-coordinate of the cloze area (left position)
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y-coordinate of the cloze area (top position)
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Width of the cloze area
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the cloze area
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Text associated with this cloze area
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The color used to fill the cloze area when displayed
    /// </summary>
    public SolidColorBrush FillColor { get; set; } = new SolidColorBrush(Color.FromArgb(
            a: 255,
            r: 221,
            g: 176,
            b: 55));
}