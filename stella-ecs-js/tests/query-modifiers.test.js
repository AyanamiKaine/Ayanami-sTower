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

class Sprite {
    constructor(name = "default") {
        this.spriteName = name;
    }
}
class ShieldEffect {
    constructor(strength = 50) {
        this.strength = strength;
    }
}

/**
 * Heals entities with Health, but will not target entities that are Frozen.
 */
class CautiousHealingSystem extends System {
    constructor() {
        super([Health, new Not(Frozen)]);
    }

    execute(query, world, deltaTime) {
        query.forEach(({ components }) => {
            const health = components.get(Health);
            health.value += 5 * deltaTime;
        });
    }
}

/**
 * Renders entities with a Position and Sprite.
 * Also checks for an optional ShieldEffect to render an additional graphic.
 * (Modified for testing to record actions instead of logging to console)
 */
class RenderingSystem extends System {
    constructor() {
        super([Position, Sprite, new Optional(ShieldEffect)]);
        this.renderedItems = []; // Test-friendly log
    }

    execute(query, world, deltaTime) {
        query.forEach(({ entity, components }) => {
            const log = {
                entity: entity,
                sprite: components.get(Sprite).spriteName,
                hasShield: components.has(ShieldEffect),
            };
            this.renderedItems.push(log);
        });
    }
}

describe("Systems with Query Modifiers", () => {
    let world;

    beforeEach(() => {
        world = new World();
        // Register all components
        world.registerComponent(Health);
        world.registerComponent(Frozen);
        world.registerComponent(Position);
        world.registerComponent(Sprite);
        world.registerComponent(ShieldEffect);
    });

    test("CautiousHealingSystem should only heal entities that are not Frozen", () => {
        // Setup entities
        const normalEntity = world.createEntity().set(new Health(100));
        const frozenEntity = world
            .createEntity()
            .set(new Health(100))
            .set(new Frozen());
        const entityWithoutHealth = world.createEntity().set(new Frozen());

        // Setup system
        const healingSystem = new CautiousHealingSystem();
        world.registerSystem(healingSystem);

        // Run the world update
        const deltaTime = 1;
        world.update(deltaTime);

        // Assertions
        const normalEntityHealth = world.getComponent(normalEntity.id, Health);
        const frozenEntityHealth = world.getComponent(frozenEntity.id, Health);

        // The normal entity should have been healed
        expect(normalEntityHealth.value).toBe(105);

        // The frozen entity's health should be unchanged
        expect(frozenEntityHealth.value).toBe(100);
    });

    test("RenderingSystem should process all entities with required components and detect optional ones", () => {
        // Setup entities
        const spriteOnlyEntity = world
            .createEntity()
            .set(new Position())
            .set(new Sprite("player"));
        const spriteWithShieldEntity = world
            .createEntity()
            .set(new Position())
            .set(new Sprite("enemy"))
            .set(new ShieldEffect());
        const positionOnlyEntity = world.createEntity().set(new Position()); // Should not be rendered

        // Setup system
        const renderingSystem = new RenderingSystem();
        world.registerSystem(renderingSystem);

        // Run the world update
        world.update(1);

        // Assertions
        const rendered = renderingSystem.renderedItems;

        // The system should have processed two entities
        expect(rendered.length).toBe(2);

        // Find the log for the first entity
        const renderedSpriteOnly = rendered.find(
            (r) => r.entity === spriteOnlyEntity.id
        );
        expect(renderedSpriteOnly).toBeDefined();
        expect(renderedSpriteOnly.sprite).toBe("player");
        expect(renderedSpriteOnly.hasShield).toBe(false);

        // Find the log for the second entity
        const renderedWithShield = rendered.find(
            (r) => r.entity === spriteWithShieldEntity.id
        );
        expect(renderedWithShield).toBeDefined();
        expect(renderedWithShield.sprite).toBe("enemy");
        expect(renderedWithShield.hasShield).toBe(true);
    });
});


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
