using System.Collections.Immutable;
using AyanamisTower.SFPM;

namespace InvictaDB.PatternMatching;

/// <summary>
/// Extension methods for efficiently matching rules against database collections.
/// </summary>
public static class RuleMatchingExtensions
{
    /// <summary>
    /// Finds all entities in a table that match the given rules.
    /// Returns entities where at least one rule fully matches.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="rules">The rules to match against.</param>
    /// <param name="table">The database table to search.</param>
    /// <returns>Entities that matched at least one rule.</returns>
    public static IEnumerable<T> FindMatching<T>(
        this IEnumerable<Rule> rules,
        ImmutableDictionary<string, T> table) where T : class
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
            yield break;

        foreach (var kvp in table)
        {
            var factSource = new EntityFactSource<T>(kvp.Value);
            if (AnyRuleMatches(ruleList, factSource))
            {
                yield return kvp.Value;
            }
        }
    }

    /// <summary>
    /// Finds all entities in a table that match ALL of the given rules.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="rules">The rules that must all match.</param>
    /// <param name="table">The database table to search.</param>
    /// <returns>Entities where all rules matched.</returns>
    public static IEnumerable<T> FindMatchingAll<T>(
        this IEnumerable<Rule> rules,
        ImmutableDictionary<string, T> table) where T : class
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
        {
            foreach (var kvp in table)
                yield return kvp.Value;
            yield break;
        }

        foreach (var kvp in table)
        {
            var factSource = new EntityFactSource<T>(kvp.Value);
            if (AllRulesMatch(ruleList, factSource))
            {
                yield return kvp.Value;
            }
        }
    }

    /// <summary>
    /// Executes the best matching rule's payload for each entity that matches.
    /// Efficient for batch processing with side effects.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="rules">The rules to match.</param>
    /// <param name="table">The database table.</param>
    /// <param name="context">Optional database for providing singleton context to rules.</param>
    public static void MatchAll<T>(
        this IEnumerable<Rule> rules,
        ImmutableDictionary<string, T> table,
        InvictaDatabase? context = null) where T : class
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
            return;

        foreach (var kvp in table)
        {
            IFactSource factSource = context != null
                ? kvp.Value.AsFactSourceWithContext(context)
                : kvp.Value.AsFactSource();
            
            ruleList.Match(factSource);
        }
    }

    /// <summary>
    /// Creates a matcher that can efficiently check multiple entities against the same rules.
    /// Pre-optimizes rules for better performance on large datasets.
    /// </summary>
    /// <param name="rules">The rules to match.</param>
    /// <returns>A reusable matcher instance.</returns>
    public static EntityMatcher CreateMatcher(this IEnumerable<Rule> rules)
    {
        return new EntityMatcher(rules);
    }

    /// <summary>
    /// Finds the first entity that matches any rule, or null if none match.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="rules">The rules to match.</param>
    /// <param name="table">The database table.</param>
    /// <returns>The first matching entity, or null.</returns>
    public static T? FindFirstMatching<T>(
        this IEnumerable<Rule> rules,
        ImmutableDictionary<string, T> table) where T : class
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
            return null;

        foreach (var kvp in table)
        {
            var factSource = new EntityFactSource<T>(kvp.Value);
            if (AnyRuleMatches(ruleList, factSource))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Counts how many entities in the table match at least one rule.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="rules">The rules to match.</param>
    /// <param name="table">The database table.</param>
    /// <returns>The count of matching entities.</returns>
    public static int CountMatching<T>(
        this IEnumerable<Rule> rules,
        ImmutableDictionary<string, T> table) where T : class
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
            return 0;

        int count = 0;
        foreach (var kvp in table)
        {
            var factSource = new EntityFactSource<T>(kvp.Value);
            if (AnyRuleMatches(ruleList, factSource))
            {
                count++;
            }
        }

        return count;
    }

    private static bool AnyRuleMatches(List<Rule> rules, IFactSource facts)
    {
        foreach (var rule in rules)
        {
            var (matched, _) = rule.Evaluate(facts);
            if (matched)
                return true;
        }
        return false;
    }

    private static bool AllRulesMatch(List<Rule> rules, IFactSource facts)
    {
        foreach (var rule in rules)
        {
            var (matched, _) = rule.Evaluate(facts);
            if (!matched)
                return false;
        }
        return true;
    }
}

/// <summary>
/// A reusable matcher that pre-optimizes rules for efficient batch matching.
/// </summary>
public class EntityMatcher
{
    private readonly List<Rule> _optimizedRules;

    /// <summary>
    /// Creates a new EntityMatcher with pre-optimized rules sorted by specificity.
    /// </summary>
    /// <param name="rules">The rules to match against.</param>
    public EntityMatcher(IEnumerable<Rule> rules)
    {
        _optimizedRules = rules.OrderByDescending(r => r.CriteriaCount).ToList();
    }

    /// <summary>
    /// Checks if an entity matches any rule.
    /// </summary>
    public bool Matches<T>(T entity) where T : class
    {
        var factSource = entity.AsFactSource();
        foreach (var rule in _optimizedRules)
        {
            var (matched, _) = rule.Evaluate(factSource);
            if (matched)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if an entity matches any rule, with database context for singletons.
    /// </summary>
    public bool Matches<T>(T entity, InvictaDatabase context) where T : class
    {
        var factSource = entity.AsFactSourceWithContext(context);
        foreach (var rule in _optimizedRules)
        {
            var (matched, _) = rule.Evaluate(factSource);
            if (matched)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the best matching rule for an entity, or null if none match.
    /// </summary>
    public Rule? GetBestMatch<T>(T entity) where T : class
    {
        var factSource = entity.AsFactSource();
        Rule? bestRule = null;
        int bestScore = 0;

        foreach (var rule in _optimizedRules)
        {
            var (matched, score) = rule.Evaluate(factSource);
            if (matched && score > bestScore)
            {
                bestScore = score;
                bestRule = rule;
            }
        }

        return bestRule;
    }

    /// <summary>
    /// Filters entities from a table that match any rule.
    /// </summary>
    public IEnumerable<T> Filter<T>(ImmutableDictionary<string, T> table) where T : class
    {
        foreach (var kvp in table)
        {
            if (Matches(kvp.Value))
            {
                yield return kvp.Value;
            }
        }
    }

    /// <summary>
    /// Partitions entities into matching and non-matching groups.
    /// </summary>
    public (List<T> Matched, List<T> Unmatched) Partition<T>(
        ImmutableDictionary<string, T> table) where T : class
    {
        var matched = new List<T>();
        var unmatched = new List<T>();

        foreach (var kvp in table)
        {
            if (Matches(kvp.Value))
                matched.Add(kvp.Value);
            else
                unmatched.Add(kvp.Value);
        }

        return (matched, unmatched);
    }
}
