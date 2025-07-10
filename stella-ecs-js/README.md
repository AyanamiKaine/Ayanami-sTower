# **Stella-ECS: A Modern Entity-Component-System Library**

AI GENERATED README

**Stella-ECS** is a high-performance, feature-rich Entity-Component-System (ECS) library for JavaScript and TypeScript. Designed with a modern, fluent API, it's perfect for building complex games and simulations. Its archetype-based architecture ensures that your systems run with maximum efficiency, while its unique relationship graph allows you to model complex connections between entities with ease.

## **Core Concepts**

ECS is an architectural pattern that separates data from behavior.

-   **World**: The main container for your entire state. It manages all entities, components, and systems.
-   **Entity**: A simple identifier. It doesn't hold any data itself but acts as a key to which components are attached. In Stella-ECS, you interact with entities through a convenient Entity wrapper object.
-   **Component**: A plain data object that stores a specific piece of information. For example, a Position component would store an entity's coordinates. Components can be defined as classes or dynamically at runtime.
-   **System**: The logic of your application. A system queries the World for entities that have a specific set of components and then performs operations on them.

## **Features**

-   **🚀 High Performance**: Built on an archetype-based model. Entities with the same component "shape" are stored together in memory for fast iteration.
-   **✨ Fluent API**: A clean, chainable API for creating and manipulating entities makes your code more readable and expressive.
-   **🔍 Powerful Queries**: Go beyond basic component matching with Not and Optional query modifiers to select exactly the entities you need.
-   **🕸️ Entity Relationships**: Model complex hierarchies (e.g., parent/child) and peer-to-peer connections (e.g., allies, waypoints) using a built-in graph structure.
-   **🔧 Dynamic Components**: Define new component types on the fly from strings, perfect for loading entity definitions from data files.
-   **💾 Full Serialization**: Save and load the entire world state, including entities, components, and their relationships, to and from JSON.
-   **📦 Lightweight**: Minimal dependencies, relying on the excellent graphology library for its relationship graph.

## **Installation**

```bash
# With npm
npm install stella-ecs
```

```bash
# With yarn
yarn add stella-ecs
```

```bash
# With bun
bun add stella-ecs
```

## **Getting Started: A Complete Example**

Let's create a simple world where entities move around.

```js
import { World, System, Entity } from "stella-ecs";

// 1. Define your components as simple classes
class Position {
    constructor(x = 0, y = 0) {
        this.x = x;
        this.y = y;
    }
}

class Velocity {
    constructor(dx = 0, dy = 0) {
        this.dx = dx;
        this.dy = dy;
    }
}

// 2. Define a system that contains your game logic
// This system will query for all entities that have BOTH a Position and a Velocity
class MovementSystem extends System {
    constructor() {
        // The constructor defines the "query" for this system
        super([Position, Velocity]);
    }

    // The execute method runs on every update for the entities that match the query
    execute(queryResult, world, deltaTime) {
        console.log("Found ${queryResult.count} moving entities.");

        // The queryResult is iterable and provides the entity and its components
        queryResult.forEach(({ entity, components }) => {
            const pos = components.get(Position);
            const vel = components.get(Velocity);

            pos.x += vel.dx * deltaTime;
            pos.y += vel.dy * deltaTime;

            console.log(
                "Entity ${entity} moved to (${pos.x.toFixed(2)}, ${pos.y.toFixed(2)})"
            );
        });
    }
}

// 3. Set up the world
const world = new World();

// Register the system with the world
world.registerSystem(new MovementSystem());

// 4. Create entities using the fluent API
const player = world
    .createEntity()
    .set(new Position(0, 0))
    .set(new Velocity(10, 5));

const enemy = world
    .createEntity()
    .set(new Position(100, 50))
    .set(new Velocity(-5, 0));

// This entity won't be processed by MovementSystem because it lacks a Velocity component
const rock = world.createEntity().set(new Position(200, 200));

// 5. Run the main loop
let lastTime = performance.now();
function gameLoop() {
    const time = performance.now();
    const deltaTime = (time - lastTime) / 1000; // deltaTime in seconds

    console.log("n--- New Frame ---");
    world.update(deltaTime); // This will execute all registered systems

    lastTime = time;
    setTimeout(gameLoop, 1000); // Run again in 1 second
}

gameLoop();
```

## **API Reference**

### **World**

The central class that manages the entire ECS state.

-   `createEntity(): Entity:` Creates a new entity and returns a fluent wrapper.
-   `registerSystem(system: System)`: Adds a system to the world.
-   `query(queryParts: Array): Archetype[]:` Performs a query and returns matching archetypes. It's recommended to use this via a System or QueryResult.
-   `update(deltaTime: number)`: Executes all registered systems in order.
-   `toJSON(): object:` Serializes the entire world state to a JSON-compatible object.
-   `static fromJSON(data: object, options: object): World:` Creates a new World instance from serialized data.
    -   `options.systems`: An array of system instances to register in the new world.
    -   `options.staticComponents`: An array of component classes that were used in the original world. This is crucial for correct deserialization.

### **Entity**

A fluent wrapper around an entity ID.

-   `set(component: object | string, initialValues?: object): Entity:` Adds or replaces a component. Can take a class instance or the name of a dynamic component.
-   `get(ComponentClass: Function | string): object | undefined:` Retrieves a component instance.
-   `remove(ComponentClass: Function | string): Entity:` Removes a component.
-   `has(ComponentClass: Function | string): boolean:` Checks if the entity has a component.
-   `destroy()`: Removes the entity and all its components from the world.
-   addChild(childEntity: Entity, attributes?: object): Entity: Creates a directed parent-child relationship (this -> child).
-   connectTo(otherEntity: Entity, attributes?: object): Entity: Creates an undirected peer-to-peer relationship (this <-> other).

### **System**

The base class for all your game logic.

-   constructor(queryParts: Array): Define the components your system is interested in.
-   execute(queryResult: QueryResult, world: World, deltaTime: number): The main logic of your system. This method **must** be implemented by your subclass.

### **QueryResult**

An iterable container for query results, returned to your System.

-   count: number: The total number of entities that matched the query.
-   forEach(callback): Iterates over each result.
-   map(callback): Creates a new array by calling a function on every result.
-   filter(callback): Creates a new array with results that pass a test.
-   find(callback): Returns the first result that satisfies a test.

### **Query Modifiers**

Import Not and Optional to create more specific queries.

-   new Not(Component): Excludes entities that have the specified component.
-   new Optional(Component): Includes a component in the result if it exists, but doesn't require it for an entity to be matched.

```js
import { Not, Optional } from "stella-ecs";

// A system that targets entities with Health, but NOT the Frozen component.
class HealingSystem extends System {
    constructor() {
        super([Health, new Not(Frozen)]);
    }
    // ...
}

// A system that renders entities with a Sprite, and optionally a ShieldEffect.
class RenderSystem extends System {
    constructor() {
        super([Sprite, new Optional(ShieldEffect)]);
    }

    execute(queryResult, world, deltaTime) {
        queryResult.forEach(({ components }) => {
            const hasShield = components.has(ShieldEffect); // Check for the optional component
            if (hasShield) {
                // render shield...
            }
        });
    }
}
```

## **Advanced Topics**

### **Entity Relationships**

Stella-ECS uses graphology to manage complex relationships between entities, separate from the component data. This is perfect for modeling scene graphs, social connections, or spatial links.

```js
// --- Galaxy Simulation Example ---

// Create galaxies and star systems as entities
const milkyWay = world.createEntity().set(new Name("Milky Way"));
const sol = world.createEntity().set(new Name("Sol"));
const alphaCentauri = world.createEntity().set(new Name("Alpha Centauri"));

// Create a parent-child relationship: Milky Way "contains" Sol.
milkyWay.addChild(sol, { type: "contains" });

// Create a peer-to-peer relationship: a hyperspace lane between two star systems.
sol.connectTo(alphaCentauri, { type: "hyperspaceLane", distance: 4.3 });

// You can then query these relationships from the world:
const systemsInMilkyWay = world.getChildren(milkyWay.id); // -> [sol.id]
const solConnections = world.getConnectionsWithDetails(sol.id);
// -> [{ neighbor: alphaCentauri.id, kind: 'undirected', attributes: { type: 'hyperspaceLane', ... } }]
```

### **Dynamic Components**

Define components at runtime using strings. This is useful for data-driven design where entity templates are loaded from JSON or other files.

```js
const world = new World();

// Define a new component type called "Mana" with a default schema
world.componentFactory.define("Mana", { value: 100, max: 100 });

// Now you can use this component by its string name
const player = world.createEntity().set("Mana", { value: 150, max: 200 }); // Override defaults

const manaComponent = player.get("Mana");
console.log(manaComponent.value); // 150
```

### **Serialization**

Save and load the entire state of your world. This is essential for implementing save games.

```js
// --- SAVING ---
const worldState = world.toJSON();
// Now you can save `worldState` to a file or database.
const jsonString = JSON.stringify(worldState);

// --- LOADING ---

// You must provide the classes for any non-dynamic components.
const staticComponents = [Position, Velocity, Name];

// You must also provide the system instances you want in the new world.
const systems = [new MovementSystem()];

const newWorld = World.fromJSON(JSON.parse(jsonString), {
    staticComponents: staticComponents,
    systems: systems,
});

// The newWorld is now an exact copy of the original!
newWorld.update(0.16);
```

## **License**

This project is licensed under the MIT License.
