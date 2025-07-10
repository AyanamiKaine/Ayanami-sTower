import { test, expect, describe } from "bun:test";
import { World, System, Entity, QueryResult } from "../stella-ecs.js";

// --- Define components needed for the test ---
class Position {
    constructor(x = 0, y = 0) {
        this.x = x;
        this.y = y;
    }
}
class Renderable {
    constructor(char = "?", color = "white") {
        this.char = char;
        this.color = color;
    }
}
class Player {}
class Follower {}
class Ally {}

describe("World and Entity API", () => {
    // We define a setup function to create a consistent world state for our tests.
    function setupWorld() {
        const world = new World();

        // Define dynamic components
        world.componentFactory.define("Mana", { value: 100, max: 150 });
        world.componentFactory.define("Health", { value: 80, max: 100 });

        const staticComponents = [Position, Renderable, Player, Follower, Ally];

        // Create entities using the new fluent API
        const playerEntity = world
            .createEntity()
            .set(new Player())
            .set(new Position(10, 10))
            .set(new Renderable("P", "cyan"))
            .set("Mana", { value: 110 });

        const followerEntity = world
            .createEntity()
            .set(new Follower())
            .set(new Position(5, 5))
            .set(new Renderable("F", "yellow"));

        const allyEntity = world
            .createEntity()
            .set(new Ally())
            .set(new Position(15, 15))
            .set(new Renderable("A", "lime"));

        // Create relationships using the new fluent API
        playerEntity.addChild(followerEntity, { type: "leaderOf" });
        playerEntity.connectTo(allyEntity, { type: "alliedWith" });

        return {
            world,
            staticComponents,
            playerEntity,
            followerEntity,
            allyEntity,
        };
    }

    test("should serialize and deserialize a world state correctly", () => {
        const {
            world,
            staticComponents,
            playerEntity,
            followerEntity,
            allyEntity,
        } = setupWorld();

        const worldState = world.toJSON();

        const newWorld = World.fromJSON(worldState, {
            staticComponents: staticComponents,
            systems: [],
        });

        // Test basic properties
        expect(newWorld.nextEntityID).toBe(world.nextEntityID);

        // Test entity and component integrity
        const newPlayer = newWorld.entityArchetypeMap.get(playerEntity.id); // Internal check for testing
        expect(newPlayer).toBeDefined();

        const newPlayerMana = newWorld.getComponent(playerEntity.id, "Mana");
        expect(newPlayerMana).toBeDefined();
        expect(newPlayerMana.value).toBe(110);
        expect(newWorld.hasComponent(playerEntity.id, Player)).toBe(true);

        // Test relationships
        const newPlayerChildren = newWorld.getChildren(playerEntity.id);
        expect(newPlayerChildren).toContain(followerEntity.id);

        // A deep comparison of the graph structure is a very robust test
        expect(newWorld.relationshipGraph.export()).toEqual(
            world.relationshipGraph.export()
        );
    });

    test("should correctly remove a component from an entity", () => {
        const { world, playerEntity } = setupWorld();

        // Verify the component exists before removal
        expect(playerEntity.has("Mana")).toBe(true);
        expect(playerEntity.get("Mana")).toBeDefined();

        // Remove the component
        playerEntity.remove("Mana");

        // Verify the component is gone
        expect(playerEntity.has("Mana")).toBe(false);
        expect(playerEntity.get("Mana")).toBeUndefined();

        // Ensure other components are unaffected
        expect(playerEntity.has(Position)).toBe(true);
        expect(playerEntity.get(Position).x).toBe(10);
    });

    test("should persist component removal after serialization", () => {
        const { world, staticComponents, playerEntity } = setupWorld();

        // Remove a component
        playerEntity.remove("Mana");

        // Serialize the world *after* the removal
        const worldState = world.toJSON();
        const newWorld = World.fromJSON(worldState, {
            staticComponents: staticComponents,
            systems: [],
        });

        // Verify the component is still gone in the new world
        expect(newWorld.hasComponent(playerEntity.id, "Mana")).toBe(false);

        // Verify other components are still present
        expect(newWorld.hasComponent(playerEntity.id, Player)).toBe(true);
    });
});

class Galaxy {}
class StarSystem {}
class Name {
    constructor(value) {
        this.value = value;
    }
}

describe("Galaxy Map Simulation", () => {
    function setupGalaxy() {
        const world = new World();

        // It's good practice to explicitly register static components for serialization tests.
        const staticComponents = [Position, Galaxy, StarSystem, Name];
        for (const c of staticComponents) {
            world.registerComponent(c);
        }

        // Create Galaxies
        const milkyWay = world
            .createEntity()
            .set(new Galaxy())
            .set(new Name("Milky Way"));
        const andromeda = world
            .createEntity()
            .set(new Galaxy())
            .set(new Name("Andromeda"));

        // Create Star Systems
        const sol = world
            .createEntity()
            .set(new StarSystem())
            .set(new Name("Sol"))
            .set(new Position(1, 1));
        const alphaCentauri = world
            .createEntity()
            .set(new StarSystem())
            .set(new Name("Alpha Centauri"))
            .set(new Position(5, 8));
        const sirius = world
            .createEntity()
            .set(new StarSystem())
            .set(new Name("Sirius"))
            .set(new Position(10, -4));
        const triangulum = world
            .createEntity()
            .set(new StarSystem())
            .set(new Name("Triangulum"))
            .set(new Position(100, 100));

        // Establish Parent/Child relationships (Galaxy -> StarSystem)
        milkyWay.addChild(sol, { type: "contains" });
        milkyWay.addChild(alphaCentauri, { type: "contains" });
        milkyWay.addChild(sirius, { type: "contains" });
        andromeda.addChild(triangulum, { type: "contains" });

        // Establish peer relationships (StarSystem <-> StarSystem)
        sol.connectTo(alphaCentauri, { type: "hyperspaceLane", distance: 4.3 });
        alphaCentauri.connectTo(sirius, {
            type: "hyperspaceLane",
            distance: 8.6,
        });

        return {
            world,
            staticComponents,
            milkyWay,
            andromeda,
            sol,
            alphaCentauri,
            sirius,
            triangulum,
        };
    }

    test("should correctly model and query galaxy and star system relationships", () => {
        const { world, milkyWay, andromeda, sol, alphaCentauri, sirius } =
            setupGalaxy();

        // Assertions for parent/child relationships
        const milkyWayChildren = world.getChildren(milkyWay.id);
        expect(milkyWayChildren.length).toBe(3);
        expect(milkyWayChildren).toContain(sol.id);
        expect(world.getParents(sol.id)).toContain(milkyWay.id);

        const andromedaChildren = world.getChildren(andromeda.id);
        expect(andromedaChildren.length).toBe(1);

        // Assertions for peer relationships
        const solConnections = world.getConnectionsWithDetails(sol.id);
        const solToAlphaCentauri = solConnections.find(
            (c) => c.neighbor == alphaCentauri.id
        );
        expect(solToAlphaCentauri).toBeDefined();
        expect(solToAlphaCentauri.kind).toBe("undirected");
        expect(solToAlphaCentauri.attributes.type).toBe("hyperspaceLane");
        expect(solToAlphaCentauri.attributes.distance).toBe(4.3);

        // Assertions for queries
        const allStarSystems = world.query([StarSystem]);
        expect(new QueryResult(allStarSystems).count).toBe(4);

        const allGalaxies = world.query([Galaxy]);
        expect(new QueryResult(allGalaxies).count).toBe(2);
    });

    test("should correctly serialize and deserialize a galaxy map", () => {
        const {
            world,
            staticComponents,
            milkyWay,
            andromeda,
            sol,
            alphaCentauri,
        } = setupGalaxy();

        // Serialize and deserialize
        const worldState = world.toJSON();
        const newWorld = World.fromJSON(worldState, { staticComponents });

        // Re-run assertions on the new world
        expect(newWorld.getComponent(milkyWay.id, Name).value).toBe(
            "Milky Way"
        );

        const newMilkyWayChildren = newWorld.getChildren(milkyWay.id);
        expect(newMilkyWayChildren.length).toBe(3);
        expect(newMilkyWayChildren).toContain(sol.id);
        expect(newWorld.getParents(sol.id)).toContain(milkyWay.id);

        const newSolConnections = newWorld.getConnectionsWithDetails(sol.id);
        const newSolToAlphaCentauri = newSolConnections.find(
            (c) => c.neighbor == alphaCentauri.id
        );
        expect(newSolToAlphaCentauri).toBeDefined();
        expect(newSolToAlphaCentauri.kind).toBe("undirected");
        expect(newSolToAlphaCentauri.attributes.type).toBe("hyperspaceLane");

        const allStarSystems = newWorld.query([StarSystem]);
        expect(new QueryResult(allStarSystems).count).toBe(4);
    });
});
