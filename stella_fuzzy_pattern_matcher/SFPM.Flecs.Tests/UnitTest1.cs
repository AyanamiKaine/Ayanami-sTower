using Flecs.NET.Core;

namespace SFPM.Flecs.Tests;


public struct NPC { };
public struct Player { };
public struct Health(int Value);
public struct Position(int X, int Y);
public struct Name(string Value);

public class UnitTest1
{

    /// <summary>
    /// Implementing the basic usage. 
    /// </summary>
    [Fact]
    public void BasicIdea()
    {
        World world = World.Create();

        var ruleExecuted = false;
        var player = world.Entity();
        var rule1 = world.Entity();
        var rule2 = world.Entity();
        var rule3 = world.Entity();
        var rule4 = world.Entity();

        player
            .Set<Name>(new("Nick"))
            .Set<Health>(new(100))
            .Set<Position>(new(10, 20));

        /*
        We can implement this because components are stored in arrays, so when we have to entites with one rule component they should be stored in one array.

        We dont need to create a array of rules ourselves, not only that but we get tagged rules for free.

        The cool thing is we can easily modify, add or remove existing rules. 

        We could also go further write rules in C# scripts so they can be modified, added or removed at runtime 
        */



        rule1.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], () =>
                {
                    ruleExecuted = true;
                }));

        rule2.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        rule3.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("curMap", curMap => { return curMap == "circus"; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        rule4.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        var acceptedRules = new List<Rule>();
        var currentHighestScore = 0;

        world.Each<NPC, Rule>((Entity entity, ref Rule rule) =>
        {
            if (rule.CriteriaCount < currentHighestScore)
                return;


            /*
            This here remains a big problem, where does the rule get its data from? From one entity? From the world?
            From an ECS query?
            */
            var (matched, matchedCriteriaCount) = rule.StrictEvaluate(new Dictionary<string, object>
            {
                { "concept", "onHit" },
                { "who", "Nick"}
            });
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

        Assert.True(ruleExecuted);
    }
}
