# Stella Fuzzy Pattern Matcher

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Table of Contents

- [Introduction](#stella-fuzzy-pattern-matcher)
- [Problem Statement](#the-main-problem)
- [Implemented Features](#implemented-features)
- [Missing Features](#missing-features)
- [Performance Goals](#performance-goals)

### Introduction

Entirely based on [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&t)

- Another good read is [You Merely Adopted Rules.csv, I Was Born Into It](https://fractalsoftworks.com/2023/11/13/you-merely-adopted-rules-csv-i-was-born-into-it/)

The main problem we want to solve is reactivity with changing dynamic contexts. We could rephrase this as branching trees. Imagine a dialog tree that includes many different flags and acknowledges many different variables like how many birds you saw. Conceptually this is nothing more than various deeply nested if else conditions and statements. When those are simple they are simple, when they are complex we have a problem.

```csharp
// Traditional approach with deeply nested conditions
if (player.Level >= 10 &&
    player.HasItem("MagicSword") &&
    !questLog.IsCompleted("DragonSlayer") &&
    world.TimeOfDay == "Night" &&
    player.Location == "MysticalForest" &&
    player.Health > 50 &&
    player.MagicPoints >= 30 &&
    !player.HasStatusEffect("Cursed") &&
    player.Reputation > 100)
{
    // Trigger special encounter with ancient dragon
    SpawnAncientDragon();
}
```

With our fuzzy pattern matcher, we can express this more elegantly as a rule:

```csharp
var dragonEncounterRule = new Rule([
        new Criteria<int>(
            factName: "PlayerLevel",
            predicate: level => level >= 10),
        new Criteria<string>(
            factName: "HasItem",
            predicate: item => item == "MagicSword"),
        new Criteria<string>(
            factName: "QuestStatus",
            predicate: status => status != "DragonSlayerComplete"),
        new Criteria<string>(
            factName: "TimeOfDay",
            predicate: time => time == "Night"),
        new Criteria<string>(
            factName: "Location",
            predicate: loc => loc == "MysticalForest"),
        new Criteria<int>(
            factName: "Health",
            predicate: health => health > 50),
        new Criteria<int>(
            factName: "MagicPoints",
            predicate: mp => mp >= 30),
        new Criteria<string>(
            factName: "Status",
            predicate: status => status != "Cursed"),
        new Criteria<int>(
            factName: "Reputation",
            predicate: rep => rep > 100)
    ], () => SpawnAncientDragon());
```

The tree can be easily extended adding new scenarios or events.

```C#
var bigDragonEncounterRule = new Rule([
        // Here we increase the player level
        new Criteria<int>(
            factName: "PlayerLevel",
            predicate: level => level >= 15),
        new Criteria<string>(
            factName: "HasItem",
            predicate: item => item == "MagicSword"),
        new Criteria<string>(
            factName: "QuestStatus",
            predicate: status => status != "DragonSlayerComplete"),
        new Criteria<string>(
            factName: "TimeOfDay",
            predicate: time => time == "Night"),
        new Criteria<string>(
            factName: "Location",
            predicate: loc => loc == "MysticalForest"),
        new Criteria<int>(
            factName: "Health",
            predicate: health => health > 50),
        new Criteria<int>(
            factName: "MagicPoints",
            predicate: mp => mp >= 30),
        new Criteria<string>(
            factName: "Status",
            predicate: status => status != "Cursed"),
        new Criteria<int>(
            factName: "Reputation",
            predicate: rep => rep > 100)
    ],
    // and trigger the spawn of a bigger acient dragon
    () => SpawnBigAncientDragon());
```

The main idea is to decouple where each branch of a tree is defined.

- A branch can be created arbitrary high or deep.
- It can have some conditions or many
- They can be easily edited, removed, added at runtime.

In essence we match a list of facts to a list of conditions if all conditions met we execute its body.

Here we defined a condition as a `Criteria`, a list of `Criteria` as one `Rule` with a `payload`.

### Partial Criteria Matching

Problem: You want create a rule/criteria for when player has name "Tom" or "Tim"
Solution: Use a predicate with OR.

```C#
new Rule([
        new Criteria<string>(
            factName: "PlayerName",
            predicate: name => name == "Tom" || name == "Tim"),
    ]
```

Now this rule is matched when the player has the name "Tom" or "Tim"

### Implemented Features

- Extensive logging in debug builds.
- Various extension methods for conveniance use.

### Missing Features

#### Define where a query queries its data from.

For now we must do that ourselves, it would be quite nicer to say something like:

```C#
var query.Source = rulesToSelectFrom;

query.Add("Who", "Nick");
query.Add("Concept", "OnHit")
```

Here the query tries to select the rule that matches the most from its source.

Maybe I cant provide a good general abstraction for a query without knowing more of the architecture where it gets used.

#### Using Flecs.Net

Using the ECS framework Flecs we use the world and its entities to gather the data from components used to match rules.

```C#
// Define components that will be used to store data in the ECS world
public record struct Name(string Value) : IComparable<Name>
{
    public readonly int CompareTo(Name other) => Value.CompareTo(other.Value);
}
public record struct Map(string Name) : IComparable<Map>
{
    public readonly int CompareTo(Map other) => Name.CompareTo(other.Name);
}

// Create a new ECS world
world = World.Create();

// Set up the world with initial data: a map and a list of rules
world
    .Set(new Map("circus"))
    .Set(new List<Rule>([
            new Rule([
                // While its totally possible to use a custom type for the criteria to use, In dont think its needed, the added dependency on the type Name has no real value in comparision to just using string. Also if you use a custom type it must implement the IComparable interface
                new Criteria<Name>("who", who => { return who.Value == "Nick"; }),
                new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                new Criteria<Map>("curMap", curMap => { return curMap.Name == "circus"; }),
            ], () => { })
    ]));

// Create a player entity with components
player = world.Entity()
    .Set<Name>(new("Nick"))
    .Set<Health>(new(100))
    .Set<Position>(new(10, 20));

// Set up query data using components from both the world and player entity
queryData = new Dictionary<string, object>
    {
        { "concept",    "onHit" },
        { "who",        player.Get<Name>()}, // Query data from player entity's Name component
        { "curMap",     world.Get<Map>()}    // Query data from world's Map component
    };

// Try to match rules using the query data
world.MatchOnWorld(queryData);
```

#### The ability for rules to add new facts.

This is useful to add "memory". Imagine we want to say EventA happened, this is a scenario for a boolean flag. But the added data can be more complex like. A custom data structure or counter. Imagine we want to store how often a specific object or enemy was encountered. This could be used to execute a specific dialog that mentions that over 100 enemies where killed.

#### Flecs.Net Intergration

Queriering data can be quite combersome. I think using an entity component system like Flecs that gets simply used as a flat data storage could work quite nicely.

### Performance Goals

I want that one query wont take longer than some microseconds(μs) over a list of 10000 rules.
