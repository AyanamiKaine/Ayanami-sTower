using NLog;

namespace SFPM.Tests;

public class RuleListExtensionsUnitTest
{
    public RuleListExtensionsUnitTest()
    {
        // Deactive Logging for unit tests
        LogManager.Configuration.LoggingRules.Clear();
        LogManager.ReconfigExistingLoggers();
    }

    [Fact]
    public void MostSpecificRuleTest()
    {
        List<Rule> rules = [
                  new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                ], ()=>{
                }),
        ];

        var mostSpecificRule = rules.MostSpecificRule();

        Assert.Equal(3, mostSpecificRule.CriteriaCount);
    }

    [Fact]
    public void LeastSpecificRuleTest()
    {
        List<Rule> rules = [
                  new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                ], ()=>{
                }),
        ];

        var mostSpecificRule = rules.LeastSpecificRule();

        Assert.Equal(1, mostSpecificRule.CriteriaCount);
    }

    [Fact]
    public void MatchOnRulesList()
    {
        var ruleExecuted = false;

        List<Rule> rules = [
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], ()=>{
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], ()=>{
                    ruleExecuted = true;
                }),
          new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                ], ()=>{
                }),
        ];

        rules.Match(new Dictionary<string, object>
            {
                { "concept",    "onHit" },
                { "who",        "Nick"},
                { "curMap",     "circus"}
            });

        Assert.True(ruleExecuted);
    }
}
