namespace Avalonia.Flecs.StellaLearning.Data;

using System;
using System.Collections.Generic;
using FSRSPythonBridge;
using CommunityToolkit.Mvvm.ComponentModel;

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
            return _card!;
        }
        set
        {
            _card = value;
            Stability = Card.Stability;
            Difficulty = Card.Difficulty;
            SpacedRepetitionState = (SpacedRepetitionState)Card.State;
            LastReview = Card.LastReview;
            NextReview = Card.Due;
            Step = Card.Step;
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
        Card = FSRS.RateCard(Card, Rating.Good);
        NumberOfTimesSeen++;
    }

    /// <summary>
    /// Forgot the cardw
    /// </summary>
    public void AgainReview()
    {
        Card = FSRS.RateCard(Card, Rating.Again);
        NumberOfTimesSeen++;
    }

    /// <summary>
    /// Remembered the card easily
    /// </summary>
    public void EasyReview()
    {
        Card = FSRS.RateCard(Card, Rating.Easy);
        NumberOfTimesSeen++;
    }

    /// <summary>
    /// Remembered the card with serious difficulty
    /// </summary>
    public void HardReview()
    {
        Card = FSRS.RateCard(Card, Rating.Hard);
        NumberOfTimesSeen++;
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