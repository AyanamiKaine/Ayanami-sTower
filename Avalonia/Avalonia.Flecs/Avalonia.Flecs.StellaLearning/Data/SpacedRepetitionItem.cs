namespace Avalonia.Flecs.StellaLearning.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Threading;
using FSRSPythonBridge;

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
public class SpacedRepetitionItem : INotifyPropertyChanged
{
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Lorem Ipsum";
    public List<string> Tags { get; set; } = [];
    public float? Stability { get; set; } = 0;
    public float? Difficulty { get; set; } = 0;
    public int Priority { get; set; } = 0;

    /// <summary>
    /// The card's current learning or relearning step or None if the card is in the Review state.
    /// </summary>
    public long? Step { get; set; } = 0;
    public DateTime? LastReview { get; set; } = DateTime.UtcNow;

    public DateTime _nextReview;
    public DateTime NextReview
    {
        get => _nextReview;
        set
        {
            if (_nextReview != value)
            {
                _nextReview = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NextReview)));
            }
        }
    }
    public int NumberOfTimesSeen { get; set; } = 0;
    public int ElapsedDays { get; set; } = 0;
    public int ScheduledDays { get; set; } = 0;
    public SpacedRepetitionState SpacedRepetitionState { get; set; } = SpacedRepetitionState.Learning;
    public SpacedRepetitionItemType SpacedRepetitionItemType { get; set; } = SpacedRepetitionItemType.Text;

    // Backing field for the Card property
    private Card? _card;

    public event PropertyChangedEventHandler? PropertyChanged;

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NextReview)));
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
        Dispatcher.UIThread.Post(() =>
        {
            Card = FSRS.RateCard(Card, Rating.Good);
        });
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