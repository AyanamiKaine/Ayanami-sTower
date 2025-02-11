namespace SFPM.Tests;

public class RuleUnitTest
{
    [Fact]
    public void Creation()
    {
        var SFPM = new SFPM();
        Assert.NotNull(SFPM);
    }




    /// <summary>
    /// When we use the pattern match we want to be able to see how each rule is scored.
    /// 
    /// This is important because we want to be able to say select the rule that matches the most
    /// does not need to match every condition in the rule. Also this makes it so rules with more
    /// conditions (i.e. that are more specific) are valued more than general once.
    /// </summary>
    [Fact]
    public void Scoring()
    {

    }

    [Fact]
    public void SimpleOneRuleTwoCriteriaMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
        ]);

        var (matched, numberMatched) = rule1.Evaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(matched);
        Assert.Equal(2, numberMatched);
    }

    [Fact]
    public void SimpleOneRuleTwoCriteriaPredicateBasedMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", who => { return who == "Nick"; }),
            new Criteria<string>("concept", concept => { return concept == "onHit"; }),
        ]);

        var (matched, numberMatched) = rule1.Evaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(matched);
        Assert.Equal(2, numberMatched);
    }

    [Fact]
    public void SimpleOneRuleOneCriteriaMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
        ]);

        var (completeMatch, numberOfMatchedCriteria) = rule1.Evaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
        });

        Assert.False(completeMatch);
        Assert.Equal(1, numberOfMatchedCriteria);
    }
}
