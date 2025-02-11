# Stella Fuzzy Pattern Matcher

## Implemented Features

## Missing Features

### The ability for rules to add new facts.

This is useful to add "memory". Imagine we want to say EventA happened, this is a scenario for a boolean flag. But the added data can be more complex like. A custom data structure or counter. Imagine we want to store how often a specific object or enemy was encountered. This could be used to execute a specific dialog that mentions that over 100 enemies where killed.

## Performance Goals

I want that one query wont take longer than some microseconds(μs) over a list of 10000 rules.
