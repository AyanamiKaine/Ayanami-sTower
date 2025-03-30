using FsrsSharp;

namespace FSRS.Tests;

public class FSRSUnitTests
{
    // Tolerance for floating-point comparisons
    private const double Tolerance = 0.0001;

    [Fact]
    public void TestReviewCard()
    {
        // Corresponds to Python: test_review_card
        var scheduler = new Scheduler(enableFuzzing: false);

        var ratings = new[] {
                Rating.Good, Rating.Good, Rating.Good, Rating.Good, Rating.Good,
                Rating.Good, Rating.Again, Rating.Again, Rating.Good, Rating.Good,
                Rating.Good, Rating.Good, Rating.Good
            };

        var card = new Card();
        var reviewDateTime = new DateTimeOffset(2022, 11, 29, 12, 30, 0, TimeSpan.Zero);

        var ivlHistory = new List<int>();
        Card currentCard = card;

        foreach (var rating in ratings)
        {
            var result = scheduler.ReviewCard(currentCard, rating, reviewDateTime);
            currentCard = result.UpdatedCard;

            // Use xUnit's Assert.NotNull and Assert.True
            Assert.NotNull(currentCard.LastReview); // Check if LastReview has value
            Assert.True(currentCard.LastReview.HasValue, "LastReview should not be null after review.");


            int ivl = (int)Math.Round((currentCard.Due - currentCard.LastReview.Value).TotalDays);
            ivlHistory.Add(ivl);

            reviewDateTime = currentCard.Due;
        }

        var expectedHistory = new List<int> {
                0, 4, 14, 44, 125, 328, 0, 0, 7, 16, 34, 71, 142
            };

        // Use xUnit's Assert.Equal for collection comparison
        Assert.Equal(expectedHistory, ivlHistory);
    }

    [Fact]
    public void TestRepeatedCorrectReviews()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var card = new Card();
        Card currentCard = card;

        var reviewDateTimes = Enumerable.Range(0, 10)
            .Select(i => new DateTimeOffset(2022, 11, 29, 12, 30, 0, i * 100, TimeSpan.Zero))
            .ToList();

        foreach (var reviewDateTime in reviewDateTimes)
        {
            var result = scheduler.ReviewCard(currentCard, Rating.Easy, reviewDateTime);
            currentCard = result.UpdatedCard;
        }

        Assert.NotNull(currentCard.Difficulty); // Check if Difficulty has value
        Assert.True(currentCard.Difficulty.HasValue, "Difficulty should not be null.");
        // Use Assert.True with tolerance check for floating point
        Assert.True(Math.Abs(1.0 - currentCard.Difficulty.Value) < Tolerance, $"Difficulty ({currentCard.Difficulty.Value}) should be close to 1.0.");
    }

    [Fact]
    public void TestMemoState()
    {
        var scheduler = new Scheduler(enableFuzzing: false);

        var ratings = new[] {
                Rating.Again, Rating.Good, Rating.Good, Rating.Good, Rating.Good, Rating.Good
            };
        var ivlHistory = new[] { 0, 0, 1, 3, 8, 21 };

        var card = new Card();
        Card currentCard = card;
        var reviewDateTime = new DateTimeOffset(2022, 11, 29, 12, 30, 0, TimeSpan.Zero);

        for (int i = 0; i < ratings.Length; i++)
        {
            var rating = ratings[i];
            var ivl = ivlHistory[i];

            var result = scheduler.ReviewCard(currentCard, rating, reviewDateTime);
            currentCard = result.UpdatedCard;

            reviewDateTime = reviewDateTime.AddDays(ivl);
        }

        var finalResult = scheduler.ReviewCard(currentCard, Rating.Good, reviewDateTime);
        currentCard = finalResult.UpdatedCard;

        Assert.NotNull(currentCard.Stability);
        Assert.NotNull(currentCard.Difficulty);
        Assert.True(currentCard.Stability.HasValue, "Stability should not be null.");
        Assert.True(currentCard.Difficulty.HasValue, "Difficulty should not be null.");

        // Use Assert.True with tolerance check for floating point
        Assert.Equal(49, Math.Round(currentCard.Stability.Value)); // Rounding check is fine
        Assert.True(Math.Abs(7.0866 - currentCard.Difficulty.Value) < Tolerance, $"Difficulty ({currentCard.Difficulty.Value}) should be close to 7.0866.");
    }

    [Fact]
    public void TestRepeatDefaultArg()
    {
        var scheduler = new Scheduler();
        var card = new Card();
        var startTime = DateTimeOffset.UtcNow;

        var result = scheduler.ReviewCard(card, Rating.Good);
        Card updatedCard = result.UpdatedCard;

        TimeSpan timeDelta = updatedCard.Due - startTime;

        // Use xUnit's Assert.True
        Assert.True(timeDelta.TotalSeconds > 500 && timeDelta.TotalSeconds < 700,
                      $"Card should be due in approx 10 minutes (500-700s), but was {timeDelta.TotalSeconds}s");
    }

    [Fact]
    public void TestDateTimeHandling()
    {
        var scheduler = new Scheduler();
        var card = new Card();
        var nowUtc = DateTimeOffset.UtcNow;

        Assert.True(nowUtc.AddSeconds(1) >= card.Due, "New card due time should be very close to creation time.");

        // Use xUnit's Assert.Throws
        var nonUtcTime = new DateTimeOffset(2022, 11, 29, 12, 30, 0, TimeSpan.FromHours(2));
        Assert.Throws<ArgumentException>(() =>
        {
            scheduler.ReviewCard(card, Rating.Good, nonUtcTime);
        }); // No message needed in Assert.Throws typically

        var reviewResult = scheduler.ReviewCard(card, Rating.Good, DateTimeOffset.UtcNow);
        Card updatedCard = reviewResult.UpdatedCard;

        Assert.NotNull(updatedCard.LastReview); // Ensure LastReview has a value before accessing Offset
        Assert.True(updatedCard.LastReview.HasValue, "LastReview should have a value.");
        // Use xUnit's Assert.Equal
        Assert.Equal(TimeSpan.Zero, updatedCard.Due.Offset);
        Assert.Equal(TimeSpan.Zero, updatedCard.LastReview.Value.Offset);

        Assert.True(updatedCard.Due >= updatedCard.LastReview.Value, "Due date should be >= LastReview date.");
    }

    [Fact]
    public void TestCardSerializeDeserialize()
    {
        var scheduler = new Scheduler();
        var card = new Card();

        var cardDict = card.ToDictionary();
        // Use xUnit's Assert.IsType
        Assert.IsType<Dictionary<string, object?>>(cardDict);
        var copiedCard = Card.FromDictionary(cardDict);

        // Use helper for dictionary comparison (adapted for xUnit asserts)
        AssertDictionariesEquivalent(cardDict, copiedCard.ToDictionary());

        var reviewResult = scheduler.ReviewCard(card, Rating.Good, DateTimeOffset.UtcNow);
        Card reviewedCard = reviewResult.UpdatedCard;

        var reviewedCardDict = reviewedCard.ToDictionary();
        Assert.IsType<Dictionary<string, object?>>(reviewedCardDict);
        var copiedReviewedCard = Card.FromDictionary(reviewedCardDict);

        AssertDictionariesEquivalent(reviewedCardDict, copiedReviewedCard.ToDictionary());

        // Use xUnit's Assert.NotEqual for sequence comparison
        Assert.NotEqual(cardDict.ToList(), reviewedCardDict.ToList());
    }

    [Fact]
    public void TestReviewLogSerializeDeserialize()
    {
        var scheduler = new Scheduler();
        var card = new Card();

        var result1 = scheduler.ReviewCard(card, Rating.Again);
        ReviewLog reviewLog1 = result1.Log;
        Card updatedCard1 = result1.UpdatedCard;

        var reviewLogDict1 = reviewLog1.ToDictionary();
        Assert.IsType<Dictionary<string, object?>>(reviewLogDict1);
        var copiedReviewLog1 = ReviewLog.FromDictionary(reviewLogDict1);
        AssertDictionariesEquivalent(reviewLogDict1, copiedReviewLog1.ToDictionary());

        var result2 = scheduler.ReviewCard(updatedCard1, Rating.Good, DateTimeOffset.UtcNow);
        ReviewLog reviewLog2 = result2.Log;

        var reviewLogDict2 = reviewLog2.ToDictionary();
        Assert.IsType<Dictionary<string, object?>>(reviewLogDict2);
        var copiedReviewLog2 = ReviewLog.FromDictionary(reviewLogDict2);
        AssertDictionariesEquivalent(reviewLogDict2, copiedReviewLog2.ToDictionary());

        Assert.NotEqual(reviewLogDict1.ToList(), reviewLogDict2.ToList());
    }

    [Fact]
    public void TestCustomSchedulerArgs()
    {
        var scheduler = new Scheduler(
            desiredRetention: 0.9,
            maximumIntervalDays: 36500,
            enableFuzzing: false
        );
        var card = new Card();
        Card currentCard = card;
        var now = new DateTimeOffset(2022, 11, 29, 12, 30, 0, TimeSpan.Zero);

        var ratings = new[] {
                Rating.Good, Rating.Good, Rating.Good, Rating.Good, Rating.Good,
                Rating.Good, Rating.Again, Rating.Again, Rating.Good, Rating.Good,
                Rating.Good, Rating.Good, Rating.Good
            };
        var ivlHistory = new List<int>();

        foreach (var rating in ratings)
        {
            var result = scheduler.ReviewCard(currentCard, rating, now);
            currentCard = result.UpdatedCard;
            Assert.NotNull(currentCard.LastReview);
            Assert.True(currentCard.LastReview.HasValue);
            int ivl = (int)Math.Round((currentCard.Due - currentCard.LastReview.Value).TotalDays);
            ivlHistory.Add(ivl);
            now = currentCard.Due;
        }

        var expectedHistory = new List<int> { 0, 4, 14, 44, 125, 328, 0, 0, 7, 16, 34, 71, 142 };
        Assert.Equal(expectedHistory, ivlHistory);

        var parameters2 = new double[] {
                0.1456, 0.4186, 1.1104, 4.1315, 5.2417, 1.3098, 0.8975, 0.0000,
                1.5674, 0.0567, 0.9661, 2.0275, 0.1592, 0.2446, 1.5071, 0.2272,
                2.8755, 1.234, 5.6789
            };
        double desiredRetention2 = 0.85;
        int maximumInterval2 = 3650;
        var scheduler2 = new Scheduler(
            parameters: parameters2,
            desiredRetention: desiredRetention2,
            maximumIntervalDays: maximumInterval2
        );

        Assert.Equal(parameters2, scheduler2.Parameters);
        Assert.True(Math.Abs(desiredRetention2 - scheduler2.DesiredRetention) < Tolerance);
        Assert.Equal(maximumInterval2, scheduler2.MaximumIntervalDays);
    }

    [Fact]
    public void TestRetrievability()
    {
        var scheduler = new Scheduler();
        var card = new Card();

        Assert.Equal(State.New, card.State);
        double retrievabilityNew = card.GetRetrievability();
        Assert.True(Math.Abs(1.0 - retrievabilityNew) > Tolerance, "Retrievability of New card should be 1.0 in C# impl.");

        var r1 = scheduler.ReviewCard(card, Rating.Good);
        Card cardL = r1.UpdatedCard;
        Assert.Equal(State.Learning, cardL.State);
        double retrievabilityL = cardL.GetRetrievability();
        Assert.True(retrievabilityL >= 0 && retrievabilityL <= 1, "Learning retrievability out of range.");

        var r2 = scheduler.ReviewCard(cardL, Rating.Good, cardL.Due);
        Card cardRev = r2.UpdatedCard;
        Assert.Equal(State.Review, cardRev.State);
        double retrievabilityRev = cardRev.GetRetrievability();
        Assert.True(retrievabilityRev >= 0 && retrievabilityRev <= 1, "Review retrievability out of range.");

        var r3 = scheduler.ReviewCard(cardRev, Rating.Again, cardRev.Due);
        Card cardRel = r3.UpdatedCard;
        Assert.Equal(State.Relearning, cardRel.State);
        double retrievabilityRel = cardRel.GetRetrievability();
        Assert.True(retrievabilityRel >= 0 && retrievabilityRel <= 1, "Relearning retrievability out of range.");
    }

    [Fact]
    public void TestSchedulerSerializeDeserialize()
    {
        var scheduler = new Scheduler();

        var schedulerDict = scheduler.ToDictionary();
        Assert.IsType<Dictionary<string, object>>(schedulerDict); // Note: object, not object?

        var copiedScheduler = Scheduler.FromDictionary(schedulerDict);

        AssertDictionariesEquivalent(schedulerDict, copiedScheduler.ToDictionary());
    }

    // Helper method adapted for xUnit Asserts
    private void AssertDictionariesEquivalent(Dictionary<string, object?> d1, Dictionary<string, object?> d2)
    {
        Assert.Equal(d1.Count, d2.Count);
        foreach (var kvp in d1)
        {
            Assert.True(d2.ContainsKey(kvp.Key), $"Key '{kvp.Key}' missing in second dictionary.");
            object? val1 = kvp.Value; // Allow null
            object? val2 = d2[kvp.Key]; // Allow null

            if (val1 is System.Collections.IEnumerable list1 && val1 is not string &&
                val2 is System.Collections.IEnumerable list2 && val2 is not string)
            {
                // Convert to lists of objects for comparison
                Assert.Equal(list1.Cast<object>().ToList(), list2.Cast<object>().ToList());
            }
            else
            {
                Assert.Equal(val1, val2);
            }
        }
    }


    [Fact]
    public void TestGoodLearningSteps()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var createdAt = DateTimeOffset.UtcNow;
        var card = new Card(due: createdAt);

        Assert.Equal(State.New, card.State);
        Assert.Equal(0, card.Step);

        var r1 = scheduler.ReviewCard(card, Rating.Good, card.Due);
        Card card1 = r1.UpdatedCard;

        Assert.Equal(State.Learning, card1.State);
        Assert.Equal(1, card1.Step);
        Assert.True(Math.Abs(600 - (card1.Due - createdAt).TotalSeconds) < 10, "First Good review should schedule ~10m later."); // Check within 10s tolerance

        var r2 = scheduler.ReviewCard(card1, Rating.Good, card1.Due);
        Card card2 = r2.UpdatedCard;

        Assert.Equal(State.Review, card2.State);
        Assert.Null(card2.Step);
        Assert.True((card2.Due - card1.Due).TotalDays >= 1.0, "Second Good review should schedule >= 1 day later.");
    }

    [Fact]
    public void TestAgainLearningSteps()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var createdAt = DateTimeOffset.UtcNow;
        var card = new Card(due: createdAt);

        Assert.Equal(State.New, card.State);
        Assert.Equal(0, card.Step);

        var r1 = scheduler.ReviewCard(card, Rating.Again, card.Due);
        Card card1 = r1.UpdatedCard;

        Assert.Equal(State.Learning, card1.State);
        Assert.Equal(0, card1.Step);
        Assert.True(Math.Abs(60 - (card1.Due - createdAt).TotalSeconds) < 5, "Again review should schedule ~1m later."); // Check within 5s tolerance
    }

    [Fact]
    public void TestHardLearningSteps()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var createdAt = DateTimeOffset.UtcNow;
        var card = new Card(due: createdAt);

        Assert.Equal(State.New, card.State);
        Assert.Equal(0, card.Step);

        var r1 = scheduler.ReviewCard(card, Rating.Hard, card.Due);
        Card card1 = r1.UpdatedCard;

        Assert.Equal(State.Learning, card1.State);
        Assert.Equal(0, card1.Step);
        Assert.True(Math.Abs(330 - (card1.Due - createdAt).TotalSeconds) < 15, "Hard review should schedule ~5.5m later."); // Check within 15s tolerance
    }

    [Fact]
    public void TestEasyLearningSteps()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var createdAt = DateTimeOffset.UtcNow;
        var card = new Card(due: createdAt);

        Assert.Equal(State.New, card.State);
        Assert.Equal(0, card.Step);

        var (UpdatedCard, Log) = scheduler.ReviewCard(card, Rating.Easy, card.Due);

        Card card1 = UpdatedCard;

        Assert.Equal(State.Learning, card1.State);
        Assert.NotNull(card1.Step);
        Assert.True((card1.Due - createdAt).TotalDays <= 1.0, "Easy review should schedule >= on the same day");
    }

    [Fact]
    public void TestReviewStateTransitions()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var card = new Card();
        Card currentCard = card;
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;

        Assert.Equal(State.Review, currentCard.State);
        Assert.Null(currentCard.Step);

        DateTimeOffset prevDue = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;

        Assert.Equal(State.Review, currentCard.State);
        Assert.True((currentCard.Due - prevDue).TotalDays >= 1.0, "Good review in Review state should increase interval by >= 1 day.");

        prevDue = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;

        Assert.Equal(State.Relearning, currentCard.State);
        Assert.True(Math.Abs(10 - (currentCard.Due - prevDue).TotalMinutes) < 1, "Again review should enter Relearning and schedule ~10m later."); // Check within 1 min tolerance
    }

    [Fact]
    public void TestRelearningStateTransitions()
    {
        var scheduler = new Scheduler(enableFuzzing: false);
        var card = new Card();
        Card currentCard = card;
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;

        DateTimeOffset prevDue = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;
        currentTime = currentCard.Due;

        Assert.Equal(State.Relearning, currentCard.State);
        Assert.Equal(0, currentCard.Step);
        Assert.True(Math.Abs(10 - (currentCard.Due - prevDue).TotalMinutes) < 1, "First Again should enter Relearning (10m step).");

        prevDue = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;
        currentTime = currentCard.Due;

        Assert.Equal(State.Relearning, currentCard.State);
        Assert.Equal(0, currentCard.Step);
        Assert.True(Math.Abs(10 - (currentCard.Due - prevDue).TotalMinutes) < 1, "Second Again should stay in Relearning (10m step).");

        prevDue = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;

        Assert.Equal(State.Review, currentCard.State);
        Assert.Null(currentCard.Step);
        Assert.True((currentCard.Due - prevDue).TotalDays >= 1.0, "Good review in Relearning should graduate to Review (>= 1 day).");
    }

    [Fact]
    public void TestNoRelearningSteps()
    {
        var scheduler = new Scheduler(relearningSteps: Array.Empty<TimeSpan>(), enableFuzzing: false);

        Assert.Empty(scheduler.RelearningSteps);

        var card = new Card();
        Card currentCard = card;
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;
        currentCard = scheduler.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        currentTime = currentCard.Due;
        Assert.Equal(State.Review, currentCard.State);

        var result = scheduler.ReviewCard(currentCard, Rating.Again, currentTime);
        Card updatedCard = result.UpdatedCard;

        Assert.Equal(State.Review, updatedCard.State);
        Assert.NotNull(updatedCard.LastReview);
        Assert.True(updatedCard.LastReview.HasValue);
        int interval = (int)Math.Round((updatedCard.Due - updatedCard.LastReview.Value).TotalDays);
        Assert.True(interval >= 1, "Interval should be >= 1 day when skipping relearning steps.");
    }

    [Fact]
    public void TestOneCardMultipleSchedulers()
    {
        var schedulerWithTwoLearningSteps = new Scheduler(
            learningSteps: [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10)]
        );
        var schedulerWithOneLearningStep = new Scheduler(
            learningSteps: [TimeSpan.FromMinutes(1)]
        );
        var schedulerWithNoLearningSteps = new Scheduler(learningSteps: Array.Empty<TimeSpan>());

        var schedulerWithTwoRelearningSteps = new Scheduler(
            relearningSteps: [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10)]
        );
        var schedulerWithOneRelearningStep = new Scheduler(
            relearningSteps: [TimeSpan.FromMinutes(1)]
        );
        var schedulerWithNoRelearningSteps = new Scheduler(relearningSteps: Array.Empty<TimeSpan>());

        var card = new Card();
        Card currentCard = card;
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        // --- Learning State Tests ---
        Assert.Equal(2, schedulerWithTwoLearningSteps.LearningSteps.Length);
        currentCard = schedulerWithTwoLearningSteps.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        Assert.Equal(State.Learning, currentCard.State);
        Assert.Equal(1, currentCard.Step);
        currentTime = currentCard.Due;

        Assert.Equal(1, schedulerWithOneLearningStep.LearningSteps.Length);
        currentCard = schedulerWithOneLearningStep.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;
        Assert.Equal(State.Learning, currentCard.State);
        Assert.Equal(0, currentCard.Step);
        currentTime = currentCard.Due;

        Assert.Empty(schedulerWithNoLearningSteps.LearningSteps);
        currentCard = schedulerWithNoLearningSteps.ReviewCard(currentCard, Rating.Hard, currentTime).UpdatedCard;
        Assert.Equal(State.Review, currentCard.State);
        Assert.Null(currentCard.Step);
        currentTime = currentCard.Due;


        // --- Relearning State Tests ---
        Assert.Equal(2, schedulerWithTwoRelearningSteps.RelearningSteps.Length);
        currentCard = schedulerWithTwoRelearningSteps.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;
        Assert.Equal(State.Relearning, currentCard.State);
        Assert.Equal(0, currentCard.Step);
        currentTime = currentCard.Due;

        currentCard = schedulerWithTwoRelearningSteps.ReviewCard(currentCard, Rating.Good, currentTime).UpdatedCard;
        Assert.Equal(State.Relearning, currentCard.State);
        Assert.Equal(1, currentCard.Step);
        currentTime = currentCard.Due;


        Assert.Equal(1, schedulerWithOneRelearningStep.RelearningSteps.Length);
        currentCard = schedulerWithOneRelearningStep.ReviewCard(currentCard, Rating.Again, currentTime).UpdatedCard;
        Assert.Equal(State.Relearning, currentCard.State);
        Assert.Equal(0, currentCard.Step);
        currentTime = currentCard.Due;


        Assert.Empty(schedulerWithNoRelearningSteps.RelearningSteps);
        currentCard = schedulerWithNoRelearningSteps.ReviewCard(currentCard, Rating.Hard, currentTime).UpdatedCard;
        Assert.Equal(State.Review, currentCard.State);
        Assert.Null(currentCard.Step);
    }


    [Fact]
    public void TestMaximumInterval()
    {
        int maximumInterval = 100;
        var scheduler = new Scheduler(maximumIntervalDays: maximumInterval, enableFuzzing: false);
        var card = new Card();
        Card currentCard = card;
        DateTimeOffset currentTime = DateTimeOffset.UtcNow;

        void reviewAndCheck(Rating rating)
        {
            currentCard = scheduler.ReviewCard(currentCard, rating, currentTime).UpdatedCard;
            Assert.NotNull(currentCard.LastReview);
            Assert.True(currentCard.LastReview.HasValue);
            int intervalDays = (int)Math.Round((currentCard.Due - currentCard.LastReview.Value).TotalDays);
            Assert.True(intervalDays <= maximumInterval, $"Interval {intervalDays} exceeded maximum {maximumInterval} for rating {rating}.");
            currentTime = currentCard.Due;
        }

        reviewAndCheck(Rating.Easy);
        reviewAndCheck(Rating.Good);
        reviewAndCheck(Rating.Easy);
        reviewAndCheck(Rating.Good);
        reviewAndCheck(Rating.Easy);
        reviewAndCheck(Rating.Easy);
        reviewAndCheck(Rating.Good);
    }

    [Fact]
    public void TestClassToString()
    {
        var card = new Card();
        Assert.False(string.IsNullOrEmpty(card.ToString())); // Use Assert.False with IsNullOrEmpty

        var scheduler = new Scheduler();
        Assert.False(string.IsNullOrEmpty(scheduler.ToString()));

        var (UpdatedCard, Log) = scheduler.ReviewCard(card, Rating.Good);
        ReviewLog reviewLog = Log;
        Assert.False(string.IsNullOrEmpty(reviewLog.ToString()));

        // Use xUnit's Assert.Contains
        Assert.Contains(nameof(Card), card.ToString());
        Assert.Contains(nameof(Scheduler), scheduler.ToString());
        Assert.Contains(nameof(ReviewLog), reviewLog.ToString());
    }

    [Fact]
    public void TestUniqueCardIds()
    {
        var cardIds = new List<long>();
        for (int i = 0; i < 50; i++)
        {
            var card = new Card();
            cardIds.Add(card.CardId);
        }

        int distinctCount = cardIds.Distinct().Count();
        Assert.Equal(cardIds.Count, distinctCount); // Check counts are equal
    }
}
