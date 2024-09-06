## The Main Idea

We want to explore the idea of implementing the entire state of a system in the form of an associated array (Key-Value) and being able to query it with rules, associating functions with those rules and executing those who matches the most rules and having the highest priority.

We could reword state to facts.

```
match [HEALTH: 20>VALUE, IS_MARRIED=TRUE, PRIORITY=10, CULTURE=ELVISH] in GAME_STATE
```

For now we dont care for efficency as we try to find a working implementation. 


## Inspiration
- https://www.youtube.com/watch?v=tAbBID3N64A