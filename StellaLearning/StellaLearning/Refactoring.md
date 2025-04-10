# Refactoring (Things I want to change and why)

The refactors here are part of strategic programming. From time to time, attend to this refactors implementing them will improve the design and the software in general.

## [PROGRESS] Implementing a UI Builder

### Problem

Its quite unergonomic and unobvious how a UI will look like, because the diconnect between how the UI is written in code to the tree structure it will assume in the background.

```C#
_root = world.Entity()
    .Add<Page>()
    .Set(new Grid())


var listSearch = world.Entity()
    .ChildOf(_root)
    .Set(new TextBox())
    .SetWatermark("Search Entries");

var totalItems = world.Entity()
    .ChildOf(_root)
    .Set(new TextBlock())
    .SetText("Total Items: 0")
```

Its written top down, and in reality its a nested structure, this should be reflected in the way we declare the UI.

### Solution

Creating a UI Builder.

```C#
var _root = _world.UI<Grid>(grid =>
{
    grid.AddComponent<Page>();

    grid.Child<TextBox>(textBox =>
    {
        textBox.Child<TextBlock>(textBlock =>
        {
            textBlock.SetText("Total Items: 0")
        });
    });
}
```

This makes the structure of the UI much more obvious.

- It will reduce errors in complex UI structures.
- It will enable type safe methods.

## [DONE] Cleaning up unused created UI-Components (FIX THE MEMORY LEAK)

### Problem (FIXED)

When we create an UI-Component we create a set of entities with the appropriate avalonia control classes. Now when we swap out a attached component for another. What happens with the first ui-component entities? They still exist, the never get destroyed. This is a simple memory leak we should fix.

### Another Problem (FIXED)

While the entity problem is mostly fixed, the avalonia problem isnt.

What is the problem? When we create for example a stack panel and add a child to it. They will never garbage collect.

We have fixed this problem. Using the flecs observer when a parent child relationship is removed removes the parent child relation in avalonia classes, setting the needed properties null.

### Another Problem : Not removing event handlers

Event handlers are hard refrences, so when a button has a click event. The added handler makes it impossible for the button to be collected because of the refrence they now share. We want weak refrences to handlers so even if an object has many handlers attached if there is no other refrence to it. It still can be collected.

The rule of thumb should be if avalonia object is not part of an component anymore its invalid and should be able to be garbage collected.

### Solution

When a UI-Component root entity has no parent anymore, destroy it and all of its children. Maybe we can implement a simply timer that periodically checks if they dont have any root elements anymore and destroy them. Or implement a callback for when an entites parent gets removed (Probably the better choice, its simpler.)

When a componen is removed it should remove all the event handlers it possible has. So for example when we remove the button component we want to remove all OnClick handlers that are currently attached to it.

#### Example Solution:

We implement the IDisposable interface and call dispose when the window closes. To dispose all related handlers.

### What I did

I implement event handlers disposables in the UI builder now when events are added to a button like button.OnClick those event delegates are removed as soon as the entity gets disposed internally. The same goes for entities and their avalonia objects, we remove the children of them as well as their parents so they can become dead objects.

## [X] Implementing UI Components as Classes

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

This should also result in better testability of components. As their global dependencies are eliminated and their information hiding increased.

### Status

- Turn pages into UI-Components (DONE)
- Turn windows into UI-Components (PROCESS)

## [IN-PROGRESS] Making it AOT-Compatible

### Problem

The current use of many reflection features in the way we access specific avalonia properties in avalonia objects is quite unnessary. There are AOT compatible ways of doing the same.

### Solution

Simply rewrite all reflection usages to functions that do not need them. Add `<IsAotCompatible>true</IsAotCompatible>` to the project file to show what things are not compatible. And slowly removing them.

## [] Implement Hot-Reloading

See the hot reloading example for a working prototype.

### Problem

Currently hotreloading does not update the UI because we need to update the method that defines the UI body and also have a way to reinvoke the updated method.

### Solution

We could define a IReload interface that gets called when an reload event gets send to an entity. And the entity calls the IReload interface on all components and childrens components that implement it. The abstraction needs to be so simply as a caller it should be a no brainer how to use it and when to use it correctly.

## [DONE] Remove named entities as a global refrence from the app class

### Problem

Because of the ongoing refactoring of the ui-components into classes, we temporarily substituted the used named entities in said components with a global refrence that was before passed as an argument instead.

Exposing such a global is not a good idea. It exposes way too much of the entire structure of the app. Should other components start to depend on the existins of specific entities created by other components tight coupling will occur and overtime will bring down the code flexability dramatically.

### Solution

Slowely but shurly remove all refrences to the named entities field in the app class and at last delete it completly.

## [] Make UI-Components better testable

### Problem

Right now now UI-Component can't be really tested. In combination with the (Implementing UI Components as Classes) refactor we should gradually improve the testability of UI-Components.

### Solution

1. [] Finish the UI-Components as classes refactor
2. [] Create a testing project
3. [] Create UI-Component Tests

## Growing Number of LOC

### Problem

Currently we have arround 20.000 lines of code. While there is no upper limit of LOC, every new line comes with new overhead, dependencies and must be maintained to combat code rot. Some components and pages reach over 1000 LOC and a good rule of thumb is components should have an upper limit of 1000 LOC. But there is something to keep in mind, you may split a 2000 LOC class into two 1000 LOC but this does not mean we created 2 independend classes if we are sloopy we simply create two classes that are in reality so coupled together, that they are effectively one.

## Disposing of Tooltips

### Problem

We dispose tooltips when they become detached from the visual tree, this is not a good default behavior because most of the time we dont want them disposed automatic. We want to dispose them automatic when they are created as part of a template, because otherwise the create tooltips over and over again, resulting in increased memory usage.

Right now we have to pass a flag to correctly know when to dispose them automatic, I think it would be better if we dont need to think about that.

### Solution

because we are using entities we could use tags to specify if and when it should be disposed.
