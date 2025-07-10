import { test, expect, describe } from "bun:test";
import { World, QueryResult } from "../index.js"; // Adjust path as needed

// --- Components for Query Tests ---
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
class Renderable {
    constructor(sprite = "default") {
        this.sprite = sprite;
    }
}
class PlayerTag {} // A component with no data, used as a tag
class EnemyTag {} // Another tag component

describe("World Queries and QueryResult", () => {
    // Helper function to set up a world with a variety of entities
    function setupTestWorld() {
        const world = new World();

        // Player entity
        world
            .createEntity()
            .set(new PlayerTag())
            .set(new Position(0, 0))
            .set(new Velocity(1, 1))
            .set(new Renderable("player"));

        // Enemy entities
        world
            .createEntity()
            .set(new EnemyTag())
            .set(new Position(10, 10))
            .set(new Velocity(0, 0))
            .set(new Renderable("goblin"));
        world
            .createEntity()
            .set(new EnemyTag())
            .set(new Position(20, 5))
            .set(new Velocity(-1, 0))
            .set(new Renderable("orc"));

        // A static, non-moving object
        world
            .createEntity()
            .set(new Position(50, 50))
            .set(new Renderable("rock"));

        // An invisible entity (no Renderable component)
        world.createEntity().set(new Position(5, 5));

        return world;
    }

    test("basic query should return correct entities and count", () => {
        const world = setupTestWorld();

        // Query for all entities with a Position
        const posQuery = new QueryResult(world.query([Position]));
        expect(posQuery.count).toBe(5);

        // Query for all renderable entities
        const renderQuery = new QueryResult(world.query([Renderable]));
        expect(renderQuery.count).toBe(4);

        // Query for enemies
        const enemyQuery = new QueryResult(world.query([EnemyTag]));
        expect(enemyQuery.count).toBe(2);

        // Query for entities that are both renderable and have a velocity
        const movingRenderableQuery = new QueryResult(
            world.query([Renderable, Velocity])
        );
        expect(movingRenderableQuery.count).toBe(3);
    });

    test("empty query should return a result with zero count", () => {
        const world = setupTestWorld();

        // Query for something that doesn't exist
        class NonExistentComponent {}
        const emptyQuery = new QueryResult(world.query([PlayerTag, EnemyTag])); // Can't be both
        const emptyQuery2 = new QueryResult(
            world.query([NonExistentComponent])
        );

        expect(emptyQuery.count).toBe(0);
        expect(emptyQuery2.count).toBe(0);
    });

    test("QueryResult.forEach should iterate over all results", () => {
        const world = setupTestWorld();
        const queryResult = new QueryResult(world.query([EnemyTag]));

        let enemyCount = 0;
        queryResult.forEach(({ entity, components }) => {
            expect(components.has(EnemyTag)).toBe(true);
            enemyCount++;
        });

        expect(enemyCount).toBe(2);
    });

    test("QueryResult.map should transform results correctly", () => {
        const world = setupTestWorld();
        const queryResult = new QueryResult(world.query([Renderable]));

        const sprites = queryResult.map(({ components }) => {
            return components.get(Renderable).sprite;
        });

        expect(sprites.length).toBe(4);
        expect(sprites).toContain("player");
        expect(sprites).toContain("goblin");
        expect(sprites).toContain("orc");
        expect(sprites).toContain("rock");
    });

    test("QueryResult.filter should select a subset of results", () => {
        const world = setupTestWorld();
        const queryResult = new QueryResult(world.query([Position, Velocity])); // All moving things

        // Filter for things moving horizontally
        const horizontalMovers = queryResult.filter(({ components }) => {
            const vel = components.get(Velocity);
            return vel.dx !== 0 && vel.dy === 0;
        });

        expect(horizontalMovers.length).toBe(1);
        const orc = horizontalMovers[0];
        expect(orc.components.get(Renderable)?.sprite).toBe("orc");
    });

    test("QueryResult.find should locate the first matching result", () => {
        const world = setupTestWorld();
        const queryResult = new QueryResult(world.query([Renderable]));

        const goblin = queryResult.find(({ components }) => {
            return components.get(Renderable).sprite === "goblin";
        });

        expect(goblin).toBeDefined();
        expect(goblin.components.get(Position).x).toBe(10);

        const dragon = queryResult.find(({ components }) => {
            return components.get(Renderable).sprite === "dragon";
        });

        expect(dragon).toBeUndefined();
    });

    test("should handle queries with dynamically defined components", () => {
        const world = new World();
        world.componentFactory.define("Mana", { current: 100 });

        const player = world
            .createEntity()
            .set(new Position(0, 0))
            .set("Mana", { current: 50 });
        const monster = world.createEntity().set(new Position(10, 10));

        const manaUsersQuery = new QueryResult(world.query(["Mana"]));
        expect(manaUsersQuery.count).toBe(1);

        const foundPlayer = manaUsersQuery.find(
            ({ entity }) => entity === player.id
        );

        expect(foundPlayer).toBeDefined();
        expect(
            foundPlayer.components.get(world.componentFactory.getClass("Mana"))
                .current
        ).toBe(50);
    });
});
