using System.Globalization;
using System.Text;

namespace FsrsSharp;

/// <summary>
/// Static class holding constants for the FSRS algorithm.
/// </summary>
public static class FsrsConstants
{
    /// <summary>
    /// Default parameters (weights) for the FSRS algorithm.
    /// </summary>
    public static readonly double[] DefaultParameters =
    [
        0.40255,
        1.18385,
        3.173,
        15.69105,
        7.1949,
        0.5345,
        1.4604,
        0.0046,
        1.54575,
        0.1192,
        1.01925,
        1.9395,
        0.11,
        0.29605,
        2.2698,
        0.2315,
        2.9898,
        0.51655,
        0.6621,
    ];

    /// <summary>
    /// Decay constant used in retrievability calculation.
    /// </summary>
    public const double Decay = -0.5;

    /// <summary>
    /// Factor derived from Decay, used in interval calculation.
    /// </summary>
    public static readonly double Factor = Math.Pow(0.9, 1 / Decay) - 1;

    /// <summary>
    /// Represents a range for applying fuzziness to intervals.
    /// </summary>
    public struct FuzzRange
    {
        /// <summary>
        /// Start range
        /// </summary>
        public double Start;

        /// <summary>
        /// End range
        /// </summary>
        public double End;

        /// <summary>
        /// Factor for the ranges
        /// </summary>
        public double Factor;
    }

    /// <summary>
    /// Defines ranges and factors for applying fuzziness to calculated intervals.
    /// </summary>
    public static readonly FuzzRange[] FuzzRanges =
    [
        new()
        {
            Start = 2.5,
            End = 7.0,
            Factor = 0.15,
        },
        new()
        {
            Start = 7.0,
            End = 20.0,
            Factor = 0.1,
        },
        new()
        {
            Start = 20.0,
            End = double.PositiveInfinity,
            Factor = 0.05,
        },
    ];
}

/// <summary>
/// Enum representing the learning state of a Card object.
/// Corresponds to Python's State IntEnum.
/// </summary>
public enum State
{
    /// <summary>
    /// All cards start with the new state
    /// </summary>
    New = 0,

    /// <summary>
    /// First state for cards
    /// </summary>
    Learning = 1,

    /// <summary>
    /// Learned cards get reviewd over time
    /// </summary>
    Review = 2,

    /// <summary>
    /// Should reviewed cards be reviewed badly they go into a relearning state.
    /// </summary>
    Relearning = 3,
}

/// <summary>
/// Enum representing the four possible ratings when reviewing a card.
/// Corresponds to Python's Rating IntEnum.
/// </summary>
public enum Rating
{
    /// <summary>
    /// forgot the card
    /// </summary>
    Again = 1,

    /// <summary>
    /// remembered the card with serious difficulty
    /// </summary>
    Hard = 2,

    /// <summary>
    /// remembered the card after a hesitation
    /// </summary>
    Good = 3,

    /// <summary>
    /// remembered the card easily
    /// </summary>
    Easy = 4,
}

/// <summary>
/// Represents a flashcard in the FSRS system.
/// Corresponds to Python's Card class.
/// </summary>
public class Card : ICloneable // Implement ICloneable for easy copying
{
    /// <summary>
    /// The unique identifier for the card.
    /// </summary>
    public long CardId { get; set; }

    /// <summary>
    /// The card's current learning state.
    /// </summary>
    public State State { get; set; }

    /// <summary>
    /// The card's current learning or relearning step, or null if the card is New or in the Review state.
    /// </summary>
    public int? Step { get; set; } = 0;

    /// <summary>
    /// Core mathematical parameter used for future scheduling (memory stability). Null if not calculated yet.
    /// </summary>
    public double? Stability { get; set; }

    /// <summary>
    /// Core mathematical parameter used for future scheduling (item difficulty). Null if not calculated yet.
    /// </summary>
    public double? Difficulty { get; set; }

    /// <summary>
    /// The date and time when the card is due next (UTC).
    /// </summary>
    public DateTimeOffset Due { get; set; }

    /// <summary>
    /// The date and time of the card's last review (UTC), or null if never reviewed.
    /// </summary>
    public DateTimeOffset? LastReview { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Card"/> class.
    /// </summary>
    /// <param name="cardId">Optional card ID. If null, generated from current UTC time.</param>
    /// <param name="state">Initial state. Defaults to New.</param> // MODIFIED Default
    /// <param name="step">Initial step. Should be null for New/Review states.</param> // MODIFIED Comment
    /// <param name="stability">Initial stability.</param>
    /// <param name="difficulty">Initial difficulty.</param>
    /// <param name="due">Initial due date. If null, defaults to current UTC time.</param>
    /// <param name="lastReview">Initial last review date.</param>
    public Card(
        long? cardId = null,
        State state = State.New,
        int? step = 0,
        double? stability = null,
        double? difficulty = null,
        DateTimeOffset? due = null,
        DateTimeOffset? lastReview = null
    )
    {
        if (cardId == null)
        {
            CardId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Thread.Sleep(1);
        }
        else
        {
            CardId = cardId.Value;
        }

        State = state;

        // Step should generally be null unless explicitly set for an existing Learning/Relearning card
        // It will be set to 0 during the first review if the card starts as New.
        Step = step; // MODIFIED Logic: No default step setting here

        Stability = stability;
        Difficulty = difficulty;

        Due = due ?? DateTimeOffset.UtcNow;
        LastReview = lastReview;
    }

    /// <summary>
    /// Creates a string representation of the Card object.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"{nameof(Card)}("
            + $"CardId={CardId}, "
            + $"State={State}, "
            + $"Step={(Step.HasValue ? Step.Value.ToString() : "null")}, "
            + $"Stability={(Stability.HasValue ? Stability.Value.ToString("F5", CultureInfo.InvariantCulture) : "null")}, "
            + $"Difficulty={(Difficulty.HasValue ? Difficulty.Value.ToString("F5", CultureInfo.InvariantCulture) : "null")}, "
            + $"Due={Due:O}, "
            + // ISO 8601 format
            $"LastReview={(LastReview.HasValue ? LastReview.Value.ToString("O") : "null")})";
    }

    /// <summary>
    /// Converts the Card object to a dictionary suitable for serialization.
    /// </summary>
    /// <returns>A dictionary representation of the Card.</returns>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>
        {
            { "card_id", CardId },
            { "state", (int)State },
            { "step", Step }, // Nullable int
            { "stability", Stability }, // Nullable double
            { "difficulty", Difficulty }, // Nullable double
            { "due", Due.ToString("O") }, // ISO 8601 format preserves offset
            {
                "last_review",
                LastReview?.ToString("O")
            } // Nullable DateTimeOffset
            ,
        };
    }

    /// <summary>
    /// Creates a Card object from a dictionary representation.
    /// </summary>
    /// <param name="sourceDict">The source dictionary.</param>
    /// <returns>A new Card object.</returns>
    /// <exception cref="ArgumentException">Thrown if dictionary contains invalid data.</exception>
    public static Card FromDictionary(Dictionary<string, object?> sourceDict)
    {
        try
        {
            long cardId = Convert.ToInt64(sourceDict["card_id"]);
            State state = (State)Convert.ToInt32(sourceDict["state"]);
            int? step =
                sourceDict["step"] == null ? (int?)null : Convert.ToInt32(sourceDict["step"]);
            double? stability =
                sourceDict["stability"] == null
                    ? (double?)null
                    : Convert.ToDouble(sourceDict["stability"], CultureInfo.InvariantCulture);
            double? difficulty =
                sourceDict["difficulty"] == null
                    ? (double?)null
                    : Convert.ToDouble(sourceDict["difficulty"], CultureInfo.InvariantCulture);
            DateTimeOffset due = DateTimeOffset.Parse(
                sourceDict["due"]!.ToString()!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
            DateTimeOffset? lastReview =
                sourceDict["last_review"] == null
                    ? (DateTimeOffset?)null
                    : DateTimeOffset.Parse(
                        sourceDict["last_review"]!.ToString()!,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind
                    );

            return new Card(
                cardId: cardId,
                state: state,
                step: step,
                stability: stability,
                difficulty: difficulty,
                due: due,
                lastReview: lastReview
            );
        }
        catch (Exception ex)
            when (ex is KeyNotFoundException || ex is FormatException || ex is InvalidCastException)
        {
            throw new ArgumentException(
                "Source dictionary contains invalid or missing data for Card creation.",
                nameof(sourceDict),
                ex
            );
        }
    }

    /// <summary>
    /// Calculates the Card object's current retrievability for a given date and time.
    /// Retrievability is the predicted probability of recalling the card correctly.
    /// </summary>
    /// <param name="currentDateTime">The current date and time (UTC). If null, uses DateTimeOffset.UtcNow.</param>
    /// <returns>The retrievability (probability between 0 and 1).</returns>
    public double GetRetrievability(DateTimeOffset? currentDateTime = null)
    {
        // If card is new or hasn't been reviewed, retrievability is not really defined.
        // Python returns 0, but 1.0 (perfect recall assumed before first lapse) might also make sense.
        // Let's stick closer to Python here for consistency, returning 0.
        // MODIFIED: Changed back to return 0 for consistency with Python test expectation
        if (State == State.New || !LastReview.HasValue || !Stability.HasValue || Stability <= 0)
        {
            return 0.0; // Return 0 if card is new or stability not set
        }

        DateTimeOffset now = currentDateTime ?? DateTimeOffset.UtcNow;
        double elapsedDays = Math.Max(0, (now - LastReview.Value).TotalDays);

        // Formula: R = (1 + factor * t / S) ^ decay
        return Math.Pow(
            1 + FsrsConstants.Factor * elapsedDays / Stability.Value,
            FsrsConstants.Decay
        );
    }

    /// <summary>
    /// Creates a shallow copy of the current Card object.
    /// </summary>
    /// <returns>A new Card object with the same property values.</returns>
    public object Clone()
    {
        return MemberwiseClone();
    }
}

/// <summary>
/// Represents the log entry of a Card object that has been reviewed.
/// Corresponds to Python's ReviewLog class.
/// </summary>
public class ReviewLog
{
    /// <summary>
    /// The ID of the card being reviewed.
    /// </summary>
    public long CardId { get; set; }

    /// <summary>
    /// The rating given to the card during the review.
    /// </summary>
    public Rating Rating { get; set; }

    /// <summary>
    /// The date and time of the review (UTC).
    /// </summary>
    public DateTimeOffset ReviewDateTime { get; set; }

    /// <summary>
    /// The number of milliseconds it took to review the card, or null if unspecified.
    /// </summary>
    public int? ReviewDurationMs { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReviewLog"/> class.
    /// </summary>
    /// <param name="cardId">The ID of the reviewed card.</param>
    /// <param name="rating">The rating given.</param>
    /// <param name="reviewDateTime">The time of the review (UTC).</param>
    /// <param name="reviewDurationMs">Optional duration in milliseconds.</param>
    public ReviewLog(
        long cardId,
        Rating rating,
        DateTimeOffset reviewDateTime,
        int? reviewDurationMs = null
    )
    {
        CardId = cardId;
        Rating = rating;
        ReviewDateTime = reviewDateTime;
        ReviewDurationMs = reviewDurationMs;
    }

    /// <summary>
    /// Creates a string representation of the ReviewLog object.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"{nameof(ReviewLog)}("
            + $"CardId={CardId}, "
            + $"Rating={Rating}, "
            + $"ReviewDateTime={ReviewDateTime:O}, "
            + // ISO 8601 format
            $"ReviewDurationMs={(ReviewDurationMs.HasValue ? ReviewDurationMs.Value.ToString() : "null")})";
    }

    /// <summary>
    /// Converts the ReviewLog object to a dictionary suitable for serialization.
    /// </summary>
    /// <returns>A dictionary representation of the ReviewLog.</returns>
    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>
        {
            { "card_id", CardId },
            { "rating", (int)Rating },
            { "review_datetime", ReviewDateTime.ToString("O") }, // ISO 8601 format
            {
                "review_duration",
                ReviewDurationMs
            } // Nullable int
            ,
        };
    }

    /// <summary>
    /// Creates a ReviewLog object from a dictionary representation.
    /// </summary>
    /// <param name="sourceDict">The source dictionary.</param>
    /// <returns>A new ReviewLog object.</returns>
    /// <exception cref="ArgumentException">Thrown if dictionary contains invalid data.</exception>
    public static ReviewLog FromDictionary(Dictionary<string, object?> sourceDict)
    {
        try
        {
            long cardId = Convert.ToInt64(sourceDict["card_id"]);
            Rating rating = (Rating)Convert.ToInt32(sourceDict["rating"]);
            DateTimeOffset reviewDateTime = DateTimeOffset.Parse(
                sourceDict["review_datetime"]!.ToString()!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            );
            int? reviewDurationMs =
                sourceDict["review_duration"] == null
                    ? (int?)null
                    : Convert.ToInt32(sourceDict["review_duration"]);

            return new ReviewLog(
                cardId: cardId,
                rating: rating,
                reviewDateTime: reviewDateTime,
                reviewDurationMs: reviewDurationMs
            );
        }
        catch (Exception ex)
            when (ex is KeyNotFoundException || ex is FormatException || ex is InvalidCastException)
        {
            throw new ArgumentException(
                "Source dictionary contains invalid or missing data for ReviewLog creation.",
                nameof(sourceDict),
                ex
            );
        }
    }
}

/// <summary>
/// The FSRS scheduler. Enables reviewing and scheduling cards according to the FSRS algorithm.
/// Corresponds to Python's Scheduler class.
/// </summary>
public class Scheduler
{
    /// <summary>
    /// The 19 model weights (parameters) of the FSRS scheduler.
    /// </summary>
    public double[] Parameters { get; private set; }

    /// <summary>
    /// The desired retention rate (e.g., 0.9 for 90%) for scheduling.
    /// </summary>
    public double DesiredRetention { get; private set; }

    /// <summary>
    /// Time intervals used for scheduling cards in the Learning state.
    /// </summary>
    public TimeSpan[] LearningSteps { get; private set; }

    /// <summary>
    /// Time intervals used for scheduling cards in the Relearning state.
    /// </summary>
    public TimeSpan[] RelearningSteps { get; private set; }

    /// <summary>
    /// The maximum number of days a Review-state card can be scheduled into the future.
    /// </summary>
    public int MaximumIntervalDays { get; private set; }

    /// <summary>
    /// Whether to apply a small amount of random 'fuzz' to calculated intervals.
    /// </summary>
    public bool EnableFuzzing { get; private set; }

    private readonly Random _random = new Random(); // For fuzzing

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    /// <param name="parameters">The 19 FSRS model parameters. Uses defaults if null.</param>
    /// <param name="desiredRetention">Desired retention rate (0 to 1). Defaults to 0.9.</param>
    /// <param name="learningSteps">Time intervals for the Learning state. Uses defaults if null.</param>
    /// <param name="relearningSteps">Time intervals for the Relearning state. Uses defaults if null.</param>
    /// <param name="maximumIntervalDays">Maximum scheduling interval in days. Defaults to 36500.</param>
    /// <param name="enableFuzzing">Enable interval fuzzing. Defaults to true.</param>
    /// <exception cref="ArgumentException">Thrown if parameters array length is not 19.</exception>
    public Scheduler(
        IEnumerable<double>? parameters = null,
        double desiredRetention = 0.9,
        IEnumerable<TimeSpan>? learningSteps = null,
        IEnumerable<TimeSpan>? relearningSteps = null,
        int maximumIntervalDays = 36500,
        bool enableFuzzing = true
    )
    {
        Parameters = [.. parameters ?? FsrsConstants.DefaultParameters];
        if (Parameters.Length != 19) // FSRS v4 has 19 parameters
        {
            throw new ArgumentException(
                $"Parameters array must contain exactly 19 values, but found {Parameters.Length}.",
                nameof(parameters)
            );
        }

        DesiredRetention = Math.Clamp(desiredRetention, 0.01, 0.99); // Keep retention within a reasonable range
        LearningSteps = [.. 
            learningSteps ?? [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10)]
        ];
        RelearningSteps = [.. relearningSteps ?? [TimeSpan.FromMinutes(10)]];
        MaximumIntervalDays = Math.Max(1, maximumIntervalDays); // Ensure at least 1 day
        EnableFuzzing = enableFuzzing;
    }

    /// <summary>
    /// Creates a string representation of the Scheduler object.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"{nameof(Scheduler)}(");
        sb.Append(
            $"Parameters=[{string.Join(", ", Parameters.Select(p => p.ToString("F5", CultureInfo.InvariantCulture)))}], "
        );
        sb.Append(
            $"DesiredRetention={DesiredRetention.ToString("F2", CultureInfo.InvariantCulture)}, "
        );
        sb.Append(
            $"LearningSteps=[{string.Join(", ", LearningSteps.Select(ts => ts.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture) + "m"))}], "
        );
        sb.Append(
            $"RelearningSteps=[{string.Join(", ", RelearningSteps.Select(ts => ts.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture) + "m"))}], "
        );
        sb.Append($"MaximumIntervalDays={MaximumIntervalDays}, ");
        sb.Append($"EnableFuzzing={EnableFuzzing})");
        return sb.ToString();
    }

    /// <summary>
    /// Reviews a card with a given rating at a given time.
    /// </summary>
    /// <param name="card">The card being reviewed. The returned card is a modified clone.</param> // MODIFIED Comment
    /// <param name="rating">The chosen rating for the review.</param>
    /// <param name="reviewDateTime">The date and time of the review (UTC). If null, uses DateTimeOffset.UtcNow.</param>
    /// <param name="reviewDurationMs">Optional duration in milliseconds.</param>
    /// <returns>A tuple containing the updated Card (a clone) and the corresponding ReviewLog.</returns> // MODIFIED Comment
    /// <exception cref="ArgumentNullException">Thrown if card is null.</exception>
    /// <exception cref="ArgumentException">Thrown if reviewDateTime is not UTC.</exception>
    public (Card UpdatedCard, ReviewLog Log) ReviewCard(
        Card card, // Pass the original card
        Rating rating,
        DateTimeOffset? reviewDateTime = null,
        int? reviewDurationMs = null
    )
    {
        if (card == null)
            throw new ArgumentNullException(nameof(card));

        DateTimeOffset now = reviewDateTime ?? DateTimeOffset.UtcNow;
        if (now.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Review DateTimeOffset must be in UTC (Offset zero).",
                nameof(reviewDateTime)
            );
        }

        // Create a clone to work with, leaving the original card unmodified.
        Card workingCard = (Card)card.Clone();

        double? daysSinceLastReview = workingCard.LastReview.HasValue
            ? (now - workingCard.LastReview.Value).TotalDays
            : null;

        var reviewLog = new ReviewLog(
            cardId: workingCard.CardId,
            rating: rating,
            reviewDateTime: now,
            reviewDurationMs: reviewDurationMs
        );

        State previousState = workingCard.State; // State of the card *before* this review

        // --- Update Stability (S) and Difficulty (D) based on previous state ---
        // MODIFIED Logic Block Start
        if (previousState == State.New)
        {
            // First review of a new card: Initialize S and D
            workingCard.Stability = InitialStability(rating);
            workingCard.Difficulty = InitialDifficulty(rating);
            // Transition state to Learning and set initial step
            workingCard.State = State.Learning;
            workingCard.Step = 0;
        }
        else // Card was previously Learning, Review, or Relearning
        {
            // Card has existing S and D (assume they are not null if not New)
            if (!workingCard.Stability.HasValue || !workingCard.Difficulty.HasValue)
            {
                // This case should ideally not happen if cards are always created as New
                // and transition correctly. Throw an exception or handle as an error.
                throw new InvalidOperationException(
                    $"Card in state {previousState} has null Stability or Difficulty."
                );
            }

            double retrievability = workingCard.GetRetrievability(now);

            if (daysSinceLastReview.HasValue && daysSinceLastReview < 1.0)
            {
                // Reviewed again on the same day (short-term update)
                workingCard.Stability = ShortTermStability(workingCard.Stability.Value, rating);
                // Difficulty update is the same regardless of short/long term review timing
                workingCard.Difficulty = NextDifficulty(workingCard.Difficulty.Value, rating);
            }
            else
            {
                // Standard update for reviews > 1 day apart or first review after New state
                workingCard.Stability = NextStability(
                    workingCard.Difficulty.Value,
                    workingCard.Stability.Value,
                    retrievability,
                    rating
                );
                workingCard.Difficulty = NextDifficulty(workingCard.Difficulty.Value, rating);
            }
        }
        // MODIFIED Logic Block End


        // --- Determine next state and schedule interval based on the *current* review ---
        // Note: workingCard.State might have changed from New to Learning in the block above

        TimeSpan nextInterval;

        switch (workingCard.State) // Use the potentially updated state
        {
            case State.Learning:
                // Ensure Step has a value; it should have been set to 0 if transitioned from New
                if (!workingCard.Step.HasValue)
                {
                    throw new InvalidOperationException(
                        "Card is in Learning state but Step is null."
                    );
                }

                bool learningStepsExhausted =
                    LearningSteps.Length == 0 || (workingCard.Step.Value >= LearningSteps.Length);

                if (learningStepsExhausted && rating >= Rating.Hard)
                {
                    workingCard.State = State.Review;
                    workingCard.Step = null;
                    int intervalDays = NextInterval(workingCard.Stability!.Value); // S guaranteed non-null here
                    nextInterval = TimeSpan.FromDays(intervalDays);
                }
                else
                {
                    if (rating == Rating.Again)
                    {
                        workingCard.Step = 0;
                        nextInterval =
                            LearningSteps.Length > 0 ? LearningSteps[0] : TimeSpan.FromMinutes(1);
                    }
                    else if (rating == Rating.Hard)
                    {
                        // Step stays the same
                        if (workingCard.Step == 0 && LearningSteps.Length == 1)
                        {
                            nextInterval = LearningSteps[0].Multiply(1.5);
                        }
                        else if (workingCard.Step == 0 && LearningSteps.Length >= 2)
                        {
                            nextInterval = (LearningSteps[0] + LearningSteps[1]).Divide(2.0);
                        }
                        else
                        {
                            nextInterval =
                                LearningSteps.Length > 0
                                    ? LearningSteps[workingCard.Step.Value]
                                    : TimeSpan.FromMinutes(5);
                        }
                    }
                    else // Good or Easy
                    {
                        if (workingCard.Step.Value + 1 >= LearningSteps.Length)
                        {
                            // Graduate
                            workingCard.State = State.Review;
                            workingCard.Step = null;
                            int intervalDays = NextInterval(workingCard.Stability!.Value); // S guaranteed non-null here
                            nextInterval = TimeSpan.FromDays(intervalDays);
                        }
                        else
                        {
                            // Advance step
                            workingCard.Step += 1;
                            nextInterval = LearningSteps[workingCard.Step.Value];
                        }
                    }
                }
                break;

            case State.Review:
                // S and D guaranteed non-null in Review state
                if (rating == Rating.Again)
                {
                    if (RelearningSteps.Length == 0)
                    {
                        int intervalDays = NextInterval(workingCard.Stability!.Value);
                        nextInterval = TimeSpan.FromDays(intervalDays);
                    }
                    else
                    {
                        workingCard.State = State.Relearning;
                        workingCard.Step = 0;
                        nextInterval = RelearningSteps[0];
                    }
                }
                else // Hard, Good, or Easy
                {
                    int intervalDays = NextInterval(workingCard.Stability!.Value);
                    nextInterval = TimeSpan.FromDays(intervalDays);
                }
                break;

            case State.Relearning:
                // Ensure Step has a value
                if (!workingCard.Step.HasValue)
                {
                    throw new InvalidOperationException(
                        "Card is in Relearning state but Step is null."
                    );
                }
                // S and D guaranteed non-null in Relearning state

                bool relearningStepsExhausted =
                    RelearningSteps.Length == 0
                    || (workingCard.Step.Value >= RelearningSteps.Length);

                if (relearningStepsExhausted && rating >= Rating.Hard)
                {
                    workingCard.State = State.Review;
                    workingCard.Step = null;
                    int intervalDays = NextInterval(workingCard.Stability!.Value);
                    nextInterval = TimeSpan.FromDays(intervalDays);
                }
                else
                {
                    if (rating == Rating.Again)
                    {
                        workingCard.Step = 0;
                        nextInterval =
                            RelearningSteps.Length > 0
                                ? RelearningSteps[0]
                                : TimeSpan.FromMinutes(1);
                    }
                    else if (rating == Rating.Hard)
                    {
                        // Step stays the same
                        if (workingCard.Step == 0 && RelearningSteps.Length == 1)
                        {
                            nextInterval = RelearningSteps[0].Multiply(1.5);
                        }
                        else if (workingCard.Step == 0 && RelearningSteps.Length >= 2)
                        {
                            nextInterval = (RelearningSteps[0] + RelearningSteps[1]).Divide(2.0);
                        }
                        else
                        {
                            nextInterval =
                                RelearningSteps.Length > 0
                                    ? RelearningSteps[workingCard.Step.Value]
                                    : TimeSpan.FromMinutes(5);
                        }
                    }
                    else // Good or Easy
                    {
                        if (workingCard.Step.Value + 1 >= RelearningSteps.Length)
                        {
                            // Graduate back to Review
                            workingCard.State = State.Review;
                            workingCard.Step = null;
                            int intervalDays = NextInterval(workingCard.Stability!.Value);
                            nextInterval = TimeSpan.FromDays(intervalDays);
                        }
                        else
                        {
                            // Advance step
                            workingCard.Step += 1;
                            nextInterval = RelearningSteps[workingCard.Step.Value];
                        }
                    }
                }
                break;

            default:
                throw new InvalidOperationException($"Unexpected card state: {workingCard.State}");
        } // End switch

        // Apply fuzzing if enabled and the card ended up in the Review state
        if (EnableFuzzing && workingCard.State == State.Review)
        {
            nextInterval = GetFuzzedInterval(nextInterval);
        }

        // Update the card's due date and last review time
        workingCard.Due = now + nextInterval;
        workingCard.LastReview = now;

        // Return the modified clone and the log
        return (workingCard, reviewLog);
    }

    /// <summary>
    /// Converts the Scheduler object to a dictionary suitable for serialization.
    /// </summary>
    /// <returns>A dictionary representation of the Scheduler.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "parameters", Parameters.ToList() }, // Store as list
            { "desired_retention", DesiredRetention },
            { "learning_steps", LearningSteps.Select(ts => (long)ts.TotalSeconds).ToList() }, // Store seconds
            { "relearning_steps", RelearningSteps.Select(ts => (long)ts.TotalSeconds).ToList() }, // Store seconds
            { "maximum_interval", MaximumIntervalDays },
            { "enable_fuzzing", EnableFuzzing },
        };
    }

    /// <summary>
    /// Creates a Scheduler object from a dictionary representation.
    /// </summary>
    /// <param name="sourceDict">The source dictionary.</param>
    /// <returns>A new Scheduler object.</returns>
    /// <exception cref="ArgumentException">Thrown if dictionary contains invalid data.</exception>
    public static Scheduler FromDictionary(Dictionary<string, object> sourceDict)
    {
        try
        {
            // Need to handle potential type differences during deserialization (e.g., lists might be object arrays)
            var parametersObj = sourceDict["parameters"];
            List<double> parameters;
            if (parametersObj is List<double> pList)
                parameters = pList;
            else if (parametersObj is System.Collections.IEnumerable pEnum)
                parameters = [.. pEnum.Cast<object>().Select(Convert.ToDouble)]; // More robust cast
            // else if (parametersObj is IEnumerable<double> pDEnum) parameters = pDEnum.ToList(); // This might be redundant with IEnumerable cast
            else
                throw new InvalidCastException("Cannot cast parameters to List<double>");

            double desiredRetention = Convert.ToDouble(
                sourceDict["desired_retention"],
                CultureInfo.InvariantCulture
            );

            var learningStepsObj = sourceDict["learning_steps"];
            List<TimeSpan> learningSteps;
            if (learningStepsObj is List<long> lsList)
            {
                learningSteps = [.. lsList.Select(TimeSpan.FromSeconds)];
            }
            else if (learningStepsObj is System.Collections.IEnumerable lsEnum)
            {
                learningSteps = [.. lsEnum
                    .Cast<object>()
                    .Select(o => TimeSpan.FromSeconds(Convert.ToInt64(o)))]; // More robust cast
            }
            // else if (learningStepsObj is IEnumerable<long> lsLEnum) learningSteps = lsLEnum.Select(TimeSpan.FromSeconds).ToList(); // Redundant
            else
            {
                throw new InvalidCastException("Cannot cast learning_steps to List<long>");
            }

            var relearningStepsObj = sourceDict["relearning_steps"];
            List<TimeSpan> relearningSteps;
            if (relearningStepsObj is List<long> rlsList)
            {
                relearningSteps = [.. rlsList.Select(TimeSpan.FromSeconds)];
            }
            else if (relearningStepsObj is System.Collections.IEnumerable rlsEnum)
            {
                relearningSteps = [.. rlsEnum
                    .Cast<object>()
                    .Select(o => TimeSpan.FromSeconds(Convert.ToInt64(o)))]; // More robust cast
            }
            // else if (relearningStepsObj is IEnumerable<long> rlsLEnum) relearningSteps = rlsLEnum.Select(TimeSpan.FromSeconds).ToList(); // Redundant
            else
            {
                throw new InvalidCastException("Cannot cast relearning_steps to List<long>");
            }

            int maximumInterval = Convert.ToInt32(sourceDict["maximum_interval"]);
            bool enableFuzzing = Convert.ToBoolean(sourceDict["enable_fuzzing"]);

            return new Scheduler(
                parameters: parameters,
                desiredRetention: desiredRetention,
                learningSteps: learningSteps,
                relearningSteps: relearningSteps,
                maximumIntervalDays: maximumInterval,
                enableFuzzing: enableFuzzing
            );
        }
        catch (Exception ex)
            when (ex is KeyNotFoundException
                || ex is FormatException
                || ex is InvalidCastException
                || ex is NullReferenceException
            )
        {
            throw new ArgumentException(
                "Source dictionary contains invalid or missing data for Scheduler creation.",
                nameof(sourceDict),
                ex
            );
        }
    }

    // --- Private Helper Methods for FSRS Calculations ---

    private double ClampDifficulty(double difficulty)
    {
        return Math.Clamp(difficulty, 1.0, 10.0);
    }

    private double InitialStability(Rating rating)
    {
        double initialStability = Parameters[(int)rating - 1];
        return Math.Max(initialStability, 0.1);
    }

    private double InitialDifficulty(Rating rating)
    {
        double initialDifficulty =
            Parameters[4] - (Math.Pow(Math.E, Parameters[5] * ((int)rating - 1)) - 1);
        return ClampDifficulty(initialDifficulty);
    }

    private int NextInterval(double stability)
    {
        stability = Math.Max(0.01, stability);
        double interval =
            stability / FsrsConstants.Factor
            * (Math.Pow(DesiredRetention, 1 / FsrsConstants.Decay) - 1);
        int intervalDays = (int)Math.Round(interval);
        intervalDays = Math.Max(1, intervalDays);
        intervalDays = Math.Min(intervalDays, MaximumIntervalDays);
        return intervalDays;
    }

    private double ShortTermStability(double stability, Rating rating)
    {
        stability = Math.Max(0.01, stability);
        return stability * Math.Exp(Parameters[17] * ((int)rating - 3 + Parameters[18]));
    }

    private double NextDifficulty(double difficulty, Rating rating)
    {
        double deltaDifficulty = -Parameters[6] * ((int)rating - 3);
        double linearDampingFactor = (10.0 - difficulty) / 9.0;
        double difficultyAfterUpdate = difficulty + deltaDifficulty * linearDampingFactor;
        double initialDifficultyEasy =
            Parameters[4] - (Math.Pow(Math.E, Parameters[5] * ((int)Rating.Easy - 1)) - 1);
        double nextDifficulty =
            Parameters[7] * initialDifficultyEasy + (1 - Parameters[7]) * difficultyAfterUpdate;
        return ClampDifficulty(nextDifficulty);
    }

    private double NextStability(
        double difficulty,
        double stability,
        double retrievability,
        Rating rating
    )
    {
        stability = Math.Max(0.01, stability);
        if (rating == Rating.Again)
        {
            return NextForgetStability(difficulty, stability, retrievability);
        }
        else
        {
            return NextRecallStability(difficulty, stability, retrievability, rating);
        }
    }

    private double NextForgetStability(double difficulty, double stability, double retrievability)
    {
        stability = Math.Max(0.01, stability);
        double stabilityLongTerm =
            Parameters[11]
            * Math.Pow(difficulty, -Parameters[12])
            * (Math.Pow(stability + 1, Parameters[13]) - 1)
            * Math.Exp((1 - retrievability) * Parameters[14]);
        double stabilityShortTermLimit = stability / Math.Exp(Parameters[17] * Parameters[18]);
        return Math.Max(0.1, Math.Min(stabilityLongTerm, stabilityShortTermLimit));
    }

    private double NextRecallStability(
        double difficulty,
        double stability,
        double retrievability,
        Rating rating
    )
    {
        stability = Math.Max(0.01, stability);
        double hardPenalty = (rating == Rating.Hard) ? Parameters[15] : 1.0;
        double easyBonus = (rating == Rating.Easy) ? Parameters[16] : 1.0;
        double stabilityIncreaseFactor =
            Math.Exp(Parameters[8])
            * (11 - difficulty)
            * Math.Pow(stability, -Parameters[9])
            * (Math.Exp((1 - retrievability) * Parameters[10]) - 1);
        double nextStability = stability * (1 + stabilityIncreaseFactor * hardPenalty * easyBonus);
        return Math.Max(0.1, nextStability);
    }

    private TimeSpan GetFuzzedInterval(TimeSpan interval)
    {
        double intervalDays = interval.TotalDays;
        if (intervalDays < 2.5)
            return interval;

        double delta = 1.0;
        foreach (var range in FsrsConstants.FuzzRanges)
        {
            delta += range.Factor * Math.Max(0.0, Math.Min(intervalDays, range.End) - range.Start);
        }

        int minIvl = (int)Math.Round(intervalDays - delta);
        int maxIvl = (int)Math.Round(intervalDays + delta);

        minIvl = Math.Max(2, minIvl);
        maxIvl = Math.Min(maxIvl, MaximumIntervalDays);
        minIvl = Math.Min(minIvl, maxIvl);

        int fuzzedIntervalDays = _random.Next(minIvl, maxIvl + 1);
        fuzzedIntervalDays = Math.Min(fuzzedIntervalDays, MaximumIntervalDays);
        return TimeSpan.FromDays(fuzzedIntervalDays);
    }
}
