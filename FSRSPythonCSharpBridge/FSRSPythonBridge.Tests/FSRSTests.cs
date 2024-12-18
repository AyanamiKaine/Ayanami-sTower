using Python.Runtime;

namespace FSRSPythonBridge.Tests;


public class FSRSTests
{
    [Fact]
    public void UsingFSRSMethod()
    {
        var dueDate = FSRS.CreateCard();
        Assert.NotNull(dueDate);
    }

    [Fact]
    public void CreateCard()
    {
        var card = FSRS.CreateCard();
        Assert.IsType<DateTime>(card.Due);
    }

    [Fact]
    public void GetCardID()
    {
        var card = FSRS.CreateCard();
        Assert.IsType<long>(card.ID);
    }


    /// <summary>
    /// If a card was not rated its stability should be null.
    /// </summary>
    [Fact]
    public void GetCardStabilityShouldBeNull()
    {
        var card = FSRS.CreateCard();
        Assert.Null(card.Stability);
    }

    [Fact]
    public void GetStepShouldBeNull()
    {
        var card = FSRS.CreateCard();
        foreach (int i in Enumerable.Range(0, 10))
        {
            card = FSRS.RateCard(card, Rating.Good);
        }
        Assert.Null(card.Step);
    }

    [Fact]
    public void GetStepShouldBeLong()
    {
        var card = FSRS.CreateCard();
        Assert.IsType<long>(card.Step);
    }


    /// <summary>
    /// After a card was reviewd it should have a float stablity value that is not null.
    /// </summary>
    [Fact]
    public void GetCardStabilityShouldBeFloat()
    {
        var card = FSRS.CreateCard();
        card = FSRS.RateCard(card, Rating.Good);
        Assert.IsType<float>(card.Stability);
    }
    /// <summary>
    /// If a card was not rated its stability should be null.
    /// </summary>
    [Fact]
    public void GetCardDifficultyShouldBeNull()
    {
        var card = FSRS.CreateCard();
        Assert.Null(card.Difficulty);
    }


    /// <summary>
    /// After a card was reviewd it should have a float stablity value that is not null.
    /// </summary>
    [Fact]
    public void GetCardDifficultyShouldBeFloat()
    {
        var card = FSRS.CreateCard();
        card = FSRS.RateCard(card, Rating.Good);
        Assert.IsType<float>(card.Difficulty);
    }

    [Fact]
    public void LastReviewShouldBeNull()
    {
        var card = FSRS.CreateCard();
        Assert.Null(card.LastReview);
    }

    [Fact]
    public void LastReviewShouldBeDateTime()
    {
        var card = FSRS.CreateCard();
        card = FSRS.RateCard(card, Rating.Good);
        Assert.IsType<DateTime>(card.LastReview);
    }

    [Fact]
    public void InitalCardStateShouldBeLearning()
    {
        var card = FSRS.CreateCard();
        card = FSRS.RateCard(card, Rating.Good);
        Assert.Equal(CardState.Learning, card.State);
    }


    /// <summary>
    /// After rating the card a certain amount of time it should go from the 
    /// Learning state in the Review state.
    /// </summary>
    [Fact]
    public void GetCurrentCardState()
    {
        var card = FSRS.CreateCard();

        foreach (int i in Enumerable.Range(0, 10))
        {
            card = FSRS.RateCard(card, Rating.Good);
        }

        Assert.Equal(CardState.Review, card.State);
    }

}
