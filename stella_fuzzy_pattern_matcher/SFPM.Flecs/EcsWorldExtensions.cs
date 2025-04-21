using Flecs.NET.Core;
using SFPM;

namespace AyanamisTower.SFPM.Flecs;

/// <summary>
/// Ecs flecs world extension methods.
/// </summary>
public static class EcsWorldExtensions
{
    /*
    Right now I see one crucial problem, while we can improve performance by splitting up rules with tags, we cannot sort the Rules component array 
    from rules with most criteria at the beginning to the least criteria at the end.
    */

    /// <summary>
    /// Evaluates rules in the ECS world as entities against provided query data and executes the best matching rule's payload.
    /// </summary>
    /// <typeparam name="TTag">The tag typed used to match only against rules with the pair (Tag, Rule).</typeparam>
    /// <param name="world">The ECS world containing the rules to evaluate.</param>
    /// <param name="queryData">Dictionary containing key-value pairs to evaluate rules against.</param>
    /// <remarks>
    /// The matching process works as follows:
    /// 1. Evaluates each rule against the query data
    /// 2. Tracks rules with the highest number of matched criteria
    /// 3. If multiple rules match with the same criteria count:
    ///    - Groups rules by priority
    ///    - Selects a random rule from the highest priority group
    /// 4. Executes the payload of the selected rule
    /// </remarks>
    public static void MatchOnEntities<TTag>(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        world.Each<TTag, Rule>(callback: (Entity _, ref Rule rule) =>
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;
            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.Evaluate(facts: queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(item: rule);
                }
            }
        });

        if (acceptedRules.Count == 1)
        {
            acceptedRules[index: 0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group the highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(keySelector: r => r.Priority)
                                                   .OrderByDescending(keySelector: g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(index: random.Next(maxValue: highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
    }
    /// <summary>
    /// Matches rules stored in the ECS world as entities against provided query data and executes the payload of the best matching rule.
    /// </summary>
    /// <param name="world">The ECS world containing the rules to evaluate.</param>
    /// <param name="queryData">Dictionary containing key-value pairs to match against rule criteria. Keys typically represent component types.</param>
    /// <remarks>
    /// The matching process follows these steps:
    /// 1. Evaluates all rules against the query data
    /// 2. Keeps track of rules with the highest number of matched criteria
    /// 3. When multiple rules match with the same criteria count:
    ///    - Groups rules by priority
    ///    - Selects the group with the highest priority
    ///    - Randomly picks one rule from that group to execute
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when world or queryData parameters are null.</exception>
    public static void MatchOnEntities(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        world.Each(callback: (ref Rule rule) =>
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;

            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.Evaluate(facts: queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(item: rule);
                }
            }
        });

        if (acceptedRules.Count == 1)
        {
            acceptedRules[index: 0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group the highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(keySelector: r => r.Priority)
                                                   .OrderByDescending(keySelector: g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(index: random.Next(maxValue: highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
    }

    /// <summary>
    /// Evaluates rules in the ECS world rules component against provided query data and executes the best matching rule's payload.
    /// </summary>
    /// <typeparam name="TTag">The tag typed used to match only against rules with the pair (Tag, Rule).</typeparam>
    /// <param name="world">The ECS world containing the rules to evaluate.</param>
    /// <param name="queryData">Dictionary containing key-value pairs to evaluate rules against.</param>
    /// <remarks>
    /// The matching process works as follows:
    /// 1. Evaluates each rule against the query data
    /// 2. Tracks rules with the highest number of matched criteria
    /// 3. If multiple rules match with the same criteria count:
    ///    - Groups rules by priority
    ///    - Selects a random rule from the highest priority group
    /// 4. Executes the payload of the selected rule
    /// </remarks>
    public static void MatchOnWorld<TTag>(this World world, Dictionary<string, object> queryData)
    {
        world.GetSecond<TTag, List<Rule>>().Match(queryData: queryData);
    }
    /// <summary>
    /// Matches rules stored in the ECS world rules component against provided query data and executes the payload of the best matching rule.
    /// </summary>
    /// <param name="world">The ECS world containing the rules to evaluate.</param>
    /// <param name="queryData">Dictionary containing key-value pairs to match against rule criteria. Keys typically represent component types.</param>
    /// <remarks>
    /// The matching process follows these steps:
    /// 1. Evaluates all rules against the query data
    /// 2. Keeps track of rules with the highest number of matched criteria
    /// 3. When multiple rules match with the same criteria count:
    ///    - Groups rules by priority
    ///    - Selects the group with the highest priority
    ///    - Randomly picks one rule from that group to execute
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when world or queryData parameters are null.</exception>
    public static void MatchOnWorld(this World world, Dictionary<string, object> queryData)
    {
        world.Get<List<Rule>>().Match(queryData: queryData);
    }

    /// <summary>
    /// Optimizes the evaluation order of rules in the world by sorting them based on their criteria count.
    /// Rules with more criteria are evaluated first to potentially reduce the number of evaluations needed.
    /// </summary>
    /// <param name="world">The Flecs world containing the rules to optimize.</param>
    /// <remarks>
    /// The optimization is performed by sorting rules in descending order based on their criteria count.
    /// This can improve performance by evaluating more specific rules (those with more criteria) before
    /// more general ones.
    /// </remarks>
    public static void OptimizeWorldRules(this World world)
    {
        ref var rules = ref world.GetMut<List<Rule>>();
        rules.Sort(comparison: (a, b) => b.CriteriaCount.CompareTo(value: a.CriteriaCount));
    }

    // TODO: We should consolidate the tag rules list optimizer and the normal rules list optimizer
    // into one, so the optimizer uses a wildcard as a tag. We simply query every rules list and
    // optimize it regardless of tag.
    /// <summary>
    /// Optimizes the evaluation order of rules in the world by sorting them based on their criteria count.
    /// Rules with more criteria are evaluated first to potentially reduce the number of evaluations needed.
    /// </summary>
    /// <param name="world">The Flecs world containing the rules to optimize.</param>
    /// <remarks>
    /// The optimization is performed by sorting rules in descending order based on their criteria count.
    /// This can improve performance by evaluating more specific rules (those with more criteria) before
    /// more general ones.
    /// </remarks>
    public static void OptimizeWorldRules<TTag>(this World world)
    {
        ref var rules = ref world.GetMutSecond<TTag, List<Rule>>();
        rules.Sort(comparison: (a, b) => b.CriteriaCount.CompareTo(value: a.CriteriaCount));
    }
}