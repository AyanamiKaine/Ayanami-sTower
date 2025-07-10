import { run, bench, summary, group } from "mitata";
import { World, System, Not, QueryResult } from "../stella-ecs.js"; // Adjust the import path to your library file

// --- Components for Benchmarking ---
// Simple components with basic data types.
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

class Rotation {
    constructor(angle = 0) {
        this.angle = angle;
    }
}

class Mass {
    constructor(value = 1) {
        this.value = value;
    }
}

// A "tag" component with no data.
class Renderable {}

// --- Systems for Benchmarking ---
// A typical system that iterates over a query and performs work.
class MovementSystem extends System {
    constructor() {
        super([Position, Velocity]);
    }

    execute(entities, world, deltaTime) {
        for (const { components } of entities) {
            const pos = components.get(Position);
            const vel = components.get(Velocity);
            pos.x += vel.dx * deltaTime;
            pos.y += vel.dy * deltaTime;
        }
    }
}

class RotationSystem extends System {
    constructor() {
        super([Rotation, Velocity]);
    }
    execute(entities, world, deltaTime) {
        for (const { components } of entities) {
            const rot = components.get(Rotation);
            const vel = components.get(Velocity);
            // Simulate some work
            rot.angle +=
                (Math.abs(vel.dx) + Math.abs(vel.dy)) * deltaTime * 0.1;
        }
    }
}

class AISystem extends System {
    constructor() {
        super([Position, Renderable, new Not(Velocity)]);
    }
    execute(entities, world, deltaTime) {
        // Simulate AI logic for non-moving, renderable entities
        for (const { entity } of entities) {
            // Do nothing, just iterate
        }
    }
}

// --- Benchmark Suite ---

const ENTITY_COUNT = 100_000;

// Group related benchmarks for clarity
const main = async () => {
    group("Entity & Component Manipulation", () => {
        bench("Entity Creation: 10k entities with 2 components", () => {
            const world = new World();
            for (let i = 0; i < 10000; i++) {
                world
                    .createEntity()
                    .set(new Position(i, i))
                    .set(new Velocity(1, 1));
            }
        });

        bench("Entity Creation: 10k entities with 5 components", () => {
            const world = new World();
            for (let i = 0; i < 10000; i++) {
                world
                    .createEntity()
                    .set(new Position(i, i))
                    .set(new Velocity(1, 1))
                    .set(new Rotation(0))
                    .set(new Mass(1))
                    .set(new Renderable());
            }
        });

        bench("Component Addition/Removal (Archetype Switching)", () => {
            const world = new World();
            const entities = [];
            for (let i = 0; i < 10000; i++) {
                entities.push(world.createEntity().set(new Position(i, i)));
            }
            // This loop forces entities to switch archetypes repeatedly
            for (const entity of entities) {
                entity.set(new Velocity(1, 1));
                entity.remove(Velocity);
            }
        });
    });

    // --- Setup for Query & System Performance Benchmarks ---
    const queryWorld = new World();
    const movementSystem = new MovementSystem();
    queryWorld.registerSystem(movementSystem);

    // Create a diverse set of entities to make queries more realistic
    for (let i = 0; i < ENTITY_COUNT; i++) {
        const e = queryWorld.createEntity().set(new Position(i, i));
        if (i % 2 === 0) {
            e.set(new Velocity(1, 1));
        }
        if (i % 3 === 0) {
            e.set(new Renderable());
        }
        if (i % 5 === 0) {
            e.set(new Mass(1));
        }
        if (i % 7 === 0) {
            e.set(new Rotation(0));
        }
    }

    group("Query & System Performance", () => {
        bench(
            `Simple Query: Iterate ${ENTITY_COUNT} entities with 2 components`,
            () => {
                const archetypes = queryWorld.query([Position, Velocity]);
                // Wrap the raw archetypes in a QueryResult to make it iterable
                const queryResult = new QueryResult(archetypes);
                // In a real scenario, you'd do work here. The loop is the benchmark.
                for (const { entity, components } of queryResult) {
                    // This space is intentionally left blank to measure raw iteration speed.
                }
            }
        );

        bench(
            `Complex Query: Iterate ${ENTITY_COUNT} entities with Not`,
            () => {
                const archetypes = queryWorld.query([
                    Position,
                    Velocity,
                    new Not(Renderable),
                ]);
                // Wrap the raw archetypes in a QueryResult to make it iterable
                const queryResult = new QueryResult(archetypes);
                for (const { entity, components } of queryResult) {
                    // This space is intentionally left blank to measure raw iteration speed.
                }
            }
        );

        bench(`System Update: Process ${ENTITY_COUNT} entities`, () => {
            // The system's update method contains the query and the iteration logic.
            // This benchmarks a more complete, real-world use case.
            queryWorld.update(1.0); // deltaTime = 1.0
        });
    });

    group("Multi-System Performance", () => {
        const multiSystemWorld = new World();
        multiSystemWorld.registerSystem(new MovementSystem());
        multiSystemWorld.registerSystem(new RotationSystem());
        multiSystemWorld.registerSystem(new AISystem());

        const NUM_MULTI_SYS_ENTITIES = 25_000;
        for (let i = 0; i < NUM_MULTI_SYS_ENTITIES; i++) {
            const e = multiSystemWorld.createEntity();
            // Create entities that match different combinations of system queries
            if (i % 2 === 0) e.set(new Position(i, i));
            if (i % 3 === 0) e.set(new Velocity(1, 1));
            if (i % 4 === 0) e.set(new Rotation(0));
            if (i % 5 === 0) e.set(new Renderable());
        }

        bench(
            `Multi-System Update: ${NUM_MULTI_SYS_ENTITIES} entities with 3 systems`,
            () => {
                multiSystemWorld.update(1.0);
            }
        );
    });

    // Run all defined benchmarks
    await run();
};

main();
