namespace SFPM;
/// <summary>
/// Provides extension methods for <see cref="List{Rule}"/> to optimize rule processing.
/// </summary>
public static class RuleListExtensions
{
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
}