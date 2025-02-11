using System.Collections;

namespace SFPM;

/// <summary>
/// Represents a query, its a set of key, value pairs.
/// </summary>
public class Query()
{
    private readonly Dictionary<string, object> _queryData = [];

    /// <summary>
    /// Adds a key and value to the query.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Query Add(string key, object value)
    {
        _queryData.Add(key, value);
        return this;
    }

    /// <summary>
    /// Matches a query against a list of rules, it tries to select a rule that matches the most
    /// (i.e. the most criteria) as its more specific. Then it runs the payload of the rule.
    /// </summary>
    /// <param name="rules"></param>
    public void Match(List<Rule> rules)
    {
        // Rules with the most criteria will be at the front of the list       
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;
        foreach (var rule in rules)
        {
            // When we already matched a rule that is the most specifc rule
            // we have, we stop matching other rules. 
            // Avoiding linear growth in the best case.
            // In the worst case allowing it.
            if (rule.CriteriaCount < currentHighestScore)
                break;

            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(_queryData);
            if (matched)
            {
                acceptedRules.Add(rule);
                if (matchedCriteriaCount > currentHighestScore)
                    currentHighestScore = matchedCriteriaCount;
            }
        }

        if (acceptedRules.Count == 1)
        {
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Sort by priority in descending order and execute the highest priority rule
            var highestPriorityRule = acceptedRules.OrderByDescending(r => r.Priority).First();
            highestPriorityRule.ExecutePayload();
        }
    }
}