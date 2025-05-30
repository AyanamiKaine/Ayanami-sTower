I was always facinated by a database system that holds the state of a program. For example instead of having a ECS system written in C# we would use sqlite3 to store the component data. And because a system is just a query it could work. The main problem could be performance. Here we want to see how far we could take a sqlite3 ecs implementation.

One major aspect is performance. Using sqlite3 for realtime data does not have to be the fastest way, it just needs to be fast enough not to be annoying. See the benchmark project for a overview.
