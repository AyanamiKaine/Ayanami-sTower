namespace SFPM.Tests;

public class QueryUnitTest
{
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
            ];


        query.Match(rules);

        Assert.True(ruleExecuted);
    }
}