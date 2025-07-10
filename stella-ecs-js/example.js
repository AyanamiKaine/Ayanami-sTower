import { World, System } from "./stella-ecs.js";

// --- STATIC (PRE-DEFINED) COMPONENTS ---
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

// --- STATIC (PRE-DEFINED) SYSTEMS ---
class RenderSystem extends System {
    constructor() {
        super([Position, Renderable]);
    }

    execute(entities, world, deltaTime) {
        console.log(`RenderSystem is processing ${entities.count} entities:`);

        for (const { entity, components } of entities) {
            const pos = components.get(Position);
            const render = components.get(Renderable);

            const isPlayer = world.getComponent(entity, Player);
            const hasMana = world.getComponent(entity, "Mana");

            let playerTag = isPlayer ? "[PLAYER]" : "";
            let manaTag = hasMana ? `[MANA: ${hasMana.value.toFixed(0)}]` : "";

            console.log(
                `  -> ${render.char} ${playerTag} at (${pos.x.toFixed(
                    2
                )}, ${pos.y.toFixed(2)}) ${manaTag}`
            );
        }
    }
}

// This system now demonstrates querying both directed and undirected relationships.
class RelationshipSystem extends System {
    constructor() {
        // This system looks for any entity that could have relationships (e.g., the player).
        super([Player]);
    }

    execute(entities, world, deltaTime) {
        console.log(`\nRelationshipSystem is running:`);
        for (const { entity } of entities) {
            console.log(`  Inspecting relationships for Player ${entity}:`);
            // Use the new, powerful API to get details about all connections.
            const connections = world.getConnectionsWithDetails(entity);

            if (connections.length === 0) {
                console.log(`    -> No relationships found.`);
                continue;
            }

            for (const conn of connections) {
                const { neighbor, kind, attributes } = conn;
                const type = attributes.type || "N/A";
                console.log(
                    `    -> Found a ${kind} relationship of type '${type}' with entity ${neighbor}`
                );
            }
        }
    }
}

// --- SIMULATION SETUP ---
console.log("--- Initializing ECS World ---");
// The world now defaults to 'mixed' graph type, no need to specify it.
const world = new World();

// --- DYNAMIC DEFINITION ---
console.log("\n--- Defining Components Dynamically ---");
world.componentFactory.define("Mana", { value: 100, max: 150 });
world.componentFactory.define("Health", { value: 80, max: 100 });

// --- DYNAMIC SYSTEM DEFINITION ---
const manaRegenSystemLogic = `
    const ManaClass = world.getComponentClassByName('Mana');
    const PositionClass = world.getComponentClassByName('Position');

    for (const { components } of entities) {
        const mana = components.get(ManaClass);
        const pos = components.get(PositionClass);

        if (mana && pos) {
            if (mana.value < mana.max) {
                mana.value += 5 * deltaTime;
            }
            pos.y += Math.sin(mana.value / 10) * 0.1;
        }
    }
`;

const DynamicManaSystem = class extends System {
    constructor() {
        super(["Mana", Position]);
        this.execute = new Function(
            "entities",
            "world",
            "deltaTime",
            manaRegenSystemLogic
        );
    }
};

// --- REGISTERING SYSTEMS ---
console.log("\n--- Registering Systems ---");
const systemsToRegister = [
    new RenderSystem(),
    new DynamicManaSystem(),
    new RelationshipSystem(),
];
for (const system of systemsToRegister) {
    world.registerSystem(system);
}

// --- CREATING ENTITIES & RELATIONSHIPS ---
console.log("\n--- Creating Entities & Relationships ---");
const playerEntity = world.createEntity();
world.addComponent(playerEntity, new Player());
world.addComponent(playerEntity, new Position(10, 10));
world.addComponent(playerEntity, new Renderable("P", "cyan"));
world.addComponent(playerEntity, "Mana");

const followerEntity = world.createEntity();
world.addComponent(followerEntity, new Follower());
world.addComponent(followerEntity, new Position(5, 5));
world.addComponent(followerEntity, new Renderable("F", "yellow"));

const allyEntity = world.createEntity();
world.addComponent(allyEntity, new Ally());
world.addComponent(allyEntity, new Position(15, 15));
world.addComponent(allyEntity, new Renderable("A", "lime"));

// Use the new, explicit API to create both types of relationships.
// 1. A directed relationship: The player leads the follower.
world.addDirectedRelationship(playerEntity, followerEntity, {
    type: "leaderOf",
});
console.log(
    `Created DIRECTED relationship: Player (${playerEntity}) -> Follower (${followerEntity})`
);

// 2. An undirected relationship: The player is allied with the ally.
world.addUndirectedRelationship(playerEntity, allyEntity, {
    type: "alliedWith",
});
console.log(
    `Created UNDIRECTED relationship: Player (${playerEntity}) <-> Ally (${allyEntity})`
);

// --- RUNNING THE SIMULATION ---
console.log("\n--- Starting Simulation Loop ---");
world.update(0.16); // Run a single frame

// --- SERIALIZATION DEMO ---
function runSerializationDemo() {
    console.log("\n\n--- Running Serialization Demo ---");

    const worldState = world.toJSON();

    console.log("\n--- Deserializing into a new World ---");

    const staticComponents = [Position, Renderable, Player, Follower, Ally];

    const newWorld = World.fromJSON(worldState, {
        systems: systemsToRegister,
        staticComponents: staticComponents,
    });

    console.log(
        "\n--- Running first update on the NEW (deserialized) world ---"
    );
    newWorld.update(0.16);
    console.log("--- Serialization Demo Complete ---");
}

// Run the demo after a short delay
setTimeout(() => {
    runSerializationDemo();
}, 500);
