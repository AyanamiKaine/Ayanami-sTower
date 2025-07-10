import { test, expect, describe } from "bun:test";
import { World, System, Entity } from "../index.js"; // Adjust path as needed

// --- Components for System Tests ---
class Position { constructor(x = 0, y = 0) { this.x = x; this.y = y; } }
class Velocity { constructor(dx = 0, dy = 0) { this.dx = dx; this.dy = dy; } }
class Health { constructor(value = 100) { this.value = value; } }
class TakesDamage { } // A "tag" component

// --- Systems for Testing ---

// A system that updates position based on velocity.
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

// A system that applies damage to entities with Health and the TakesDamage tag.
class DamageSystem extends System {
    constructor(damageAmount = 10) {
        super([Health, TakesDamage]);
        this.damageAmount = damageAmount;
    }

    execute(entities, world, deltaTime) {
        for (const { components } of entities) {
            const health = components.get(Health);
            health.value -= this.damageAmount;
        }
    }
}

describe("ECS Systems", () => {

    test("MovementSystem should update entity positions", () => {
        const world = new World();
        world.registerSystem(new MovementSystem());

        const movingEntity = world.createEntity()
            .set(new Position(10, 20))
            .set(new Velocity(5, -2));

        const staticEntity = world.createEntity()
            .set(new Position(100, 100));

        // Run the world update
        world.update(2.0); // deltaTime of 2 seconds

        const pos = movingEntity.get(Position);
        expect(pos.x).toBe(10 + 5 * 2.0); // 20
        expect(pos.y).toBe(20 + (-2) * 2.0); // 16

        // Ensure the static entity was not affected
        const staticPos = staticEntity.get(Position);
        expect(staticPos.x).toBe(100);
    });

    test("DamageSystem should only affect entities with the correct components", () => {
        const world = new World();
        world.registerSystem(new DamageSystem(15));

        const vulnerableEntity = world.createEntity()
            .set(new Health(100))
            .set(new TakesDamage());

        const invulnerableEntity = world.createEntity()
            .set(new Health(50)); // Has health but no TakesDamage tag

        const toughEntity = world.createEntity()
            .set(new TakesDamage()); // Has tag but no Health component

        world.update(1.0);

        // The vulnerable entity should take damage
        expect(vulnerableEntity.get(Health).value).toBe(100 - 15);

        // The other entities should be unaffected
        expect(invulnerableEntity.get(Health).value).toBe(50);
        expect(toughEntity.has(Health)).toBe(false);
    });

    test("entity should cease to be processed by a system after component removal", () => {
        const world = new World();
        world.registerSystem(new MovementSystem());

        const entity = world.createEntity()
            .set(new Position(0, 0))
            .set(new Velocity(10, 10));

        // First update, entity should move
        world.update(1.0);
        expect(entity.get(Position).x).toBe(10);
        expect(entity.get(Position).y).toBe(10);

        // Remove the Velocity component
        entity.remove(Velocity);

        // Second update, entity should NOT move
        world.update(1.0);
        expect(entity.get(Position).x).toBe(10); // Unchanged
        expect(entity.get(Position).y).toBe(10); // Unchanged
    });

    test("entity should be processed by a system after a component is added", () => {
        const world = new World();
        world.registerSystem(new DamageSystem(25));

        const entity = world.createEntity()
            .set(new Health(100));

        // First update, entity should not take damage
        world.update(1.0);
        expect(entity.get(Health).value).toBe(100);

        // Add the TakesDamage tag component
        entity.set(new TakesDamage());

        // Second update, entity should now take damage
        world.update(1.0);
        expect(entity.get(Health).value).toBe(75);
    });
});
