# Information Hiding and ECS

An ECS system encodes fields of objects into components that can be add, set, or removed.
This makes it possible to write behavior not based on the type, but instead of its data structure.
The type is a result of a specific combination of components.

To make a type more specific we simply add more components to it. We can even just add tag components
that are simply used as identifiers.

Here we want to explore the actual effective use of an ECS system for information hiding.
