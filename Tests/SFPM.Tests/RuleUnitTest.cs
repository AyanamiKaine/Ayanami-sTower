using NLog;
using Xunit.Abstractions;

namespace AyanamisTower.SFPM.Tests;

/// <summary>
/// Contains unit tests for the <see cref="Rule"/> class.
/// </summary>
public class RuleUnitTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleUnitTest"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper.</param>
    public RuleUnitTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Deactivate Logging for unit tests
        LogManager.Configuration.LoggingRules.Clear();
        LogManager.ReconfigExistingLoggers();
    }

    /// <summary>
    /// Tests a simple rule with two criteria that must strictly match all facts.
    /// </summary>
    [Fact]
    public void SimpleOneRuleTwoCriteriaStrictMatch()
    {
        var rule1 = new Rule(criterias:
        [
            new Criteria<string>(factName: "who", expectedValue: "Nick", @operator: Operator.Equal),
            new Criteria<string>(factName: "concept", expectedValue: "onHit", @operator: Operator.Equal),
        ], payload: () => { });

        var (matched, numberMatched) = rule1.Evaluate(facts: new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(condition: matched);
        Assert.Equal(expected: 2, actual: numberMatched);
    }

    /// <summary>
    /// Tests a simple rule with two criteria using predicates that must strictly match all facts.
    /// </summary>
    [Fact]
    public void SimpleOneRuleTwoCriteriaPredicateBasedStrictMatch()
    {
        var rule1 = new Rule(criterias:
        [
            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
            new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
        ], payload: () => { });

        var (matched, numberMatched) = rule1.Evaluate(facts: new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"}
        });

        Assert.True(condition: matched);
        Assert.Equal(expected: 2, actual: numberMatched);
    }

    /// <summary>
    /// Tests a simple rule with two criteria where only one fact matches, expecting no match.
    /// </summary>
    [Fact]
    public void SimpleOneRuleOneCriteriaStrictMatch()
    {
        var rule1 = new Rule(criterias:
        [
            new Criteria<string>(factName: "who", expectedValue: "Nick", @operator: Operator.Equal),
            new Criteria<string>(factName: "concept", expectedValue: "onHit", @operator: Operator.Equal),
        ], payload: () => { });

        var (completeMatch, numberOfMatchedCriteria) = rule1.Evaluate(facts: new Dictionary<string, object>
        {
            { "concept", "onHit" },
        });

        Assert.False(condition: completeMatch);
        Assert.Equal(expected: 0, actual: numberOfMatchedCriteria);
    }

    /// <summary>
    /// Tests that when multiple rules match, one of the most specific rules is selected randomly.
    /// </summary>
    [Fact]
    public void RandomRuleSelectionIfMultipleRulesMatch()
    {
        var query = new Query();
        // Should never execute
        var rule1Executed = false;
        // These should always execute
        var rule2Executed = false;
        var rule3Executed = false;
        var rule4Executed = false;

        query
            .Add(key: "who", value: "Nick")
            .Add(key: "concept", value: "onHit")
            .Add(key: "curMap", value: "circus")
            .Add(key: "health", value: 0.66)
            .Add(key: "nearAllies", value: 2)
            .Add(key: "hitBy", value: "zombieClown");


        List<Rule> rules = [
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "Ouch");
                    // Should never execute
                    rule1Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 1),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "ow help!");
                    rule2Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "This Circus Sucks!");
                    rule3Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => hitBy == "zombieClown"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "Stupid Clown!");
                    rule4Executed = true;
                }),
        ];


        // We query match 1000 times so we can guarantee that each rule at least once matched and got executed.
        // It's a 33/100 chance that one of the rules gets executed. 
        for (int i = 0; i < 1000; i++)
        {
            query.Match(rules: rules);
        }

        Assert.False(condition: rule1Executed);
        Assert.True(condition: rule2Executed);
        Assert.True(condition: rule3Executed);
        Assert.True(condition: rule4Executed);
    }

    /// <summary>
    /// This example was given in this [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&amp;t) talk.
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
            .Add(key: "who", value: "Nick")
            .Add(key: "concept", value: "onHit")
            .Add(key: "curMap", value: "circus")
            .Add(key: "health", value: 0.66)
            .Add(key: "nearAllies", value: 2)
            .Add(key: "hitBy", value: "zombieClown");


        List<Rule> rules = [
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "Ouch");
                    rule1Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 1),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "ow help!");
                    rule2Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "This Circus Sucks!");
                    rule3Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => hitBy == "zombieClown"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "Stupid Clown!");
                    rule4Executed = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => hitBy == "zombieClown"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                    _testOutputHelper.WriteLine(message: "I hate circus clowns!");
                    rule5Executed = true;
                }),
        ];

        query.Match(rules: rules);
        Assert.False(condition: rule1Executed);
        Assert.False(condition: rule2Executed);
        Assert.False(condition: rule3Executed);
        Assert.False(condition: rule4Executed);
        Assert.True(condition: rule5Executed);
    }
}
