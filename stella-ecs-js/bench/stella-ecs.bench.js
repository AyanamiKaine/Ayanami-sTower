import { run, bench, group } from "mitata";
import { World } from "../stella-ecs";

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

// --- Benchmark Suite ---

const ENTITY_COUNT = 100_000;
const DUMMY_DELTA_TIME = 1.0 / 60.0;

const main = async () => {
    group("Entity & Component Manipulation", () => {
        bench("Entity Creation: 10k entities with 2 components", () => {
            const world = new World();
            for (let i = 0; i < 10000; i++) {
                const e = world.createEntity();
                e.set(new Position(i, i));
                e.set(new Velocity(1, 1));
            }
        });

        bench("Entity Creation: 10k entities with 5 components", () => {
            const world = new World();
            for (let i = 0; i < 10000; i++) {
                const e = world.createEntity();
                e.set(new Position(i, i));
                e.set(new Velocity(1, 1));
                e.set(new Rotation(0));
                e.set(new Mass(1));
                e.set(new Renderable());
            }
        });

        bench("Component Addition/Removal", () => {
            const world = new World();
            const entities = [];
            for (let i = 0; i < 10000; i++) {
                entities.push(world.createEntity());
                entities[i].set(new Position(i, i));
            }

            // Get the component storage once before the loop
            const velocityStorage = world.componentStorages.get("Velocity");

            // This loop forces entities to change structure repeatedly
            for (const entity of entities) {
                entity.set(new Velocity(1, 1));
                // To remove a component, we access its storage directly.
                // An entity.remove(Component) method would be a nice API addition.
                velocityStorage.remove(entity);
            }
        });
    });

    // --- Setup for Query & System Performance Benchmarks ---
    const queryWorld = new World();
    // Create a diverse set of entities to make queries more realistic
    for (let i = 0; i < ENTITY_COUNT; i++) {
        const e = queryWorld.createEntity();
        e.set(new Position(i, i));

        if (i % 2 === 0) e.set(new Velocity(1, 1));
        if (i % 3 === 0) e.set(new Renderable());
        if (i % 5 === 0) e.set(new Mass(1));
        if (i % 7 === 0) e.set(new Rotation(0));
    }

    group("Query & System Performance", () => {
        bench(
            `Simple Query: Iterate ${ENTITY_COUNT} entities with 2 components`,
            () => {
                const query = queryWorld.query().with(Position).with(Velocity);
                // In a real scenario, you'd do work here. The loop is the benchmark.
                for (const result of query) {
                    // This space is intentionally left blank to measure raw iteration speed.
                }
            }
        );

        bench(
            `Complex Query: Iterate ${ENTITY_COUNT} entities with 'without'`,
            () => {
                const query = queryWorld
                    .query()
                    .with(Position)
                    .with(Renderable)
                    .without(Velocity);

                for (const result of query) {
                    // This space is intentionally left blank to measure raw iteration speed.
                }
            }
        );

        bench(
            `System Update (Movement): Process ${ENTITY_COUNT} entities`,
            () => {
                // This benchmarks a more complete, real-world use case.
                // A "system" is just a query and a loop.
                const movementQuery = queryWorld
                    .query()
                    .with(Position)
                    .with(Velocity);
                for (const { position, velocity } of movementQuery) {
                    position.x += velocity.dx * DUMMY_DELTA_TIME;
                    position.y += velocity.dy * DUMMY_DELTA_TIME;
                }
            }
        );
    });

    group("Multi-System Performance", () => {
        bench(
            `Multi-System Update: ${ENTITY_COUNT} entities with 3 systems`,
            () => {
                // In a game loop, you would run all your systems sequentially.

                // 1. Movement System
                const movementQuery = queryWorld
                    .query()
                    .with(Position)
                    .with(Velocity);
                for (const { position, velocity } of movementQuery) {
                    position.x += velocity.dx * DUMMY_DELTA_TIME;
                    position.y += velocity.dy * DUMMY_DELTA_TIME;
                }

                // 2. Rotation System
                const rotationQuery = queryWorld
                    .query()
                    .with(Rotation)
                    .with(Velocity);
                for (const { rotation, velocity } of rotationQuery) {
                    rotation.angle +=
                        (Math.abs(velocity.dx) + Math.abs(velocity.dy)) *
                        DUMMY_DELTA_TIME *
                        0.1;
                }

                // 3. AI System
                const aiQuery = queryWorld
                    .query()
                    .with(Position)
                    .with(Renderable)
                    .without(Velocity);
                for (const result of aiQuery) {
                    // Simulate AI logic, just iterate for now.
                }
            }
        );
    });

    // Run all defined benchmarks
    await run();
};

main().catch(console.error);
