# Fuzzy Rule Matcher

A lightweight C# library for rule-based pattern matching with support for fuzzy matching and priority-based rule selection.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/SFPM.svg)](https://www.nuget.org/packages/FuzzyRuleMatcher/)

## Table of Contents

- [Introduction](#Fuzzy-Rule-Matcher)
- [Installation](#Installation)
- [Quickstart](#Quick-Start)
- [Performance Considerations](Performance-Considerations)
- [License](#license)

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

## Installation

Install via NuGet:

```sh
dotnet add package SFPM
```

## Quick Start

```C#
using SFPM;

// Create rules with criteria
var rules = new List<Rule>
{
    new Rule(
        new List<ICriteria>
        {
            new Criteria<string>("weather", weather => weather == "Rainy"),
            new Criteria<int>("stamina", stamina => stamina < 5),
            new Criteria<bool>("isSprinting", isSprinting => isSprinting == true)
        },
        () => Console.WriteLine("Player is tired and running in the rain!")
    )
};

// Create query with facts
var query = new Query()
    .Add("weather", "Rainy")
    .Add("stamina", 3)
    .Add("isSprinting", true);

// Match query against rules
query.Match(rules);
```

## Rule Matching

Rules are evaluated based on their criteria. The rule with the most matching criteria is selected. When multiple rules match with the same number of criteria:

1. Rules are grouped by priority
2. Highest priority rules are selected
3. If multiple rules have the same priority, one is chosen randomly

## Rule List Extensions and Optimization

SFPM provides extension methods for optimizing and managing rule collections:

```csharp
// Create a list of rules with varying complexity
var rules = new List<Rule>
{
    // Complex rule (3 criteria)
    new Rule(
        new List<ICriteria>
        {
            new Criteria<int>("health", health => health < 50),
            new Criteria<bool>("isInCombat", true),
            new Criteria<string>("weather", "Stormy")
        },
        () => Console.WriteLine("Critical situation!")
    ) { Priority = 3 },

    // Medium rule (2 criteria)
    new Rule(
        new List<ICriteria>
        {
            new Criteria<int>("health", health => health < 80),
            new Criteria<bool>("isInCombat", isInCombat => isInCombat == true)
        },
        () => Console.WriteLine("Combat situation")
    ) { Priority = 2 },

    // Simple rule (1 criterion)
    new Rule(
        new List<ICriteria>
        {
            new Criteria<int>("health", health => health < 60)
        },
        () => Console.WriteLine("Low health warning")
    ) { Priority = 1 }
};

// Optimize rules for performance (sorts by criteria count)
rules.OptimizeRules();

// Find rules by specificity
var mostSpecific = rules.MostSpecificRule(); // Returns the rule with 3 criteria
var leastSpecific = rules.LeastSpecificRule(); // Returns the rule with 1 criterion

// Direct matching using extension method
var facts = new Dictionary<string, object>
{
    { "health", 20 },
    { "isInCombat", true },
    { "weather", "Stormy" }
};

rules.Match(facts); // Will output "Critical situation!" due to highest match count and priority
```

## Performance Considerations

- Use `OptimizeRules()` when your rule set is static to improve matching performance
- Rules with more criteria are evaluated first after optimization
- The `Match` extension method automatically handles:
  - Priority-based selection
  - Random selection for equal priority rules
  - Performance optimization by skipping lower criteria count rules

## Performance Optimization

```C#
// Sort rules by criteria count for better performance
rules.OptimizeRules();

// Get most/least specific rules
var mostSpecific = rules.MostSpecificRule();
var leastSpecific = rules.LeastSpecificRule();
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
