import { test, expect, describe } from "bun:test";
import { World, System } from "../index.js"; // Adjust path to your index.js as needed

// --- Define components needed for the test ---
class Position { constructor(x = 0, y = 0) { this.x = x; this.y = y; } }
class Renderable { constructor(char = '?', color = 'white') { this.char = char; this.color = color; } }
class Player { }
class Follower { }
class Ally { }


describe("World and Entity API", () => {
    // We define a setup function to create a consistent world state for our tests.
    function setupWorld() {
        const world = new World();

        // Define dynamic components
        world.componentFactory.define('Mana', { value: 100, max: 150 });
        world.componentFactory.define('Health', { value: 80, max: 100 });

        const staticComponents = [Position, Renderable, Player, Follower, Ally];

        // Create entities using the new fluent API
        const playerEntity = world.createEntity()
            .set(new Player())
            .set(new Position(10, 10))
            .set(new Renderable('P', 'cyan'))
            .set('Mana', { value: 110 });

        const followerEntity = world.createEntity()
            .set(new Follower())
            .set(new Position(5, 5))
            .set(new Renderable('F', 'yellow'));

        const allyEntity = world.createEntity()
            .set(new Ally())
            .set(new Position(15, 15))
            .set(new Renderable('A', 'lime'));

        // Create relationships using the new fluent API
        playerEntity.addChild(followerEntity, { type: 'leaderOf' });
        playerEntity.connectTo(allyEntity, { type: 'alliedWith' });

        return { world, staticComponents, playerEntity, followerEntity, allyEntity };
    }


    test("should serialize and deserialize a world state correctly", () => {
        const { world, staticComponents, playerEntity, followerEntity, allyEntity } = setupWorld();

        const worldState = world.toJSON();

        const newWorld = World.fromJSON(worldState, {
            staticComponents: staticComponents,
            systems: []
        });

        // Test basic properties
        expect(newWorld.nextEntityID).toBe(world.nextEntityID);

        // Test entity and component integrity
        const newPlayer = newWorld.entityArchetypeMap.get(playerEntity.id); // Internal check for testing
        expect(newPlayer).toBeDefined();

        const newPlayerMana = newWorld.getComponent(playerEntity.id, 'Mana');
        expect(newPlayerMana).toBeDefined();
        expect(newPlayerMana.value).toBe(110);
        expect(newWorld.hasComponent(playerEntity.id, Player)).toBe(true);

        // Test relationships
        const newPlayerChildren = newWorld.getChildren(playerEntity.id);
        expect(newPlayerChildren).toContain(String(followerEntity.id));

        // A deep comparison of the graph structure is a very robust test
        expect(newWorld.relationshipGraph.export()).toEqual(world.relationshipGraph.export());
    });

    test("should correctly remove a component from an entity", () => {
        const { world, playerEntity } = setupWorld();

        // Verify the component exists before removal
        expect(playerEntity.has('Mana')).toBe(true);
        expect(playerEntity.get('Mana')).toBeDefined();

        // Remove the component
        playerEntity.remove('Mana');

        // Verify the component is gone
        expect(playerEntity.has('Mana')).toBe(false);
        expect(playerEntity.get('Mana')).toBeUndefined();

        // Ensure other components are unaffected
        expect(playerEntity.has(Position)).toBe(true);
        expect(playerEntity.get(Position).x).toBe(10);
    });

    test("should persist component removal after serialization", () => {
        const { world, staticComponents, playerEntity } = setupWorld();

        // Remove a component
        playerEntity.remove('Mana');

        // Serialize the world *after* the removal
        const worldState = world.toJSON();
        const newWorld = World.fromJSON(worldState, {
            staticComponents: staticComponents,
            systems: []
        });

        // Verify the component is still gone in the new world
        expect(newWorld.hasComponent(playerEntity.id, 'Mana')).toBe(false);

        // Verify other components are still present
        expect(newWorld.hasComponent(playerEntity.id, Player)).toBe(true);
    });
});
