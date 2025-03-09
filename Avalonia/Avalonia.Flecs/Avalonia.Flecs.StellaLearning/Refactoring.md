# Refactoring (Things I want to change and why)

## [] Implementing UI Components as Classes

### Problem

Right now all UI components are like the SpaceRepetitionPage are implemented as one create function that returns the highest entity in the UI-TREE. The way I did it creates way to much exposure of the entites. You can accidentally create an AddItem button that is already created elsewhere. Entities that are part of a ui tree should be encapsulated in it. So its simply not possible to get the entity accidentally and mutating it.

Basically all UI-Components are globals and leak everywhere, this is terrible.

While at first I thought doing it like react and many JS frameworks that simply return a UI-Component via a function is a good choice. Doing this with the ECS system exposes way to many globals and does way too little information hiding. An alternative approach is needed.

### Solution

To solve this we should implement them as classes that can be instantiated. And each instance is ensured to be created out of unique entities. By default we shouldnt expose all entities in this class, but instead only the most higher level one. Or maybe we should not even expose anything related to avalonia and flecs. Keep information hiding at a maximum while deepening the ui component.

Right now all UI-Components have a create function that returns the highest level entity in the ui-tree. Most of that logic should be moved into a constructor and we should create an interface that then can be used instead.

```C#
public static class SpacedRepetitionPage
{
    public static Entity Create(NamedEntities entities)
    {
        //...
    }
}
```

An improved interface could look like this:

```C#
public class SpacedRepetitionPage : IUIComponent
{
    private readonly Entity root;

    public SpacedRepetitionPage(World world)
    {
        root = world.Entity()
            .Add<Page>();
        //Create a UI-Component out of entities.
        //...
    }

    // This can even be defined as a default implementation
    // in the interface as the logic is always the same
    // Maybe the naming can be improved.
    public void AttachToEntity(Entity entityToAttachTo)
    {
        root.ChildOf(entityToAttachTo);
    }
}
```
