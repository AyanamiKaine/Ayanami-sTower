import { World, Not, Optional, System, QueryResult } from "../index.js";

// Define some simple components for testing purposes
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

class Health {
    constructor(value = 100) {
        this.value = value;
    }
}

class Frozen {
    constructor(duration = 1) {
        this.duration = duration;
    }
}

describe("Query Modifiers (`Not`, `Optional`)", () => {
    let world;

    beforeEach(() => {
        // Create a new world before each test to ensure isolation
        world = new World();

        // Register all component types used in the tests
        world.registerComponent(Position);
        world.registerComponent(Velocity);
        world.registerComponent(Health);
        world.registerComponent(Frozen);

        // Create a diverse set of entities
        // Entity 0: Has Position and Velocity
        world.createEntity().set(new Position(1, 1)).set(new Velocity(10, 10));

        // Entity 1: Has Position, Velocity, and Health
        world
            .createEntity()
            .set(new Position(2, 2))
            .set(new Velocity(20, 20))
            .set(new Health());

        // Entity 2: Has Position, Velocity, and is Frozen
        world
            .createEntity()
            .set(new Position(3, 3))
            .set(new Velocity(30, 30))
            .set(new Frozen());

        // Entity 3: Has only Position
        world.createEntity().set(new Position(4, 4));
    });

    // --- Tests for the `Not` modifier ---

    test("`Not` modifier should exclude entities with the specified component", () => {
        // Query for entities with Position and Velocity, but NOT Frozen
        const archetypes = world.query([Position, Velocity, new Not(Frozen)]);
        const result = new QueryResult(archetypes);

        // Should match Entity 0 and Entity 1, but not Entity 2 (which is frozen) or Entity 3 (no velocity)
        expect(result.count).toBe(2);

        const entityIds = result.map((res) => res.entity);
        expect(entityIds).toContain(0);
        expect(entityIds).toContain(1);
        expect(entityIds).not.toContain(2);
    });

    test("`Not` modifier should correctly query when no entities have the excluded component", () => {
        // Define a component that no entity has
        class Mana {}
        world.registerComponent(Mana);

        // Query for entities with Position, but NOT Mana
        const archetypes = world.query([Position, new Not(Mana)]);
        const result = new QueryResult(archetypes);

        // Should match all entities that have a Position component (0, 1, 2, 3)
        expect(result.count).toBe(4);
    });

    test("`Not` modifier should return an empty result if all matching entities have the excluded component", () => {
        // Query for entities with Position, but NOT Position (a contradictory query)
        const archetypes = world.query([Position, new Not(Position)]);
        const result = new QueryResult(archetypes);

        // Should match no entities
        expect(result.count).toBe(0);
    });

    test("`Not` modifier should work with string-defined components", () => {
        world.componentFactory.define("Tag.Player", {});
        world.createEntity().set(new Position(5, 5)).set("Tag.Player", {});

        const archetypes = world.query([Position, new Not("Tag.Player")]);
        const result = new QueryResult(archetypes);

        // Should match entities 0, 1, 2, 3 but not the new entity 4
        expect(result.count).toBe(4);
        const entityIds = result.map((res) => res.entity);
        expect(entityIds).not.toContain(4);
    });

    // --- Tests for the `Optional` modifier ---

    test("`Optional` modifier should not filter out entities that lack the optional component", () => {
        // Query for entities with Position, and optionally Health.
        // This should return ALL entities with at least a Position component.
        const archetypes = world.query([Position, new Optional(Health)]);
        const result = new QueryResult(archetypes);

        // Should match entities 0, 1, 2, 3
        expect(result.count).toBe(4);

        // Let's verify the components
        const foundHealth = result.filter((res) => res.components.has(Health));
        const missingHealth = result.filter(
            (res) => !res.components.has(Health)
        );

        // Only entity 1 has Health
        expect(foundHealth.length).toBe(1);
        expect(foundHealth[0].entity).toBe(1);

        // Entities 0, 2, 3 are missing Health
        expect(missingHealth.length).toBe(3);
    });

    test("Query with `Optional` should return the same entities as a query without it", () => {
        const queryWithOptional = new QueryResult(
            world.query([Position, Velocity, new Optional(Health)])
        );
        const queryWithoutOptional = new QueryResult(
            world.query([Position, Velocity])
        );

        // Both queries should match entities 0, 1, and 2.
        expect(queryWithOptional.count).toBe(queryWithoutOptional.count);
        expect(queryWithOptional.count).toBe(3);

        const idsWith = queryWithOptional.map((e) => e.entity).sort();
        const idsWithout = queryWithoutOptional.map((e) => e.entity).sort();

        expect(idsWith).toEqual(idsWithout);
    });

    // --- Tests combining modifiers ---

    test("Should correctly combine `Not` and `Optional` modifiers", () => {
        // Query for entities with Position, optionally with Velocity, but NOT Frozen.
        const archetypes = world.query([
            Position,
            new Optional(Velocity),
            new Not(Frozen),
        ]);
        const result = new QueryResult(archetypes);

        // This should match:
        // - Entity 0 (has Position, has Velocity, not Frozen)
        // - Entity 1 (has Position, has Velocity, not Frozen)
        // - Entity 3 (has Position, no Velocity, not Frozen)
        // It should NOT match:
        // - Entity 2 (is Frozen)
        expect(result.count).toBe(3);

        const entityIds = result.map((res) => res.entity);
        expect(entityIds).toContain(0);
        expect(entityIds).toContain(1);
        expect(entityIds).toContain(3);
        expect(entityIds).not.toContain(2);
    });
});
