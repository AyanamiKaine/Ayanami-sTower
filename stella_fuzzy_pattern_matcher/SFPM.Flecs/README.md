# SFPM.Flecs (Using Flecs.Net to query data from)

Instead of creating our own query system and handling key value stores, we will be using entities and components instead.

## Why?

One problem we have is, the payload of rules should be able to write data back, something like `event_x_happened = true`, so we need to implicity pass the database/key-value-store to the payload. But how would you keep the data from the database/key-value-store in sync from its source?

What do I mean with that?

Imagine we have various npc objects there data is stored in their fields not in a database/key-value-store so we would have to mirror them instead. We could store a refrence but I feel there is something missing. A flat storage like its found in a ECS world would be much better to handle, as we can simply pass an ECS-World implicitly. And changing in it are reflected correctly and are kept in sync with systems that are running. Also we could create a query system that runs over rules that are defined as components for entities.
