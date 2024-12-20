namespace Avalonia.Flecs.StellaLearning.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Threading;
using FSRSPythonBridge;
using CommunityToolkit.Mvvm.ComponentModel;

public enum SpacedRepetitionItemType
{
    Image,
    Video,
    Quiz,
    Flashcard,
    Text,
    Exercise,
    File,
    PDF,
    Executable,
}

public enum SpacedRepetitionState
{
    Learning = 1,
    Review = 2,
    Relearning = 3
}
///<summary>
///Defines an SpacedRepetitionItem that can be used for spaced repetition
///</summary>
public partial class SpacedRepetitionItem : ObservableObject
{
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
    private long? _step;

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

    public void GoodReview()
    {
        Card = FSRS.RateCard(Card, Rating.Good);
    }

    public void AgainReview()
    {
        Card = FSRS.RateCard(Card, Rating.Again);
    }

    public void EasyReview()
    {
        Card = FSRS.RateCard(Card, Rating.Easy);
    }

    public void HardReview()
    {
        Card = FSRS.RateCard(Card, Rating.Hard);
    }
}

public class SpacedRepetitionQuiz : SpacedRepetitionItem
{
    public string Question { get; set; } = "Lorem Ispusm";
    public List<string> Answers { get; set; } = ["Lorem Ispusm", "Lorem Ispusmiusm Dorema"];
    public int CorrectAnswerIndex { get; set; } = 0;
}

public class SpacedRepetitionFlashcard : SpacedRepetitionItem
{
    public string Front { get; set; } = "Front";
    public string Back { get; set; } = "Back";
}

public class SpacedRepetitionVideo : SpacedRepetitionItem
{
    public string VideoUrl { get; set; } = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
}

public class SpacedRepetitionFile : SpacedRepetitionItem
{
    public string FilePath { get; set; } = "C:/Users/username/Documents/MyFile.txt";
}

public class SpacedRepetitionExercise : SpacedRepetitionItem
{
    public string Problem { get; set; } = "Lorem Ipsum";
    public string Solution { get; set; } = "Lorem Ipsum";
}