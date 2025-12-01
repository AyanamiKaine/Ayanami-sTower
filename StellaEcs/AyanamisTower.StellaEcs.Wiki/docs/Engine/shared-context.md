---
sidebar_position: 3
---

# Shared Context

The shared context of the engine is created by the Entity-Component-System used. Every aspect of the game world is created using entities, components and systems that update the data of components. This is transparent. The idea is that even buttons, menus, are just entities with UI data attached. If you want to add a new button to a menu. You should be able to query for the menu and add a button as a child. If this is not possible its most likely a bug.

# Transparency

The key aspect is that every element in the game is transparent, can be modified or looked at. Information about the game world is not hidden. This is in stark contrast to games writing a limited modding API. Here every feature and asset used in the game itself is exposed to the mod developer.

This is also shown by the REST-API of the ECS system, but more on that later.