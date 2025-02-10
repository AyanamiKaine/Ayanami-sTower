namespace SFPM;

/// <summary>
/// A list of criterions that if all matched the rule "matches", if
/// no criterion is matched the rule rejects the match. 
/// 
/// Partial matching is possible.
/// </summary>
public class Rule(List<ICriteria> criterias)
{
    private readonly List<ICriteria> criterias = criterias ?? throw new ArgumentNullException(nameof(criterias)); // Use the interface ICriteria

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts.
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) Evaluate(Dictionary<string, object> facts)
    {
        int matchedCriteriaCount = 0;
        foreach (var criteria in criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
                if (criteria.Matches(factValue)) // Call the interface method
                {
                    matchedCriteriaCount++; // Increment the counter if the criteria matches
                }
            }
            // We do not return false immediately if a criteria fails to match.
            // We continue to check all criteria to count the matches.
        }

        // Rule is considered fully true if all criteria are matched.
        bool isTrue = matchedCriteriaCount == criterias.Count;
        return (isTrue, matchedCriteriaCount);
    }


    /// <summary>
    /// Checks if the rule is true based on a set of facts. (Legacy method - for backwards compatibility or simpler use cases)
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>True if all criteria match the facts, otherwise false.</returns>
    public bool IsTrue(Dictionary<string, object> facts) // Keeping the old IsTrue for compatibility
    {
        return Evaluate(facts).IsTrue; // Just calls the new Evaluate and returns the boolean part
    }

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => criterias.Count;

    /// <summary>
    /// Combines two rules by concatenating their criteria lists.
    /// </summary>
    /// <param name="rule1">The first rule to combine.</param>
    /// <param name="rule2">The second rule to combine.</param>
    /// <returns>A new Rule containing all criteria from both input rules.</returns>
    public static Rule operator +(Rule rule1, Rule rule2)
    {
        ArgumentNullException.ThrowIfNull(rule1);
        ArgumentNullException.ThrowIfNull(rule2);

        // Concatenate the criteria lists from both rules using LINQ's Concat
        List<ICriteria> combinedCriteria = [.. rule1.criterias, .. rule2.criterias];

        // Create a new Rule with the combined criteria
        return new Rule(combinedCriteria);
    }
}