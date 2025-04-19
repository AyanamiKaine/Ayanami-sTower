# TODO (Things that must be done)

## Enable a way to edit items (DONE)

### Problem

Currently items cannot be edited onces created, being able to change wrong information is crucial. Also we need a really nice user interface to change the priority of items, like being able to drage items above or below an existing one showing the relationship of priorities.

### Solution

Should be straight forward, creat edit windows. And a way to easily change priority using drag and drop logic in a list.

## Tag System (DONE)

### Problem

Currently spaced repetition items have the ability to holds tags. But there is no way to query items after defined tag.

### Solution

Either show tags in the spaced repetition list, simply make it possible to search by them in the search bar or something else I dont know yet.

### Implemented Solution

Tags are implemented for spaced repetition items, and can be used to search for items with specific tags

## Implemented a application wide tag system

### Problem

Currently the tag system was specificly created for spaced repetition items. We want to expand that idea to an application wide tag system that can be reused for everything that needs tags.

### Solution

We can attach an tags list component on the world.

## GIGANTIC MEMORY PROBLEM WHEN SCROLLING IN THE LIST

## Implement the ability for spaced repetition items to refrence literature

### Problem

It would be nice to set refrerences to literature items so when have problem in our review we can open the literature and rereading it.

## Flecs Experimentation

### Problem

Inflexibility creaps in, when modeling ideas like spaced repetition items, literature, or art related objects. Its not as clear to define class boundries. Now we still have flecs and can use its components to model fields instead. We would be losing compile time type safety in some places. But we would increase our flexibility. And besides that currently we still have runtime type checks because of serialization and deserializations we are doing.

This could also be a great oppurtonity to find out how good flecs built in serialization works.

The result will be probably much more runtime checks, but also because we will be using much more simpler components we can just say `.Ensure<Type>()`. The bigger question is how will things like the observable property work?

## Implement Scaling Options for the User

The user should be able to change the global scaling of the app, similar how a user can use the keyboard shortcut (ctrl,+) to increase it in the browser.

```C#
// I dont know yet how it works, but those fields can change the scaling but break some other part.
_mainWindow.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
_mainWindow.RenderTransform = new ScaleTransform(1.5, 1.5);
```

## Implement a linking mechanism

Currently we cannot link different content together. It would be quite nice if we could link spaced repetition items to lets says literature items.
