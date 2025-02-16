using NLog;

namespace SFPM;

/// <summary>
/// Represents a query, it's a set facts, represented as a key value pair.
/// </summary>
public class Query()
{

    /// <summary>
    /// Sets the query data from a dictionary
    /// </summary>
    /// <param name="queryData"></param>
    public Query(Dictionary<string, object> queryData) : this()
    {
        _queryData = queryData;
    }

    private readonly Dictionary<string, object> _queryData = [];
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Adds a key and value to the query.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Query Add(string key, object value)
    {
        _queryData.Add(
            key: key,
            value: value);
        return this;
    }

    /// <summary>
    /// Matches a query against a list of rules, it tries to select a rule that matches the most
    /// (i.e. the most criteria) as its more specific. Then it runs the payload of the rule.
    ///
    /// If more than one rule with the same amount of criteria matches a random one in selected.
    ///
    /// To improve performance, sort rules from most specific rule to less.
    /// I.e. the first rule in the list should have the most criteria.
    /// </summary>
    /// <param name="rules"></param>
    public void Match(List<Rule> rules)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;
        Logger.ConditionalDebug(message: $"SFPM.Query.Match: Matching against {rules.Count} rules.");

        foreach (var rule in rules)
        {
            if (rule.CriteriaCount < currentHighestScore)
            {
                Logger.ConditionalDebug(message: "SFPM.Query.Match: Skipping current rule as it has less criterias, then the current highest matched one");
            }

            var (matched, matchedCriteriaCount) = rule.Evaluate(facts: _queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(item: rule);
                }
            }
        }

        if (acceptedRules.Count == 1)
        {
            acceptedRules[index: 0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            Logger.ConditionalDebug(message: $"SFPM.Query.Match: More than one rule with the same number of criteria matched({acceptedRules.Count}). Grouping them by priority, selecting the highest priority one, if multiple rules have the same priority selecting a random one.");
            // Group the highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(keySelector: r => r.Priority)
                                                   .OrderByDescending(keySelector: g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(index: random.Next(maxValue: highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
    }
}