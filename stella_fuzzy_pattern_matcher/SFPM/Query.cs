using System.Collections;

namespace SFPM;

/// <summary>
/// Represents a query, its a set of key, value pairs.
/// </summary>
public class Query()
{

    /// <summary>
    /// Sets the query data from an dictionary
    /// </summary>
    /// <param name="queryData"></param>
    public Query(Dictionary<string, object> queryData) : this()
    {
        _queryData = queryData;
    }

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
    /// 
    /// To improve performance, sort rules from most specific rule to less.
    /// I.e. the first rule in the list should have the most criteria.
    /// </summary>
    /// <param name="rules"></param>
    public void Match(List<Rule> rules)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;
        foreach (var rule in rules)
        {
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
            // Group rules by priority
            var groupedByPriority = acceptedRules.GroupBy(r => r.Priority)
                                               .OrderByDescending(g => g.Key)
                                               .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = groupedByPriority.ElementAt(random.Next(groupedByPriority.Count()));
            selectedRule.ExecutePayload();
        }
    }
}