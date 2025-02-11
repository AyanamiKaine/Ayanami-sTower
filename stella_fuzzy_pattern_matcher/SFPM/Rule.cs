namespace SFPM;

/// <summary>
/// A list of criterions that if all matched the rule "matches", if
/// no criterion is matched the rule rejects the match. 
/// 
/// Partial matching is possible.
/// </summary>
/// <param name="criterias"></param>
/// <param name="payload">The payload is a function that gets executed when the rule is the most matched rule</param>
public class Rule(List<ICriteria> criterias, Action payload)
{
    private readonly Action _payload = payload;
    private readonly List<ICriteria> _criterias = criterias ?? throw new ArgumentNullException(nameof(criterias)); // Use the interface ICriteria

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// IT WILL RETURN IMMEDIATLY IF ONE CRITERIA IS NOT MATCHED. The matched count will be 0.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts.
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) StrictEvaluate(Dictionary<string, object> facts)
    {
        int matchedCriteriaCount = 0;
        foreach (var criteria in _criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
                if (criteria.Matches(factValue)) // Call the interface method
                {
                    matchedCriteriaCount++; // Increment the counter if the criteria matches
                }
            }
            return (false, 0);
        }

        // Rule is considered fully true if all criteria are matched.
        bool isTrue = matchedCriteriaCount == _criterias.Count;
        return (isTrue, matchedCriteriaCount);
    }

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// This is useful if you want to allow paritally matched criteria in a rule.
    /// IT WILL NOT RETURN IMMEDIATLY IF ONE CRITERIA IS NOT MATCHED.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts.
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) RelaxedEvaluate(Dictionary<string, object> facts)
    {
        int matchedCriteriaCount = 0;
        foreach (var criteria in _criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
                if (criteria.Matches(factValue)) // Call the interface method
                {
                    matchedCriteriaCount++; // Increment the counter if the criteria matches
                }
            }
        }

        // Rule is considered fully true if all criteria are matched.
        bool isTrue = matchedCriteriaCount == _criterias.Count;
        return (isTrue, matchedCriteriaCount);
    }


    /// <summary>
    /// Checks if the rule is true based on a set of facts. (Legacy method - for backwards compatibility or simpler use cases)
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>True if all criteria match the facts, otherwise false.</returns>
    public bool IsTrue(Dictionary<string, object> facts) // Keeping the old IsTrue for compatibility
    {
        return StrictEvaluate(facts).IsTrue; // Just calls the new Evaluate and returns the boolean part
    }

    /// <summary>
    /// Executes the payload action associated with this rule.
    /// </summary>
    public void ExecutePayload()
    {
        _payload();
    }

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => _criterias.Count;
}