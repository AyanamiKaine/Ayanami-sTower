# dotnet hotreloading and flecs

## Decoupling behavior from data

One major problem with hot reloading is that when we change the method of an object we need to serialize the outdated object and deserialize it to the new version of the type so the method gets updated. Similar things would needed to be done when we want to add fields to it. And some of it is not even supported by dotnet.

Now in Flecs as its an ECS framework behavior is found in systems, simply a query that runs on a set of entities. There is no need to serialize anything as we can simply swap out the function that runs on the query, at least I believe that.

This is also interesting because we can change already defined data in the flecs explorer.