import { test, expect, describe, beforeEach } from "bun:test";
import { World } from "../src/World";

// Helper Component Classes
class Position {
    constructor(x, y) {
        this.x = x;
        this.y = y;
    }
}

class Velocity {
    constructor(dx, dy) {
        this.dx = dx;
        this.dy = dy;
    }
}

class Tag {
    constructor(name) {
        this.name = name;
    }
}

class Health {
    constructor(value) {
        this.value = value;
    }
}

describe("Query System", () => {
    let world;

    // Setup a fresh world with entities for each test
    beforeEach(() => {
        world = new World();

        // Entity 0: Has Position, Velocity, Health
        const e0 = world.createEntity();
        e0.set(new Position(10, 10));
        e0.set(new Velocity(1, 0));
        e0.set(new Health(100));

        // Entity 1: Has Position, Health
        const e1 = world.createEntity();
        e1.set(new Position(20, 20));
        e1.set(new Health(80));

        // Entity 2: Has Position, Velocity, Health, Tag
        const e2 = world.createEntity();
        e2.set(new Position(30, 30));
        e2.set(new Velocity(0, -1));
        e2.set(new Health(120));
        e2.set(new Tag("player"));

        // Entity 3: Has Velocity only
        const e3 = world.createEntity();
        e3.set(new Velocity(5, 5));
    });

    test("should query entities with a single component", () => {
        const query = world.query().with(Position);
        const results = [...query];

        expect(results.length).toBe(3);
        expect(results.some((r) => r.entity.id === 0)).toBe(true);
        expect(results.some((r) => r.entity.id === 1)).toBe(true);
        expect(results.some((r) => r.entity.id === 2)).toBe(true);

        // Check the component data
        const resultForE0 = results.find((r) => r.entity.id === 0);
        expect(resultForE0.position).toBeDefined();
        expect(resultForE0.position.x).toBe(10);
    });

    test("should query entities with multiple required components", () => {
        const query = world.query().with(Position).with(Velocity);
        const results = [...query];

        expect(results.length).toBe(2);
        expect(results.some((r) => r.entity.id === 0)).toBe(true);
        expect(results.some((r) => r.entity.id === 2)).toBe(true);

        // Check the component data
        const resultForE2 = results.find((r) => r.entity.id === 2);
        expect(resultForE2.position).toBeDefined();
        expect(resultForE2.velocity).toBeDefined();
        expect(resultForE2.position.x).toBe(30);
        expect(resultForE2.velocity.dy).toBe(-1);
    });

    test("should query entities with a component but without another", () => {
        const query = world.query().with(Position).with(Health).without(Tag);
        const results = [...query];

        expect(results.length).toBe(2);
        expect(results.some((r) => r.entity.id === 0)).toBe(true);
        expect(results.some((r) => r.entity.id === 1)).toBe(true);
    });

    test("should correctly handle optional components", () => {
        const query = world.query().with(Position).optional(Velocity);
        const results = [...query];

        expect(results.length).toBe(3); // e0, e1, e2 all have Position

        const resultForE0 = results.find((r) => r.entity.id === 0);
        expect(resultForE0.position).toBeDefined();
        expect(resultForE0.velocity).toBeDefined(); // e0 has Velocity
        expect(resultForE0.velocity.dx).toBe(1);

        const resultForE1 = results.find((r) => r.entity.id === 1);
        expect(resultForE1.position).toBeDefined();
        expect(resultForE1.velocity).toBeUndefined(); // e1 does not have Velocity

        const resultForE2 = results.find((r) => r.entity.id === 2);
        expect(resultForE2.position).toBeDefined();
        expect(resultForE2.velocity).toBeDefined(); // e2 has Velocity
    });

    test("should filter results with a where predicate", () => {
        const query = world
            .query()
            .with(Position)
            .with(Health)
            .where((res) => res.health.value > 90);
        const results = [...query];

        expect(results.length).toBe(2);
        expect(results.some((r) => r.entity.id === 0)).toBe(true); // Health: 100
        expect(results.some((r) => r.entity.id === 2)).toBe(true); // Health: 120
    });

    test("should handle complex queries with all modifiers", () => {
        const query = world
            .query()
            .with(Position)
            .with(Health)
            .without(Tag)
            .optional(Velocity)
            .where((res) => res.position.x < 15);

        const results = [...query];

        // Should match Entity 0:
        // - with(Position): yes (10, 10)
        // - with(Health): yes (100)
        // - without(Tag): yes
        // - optional(Velocity): yes (1, 0)
        // - where(position.x < 15): yes (10 < 15)
        // -> MATCH

        // Should not match Entity 1:
        // - where(position.x < 15): no (20 < 15 is false)
        // -> NO MATCH

        // Should not match Entity 2:
        // - without(Tag): no (it has a Tag)
        // -> NO MATCH

        expect(results.length).toBe(1);
        expect(results[0].entity.id).toBe(0);
        expect(results[0].position.x).toBe(10);
        expect(results[0].health.value).toBe(100);
        expect(results[0].velocity.dx).toBe(1);
    });

    test("should return an empty iterator for queries with no matches", () => {
        // Query for a component that no entity has
        class NonExistentComponent {}
        const query1 = world.query().with(NonExistentComponent);
        expect([...query1].length).toBe(0);

        // Query that is impossible to satisfy
        const query2 = world.query().with(Position).without(Position);
        expect([...query2].length).toBe(0);

        // Query with a predicate that always fails
        const query3 = world
            .query()
            .with(Position)
            .where(() => false);
        expect([...query3].length).toBe(0);
    });

    test("should be iterable multiple times", () => {
        const query = world.query().with(Position);

        const results1 = [...query];
        expect(results1.length).toBe(3);

        const results2 = [...query];
        expect(results2.length).toBe(3);
        expect(results1).toEqual(results2);
    });

    test("should allow modification of components during iteration", () => {
        const ecsWorld = new World();

        const e0 = ecsWorld.createEntity();
        e0.set(new Position(10, 10)).set(new Velocity(1, 2));

        const e1 = ecsWorld.createEntity();
        e1.set(new Position(10, 10)).set(new Velocity(10, 10));

        // This is our "movement system". It finds all entities with both
        // a Position and a Velocity component.
        const movementQuery = ecsWorld.query().with(Position).with(Velocity);

        // We loop through each entity that matches the query.
        // The { position, velocity } destructuring is possible because the query
        // result names are based on the component class names.
        for (const { position, velocity } of movementQuery) {
            position.x += velocity.dx;
            position.y += velocity.dy;
        }

        const pos0 = e0.get(Position);
        expect(pos0.x).toBe(11);
        expect(pos0.y).toBe(12);

        const pos1 = e1.get(Position);
        expect(pos1.x).toBe(20);
        expect(pos1.y).toBe(20);
    });
});
