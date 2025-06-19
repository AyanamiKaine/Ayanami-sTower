using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AyanamisTower.SFPM;

/// <summary>
/// Provides extension methods for <see cref="IEnumerable{Rule}"/> to optimize and process rule collections.
/// </summary>
public static class RuleEnumerableExtensions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Orders the rules by their criteria count in descending order.
    /// </summary>
    /// <param name="rules">The collection of rules to order.</param>
    /// <returns>An <see cref="IOrderedEnumerable{Rule}"/> with rules sorted from most to least specific.</returns>
    /// <remarks>
    /// This is a non-destructive operation that returns a new, ordered enumerable.
    /// It can be used to improve pattern matching performance by evaluating more specific rules first.
    /// </remarks>
    public static IOrderedEnumerable<Rule> OrderBySpecificity(this IEnumerable<Rule> rules)
    {
        return rules.OrderByDescending(r => r.CriteriaCount);
    }

    /// <summary>
    /// Returns the rule with the highest number of criteria from the collection.
    /// </summary>
    /// <param name="rules">The collection of rules to search. Must not be empty.</param>
    /// <returns>The rule with the most criteria from the list.</returns>
    /// <exception cref="ArgumentException">Thrown when the rules collection is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when rules parameter is null.</exception>
    public static Rule MostSpecificRule(this IEnumerable<Rule> rules)
    {
        var ruleList = rules.ToList();
        if (!ruleList.Any())
            throw new ArgumentException(
                message: "Rules collection cannot be empty.",
                paramName: nameof(rules)
            );
        return ruleList.MaxBy(keySelector: r => r.CriteriaCount)!;
    }

    /// <summary>
    /// Finds and returns the rule with the least number of criteria from the given collection of rules.
    /// </summary>
    /// <param name="rules">The collection of rules to search through.</param>
    /// <returns>The rule with the minimum number of criteria.</returns>
    /// <exception cref="ArgumentException">Thrown when the rules collection is empty.</exception>
    public static Rule LeastSpecificRule(this IEnumerable<Rule> rules)
    {
        var ruleList = rules.ToList();
        if (!ruleList.Any())
            throw new ArgumentException(
                message: "Rules collection cannot be empty.",
                paramName: nameof(rules)
            );
        return ruleList.MinBy(keySelector: r => r.CriteriaCount)!;
    }

    /// <summary>
    /// Matches rules against the provided fact source and executes the payload of the best matching rule.
    /// </summary>
    /// <param name="rules">The collection of rules to match against.</param>
    /// <param name="facts">The source containing the facts to match.</param>
    public static void Match(this IEnumerable<Rule> rules, IFactSource facts)
    {
        Logger.ConditionalDebug(
            $"SFPM.RuleEnumerableExtensions.Match: Matching against a rule collection."
        );

        var fullyMatchedRules = new List<Rule>();

        // First, iterate through all rules and find every single one that fully matches the facts.
        foreach (var rule in rules)
        {
            if (rule == null)
                continue;

            string ruleId = string.IsNullOrEmpty(rule.Name) ? "[Unnamed Rule]" : rule.Name;
            Logger.ConditionalDebug(
                $"SFPM.RuleEnumerableExtensions.Match: Evaluating rule '{ruleId}' (Criteria: {rule.CriteriaCount}, Priority: {rule.Priority})."
            );

            var (matched, _) = rule.Evaluate(facts);
            if (matched)
            {
                fullyMatchedRules.Add(rule);
            }
        }

        // If no rules matched at all, we can stop here.
        if (fullyMatchedRules.Count == 0)
        {
            Logger.ConditionalDebug(
                "SFPM.RuleEnumerableExtensions.Match: No rules matched the provided facts."
            );
            return;
        }

        // From the list of matched rules, find the highest criteria count.
        var highestScore = fullyMatchedRules.Max(r => r.CriteriaCount);

        // Now, filter the list to only include rules with that highest score.
        var bestRules = fullyMatchedRules.Where(r => r.CriteriaCount == highestScore).ToList();


        // --- Select and Execute Payload from the best candidates ---
        if (bestRules.Count == 1)
        {
            var selectedRule = bestRules[0];
            string selectedRuleId = string.IsNullOrEmpty(selectedRule.Name)
                ? "[Unnamed Rule]"
                : selectedRule.Name;
            Logger.ConditionalDebug(
                $"SFPM.RuleEnumerableExtensions.Match: Exactly one best rule matched: '{selectedRuleId}'. Executing payload."
            );
            selectedRule.ExecutePayload();
        }
        else // bestRules.Count > 1
        {
            // Multiple rules tied for the highest criteria count, so we use priority to break the tie.
            Logger.ConditionalDebug(
                $"SFPM.RuleEnumerableExtensions.Match: {bestRules.Count} rules tied for the best match (Criteria: {highestScore}). Selecting based on priority."
            );

            var highestPriority = bestRules.Max(r => r.Priority);
            var priorityCandidates = bestRules
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
                    $"SFPM.RuleEnumerableExtensions.Match: Selecting rule '{selectedRuleId}' based on highest priority ({highestPriority})."
                );
            }
            else
            {
                // If there's still a tie (multiple rules with the same highest priority), we select one randomly.
                var random = new Random();
                int randomIndex = random.Next(priorityCandidates.Count);
                selectedRule = priorityCandidates[randomIndex];
                string selectedRuleId = string.IsNullOrEmpty(selectedRule.Name)
                    ? "[Unnamed Rule]"
                    : selectedRule.Name;
                Logger.ConditionalDebug(
                    $"SFPM.RuleEnumerableExtensions.Match: {priorityCandidates.Count} rules tied on highest priority ({highestPriority}). Randomly selected rule '{selectedRuleId}'."
                );
            }
            selectedRule.ExecutePayload();
        }
    }
}
