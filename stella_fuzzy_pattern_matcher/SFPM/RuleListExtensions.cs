using NLog;

namespace SFPM;
/// <summary>
/// Provides extension methods for <see cref="List{Rule}"/> to optimize rule processing.
/// </summary>
public static class RuleListExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Optimizes the rules list by sorting rules based on their criteria count in descending order.
    /// </summary>
    /// <param name="rules">The list of rules to optimize.</param>
    /// <remarks>
    /// Rules with more criteria are placed before rules with fewer criteria.
    /// This optimization can improve pattern matching performance by evaluating more specific rules first.
    /// </remarks>
    public static void OptimizeRules(this List<Rule> rules)
    {
        rules.Sort(comparison: (a, b) => b.CriteriaCount.CompareTo(value: a.CriteriaCount));
    }

    /// <summary>
    /// Returns the rule with the highest number of criteria from the list.
    /// </summary>
    /// <param name="rules">The list of rules to search. Must not be empty.</param>
    /// <returns>The rule with the most criteria from the list.</returns>
    /// <exception cref="ArgumentException">Thrown when the rules list is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when rules parameter is null.</exception>
    /// <remarks>
    /// This method uses LINQ's MaxBy operation to find the rule with the highest criteria count.
    /// The operation runs in O(n) time when n is the number of rules in the list.
    /// </remarks>
    public static Rule MostSpecificRule(this List<Rule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException(message: "Rules list cannot be empty.", paramName: nameof(rules));
        return rules.MaxBy(keySelector: r => r.CriteriaCount)!;
    }

    /// <summary>
    /// Finds and returns the rule with the least number of criteria from the given list of rules.
    /// </summary>
    /// <param name="rules">The list of rules to search through.</param>
    /// <returns>The rule with the minimum number of criteria.</returns>
    /// <exception cref="ArgumentException">Thrown when the rules list is empty.</exception>
    /// <remarks>
    /// The rule's specificity is determined by its criteria count - the fewer criteria a rule has,
    /// the less specific it is considered to be.
    /// </remarks>
    public static Rule LeastSpecificRule(this List<Rule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException(message: "Rules list cannot be empty.", paramName: nameof(rules));
        return rules.MinBy(keySelector: r => r.CriteriaCount)!;
    }

    /// <summary>
    /// Matches rules against the provided query data and executes the payload of the best matching rule.
    /// </summary>
    /// <param name="rules">The list of rules to match against.</param>
    /// <param name="queryData">The dictionary containing the data to match.</param>
    public static void Match(this List<Rule> rules, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;
        Logger.ConditionalDebug(message: $"SFPM.Query.Match: Matching against {rules.Count} rules.");
        foreach (var rule in rules)
        {
            Logger.ConditionalDebug(message: $"SFPM.Query.Match: Trying to match new rule with the critera amount {rule.CriteriaCount}");
            if (rule.CriteriaCount < currentHighestScore)
            {
                Logger.ConditionalDebug(message: "SFPM.Query.Match: Skipping current rule as it has less criterias, then the current highest matched one");
            }

            var (matched, matchedCriteriaCount) = rule.Evaluate(facts: queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    Logger.ConditionalDebug(message: $"SFPM.Query.Match: New more specifc rule found, clearing accepted rules list. Old highest criteria rule count: {currentHighestScore}. New Highest criteria rule count: {matchedCriteriaCount}");
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    Logger.ConditionalDebug(message: "SFPM.Query.Match: Rule with the same number of highest criteria matched, adding it to the accepted rules list.");
                    acceptedRules.Add(item: rule);
                }
            }
        }

        if (acceptedRules.Count == 1)
        {
            Logger.ConditionalDebug(message: "SFPM.Query.Match: Matching one Rule only");
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