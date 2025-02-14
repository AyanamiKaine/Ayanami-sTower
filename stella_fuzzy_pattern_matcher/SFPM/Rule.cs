using NLog;

namespace SFPM;

/// <summary>
/// A list of criterions that if all matched the rule "matches", if
/// no criterion is matched the rule rejects the match.
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
    /// <summary>
    /// Gets or sets the action to be executed when this rule matches.
    /// Represents a delegate that encapsulates a method that takes no parameters and does not return a value.
    /// </summary>
    /// <value>
    /// The action delegate to be executed.
    /// </value>
    public Action Payload { get; set; } = payload;
    /// <summary>
    /// Gets or sets the list of criteria that compose this rule.
    /// </summary>
    /// <value>
    /// A list of <see cref="ICriteria"/> objects that define the conditions for this rule.
    /// </value>
    /// <exception cref="ArgumentNullException">
    /// Thrown when attempting to set a null value for the criteria list.
    /// </exception>
    public List<ICriteria> Criterias { get; set; } = criterias ?? throw new ArgumentNullException(nameof(criterias));

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => Criterias.Count;

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// IT WILL RETURN IMMEDIATLY IF ONE CRITERIA IS NOT MATCHED. The matched count will be 0.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts, if not all criteria matched returns 0
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) Evaluate(Dictionary<string, object> facts)
    {
        logger.ConditionalDebug($"SFPM.Rule.Evaluate: Evaluating rule with {CriteriaCount} criteria.");
        logger.ConditionalDebug($"SFPM.Rule.Evaluate: Facts provided: {string.Join(", ", facts)}");

        int matchedCriteriaCount = 0;
        foreach (var criteria in Criterias)
        {
            if (!string.IsNullOrEmpty(criteria.FactName) && facts.TryGetValue(criteria.FactName ?? string.Empty, out object? factValue))
            {
                logger.ConditionalDebug($"SFPM.Rule.Evaluate: Checking criteria for fact '{criteria.FactName}' with value '{factValue}'.");
                if (criteria.Matches(factValue: factValue))
                {
                    logger.ConditionalDebug($"SFPM.Rule.Evaluate: Criteria for fact '{criteria.FactName}' matched.");
                    matchedCriteriaCount++;
                }
                else
                {
                    logger.ConditionalDebug($"SFPM.Rule.Evaluate: Criteria for fact '{criteria.FactName}' did NOT match. Evaluate returning false.");
                    return (false, 0);
                }
            }
            else
            {
                logger.ConditionalDebug($"SFPM.Rule.Evaluate: Fact '{criteria.FactName}' not found or fact name is empty. Evaluate returning false.");
                return (false, 0);
            }
        }
        logger.ConditionalDebug("SFPM.Rule.Evaluate: Strict Evaluate finished. Rule isTrue: {IsTrue}, Matched criteria count: {MatchedCriteriaCount}.", true, matchedCriteriaCount);
        return (true, matchedCriteriaCount);
    }

    /// <summary>
    /// Executes the payload action associated with this rule.
    /// </summary>
    public void ExecutePayload()
    {
        logger.ConditionalDebug("SFPM.Rule.ExecutePayload: Executing payload for rule (Priority: {Priority}).", Priority); // Info level for payload execution, including priority
        try
        {
            Payload();
            logger.ConditionalDebug("SFPM.Rule.ExecutePayload: Payload executed successfully (Priority: {Priority}).", Priority); // Debug log on successful payload execution
        }
        catch (Exception ex)
        {
            logger.ConditionalDebug(ex, "SFPM.Rule.ExecutePayload: Exception during payload execution (Priority: {Priority}).", Priority); // Error log with exception details
            Console.WriteLine(ex.Message);
        }
    }
}