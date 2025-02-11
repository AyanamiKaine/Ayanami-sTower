# Stella Fuzzy Pattern Matcher

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

### The ability for rules to add new facts.

This is useful to add "memory". Imagine we want to say EventA happened, this is a scenario for a boolean flag. But the added data can be more complex like. A custom data structure or counter. Imagine we want to store how often a specific object or enemy was encountered. This could be used to execute a specific dialog that mentions that over 100 enemies where killed.

### Extra Debugging Features

It would be quite nice if we log information about how queries are matched and what rules are rejected and why.

## Performance Goals

I want that one query wont take longer than some microseconds(μs) over a list of 10000 rules.
