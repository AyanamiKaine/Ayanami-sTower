import { run, bench, group } from "mitata";
import { World } from "../stella-ecs.js";

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

// A component for the INEFFICIENT relationship benchmark
class Parent {
    constructor(targetId) {
        this.target = targetId;
    }
}

// A component for the EFFICIENT relationship benchmark
class Children {
    constructor() {
        this.list = [];
    }
}

// A "large" component for benchmarking memory access
class ParticleEmitter {
    constructor() {
        this.particles = new Array(50).fill(0).map(() => ({
            x: Math.random(),
            y: Math.random(),
            lifetime: Math.random() * 2,
        }));
    }
}

// --- Benchmark Suite ---

// The thing is, I dont even though what a good target for a number of entities is.
// Maybe many, maybe dont that many. I surly dont know.
const ENTITY_COUNT = 25000;
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

            let velocityStorage; // Will be cached on the first iteration

            // This loop forces entities to change structure repeatedly
            for (const entity of entities) {
                entity.set(new Velocity(1, 1));

                // On the first iteration, the `set` call above creates the
                // storage. Now we can get and cache it.
                if (!velocityStorage) {
                    velocityStorage = world.componentStorages[Velocity.id];
                }

                velocityStorage.remove(entity);
            }
        });
    });

    group("Entity Deletion & Relationships", () => {
        bench("Entity Deletion: 10k entities", () => {
            const world = new World();
            const entities = [];
            for (let i = 0; i < 10000; i++) {
                const e = world.createEntity();
                e.set(new Position(i, i));
                if (i % 2 === 0) e.set(new Velocity(1, 1));
                if (i % 5 === 0) e.set(new Renderable());
                entities.push(e);
            }

            const positionStorage = world.componentStorages[Position.id];
            const velocityStorage = world.componentStorages[Velocity.id];
            const renderableStorage = world.componentStorages[Renderable.id];

            // This simulates a `world.destroyEntity(entity)` call by removing all its components
            for (const entity of entities) {
                positionStorage.remove(entity);
                if (velocityStorage?.has(entity)) {
                    velocityStorage.remove(entity);
                }
                if (renderableStorage?.has(entity)) {
                    renderableStorage.remove(entity);
                }
            }
        });

        bench(
            "Naive Relationship Query: Find all children of 100 parents",
            () => {
                const world = new World();
                const parents = [];
                for (let i = 0; i < 100; i++) {
                    parents.push(world.createEntity());
                }

                for (let i = 0; i < 10000; i++) {
                    const child = world.createEntity();
                    child.set(new Position(i, i));
                    child.set(new Parent(parents[i % 100].id));
                }

                // Benchmark: query for children of each parent by scanning all relationships
                for (const parent of parents) {
                    const childrenQuery = world
                        .query()
                        .with(Parent)
                        .where((res) => res.parent.target === parent.id);
                    for (const _ of childrenQuery) {
                        // Just iterate
                    }
                }
            }
        );

        bench(
            "Optimized Relationship Query: Find all children of 100 parents",
            () => {
                const world = new World();
                const parents = [];
                for (let i = 0; i < 100; i++) {
                    const p = world.createEntity();
                    p.set(new Children()); // Parents now have a Children component
                    parents.push(p);
                }

                for (let i = 0; i < 10000; i++) {
                    const child = world.createEntity();
                    child.set(new Position(i, i));
                    // Add the child's ID to the parent's list
                    const parent = parents[i % 100];
                    parent.get(Children).list.push(child.id);
                }

                // Benchmark: query for children by direct lookup
                for (const parent of parents) {
                    const childrenList = parent.get(Children).list;
                    for (const childId of childrenList) {
                        // In a real system you'd get the entity: const entity = world.entities[childId];
                    }
                }
            }
        );
    });

    // --- Setup for Query & System Performance Benchmarks ---
    const queryWorld = new World();
    // Create a diverse set of entities to make queries more realistic
    for (let i = 0; i < ENTITY_COUNT; i++) {
        const e = queryWorld.createEntity();
        e.set(new Position(i, i));

        if (i % 2 === 0) e.set(new Velocity(1, 1));
        if (i % 3 === 0) e.set(new Renderable());
        if (i % 5 === 0) e.set(new Mass(1)); // Relatively sparse
        if (i % 7 === 0) e.set(new Rotation(0));
        if (i % 20 === 0) e.set(new ParticleEmitter()); // Large component
    }

    group("Query & System Performance", () => {
        bench(
            `Simple Query: Iterate ${ENTITY_COUNT} entities with 2 components`,
            () => {
                const query = queryWorld.query().with(Position).with(Velocity);
                for (const _ of query) {
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

                for (const _ of query) {
                }
            }
        );

        bench(
            "Sparse Iteration: Querying a component few entities have",
            () => {
                // The query system should be smart enough to iterate over the smaller set (Mass)
                const query = queryWorld.query().with(Position).with(Mass);
                for (const _ of query) {
                }
            }
        );

        bench(
            "Big Component Iteration: Querying entities with large components",
            () => {
                const query = queryWorld.query().with(ParticleEmitter);
                for (const { particleemitter } of query) {
                    // Simulate some work on the big component
                    particleemitter.particles[0].lifetime -= DUMMY_DELTA_TIME;
                }
            }
        );

        bench(
            `System Update (Movement): Process ${ENTITY_COUNT} entities`,
            () => {
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
                for (const _ of aiQuery) {
                    // Simulate AI logic, just iterate for now.
                }
            }
        );
    });

    // Run all defined benchmarks
    await run();
};

main().catch(console.error);