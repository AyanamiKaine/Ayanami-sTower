﻿using Flecs.NET.Core;
using SFPM;

namespace AyanamisTower.SFPM.Flecs.Tests;

internal struct NPC { };

internal record struct Player { };

internal record struct Health(int Value);

internal record struct Position(int X, int Y);

// For now components need to implement the IComparable interface so it can be used in a criteria.
// Maybe its good, maybe its bad. Could this be automated?
internal record struct Name(string Value) : IComparable<Name>
{
    public readonly int CompareTo(Name other) => Value.CompareTo(strB: other.Value);
}

internal record struct Map(string Name) : IComparable<Map>
{
    public readonly int CompareTo(Map other) => Name.CompareTo(strB: other.Name);
}

/// <summary>
/// Contains unit tests for the EcsWorldExtensions.
/// </summary>
public class EcsWorldExtensionsUnitTest
{
    /// <summary>
    /// Implementing the basic usage.
    /// </summary>
    [Fact]
    public void BasicIdeaDefiningRulesAsEntities()
    {
        World world = World.Create();
        world.Set(data: new Map(Name: "circus"));

        var ruleExecuted = false;
        var player = world.Entity();
        var rule1 = world.Entity();
        var rule2 = world.Entity();
        var rule3 = world.Entity();
        var rule4 = world.Entity();

        player
            .Set<Name>(data: new(Value: "Nick"))
            .Set<Health>(data: new(Value: 100))
            .Set<Position>(data: new(X: 10, Y: 20));

        /*
        We can implement this because components are stored in arrays, so when we have to entites with one rule component they should be stored in one array.

        We dont need to create a array of rules ourselves, not only that but we get tagged rules for free.

        The cool thing is we can easily modify, add or remove existing rules.

        We could also go further write rules in C# scripts so they can be modified, added or removed at runtime
        */

        rule1.Set<NPC, Rule>(
            data: new Rule(
                criterias:
                [
                    new Criteria<string>(
                        factName: "who",
                        predicate: who => who == "Nick",
                        predicateName: "IsNick"
                    ),
                    new Criteria<string>(
                        factName: "concept",
                        predicate: concept => concept == "onHit",
                        predicateName: "conceptIsHit"
                    ),
                ],
                payload: () => ruleExecuted = false
            )
        );

        rule2.Set<NPC, Rule>(
            data: new Rule(
                criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(
                        factName: "concept",
                        predicate: concept => concept == "onHit"
                    ),
                    new Criteria<int>(
                        factName: "nearAllies",
                        predicate: nearAllies => nearAllies > 1
                    ),
                ],
                payload: () => ruleExecuted = false
            )
        );

        rule3.Set<NPC, Rule>(
            data: new Rule(
                criterias:
                [
                    new Criteria<Name>(factName: "who", predicate: who => who.Value == "Nick"),
                    new Criteria<string>(
                        factName: "concept",
                        predicate: concept => concept == "onHit"
                    ),
                    new Criteria<Map>(
                        factName: "curMap",
                        predicate: curMap => curMap.Name == "circus"
                    ),
                ],
                payload: () =>
                {
                    // In the payload of rules we can interact with the ecs world,
                    // here we simulate a map change.
                    ref Map curMap = ref world.GetMut<Map>();
                    curMap.Name = "AirPort";
                    ruleExecuted = true;
                }
            )
        );

        rule4.Set<NPC, Rule>(
            data: new Rule(
                criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                    new Criteria<string>(
                        factName: "concept",
                        predicate: concept => concept == "onHit"
                    ),
                    new Criteria<string>(
                        factName: "hitBy",
                        predicate: hitBy => hitBy == "zombieClown"
                    ),
                ],
                payload: () => ruleExecuted = false
            )
        );

        var queryData = new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", player.Get<Name>() }, // Here we query the data from an entity and its component
            {
                "curMap",
                world.Get<Map>()
            } // If the component data changes it gets automaticall reflected here
            ,
        };

        world.MatchOnEntities<NPC>(queryData: queryData);

        Assert.True(condition: ruleExecuted);
        Assert.Equal(expected: "AirPort", actual: world.Get<Map>().Name);
    }

    /// <summary>
    /// To get more control over the rules array we can define it as a singleton on the world itself
    /// </summary>
    [Fact]
    public void DefiningARuleGlobalComponent()
    {
        var ruleExecuted = false;

        World world = World.Create();
        world.Set(data: new Map(Name: "circus"));
        world.Set<NPC, List<Rule>>(
            data: new List<Rule>(
                collection:
                [
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                        ],
                        payload: () => ruleExecuted = false
                    ),
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<int>(
                                factName: "nearAllies",
                                predicate: nearAllies => nearAllies > 1
                            ),
                        ],
                        payload: () => ruleExecuted = false
                    ),
                    new Rule(
                        criterias:
                        [
                            // TODO: Its important to benchmark Criteria<CustomType> vs Criteria<PrimitiveType> to understand
                            // the drawbacks if there are any.
                            new Criteria<Name>(
                                factName: "who",
                                predicate: who => who.Value == "Nick"
                            ),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<Map>(
                                factName: "curMap",
                                predicate: curMap => curMap.Name == "circus"
                            ),
                        ],
                        payload: () =>
                        {
                            // In the payload of rules we can interact with the ecs world,
                            // here we simulate a map change.
                            ref Map curMap = ref world.GetMut<Map>();
                            curMap.Name = "AirPort";
                            ruleExecuted = true;
                        }
                    ),
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<string>(
                                factName: "hitBy",
                                predicate: hitBy => hitBy == "zombieClown"
                            ),
                        ],
                        payload: () => ruleExecuted = false
                    ),
                ]
            )
        );

        var player = world.Entity();

        player
            .Set<Name>(data: new(Value: "Nick"))
            .Set<Health>(data: new(Value: 100))
            .Set<Position>(data: new(X: 10, Y: 20));

        /*
        We can implement this because components are stored in arrays, so when we have to entites with one rule component they should be stored in one array.

        We dont need to create a array of rules ourselves, not only that but we get tagged rules for free.

        The cool thing is we can easily modify, add or remove existing rules.

        We could also go further write rules in C# scripts so they can be modified, added or removed at runtime
        */

        var queryData = new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", player.Get<Name>() }, // Here we query the data from an entity and its component
            {
                "curMap",
                world.Get<Map>()
            } // If the component data changes it gets automaticall reflected here
            ,
        };

        world.MatchOnWorld<NPC>(queryData: queryData);

        Assert.True(condition: ruleExecuted);
        Assert.Equal(expected: "AirPort", actual: world.Get<Map>().Name);
    }

    /// <summary>
    /// Tests the optimization of rules stored as a world component.
    /// </summary>
    [Fact]
    public void OptimizingRulesList()
    {
        World world = World.Create();
        world.Set(data: new Map(Name: "circus"));
        world.Set<NPC, List<Rule>>(
            data: new List<Rule>(
                collection:
                [
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                        ],
                        payload: () => { }
                    ),
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<int>(
                                factName: "nearAllies",
                                predicate: nearAllies => nearAllies > 1
                            ),
                        ],
                        payload: () => { }
                    ),
                    new Rule(
                        criterias:
                        [
                            // Using a custom type is maybe not a good idea, should we really depend on the specifc type?
                            // Here Name, I dont think so.
                            new Criteria<Name>(
                                factName: "who",
                                predicate: who => who.Value == "Nick"
                            ),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<Map>(
                                factName: "curMap",
                                predicate: curMap => curMap.Name == "circus"
                            ),
                        ],
                        payload: () => { }
                    ),
                    new Rule(
                        criterias:
                        [
                            new Criteria<string>(factName: "who", predicate: who => who == "Nick"),
                            new Criteria<string>(
                                factName: "concept",
                                predicate: concept => concept == "onHit"
                            ),
                            new Criteria<string>(
                                factName: "hitBy",
                                predicate: hitBy => hitBy == "zombieClown"
                            ),
                        ],
                        payload: () => { }
                    ),
                ]
            )
        );

        world.OptimizeWorldRules<NPC>();

        /*
        Initally the first rule in the rules list has only 2 criteria after we optimize it the rules with the most
        criteria should be at the front. In this case 3.
        */

        Assert.Equal(
            expected: 3,
            actual: world.GetSecond<NPC, List<Rule>>()[index: 0].CriteriaCount
        );
    }
}
