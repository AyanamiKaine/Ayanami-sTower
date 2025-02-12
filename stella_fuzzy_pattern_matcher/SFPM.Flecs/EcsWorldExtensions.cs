using Flecs.NET.Core;

namespace SFPM.Flecs;

/// <summary>
/// Ecs flecs world extension methods.
/// </summary>
public static class EcsWorldExtensions
{
    /*
    Right now I see one crucial problem, while we can improve performance by splitting up rules with tags, we cannot sort the Rules component array 
    from rules with most criteria at the beginning to least criteria at the end.
    */

    /// <summary>
    /// Evaluates rules in the ECS world as entities against provided query data and executes the best matching rule's payload.
    /// </summary>
    /// <typeparam name="Tag">The tag typed used to match only against rules with the pair (Tag, Rule).</typeparam>
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
    public static void MatchOnEntities<Tag>(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        world.Each<Tag, Rule>((Entity _, ref Rule rule) =>
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;

            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of a component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(rule);
                }
            }
        });

        if (acceptedRules.Count == 1)
        {
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(r => r.Priority)
                                                   .OrderByDescending(g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(random.Next(highestPriorityRules.Count()));
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
    ///    - Selects the group with highest priority
    ///    - Randomly picks one rule from that group to execute
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when world or queryData parameters are null.</exception>
    public static void MatchOnEntities(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        world.Each((ref Rule rule) =>
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;

            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of a component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(rule);
                }
            }
        });

        if (acceptedRules.Count == 1)
        {
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(r => r.Priority)
                                                   .OrderByDescending(g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(random.Next(highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
    }

    /// <summary>
    /// Evaluates rules in the ECS world rules component against provided query data and executes the best matching rule's payload.
    /// </summary>
    /// <typeparam name="Tag">The tag typed used to match only against rules with the pair (Tag, Rule).</typeparam>
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
    public static void MatchOnWorld<Tag>(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        foreach (var rule in world.GetSecond<Tag, List<Rule>>())
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;

            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of a component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(rule);
                }
            }
        }
        if (acceptedRules.Count == 1)
        {
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(r => r.Priority)
                                                   .OrderByDescending(g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(random.Next(highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
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
    ///    - Selects the group with highest priority
    ///    - Randomly picks one rule from that group to execute
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when world or queryData parameters are null.</exception>
    public static void MatchOnWorld(this World world, Dictionary<string, object> queryData)
    {
        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        foreach (var rule in world.Get<List<Rule>>())
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;

            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query? Conceptually a key would be the type of a component like Health and the value the data of the component.
            */
            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(queryData);
            if (matched)
            {
                if (matchedCriteriaCount > currentHighestScore)
                {
                    currentHighestScore = matchedCriteriaCount;
                    acceptedRules.Clear();
                }
                if (matchedCriteriaCount == currentHighestScore)
                {
                    acceptedRules.Add(rule);
                }
            }
        }

        if (acceptedRules.Count == 1)
        {
            acceptedRules[0].ExecutePayload();
        }
        else if (acceptedRules.Count > 1)
        {
            // Group highest priority rules
            var highestPriorityRules = acceptedRules.GroupBy(r => r.Priority)
                                                   .OrderByDescending(g => g.Key)
                                                   .First();
            // Randomly select one rule from the highest priority group
            var random = new Random();
            var selectedRule = highestPriorityRules.ElementAt(random.Next(highestPriorityRules.Count()));
            selectedRule.ExecutePayload();
        }
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
        rules.Sort((a, b) => b.CriteriaCount.CompareTo(a.CriteriaCount));
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
    public static void OptimizeWorldRules<Tag>(this World world)
    {
        ref var rules = ref world.GetMutSecond<Tag, List<Rule>>();
        rules.Sort((a, b) => b.CriteriaCount.CompareTo(a.CriteriaCount));
    }
}