# SFPM.Flecs (Using Flecs.Net to query data from)

Instead of creating our own query system and handling key value stores, we will be using entities and components instead.

## Why?

One problem we have is, the payload of rules should be able to write data back, something like `event_x_happened = true`, so we need to implicity pass the database/key-value-store to the payload. But how would you keep the data from the database/key-value-store in sync from its source?

What do I mean with that?

Imagine we have various npc objects there data is stored in their fields not in a database/key-value-store so we would have to mirror them instead. We could store a refrence but I feel there is something missing. A flat storage like its found in a ECS world would be much better to handle, as we can simply pass an ECS-World implicitly. And changing in it are reflected correctly and are kept in sync with systems that are running. Also we could create a query system that runs over rules that are defined as components for entities.

## ECS World Extensions

Extension methods for integrating SFPM (Stella Fuzzy Pattern Matcher) rules with Flecs.NET Entity Component System.

### Basic Usage

There are two main ways to define rules in your ECS world:

1. Rules as Entities

```C#
// Create world and entities
World world = World.Create();
var player = world.Entity()
    .Set<Name>(new(Value: "Nick"))
    .Set<Health>(new(Value: 100));

// Create a rule entity with NPC tag
var rule = world.Entity()
    .Set<NPC, Rule>(new Rule(criterias: [
        new Criteria<Name>("who", who => who.Value == "Nick"),
        new Criteria<string>("concept", concept => concept == "onHit"),
        new Criteria<Map>("curMap", map => map.Name == "circus")
    ], payload: () => {
        // Rule actions here
    }));

// Match rules against query data
var queryData = new Dictionary<string, object>
{
    { "concept", "onHit" },
    { "who", player.Get<Name>() },  // Get component data from entity
    { "curMap", world.Get<Map>() }  // Get singleton component
};

world.MatchOnEntities<NPC>(queryData);
```

2. Rules as World Component

```C#
// Set rules as a world-level component
world.Set<NPC, List<Rule>>(new List<Rule>
{
    new Rule(criterias: [
        new Criteria<string>("who", who => who == "Nick"),
        new Criteria<string>("concept", concept => concept == "onHit"),
        new Criteria<int>("nearAllies", allies => allies > 1)
    ], payload: () => {
        // Rule actions here
    })
});

// Match world-level rules
world.MatchOnWorld<NPC>(queryData);
```

## Performance Optimization

Rules can be optimized to evaluate more specific rules first:

```C#
// Optimize entity-based rules
world.OptimizeWorldRules();

// Optimize tagged rules
world.OptimizeWorldRules<NPC>();
```

## Rule Evaluation Process

The matching process:

1. Rules are evaluated against provided query data
2. Rules with most matched criteria get priority
3. Among equally matched rules:
   - Grouped by priority
   - Random selection from highest priority group
4. Selected rule's payload is executed

## Tips

- Use `MatchOnEntities<T>` for rules stored as components on entities
- Use `MatchOnWorld<T>` for rules stored as world-level components
- Rules can access and modify ECS components in their payloads
- Component data in query can come from entities or world singletons

This extension integrates SFPM's rule system with Flecs.NET's ECS architecture for dynamic, data-driven behavior systems.
