![Fluid Hierarchical Task Network](https://i.imgur.com/xKfIV0f.png)

# Fluid Smart Objects Example

This project demonstrates how to use **Smart Objects** and **Domain Slots** with the Fluid Hierarchical Task Network (HTN), its a fork of https://github.com/ptrefall/fluid-smart-objects, it updates it to dotnet9 and adds some refactors.

## Purpose

The primary goal of this example is to showcase a flexible mechanism where an AI agent can acquire new tasks or modify its existing task priorities by interacting with specific objects in its environment. This is achieved by:

1.  Defining **Smart Objects** that encapsulate their own HTN domains (sets of behaviors).
2.  Using **Domain Slots** in the agent's primary HTN domain as placeholders where these Smart Object domains can be dynamically "plugged in" or "unplugged."

## Core Concepts Explained

### 1. Hierarchical Task Network (HTN)

Fluid HTN is a planner that allows you to define complex AI behaviors by breaking them down into a hierarchy of tasks.

- **Domain:** A collection of all tasks (compound and primitive), conditions, and effects an agent knows.
- **Compound Tasks:** High-level tasks that need to be decomposed into simpler sub-tasks (e.g., "Explore Area"). Selectors are a type of compound task that try their children in sequence.
- **Primitive Tasks (Actions):** The most basic actions an agent can perform, which have direct effects on the world state (e.g., "Move to X", "Pick up Item").
- **Conditions:** Logical checks that determine if a task is valid or can be selected.
- **Effects:** Changes to the world state that occur after a primitive task is executed.
- **Planner:** The engine that searches through the domain to find a sequence of primitive tasks to achieve goals or respond to the current world state.

### 2. Smart Objects

In this context, a "Smart Object" is an entity within the simulation that possesses its own HTN domain. This domain defines specific behaviors or interactions related to that object.

- In the example, `ImportantThing` is a `SmartObject`.
- Its domain defines a task: "Walk away" (which includes moving to location 2 and then causing the player to unsubscribe).
- This allows behaviors to be modular and tied to objects rather than being monolithically defined solely within the agent.

### 3. Domain Slots

Domain Slots are predefined points within an agent's main HTN domain where other HTN domains (typically from Smart Objects) can be dynamically inserted or removed.

- The `Player` agent has a `Slot(AIDomainSlots.HighPriority)` defined in its domain.
- When the `Player` "subscribes" to a `SmartObject`, that object's domain is loaded into the specified slot.
- The planner then considers tasks from this slotted domain, often with a priority determined by the slot's position in the agent's main domain structure.
- "Unsubscribing" removes the domain from the slot, reverting the agent's potential behaviors.
- This mechanism enables dynamic extension and modification of an agent's capabilities at runtime.

## How the Example Works - Step-by-Step

1.  **Initialization:**

    - The `Player` agent starts at `Location 0`.
    - The `World` contains a `SmartObject` called `ImportantThing`.
    - The `Player`'s initial HTN domain is structured as follows (simplified):
      1.  `HighPriority Slot` (initially empty)
      2.  `Walk About` task (Condition: at Location 0; Effect: move to Location 1, subscribe to `ImportantThing`)
      3.  `Idle` task

2.  **First `Player.Think()` call:**

    - The planner evaluates tasks:
      - The `HighPriority Slot` is empty, so it's skipped.
      - The `Walk About` task's condition (at Location 0) is met.
    - Execution:
      - The `Player` executes "Walk about action", moving to `Location 1`.
      - As part of this action's effects, `Player.Subscribe(ImportantThing)` is called.
      - **Crucially:** The `ImportantThing`'s domain (which contains the "Walk away" task) is now loaded into the `Player`'s `HighPriority Slot`.
    - Output: `Arrived at location 1`

3.  **Second `Player.Think()` call:**

    - The planner evaluates tasks again:
      - The `HighPriority Slot` is checked first. It now contains the `ImportantThing`'s domain.
      - The "Walk away" task from the `ImportantThing`'s domain is considered. Its condition (at Location 1) is met.
    - Execution:
      - The `Player` executes the "Walk away" action (from the `ImportantThing`), moving to `Location 2`.
      - As part of this action's effects, `Player.Unsubscribe(ImportantThing)` is called.
      - **Crucially:** The `ImportantThing`'s domain is removed from the `Player`'s `HighPriority Slot`.
    - Output: `Arrived at location 2`
    - The planner continues its current `Think()` cycle:
      - The `HighPriority Slot` is now empty again and fails.
      - The `Walk About` task's condition (at Location 0) is _not_ met (Player is at Location 2).
      - The `Idle` task is selected as the next valid option.
    - Execution:
      - The `Player` executes the "Idle action".
    - Output: `Idle`

4.  **End:**
    - Output: `The End!`

## Code Structure

- **`Program.cs`**: The main entry point that sets up the simulation and calls `Player.Think()`.
- **`Player.cs`**: Defines the agent, its primary HTN domain, its `AIContext`, and the `Subscribe`/`Unsubscribe` logic for interacting with Smart Object domains via slots.
- **`SmartObject.cs`**: Defines the `SmartObject` class, which encapsulates an HTN domain representing its specific behaviors. `ImportantThing` is an instance of this.
- **`World.cs`**: A simple class to hold world entities, in this case, the `ImportantThing`.
- **`AIContext.cs`**: The context for the HTN planner, holding the current world state (e.g., `Location`) and other planning-related data.
- **`AIDomainSlots.cs`**: An enumeration defining the types of domain slots available (e.g., `HighPriority`).
- **`DomainExt.cs`**: Extension methods to make working with domain slots more convenient.

## Running the Example

This is a console application. Compile and run the project, and it will produce the output described, including the detailed decomposition log from the HTN planner.
