# Stella Fuzzy Pattern Matcher (SFPM)

A lightweight C# library for rule-based pattern matching with support for fuzzy matching and priority-based rule selection.

## Features

- 🎯 Rule-based pattern matching with multiple criteria
- 🔍 Support for custom predicates and comparison operators
- ⚡ Performance optimizations for rule evaluation
- 🎲 Handles multiple matching rules with priority and random selection
- 📝 Comprehensive logging with NLog integration
- 📊 Easy to use fluent API

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
            new Criteria<string>("weather", "Rainy"),
            new Criteria<int>("stamina", 5, Operator.LessThanOrEqual),
            new Criteria<bool>("isSprinting", true)
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

...existing code...

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
            new Criteria<int>("health", 50, Operator.LessThan),
            new Criteria<bool>("isInCombat", true),
            new Criteria<string>("weather", "Stormy")
        },
        () => Console.WriteLine("Critical situation!")
    ) { Priority = 3 },

    // Medium rule (2 criteria)
    new Rule(
        new List<ICriteria>
        {
            new Criteria<int>("health", 80, Operator.LessThan),
            new Criteria<bool>("isInCombat", true)
        },
        () => Console.WriteLine("Combat situation")
    ) { Priority = 2 },

    // Simple rule (1 criterion)
    new Rule(
        new List<ICriteria>
        {
            new Criteria<int>("health", 30, Operator.LessThan)
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

### Performance Considerations

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

## Custom Predicates

```C#
var customRule = new Rule(
    new List<ICriteria>
    {
        new Criteria<int>("enemyCount",
            count => count > 5 && count < 10,
            "EnemyCountBetween5And10"),
        new Criteria<double>("playerHealth",
            health => health < 50.0,
            "HealthBelow50Percent")
    },
    () => Console.WriteLine("Player is in danger!")
);
```
