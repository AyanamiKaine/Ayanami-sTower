# Refactoring (Things I want to change and why)

The refactors here are part of strategic programming. From time to time, attend to this refactors implementing them will improve the design and the software in general.

## [] Cleaning up unused created UI-Components

### Problem

When we create an UI-Component we create a set of entities with the appropriate avalonia control classes. Now when we swap out a attached component for another. What happens with the first ui-component entities? They still exist, the never get destroyed. This is a simple memory leak we should fix.

### Solution

When a UI-Component root entity has no parent anymore, destroy it and all of its children. Maybe we can implement a simply timer that periodically checks if they dont have any root elements anymore and destroy them. Or implement a callback for when an entites parent gets removed (Probably the better choice, its simpler.)

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

    public SpacedRepetitionPage(World world, entityToAttachTo)
    {
        root = world.Entity()
            .Add<Page>()
            .ChildOf(entityToAttachTo);
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

### Status

- Turn pages into UI-Components (DONE)
- Turn windows into UI-Components (PROCESS)

## [] Remove named entities as a global refrence from the app class

### Problem

Because of the ongoing refactoring of the ui-components into classes, we temporarily substituted the used named entities in said components with a global refrence that was before passed as an argument instead.

Exposing such a global is not a good idea. It exposes way too much of the entire structure of the app. Should other components start to depend on the existins of specific entities created by other components tight coupling will occur and overtime will bring down the code flexability dramatically.

### Solution

Slowely but shurly remove all refrences to the named entities field in the app class and at last delete it completly.
