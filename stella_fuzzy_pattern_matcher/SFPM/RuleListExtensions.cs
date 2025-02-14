using NLog;

namespace SFPM;
/// <summary>
/// Provides extension methods for <see cref="List{Rule}"/> to optimize rule processing.
/// </summary>
public static class RuleListExtensions
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
        rules.Sort((a, b) => b.CriteriaCount.CompareTo(a.CriteriaCount));
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
    /// The operation runs in O(n) time where n is the number of rules in the list.
    /// </remarks>
    public static Rule MostSpecificRule(this List<Rule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException("Rules list cannot be empty.", nameof(rules));
        return rules.MaxBy(r => r.CriteriaCount)!;
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
            throw new ArgumentException("Rules list cannot be empty.", nameof(rules));
        return rules.MinBy(r => r.CriteriaCount)!;
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
        foreach (var rule in rules)
        {
            if (rule.CriteriaCount < currentHighestScore)
            {
                logger.ConditionalDebug("SFPM.Query.Match: Skipping current rule as it has less criterias, then the current highest matched one");
            }

            var (matched, matchedCriteriaCount) = rule.Evaluate(facts: queryData);
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
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            logger.ConditionalDebug($"SFPM.Query.Match: More than one rule with the same number of criteria matched({acceptedRules.Count}). Grouping them by priority, selecting the highest priority one, if multiple rules have the same priority selecting a random one.");
            // Group highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(r => r.Priority)
                                                   .OrderByDescending(g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(index: random.Next(highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
    }
}