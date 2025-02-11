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
    public void SimpleOneRuleTwoCriteriaStrictMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
        ], () => { });

        var (matched, numberMatched) = rule1.StrictEvaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(matched);
        Assert.Equal(2, numberMatched);
    }

    [Fact]
    public void SimpleOneRuleTwoCriteriaPredicateBasedStrictMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", who => { return who == "Nick"; }),
            new Criteria<string>("concept", concept => { return concept == "onHit"; }),
        ], () => { });

        var (matched, numberMatched) = rule1.StrictEvaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(matched);
        Assert.Equal(2, numberMatched);
    }

    [Fact]
    public void SimpleOneRuleOneCriteriaStrictMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
        ], () => { });

        var (completeMatch, numberOfMatchedCriteria) = rule1.StrictEvaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
        });

        Assert.False(completeMatch);
        Assert.Equal(0, numberOfMatchedCriteria);
    }

    [Fact]
    public void SimpleOneRuleOneCriteriaRelaxedMatch()
    {
        var rule1 = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
        ], () => { });

        var (completeMatch, numberOfMatchedCriteria) = rule1.RelaxedEvaluate(new Dictionary<string, object>
        {
            { "concept", "onHit" },
        });

        Assert.False(completeMatch);
        Assert.Equal(1, numberOfMatchedCriteria);
    }

    [Fact]
    public void RandomRuleSelectionIfMultipleRulesMatch()
    {
        var query = new Query();
        // Should never execute
        var rule1Executed = false;
        // These should should always execute
        var rule2Executed = false;
        var rule3Executed = false;
        var rule4Executed = false;

        query
            .Add("who", "Nick")
            .Add("concept", "onHit")
            .Add("curMap", "circus")
            .Add("health", 0.66)
            .Add("nearAllies", 2)
            .Add("hitBy", "zombieClown");


        List<Rule> rules = [
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                    Console.WriteLine("Ouch");
                    // Should never execute
                    rule1Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                    Console.WriteLine("ow help!");
                    rule2Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    Console.WriteLine("This Circus Sucks!");
                    rule3Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], ()=>{
                    Console.WriteLine("Stupid Clown!");
                    rule4Executed = true;
                }),
        ];


        // We query match 1000000 times so we can guarantee that each rule atleast once matched and got executed.
        for (int i = 0; i < 100000; i++)
        {
            query.Match(rules);
        }

        Assert.False(rule1Executed);
        Assert.True(rule2Executed);
        Assert.True(rule3Executed);
        Assert.True(rule4Executed);
    }

    /// <summary>
    /// This example was given in this [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&t) talk.
    /// </summary>
    [Fact]
    public void LeftForDeadExample()
    {
        var query = new Query();
        var rule1Executed = false;
        var rule2Executed = false;
        var rule3Executed = false;
        var rule4Executed = false;
        var rule5Executed = false;

        query
            .Add("who", "Nick")
            .Add("concept", "onHit")
            .Add("curMap", "circus")
            .Add("health", 0.66)
            .Add("nearAllies", 2)
            .Add("hitBy", "zombieClown");


        List<Rule> rules = [
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                    Console.WriteLine("Ouch");
                    rule1Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                    Console.WriteLine("ow help!");
                    rule2Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    Console.WriteLine("This Circus Sucks!");
                    rule3Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], ()=>{
                    Console.WriteLine("Stupid Clown!");
                    rule4Executed = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    Console.WriteLine("I hate circus clowns!");
                    rule5Executed = true;
                }),
        ];

        query.Match(rules);

        Assert.True(rule5Executed);
    }
}
