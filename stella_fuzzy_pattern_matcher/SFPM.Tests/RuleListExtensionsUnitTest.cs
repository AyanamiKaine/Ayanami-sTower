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
                  new Rule(criterias:
                  [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 1),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                ], payload: ()=>{
                }),
        ];

        var mostSpecificRule = rules.MostSpecificRule();

        Assert.Equal(expected: 3, actual: mostSpecificRule.CriteriaCount);
    }

    [Fact]
    public void LeastSpecificRuleTest()
    {
        List<Rule> rules = [
                  new Rule(criterias:
                  [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 1),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                ], payload: ()=>{
                }),
        ];

        var mostSpecificRule = rules.LeastSpecificRule();

        Assert.Equal(expected: 1, actual: mostSpecificRule.CriteriaCount);
    }

    [Fact]
    public void MatchOnRulesList()
    {
        var ruleExecuted = false;

        List<Rule> rules = [
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => nearAllies > 1),
                ], payload: ()=>{
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(factName: "concept", predicate: concept => concept == "onHit"),
                    new Criteria<string>(factName: "curMap", predicate: curMap => curMap == "circus"),
                ], payload: ()=>{
                    ruleExecuted = true;
                }),
          new Rule(criterias:
          [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                ], payload: ()=>{
                }),
        ];

        rules.Match(queryData: new Dictionary<string, object>
            {
                { "concept",    "onHit" },
                { "who",        "Nick"},
                { "curMap",     "circus"}
            });

        Assert.True(condition: ruleExecuted);
    }
}
