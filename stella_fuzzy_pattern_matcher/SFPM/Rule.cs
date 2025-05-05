using NLog;

namespace AyanamisTower.SFPM;

/// <summary>
/// Represents a rule in a fuzzy pattern matching system that contains criteria and an action to execute when matched.
/// </summary>
/// <param name="criterias"></param>
/// <param name="payload">The payload is a function that gets executed when the rule is the most matched rule</param>
/// <param name="name">Optional name identifier for the rule used in debugging.</param>
public class Rule(List<ICriteria> criterias, Action payload, string name = "")
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Gets or sets the name of the rule. Used for debugging purposes. When defined helps finding the rule in the logging output.
    /// </summary>
    /// <value>
    /// The name that identifies this rule.
    /// </value>
    public string Name { get; set; } = name;

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
    public List<ICriteria> Criterias { get; set; } =
        criterias ?? throw new ArgumentNullException(paramName: nameof(criterias));

    /// <summary>
    /// Gets the number of criteria in this rule.
    /// </summary>
    public int CriteriaCount => Criterias.Count;

    /// <summary>
    /// Checks if the rule is true based on facts from the source and returns the number of criteria.
    /// Returns immediately if one criteria is not matched.
    /// </summary>
    /// <param name="facts">The source of facts to check against the criteria.</param>
    /// <returns>A tuple containing:
    ///     - Item1: True if all criteria match the facts, otherwise false.
    ///     - Item2: The number of criteria in the rule if true, otherwise 0.
    /// </returns>
    public (bool IsTrue, int MatchedCriteriaCount) Evaluate(IFactSource facts) // Changed parameter
    {
        string ruleId = string.IsNullOrEmpty(Name) ? "[Unnamed Rule]" : Name;
        Logger.ConditionalDebug(
            $"SFPM.Rule.Evaluate: Evaluating rule '{ruleId}' with {CriteriaCount} criteria."
        );

        if (Criterias.Count == 0)
            return (true, 0); // An empty rule technically matches

        foreach (var criteria in Criterias)
        {
            if (criteria == null) // Defensive check
            {
                Logger.Warn(
                    $"SFPM.Rule.Evaluate: Rule '{ruleId}' contains a null criteria. Skipping it."
                );
                continue; // Or maybe treat as failure? return (false, 0);
            }

            Logger.ConditionalDebug(
                $"SFPM.Rule.Evaluate: Rule '{ruleId}' checking criteria for fact '{criteria.FactName}'."
            );
            if (!criteria.Evaluate(facts)) // Call the new Evaluate on the criteria
            {
                Logger.ConditionalDebug(
                    $"SFPM.Rule.Evaluate: Rule '{ruleId}': Criteria for fact '{criteria.FactName}' did NOT match. Evaluate returning false."
                );
                return (false, 0); // Short-circuit
            }
            // Logger.ConditionalDebug($"SFPM.Rule.Evaluate: Rule '{ruleId}': Criteria for fact '{criteria.FactName}' matched.");
        }

        Logger.ConditionalDebug(
            $"SFPM.Rule.Evaluate: Rule '{ruleId}': All {CriteriaCount} criteria matched. Rule is true."
        );
        return (true, CriteriaCount); // All criteria passed
    }

    /// <summary>
    /// Executes the payload action associated with this rule.
    /// </summary>
    public void ExecutePayload()
    {
        Logger.ConditionalDebug(
            message: "SFPM.Rule.ExecutePayload: Executing payload for rule (Priority: {Priority}).",
            argument: Priority
        ); // Info level for payload execution, including priority
        try
        {
            Payload();
            Logger.ConditionalDebug(
                message: "SFPM.Rule.ExecutePayload: Payload executed successfully (Priority: {Priority}).",
                argument: Priority
            ); // Debug log on successful payload execution
        }
        catch (Exception ex)
        {
            Logger.ConditionalDebug(
                exception: ex,
                message: "SFPM.Rule.ExecutePayload: Exception during payload execution (Priority: {Priority}).",
                args: Priority
            ); // Error log with exception details
            Console.WriteLine(value: ex.Message);
        }
    }
}
