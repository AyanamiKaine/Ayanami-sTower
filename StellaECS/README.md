# Using Sqlite for an ECS system

Here I document my exploration to use Sqlite as an integral part of an Entity-Component-System framework.

## Reasoning

The idea came to me after using the [Flecs](https://github.com/SanderMertens/flecs) ECS Framework it partially implements a relational database in some form, mostly for performance reasons typical sqlite features like rollback and transactions are ommited.

The biggest reason was my curiosity. Can you actually use Sqlite in realtime heavy systems like a video game? Are saw some comments that said it was not only stupid but also impossible.

## Goals

My goal is it to use sqlite as the backbone of the ECS framework that is performant.

- It should be able to update 60 times the second.

## Benchmarks

Doing some tests comparing EF-Core, Flecs ECS, and raw sqlite. For realtime processing sqlite is sadly too slow. for a turn based game it think it works great.

## Side Notes

I really want just Sqlite as a dependency. Also I think I want to only create thin abstraction around Sqlite. What about certain sqlite performance improvements mostly found in another [branch](https://sqlite.org/src/timeline?r=bedrock) like WAL2 and CONCURRENT? For more read [this](https://news.ycombinator.com/item?id=38988949).

Maybe a look into [EF Core](https://learn.microsoft.com/en-us/ef/core/) is also worthy, using LINQ as the query language would make things really easy. As we dont have to create our own like Flecs did it.
