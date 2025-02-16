using NLog;

namespace SFPM;

/// <summary>
/// Represents a rule in a fuzzy pattern matching system that contains criteria and an action to execute when matched.
/// </summary>
/// <param name="criterias"></param>
/// <param name="payload">The payload is a function that gets executed when the rule is the most matched rule</param>
/// <param name="Name">Optional name identifier for the rule used in debugging.</param>
public class Rule(List<ICriteria> criterias, Action payload, string Name = "")
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Gets or sets the name of the rule. Used for debugging purposes. When defined helps finding the rule in the logging output.
    /// </summary>
    /// <value>
    /// The name that identifies this rule.
    /// </value>
    public required string Name { get; set; } = Name;
    /// <summary>
    /// We might set a priority for a rule, its used when a query matches more than one rule with the same
    /// number of criteria. We then select the rule with the highest priority. If they have both the same 
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
    public List<ICriteria> Criterias { get; set; } = criterias ?? throw new ArgumentNullException(paramName: nameof(criterias));

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => Criterias.Count;

    /// <summary>
    /// Checks if the rule is true based on a set of facts and returns the number of matched criteria.
    /// IT WILL RETURN IMMEDIATELY IF ONE CRITERIA IS NOT MATCHED. The matched count will be 0.
    /// </summary>
    /// <param name="facts">A dictionary of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria that matched the facts, if not all criteria matched returns 0
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) Evaluate(Dictionary<string, object> facts)
    {
        Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Evaluating rule with {CriteriaCount} criteria.");
        Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Facts provided: {string.Join(separator: ", ", values: facts)}");

        var matchedCriteriaCount = 0;
        foreach (var criteria in Criterias)
        {
            if (!string.IsNullOrEmpty(value: criteria.FactName) && facts.TryGetValue(key: criteria.FactName, value: out var factValue))
            {
                Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Checking criteria for fact '{criteria.FactName}' with value '{factValue}'.");
                if (criteria.Matches(factValue: factValue))
                {
                    Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Criteria for fact '{criteria.FactName}' matched.");
                    matchedCriteriaCount++;
                }
                else
                {
                    Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Criteria for fact '{criteria.FactName}' did NOT match. Evaluate returning false.");
                    return (false, 0);
                }
            }
            else
            {
                Logger.ConditionalDebug(message: $"SFPM.Rule.Evaluate: Fact '{criteria.FactName}' not found or fact name is empty. Evaluate returning false.");
                return (false, 0);
            }
        }
        Logger.ConditionalDebug(message: "SFPM.Rule.Evaluate: Strict Evaluate finished. Rule isTrue: {IsTrue}, Matched criteria count: {MatchedCriteriaCount}.", argument1: true, argument2: matchedCriteriaCount);
        return (true, matchedCriteriaCount);
    }

    /// <summary>
    /// Executes the payload action associated with this rule.
    /// </summary>
    public void ExecutePayload()
    {
        Logger.ConditionalDebug(message: "SFPM.Rule.ExecutePayload: Executing payload for rule (Priority: {Priority}).", argument: Priority); // Info level for payload execution, including priority
        try
        {
            Payload();
            Logger.ConditionalDebug(message: "SFPM.Rule.ExecutePayload: Payload executed successfully (Priority: {Priority}).", argument: Priority); // Debug log on successful payload execution
        }
        catch (Exception ex)
        {
            Logger.ConditionalDebug(exception: ex, message: "SFPM.Rule.ExecutePayload: Exception during payload execution (Priority: {Priority}).", args: Priority); // Error log with exception details
            Console.WriteLine(value: ex.Message);
        }
    }
}