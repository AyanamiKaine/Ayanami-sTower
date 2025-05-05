using NLog;

namespace AyanamisTower.SFPM;

/// <summary>
/// Provides extension methods for <see cref="List{Rule}"/> to optimize rule processing.
/// </summary>
public static class RuleListExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static bool IsOptimized(List<Rule> rules)
    {
        if (rules == null || rules.Count < 2)
            return true; // Empty or single element is "optimized"
        for (int i = 0; i < rules.Count - 1; i++)
        {
            if ((rules[i]?.CriteriaCount ?? -1) < (rules[i + 1]?.CriteriaCount ?? -1))
            {
                return false; // Found an element smaller than the next one
            }
        }
        return true;
    }

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
            throw new ArgumentException(
                message: "Rules list cannot be empty.",
                paramName: nameof(rules)
            );
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
            throw new ArgumentException(
                message: "Rules list cannot be empty.",
                paramName: nameof(rules)
            );
        return rules.MinBy(keySelector: r => r.CriteriaCount)!;
    }

    /// <summary>
    /// Matches rules against the provided fact source and executes the payload of the best matching rule.
    /// </summary>
    /// <param name="rules">The list of rules to match against.</param>
    /// <param name="facts">The source containing the facts to match.</param>
    public static void Match(this List<Rule> rules, IFactSource facts) // Changed parameter type
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0; // Stores the CriteriaCount of the best match found so far
        bool optimized = IsOptimized(rules); // Check if rules appear optimized (optional check)

        Logger.ConditionalDebug(
            $"SFPM.RuleListExtensions.Match: Matching against {rules.Count} rules."
        );

        foreach (var rule in rules)
        {
            if (rule == null)
                continue; // Skip null rules

            string ruleId = string.IsNullOrEmpty(rule.Name) ? "[Unnamed Rule]" : rule.Name;
            Logger.ConditionalDebug(
                $"SFPM.RuleListExtensions.Match: Evaluating rule '{ruleId}' (Criteria: {rule.CriteriaCount}, Priority: {rule.Priority})."
            );

            // Optimization: If rules are sorted by CriteriaCount descending, we can potentially break early.
            if (optimized && rule.CriteriaCount < currentHighestScore)
            {
                Logger.ConditionalDebug(
                    $"SFPM.RuleListExtensions.Match: Skipping rule '{ruleId}' as it has fewer criteria ({rule.CriteriaCount}) than the current best match ({currentHighestScore}) and rules are optimized."
                );
                break; // Stop checking less specific rules if a more specific one already matched
            }

            // Call the updated Evaluate method on the rule
            var (matched, matchedCriteriaCount) = rule.Evaluate(facts);

            if (matched)
            {
                // We only care about rules where ALL criteria matched.
                // matchedCriteriaCount here will be equal to rule.CriteriaCount if matched is true.
                if (matchedCriteriaCount > currentHighestScore)
                {
                    // Found a more specific rule that matches
                    Logger.ConditionalDebug(
                        $"SFPM.RuleListExtensions.Match: New best matching rule found: '{ruleId}' (Criteria: {matchedCriteriaCount}). Clearing previous matches."
                    );
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear(); // Clear less specific matches
                    acceptedRules.Add(rule);
                }
                else if (matchedCriteriaCount == currentHighestScore && currentHighestScore > 0)
                {
                    // Found another rule with the same highest specificity
                    Logger.ConditionalDebug(
                        $"SFPM.RuleListExtensions.Match: Additional rule matched with same highest criteria count ({matchedCriteriaCount}): '{ruleId}'. Adding to candidates."
                    );
                    acceptedRules.Add(rule);
                }
                // else: matchedCriteriaCount < currentHighestScore, ignore this rule.
            }
            // else: Rule didn't match, continue to the next rule.
        }

        // --- Select and Execute Payload ---
        if (acceptedRules.Count == 0)
        {
            Logger.ConditionalDebug(
                "SFPM.RuleListExtensions.Match: No rules matched the provided facts."
            );
        }
        else if (acceptedRules.Count == 1)
        {
            string selectedRuleId = string.IsNullOrEmpty(acceptedRules[0].Name)
                ? "[Unnamed Rule]"
                : acceptedRules[0].Name;
            Logger.ConditionalDebug(
                $"SFPM.RuleListExtensions.Match: Exactly one best rule matched: '{selectedRuleId}'. Executing payload."
            );
            acceptedRules[0].ExecutePayload();
        }
        else // acceptedRules.Count > 1
        {
            // Multiple rules tied for the highest criteria count, use priority
            Logger.ConditionalDebug(
                $"SFPM.RuleListExtensions.Match: {acceptedRules.Count} rules tied for the best match (Criteria: {currentHighestScore}). Selecting based on priority."
            );

            var highestPriority = acceptedRules.Max(r => r.Priority);
            var priorityCandidates = acceptedRules
                .Where(r => r.Priority == highestPriority)
                .ToList();

            Rule selectedRule;
            if (priorityCandidates.Count == 1)
            {
                selectedRule = priorityCandidates[0];
                string selectedRuleId = string.IsNullOrEmpty(selectedRule.Name)
                    ? "[Unnamed Rule]"
                    : selectedRule.Name;
                Logger.ConditionalDebug(
                    $"SFPM.RuleListExtensions.Match: Selecting rule '{selectedRuleId}' based on highest priority ({highestPriority})."
                );
            }
            else
            {
                // Multiple rules tied on priority, select randomly
                var random = new Random();
                int randomIndex = random.Next(priorityCandidates.Count);
                selectedRule = priorityCandidates[randomIndex];
                string selectedRuleId = string.IsNullOrEmpty(selectedRule.Name)
                    ? "[Unnamed Rule]"
                    : selectedRule.Name;
                Logger.ConditionalDebug(
                    $"SFPM.RuleListExtensions.Match: {priorityCandidates.Count} rules tied on highest priority ({highestPriority}). Randomly selected rule '{selectedRuleId}'."
                );
            }
            selectedRule.ExecutePayload();
        }
    }
}
