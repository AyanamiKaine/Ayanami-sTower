using NLog;

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

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// We might set a priority for a rule, its used when a query matches more than one rule with the same
    /// number of criteria. We then select the rule with the highest priority. If the have both the same 
    /// priority we select a random one.
    /// </summary>
    public int Priority { get; set; }
    private readonly Action _payload = payload;
    private readonly List<ICriteria> _criterias = criterias ?? throw new ArgumentNullException(nameof(criterias)); // Use the interface ICriteria

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// IT WILL RETURN IMMEDIATLY IF ONE CRITERIA IS NOT MATCHED. The matched count will be 0.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts, if not all criteria matched returns 0
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) StrictEvaluate(Dictionary<string, object> facts)
    {
#if DEBUG
        logger.Debug($"SFPM.Rule.StrictEvaluate: Evaluating rule with {CriteriaCount} criteria.");
        logger.Debug($"SFPM.Rule.StrictEvaluate: Facts provided: {string.Join(", ", facts)}");
#endif

        int matchedCriteriaCount = 0;
        foreach (var criteria in _criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
#if DEBUG
                logger.Debug($"SFPM.Rule.StrictEvaluate: Checking criteria for fact '{criteria.FactName}' with value '{factValue}'.");
#endif
                if (criteria.Matches(factValue))
                {
#if DEBUG
                    logger.Debug($"SFPM.Rule.StrictEvaluate: Criteria for fact '{criteria.FactName}' matched.");
#endif
                    matchedCriteriaCount++;
                }
                else
                {
#if DEBUG
                    logger.Debug($"SFPM.Rule.StrictEvaluate: Criteria for fact '{criteria.FactName}' did NOT match. StrictEvaluate returning false.");
#endif
                    return (false, 0);
                }
            }
            else
            {
#if DEBUG
                logger.Debug($"SFPM.Rule.StrictEvaluate: Fact '{criteria.FactName}' not found or fact name is empty. StrictEvaluate returning false.");
#endif
                return (false, 0);
            }
        }
        return (true, matchedCriteriaCount);
    }

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// This is useful if you want to allow paritally matched criteria in a rule. But it reduces performances
    /// as we have to evaluate all criteria. Grows linearly.
    /// IT WILL NOT RETURN IMMEDIATLY IF ONE CRITERIA IS NOT MATCHED.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts.
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) RelaxedEvaluate(Dictionary<string, object> facts)
    {
#if DEBUG
        logger.Debug("SFPM.Rule.RelaxedEvaluate: Starting relaxed evaluation for rule with {CriteriaCount} criteria.", CriteriaCount); // Debug log for method entry
#endif

        int matchedCriteriaCount = 0;
        foreach (var criteria in _criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
                if (criteria.Matches(factValue))
                {
                    matchedCriteriaCount++;
#if DEBUG
                    logger.Trace("SFPM.Rule.RelaxedEvaluate: Criteria for fact '{FactName}' matched (relaxed). Matched count: {MatchedCriteriaCount}", criteria.FactName,
                    matchedCriteriaCount); // Trace level for relaxed match
#endif
                }
#if DEBUG

                else
                {
                    logger.Trace("SFPM.Rule.RelaxedEvaluate: Criteria for fact '{FactName}' did NOT match (relaxed).", criteria.FactName); // Trace level for relaxed non-match
                }
#endif

            }
#if DEBUG
            else
            {
                logger.Trace("SFPM.Rule.RelaxedEvaluate: Fact '{FactName}' not found or fact name is empty (relaxed).", criteria.FactName); // Trace for fact not found in relaxed
            }
#endif
        }

        // Rule is considered fully true if all criteria are matched.
        bool isTrue = matchedCriteriaCount == _criterias.Count;
#if DEBUG
        logger.Debug("SFPM.Rule.RelaxedEvaluate: Relaxed evaluation finished. Rule isTrue: {IsTrue}, Matched criteria count: {MatchedCriteriaCount}.", isTrue, matchedCriteriaCount); // Debug log for method exit and result
#endif
        return (isTrue, matchedCriteriaCount);
    }


    /// <summary>
    /// Checks if the rule is true based on a set of facts. (Legacy method - for backwards compatibility or simpler use cases)
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>True if all criteria match the facts, otherwise false.</returns>
    public bool IsTrue(Dictionary<string, object> facts) // Keeping the old IsTrue for compatibility
    {
#if DEBUG
        logger.Debug("SFPM.Rule.IsTrue: Calling StrictEvaluate for IsTrue check."); // Debug log before calling StrictEvaluate
#endif
        bool result = StrictEvaluate(facts).IsTrue;
#if DEBUG
        logger.Debug("SFPM.Rule.IsTrue: StrictEvaluate returned {Result} for IsTrue.", result); // Debug log after StrictEvaluate
#endif
        return result; // Just calls the new Evaluate and returns the boolean part
    }

    /// <summary>
    /// Executes the payload action associated with this rule.
    /// </summary>
    public void ExecutePayload()
    {
#if DEBUG
        logger.Info("SFPM.Rule.ExecutePayload: Executing payload for rule (Priority: {Priority}).", Priority); // Info level for payload execution, including priority
#endif
        try
        {
            _payload();
#if DEBUG
            logger.Debug("SFPM.Rule.ExecutePayload: Payload executed successfully (Priority: {Priority}).", Priority); // Debug log on successful payload execution
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            logger.Error(ex, "SFPM.Rule.ExecutePayload: Exception during payload execution (Priority: {Priority}).", Priority); // Error log with exception details
                                                                                                                                // Consider re-throwing or handling the exception as needed in your application logic.
                                                                                                                                // For now, we're logging the error.
#endif
        }
    }

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => _criterias.Count;
}