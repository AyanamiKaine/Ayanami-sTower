# Stella Fuzzy Pattern Matcher

Entirely based on [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&t)

The main problem we want to solve is reactivity with changing dynamic contexts. We could rephrase this as branching trees. Imagine a dialog tree that includes many different flags and acknowledges many different variables like how many birds you saw. Conceptually this is nothing more than various deeply nested if else conditions and statements. When those are simple they are simple, when they are complex we have a problem.

The main idea is to decouple where each branch of a tree is defined.
- A branch can be created arbitrary high or deep. 
- It can have some conditions or many
- They can be easily edited, removed, added at runtime.

In essence we match a list of facts to a list of conditions if all conditions met we execute its body.

Here we defined a condition as a `Criteria`, a list of `Criteria` as one `Rule` with a `payload`.

## Implemented Features

## Missing Features

### Define where a query queries its data from.

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
Entity player = world.Entity()
    .Set(new Health(20))
    .Set(new Name("Nick"));

query.Add("Who", player.Get<Name>());
query.Add("Concept", "OnHit")
```

### The ability for rules to add new facts.

This is useful to add "memory". Imagine we want to say EventA happened, this is a scenario for a boolean flag. But the added data can be more complex like. A custom data structure or counter. Imagine we want to store how often a specific object or enemy was encountered. This could be used to execute a specific dialog that mentions that over 100 enemies where killed.

### Extra Debugging Features

It would be quite nice if we log information about how queries are matched and what rules are rejected and why.

### Flecs.Net Intergration

Queriering data can be quite combersome. I think using an entity component system like Flecs that gets simply used as a flat data storage could work quite nicely.

## Performance Goals

I want that one query wont take longer than some microseconds(μs) over a list of 10000 rules.

### Last Result

```
| Method                 | Mean          | Error        | StdDev        | Rank  | Gen0      | Gen1   | Allocated |
| OneQueryOver10000Rules | 521,152 ns    | 9,924.667 ns | 11,031.241 ns | 10    | 6.8359    | 0.9766 | 132096 B  |
```

Or rounded in 521 microseconds(µs).
