using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    NewState = 0,
    Learning = 1,
    Review = 2,
    Relearning = 3
}
///<summary>
///Defines an SpacedRepetitionItem that can be used for spaced repetition
///</summary>
public class SpacedRepetitionItem
{
    public Guid Uid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Lorem Ipsum";
    public List<string> Tags { get; set; } = [];
    public double Stability { get; set; } = 0;
    public double Difficulty { get; set; } = 0;
    public int Priority { get; set; } = 0;
    public int Reps { get; set; } = 0;
    public int Lapsed { get; set; } = 0;
    public DateTime LastReview { get; set; } = DateTime.UtcNow;
    public DateTime NextReview { get; set; } = DateTime.UtcNow;
    public int NumberOfTimesSeen { get; set; } = 0;
    public int ElapsedDays { get; set; } = 0;
    public int ScheduledDays { get; set; } = 0;
    public SpacedRepetitionState SpacedRepetitionState { get; set; } = SpacedRepetitionState.NewState;
    public SpacedRepetitionItemType SpacedRepetitionItemType { get; set; } = SpacedRepetitionItemType.Text;
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