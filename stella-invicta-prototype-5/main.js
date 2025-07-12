import { World, QueryResult } from "stella-ecs-js";
import Graph from "graphology";

// --- Component Definitions ---

// Hierarchy Tags
class Galaxy {}
class StarSystem {}
class Planet {}
class Moon {}

// Data Components
class Name {
    constructor(value) {
        this.value = value;
    }
}

class Position {
    constructor(x = 0, y = 0) {
        this.x = x;
        this.y = y;
    }
}

class PlanetType {
    /**
     * @param {string} type - e.g., "Terrestrial", "Gas Giant", "Ice Giant".
     */
    constructor(type) {
        this.type = type;
    }
}

class OrbitalDistance {
    /**
     * @param {number} distance - The distance from the parent body in AU.
     */
    constructor(distance) {
        this.distance = distance;
    }
}

class HyperspaceLane {
    constructor(travelTime) {
        this.travelTime = travelTime;
    }
}

// --- Galaxy Generation ---

/**
 * Generates a new galaxy with star systems, planets, and moons.
 * @returns {World} A fully populated world instance.
 */
function generateGalaxy() {
    const world = new World();

    // --- 1. Create the Galaxy Entity ---
    const milkyWay = world
        .createEntity()
        .set(new Galaxy())
        .set(new Name("Milky Way"));

    // --- 2. Create Star Systems and their contents ---

    // Sol System
    const sol = world
        .createEntity()
        .set(new StarSystem())
        .set(new Name("Sol"))
        .set(new Position(10, 15));
    milkyWay.addChild(sol);

    const earth = world
        .createEntity()
        .set(new Planet())
        .set(new Name("Earth"))
        .set(new PlanetType("Terrestrial"))
        .set(new OrbitalDistance(1));
    sol.addChild(earth);

    const luna = world
        .createEntity()
        .set(new Moon())
        .set(new Name("Luna"))
        .set(new OrbitalDistance(0.00257));
    earth.addChild(luna);

    const mars = world
        .createEntity()
        .set(new Planet())
        .set(new Name("Mars"))
        .set(new PlanetType("Terrestrial"))
        .set(new OrbitalDistance(1.52));
    sol.addChild(mars);

    const phobos = world
        .createEntity()
        .set(new Moon())
        .set(new Name("Phobos"))
        .set(new OrbitalDistance(0.000062));
    mars.addChild(phobos);

    const deimos = world
        .createEntity()
        .set(new Moon())
        .set(new Name("Deimos"))
        .set(new OrbitalDistance(0.000156));
    mars.addChild(deimos);

    const jupiter = world
        .createEntity()
        .set(new Planet())
        .set(new Name("Jupiter"))
        .set(new PlanetType("Gas Giant"))
        .set(new OrbitalDistance(5.2));
    sol.addChild(jupiter);

    const io = world
        .createEntity()
        .set(new Moon())
        .set(new Name("Io"))
        .set(new OrbitalDistance(0.0028));
    jupiter.addChild(io);
    const europa = world
        .createEntity()
        .set(new Moon())
        .set(new Name("Europa"))
        .set(new OrbitalDistance(0.0045));
    jupiter.addChild(europa);

    // Alpha Centauri System
    const alphaCentauri = world
        .createEntity()
        .set(new StarSystem())
        .set(new Name("Alpha Centauri"))
        .set(new Position(12, 20));
    milkyWay.addChild(alphaCentauri);

    const proximaB = world
        .createEntity()
        .set(new Planet())
        .set(new Name("Proxima Centauri b"))
        .set(new PlanetType("Super-Earth"))
        .set(new OrbitalDistance(0.05));
    alphaCentauri.addChild(proximaB);

    // Sirius System
    const sirius = world
        .createEntity()
        .set(new StarSystem())
        .set(new Name("Sirius"))
        .set(new Position(5, 8));
    milkyWay.addChild(sirius);

    // --- 3. Establish Hyperspace Lanes ---
    sol.connectTo(alphaCentauri, { lane: new HyperspaceLane(1) });
    sol.connectTo(sirius, { lane: new HyperspaceLane(2) });

    return world;
}

// --- Querying and Displaying the Galaxy ---

/**
 * Recursively displays a celestial body and all its children.
 * @param {World} world - The world instance.
 * @param {number} entityId - The ID of the entity to display.
 * @param {number} indentLevel - The current level of indentation for printing.
 */
function displayCelestialBody(world, entityId, indentLevel = 0) {
    const indent = "  ".repeat(indentLevel);
    const name = world.getComponent(entityId, Name)?.value || "Unnamed";
    let details = "";

    // Add specific details based on component type
    if (world.hasComponent(entityId, StarSystem)) {
        const pos = world.getComponent(entityId, Position);
        details = `(Star System at ${pos.x}, ${pos.y})`;
    } else if (world.hasComponent(entityId, Planet)) {
        const type = world.getComponent(entityId, PlanetType)?.type;
        const dist = world.getComponent(entityId, OrbitalDistance)?.distance;
        details = `(Planet - ${type}, ${dist} AU)`;
    } else if (world.hasComponent(entityId, Moon)) {
        const dist = world.getComponent(entityId, OrbitalDistance)?.distance;
        details = `(Moon, ${dist} AU)`;
    }

    console.log(`${indent}- ${name} ${details}`);

    // --- Display Hyperspace lanes for Star Systems ---
    if (world.hasComponent(entityId, StarSystem)) {
        const connections = world.getConnectionsWithDetails(entityId);
        const hyperspaceConnections = connections.filter(
            (c) => c.kind === "undirected" && c.attributes.lane
        );
        if (hyperspaceConnections.length > 0) {
            console.log(`${indent}  Hyperspace Connections:`);
            for (const conn of hyperspaceConnections) {
                const neighborName = world.getComponent(
                    conn.neighbor,
                    Name
                ).value;
                const travelTime = conn.attributes.lane.travelTime;
                console.log(
                    `${indent}  - to ${neighborName} (Travel Time: ${travelTime})`
                );
            }
        }
    }

    // --- Recurse for children ---
    const children = world.getChildren(entityId);
    if (children.length > 0) {
        for (const childId of children) {
            displayCelestialBody(world, childId, indentLevel + 1);
        }
    }
}

/**
 * Queries the world and displays information about the galaxy.
 * @param {World} world The world to query.
 */
function displayGalaxyInfo(world) {
    console.log("--- Galaxy Information ---");

    const galaxyArchetypes = world.query([Galaxy]);
    const galaxyQueryResult = new QueryResult(galaxyArchetypes);

    for (const { entity } of galaxyQueryResult) {
        const galaxyName = world.getComponent(entity, Name).value;
        console.log(`\nGalaxy: ${galaxyName}`);

        // Start the recursive display from the top-level galaxy entity
        const children = world.getChildren(entity);
        for (const childId of children) {
            displayCelestialBody(world, childId, 1);
        }
    }

    console.log("\n--- Travel Time Explanation ---");
    console.log(
        "A direct hyperspace lane connection means standard travel time."
    );
    console.log(
        "If two systems are not directly connected, travel between them would take twice as long."
    );
}

// --- Main Execution ---
const myGalaxyWorld = generateGalaxy();
displayGalaxyInfo(myGalaxyWorld);
