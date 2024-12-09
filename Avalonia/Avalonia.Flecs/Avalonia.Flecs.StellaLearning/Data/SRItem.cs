using System;

namespace Avalonia.Flecs.StellaLearning.Data
{
    public enum SRItemState
    {
        NewState,
        Learning,
        Review,
        Relearning,
    }

    public record struct SRItem(
        Guid Id,
        string Name,
        string Description,
        double Stability,
        double Difficulty,
        double Priority,
        int Repetitions,
        int Lapsed,
        DateTime LastReviewDate,
        DateTime NextReviewDate,
        int NumberOfTimesReviewed,
        int ElapsedDays,
        int ScheduledDays);
}