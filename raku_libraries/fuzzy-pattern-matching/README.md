# Fuzzy Pattern Matching

I want to explore the idea of implementing fuzzy pattern matching.

## The basic idea

This is heavily inspired by this talk [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&t)

## The problem we want to solve

We want to create a system that is reactive to the world. For small things it does not really matter what we do, if-else statements do the job. But what about bigger things? The problem lies in the sheer number of different state in a world. Some games handle this by creating branching trees to create choice and explicit stored flags (Paradox interactive uses flags for example).

But some games need more than if-else statements.

## A possible solution

Imagine the world state as a series of facts.

- Player.Name = "Markus"
- Company.Money = 200
- Player.EnemiesNearby = 3

### Criterion

Now imagine a `criterion` that matches one fact.

- Player.Name == "Tom"
- Company.Money > 100
- Player.EnemiesNearby >= 3

A criterion is an expression that evaluates to true or false.

A criterion has as input a key of a fact. And in its constructor we would define a lambda that returns a boolean.

I think a criterion should have the ability to intergerate an object. Asking if it has a certain field and if so if the value of the field mets its condition. BUT it should definitely be possible to extend this so we can easily implement it for SQL.

Also maybe we should add the reason why a criterion didnt match in the object. So users can say why-didnt-match, .Reason or something like that that returns a string showing what was expected and what actual happened.

### Where does criterion get its data.

We must tackle the question where a criterion gets its data from. Imagine a predicate like `$x > 100` where does x come from? What we want is to define where the data could be found via a key. Something like:

```
Criterion.new(field => "health", object-with-field => Player, predicate => -> $x --> Bool { $x > 100})
```

Here we **explicitly** say where the key `"health"` should be used to get a value in this case we supply an object called `Player` with a `health` field.

We could do something more **implicit** where we have a database where all facts/state is stored like a relational database or ECS framework where we can easily query data from it.

# Running Tests

To run all tests run `prove6 --lib t/`
