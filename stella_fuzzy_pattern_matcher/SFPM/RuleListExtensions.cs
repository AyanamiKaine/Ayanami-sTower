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
}