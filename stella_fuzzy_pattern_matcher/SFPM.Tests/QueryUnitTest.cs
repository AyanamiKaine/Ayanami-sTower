using NLog;

namespace SFPM.Tests;

public class QueryUnitTest
{

    public QueryUnitTest()
    {
        // Deactive Logging for unit tests
        LogManager.Configuration.LoggingRules.Clear();
        LogManager.ReconfigExistingLoggers();
    }

    [Fact]
    public void Creation()
    {
        var query = new Query();
    }

    [Fact]
    public void AddingKeyValue()
    {
        var query = new Query();

        // In this example we already populate the query with data,
        // and dont actually look it up.
        query
            .Add("concept", "OnHit")
            .Add("attacker", "Hunter")
            .Add("damage", 12.4);
    }


    /// <summary>
    /// We want to match a query, against a set of rules and
    /// select the rule that matches the most and execute its payload.
    /// </summary>
    [Fact]
    public void QueryMatchingARule()
    {
        var query = new Query();
        var ruleExecuted = false;

        query
            .Add("concept", "OnHit")
            .Add("attacker", "Hunter")
            .Add("damage", 12.4);

        List<Rule> rules = [
                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                    // This should never be executed.
                    ruleExecuted = false;
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => { return attacker == "Hunter"; }),
                    new Criteria<string>("concept", concept => { return concept == "OnHit"; }),
                    new Criteria<double>("damage", damage => { return damage == 12.4; }),
                ], ()=>{
                    // This should be executed.
                    ruleExecuted = true;
                }),

                new Rule([
                    new Criteria<string>("concept", concept => concept == "OnHit"),
                    new Criteria<double>("damage", damage => damage > 10.0),
                ], () => {
                    // Less specific rule, shouldn't be chosen
                    ruleExecuted = false;
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => attacker.StartsWith("H")),
                    new Criteria<double>("damage", damage => damage < 20.0),
                ], () => {
                    // Less specific rule, shouldn't be chosen
                    ruleExecuted = false;
                })
        ];

        query.Match(rules);

        Assert.True(ruleExecuted);
    }

    /// <summary>
    /// We should be able to add new information to queries, or change existing ones.
    /// </summary>
    [Fact]
    public void AddingMemoryToQuery()
    {
        var query = new Query();
        var ruleExecuted = false;

        query
            .Add("concept", "OnHit")
            .Add("attacker", "Hunter")
            .Add("damage", 12.4);

        List<Rule> rules = [
                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => { return attacker == "Hunter"; }),
                    new Criteria<string>("concept", concept => { return concept == "OnHit"; }),
                    new Criteria<double>("damage", damage => { return damage == 12.4; }),
                ], ()=>{
                    query.Add("EventAHappened", true);
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => { return attacker == "Hunter"; }),
                    new Criteria<string>("concept", concept => concept == "OnHit"),
                    new Criteria<double>("damage", damage => { return damage == 12.4; }),
                    new Criteria<bool>("EventAHappened", EventAHappened =>{ return EventAHappened == true; })
                ], () =>
                {
                        ruleExecuted = true;
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => attacker.StartsWith("H")),
                    new Criteria<double>("damage", damage => damage < 20.0),
                ], () =>
                {
                })
        ];
        // We first match the rule that adds the EventAHappened to the query
        query.Match(rules);
        // Now we should match the rule with the EventAHappened criteria
        query.Match(rules);
        Assert.True(ruleExecuted);
    }
}