using Python.Runtime;

namespace FSRSPythonBridge.Tests;


public class PythonFixture
{
    public FSRS Fsrs { get; private set; }

    public PythonFixture()
    {
        Fsrs = new FSRS();
    }
}

public class FSRSTests : IClassFixture<PythonFixture>
{
    private readonly FSRS fsrs;

    public FSRSTests(PythonFixture fixture)
    {
        fsrs = fixture.Fsrs;
    }

    [Fact]
    public void Creation()
    {
        Assert.NotNull(fsrs);
    }

    [Fact]
    public void UsingFSRSMethod()
    {
        var dueDate = fsrs.CreateCard();
        Assert.NotNull(dueDate);
    }

    [Fact]
    public void FSRSRating()
    {
        int expectedRating = (int)Rating.Good;
        int actualRating = fsrs.Rating;
        Assert.Equal(expectedRating, actualRating);
    }

    [Fact]
    public void CreateCard()
    {
        var card = fsrs.CreateCard();
        Assert.IsType<DateTime>(card.Due);
    }

    [Fact]
    public void GetCardID()
    {
        var card = fsrs.CreateCard();
        Assert.IsType<long>(card.ID);
    }


    /// <summary>
    /// If a card was not rated its stability should be null.
    /// </summary>
    [Fact]
    public void GetCardStabilityShouldBeNull()
    {
        var card = fsrs.CreateCard();
        Assert.Null(card.Stability);
    }


    /// <summary>
    /// After a card was reviewd it should have a float stablity value that is not null.
    /// </summary>
    [Fact]
    public void GetCardStabilityShouldBeFloat()
    {
        var card = fsrs.CreateCard();
        card = fsrs.RateCard(card, Rating.Good);
        Assert.IsType<float>(card.Stability);
    }
    /// <summary>
    /// If a card was not rated its stability should be null.
    /// </summary>
    [Fact]
    public void GetCardDifficultyShouldBeNull()
    {
        var card = fsrs.CreateCard();
        Assert.Null(card.Difficulty);
    }


    /// <summary>
    /// After a card was reviewd it should have a float stablity value that is not null.
    /// </summary>
    [Fact]
    public void GetCardDifficultyShouldBeFloat()
    {
        var card = fsrs.CreateCard();
        card = fsrs.RateCard(card, Rating.Good);
        Assert.IsType<float>(card.Difficulty);
    }

    [Fact]
    public void LastReviewShouldBeNull()
    {
        var card = fsrs.CreateCard();
        Assert.Null(card.LastReview);
    }

    [Fact]
    public void LastReviewShouldBeDateTime()
    {
        var card = fsrs.CreateCard();
        card = fsrs.RateCard(card, Rating.Good);
        Assert.IsType<DateTime>(card.LastReview);
    }

    [Fact]
    public void InitalCardStateShouldBeLearning()
    {
        var card = fsrs.CreateCard();
        card = fsrs.RateCard(card, Rating.Good);
        Assert.Equal(CardState.Learning, card.State);
    }


    /// <summary>
    /// After rating the card a certain amount of time it should go from the 
    /// Learning state in the Review state.
    /// </summary>
    [Fact]
    public void GetCurrentCardState()
    {
        var card = fsrs.CreateCard();

        foreach (int i in Enumerable.Range(0, 10))
        {
            card = fsrs.RateCard(card, Rating.Good);
        }

        Assert.Equal(CardState.Review, card.State);
    }

}
