using NLog;

namespace AyanamisTower.SFPM.Tests;

/// <summary>
/// Contains unit tests for the Query class.
/// </summary>
public class QueryUnitTest
{

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryUnitTest"/> class.
    /// </summary>
    public QueryUnitTest()
    {
        // Deactivate Logging for unit tests
        LogManager.Configuration.LoggingRules.Clear();
        LogManager.ReconfigExistingLoggers();
    }

    /// <summary>
    /// Tests the creation of a Query object.
    /// </summary>
    [Fact]
    public void Creation()
    {
        var query = new Query();
    }

    /// <summary>
    /// Tests adding key-value pairs to a Query object.
    /// </summary>
    [Fact]
    public void AddingKeyValue()
    {
        var query = new Query();

        // In this example we already populate the query with data,
        // and dont actually look it up.
        query
            .Add(key: "concept", value: "OnHit")
            .Add(key: "attacker", value: "Hunter")
            .Add(key: "damage", value: 12.4);
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
            .Add(key: "concept", value: "OnHit")
            .Add(key: "attacker", value: "Hunter")
            .Add(key: "damage", value: 12.4);

        List<Rule> rules = [
                new Rule(criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                    // This should never be executed.
                    ruleExecuted = false;
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "attacker", predicate: attacker => attacker == "Hunter"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "OnHit"),
                    new Criteria<double>(factName: "damage", predicate: damage => damage == 12.4),
                ], payload: ()=>{
                    // This should be executed.
                    ruleExecuted = true;
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "OnHit"),
                    new Criteria<double>(factName: "damage", predicate: damage => damage > 10.0),
                ], payload: () => {
                    // Less specific rule, shouldn't be chosen
                    ruleExecuted = false;
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "attacker", predicate: attacker => attacker.StartsWith(value: 'H')),
                    new Criteria<double>(factName: "damage", predicate: damage => damage < 20.0),
                ], payload: () => {
                    // Less specific rule, shouldn't be chosen
                    ruleExecuted = false;
                })
        ];

        query.Match(rules: rules);

        Assert.True(condition: ruleExecuted);
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
            .Add(key: "concept", value: "OnHit")
            .Add(key: "attacker", value: "Hunter")
            .Add(key: "damage", value: 12.4);

        List<Rule> rules = [
                new Rule(criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "attacker", predicate: attacker => attacker == "Hunter"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "OnHit"),
                    new Criteria<double>(factName: "damage", predicate: damage => damage == 12.4),
                ], payload: ()=>{
                    query.Add(key: "EventAHappened", value: true);
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "attacker", predicate: attacker => attacker == "Hunter"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "OnHit"),
                    new Criteria<double>(factName: "damage", predicate: damage => damage == 12.4),
                    new Criteria<bool>(factName: "EventAHappened", predicate: eventAHappened => eventAHappened == true)
                ], payload: () =>
                {
                        ruleExecuted = true;
                }),

                new Rule(criterias:
                [
                    new Criteria<string>(factName: "attacker", predicate: attacker => attacker.StartsWith(value: 'H')),
                    new Criteria<double>(factName: "damage", predicate: damage => damage < 20.0),
                ], payload: () =>
                {
                })
        ];
        // We first match the rule that adds the EventAHappened to the query
        query.Match(rules: rules);
        // Now we should match the rule with the EventAHappened criteria
        query.Match(rules: rules);
        Assert.True(condition: ruleExecuted);
    }
}