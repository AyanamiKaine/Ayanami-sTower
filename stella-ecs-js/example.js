import { World, System } from './index.js';

// --- STATIC (PRE-DEFINED) COMPONENTS ---
class Position { constructor(x = 0, y = 0) { this.x = x; this.y = y; } }
class Renderable { constructor(char = '?', color = 'white') { this.char = char; this.color = color; } }
class Player { }

// --- STATIC (PRE-DEFINED) SYSTEM ---
// This system is now much cleaner and easier to read.
// It doesn't know or care about "archetypes".
class RenderSystem extends System {
    constructor() {
        // Define the components this system operates on.
        super([Position, Renderable]);
    }

    /**
     * @param {QueryResult} entities - An iterable containing all entities that have Position and Renderable components.
     * @param {World} world - The world instance.
     * @param {number} deltaTime - The time since the last frame.
     */
    execute(entities, world, deltaTime) {
        console.log(`RenderSystem is processing ${entities.count} entities:`);

        // The 'for...of' loop is now the primary, ergonomic way to iterate.
        for (const { entity, components } of entities) {
            // Get the components from the pre-fetched map.
            const pos = components.get(Position);
            const render = components.get(Renderable);

            // We can still use the world to get components not in our primary query.
            const isPlayer = world.getComponent(entity, Player);
            const hasMana = world.getComponent(entity, 'Mana');

            let playerTag = isPlayer ? '[PLAYER]' : '';
            let manaTag = hasMana ? `[MANA: ${hasMana.value.toFixed(0)}]` : '';

            console.log(`  -> ${render.char} ${playerTag} at (${pos.x.toFixed(2)}, ${pos.y.toFixed(2)}) ${manaTag}`);
        }
    }
}

// --- SIMULATION SETUP ---
console.log('--- Initializing ECS World ---');
const world = new World();

// --- DYNAMIC DEFINITION ---
console.log('\n--- Defining Components Dynamically ---');
world.componentFactory.define('Mana', { value: 100, max: 150 });
world.componentFactory.define('Health', { value: 80, max: 100 });

// --- DYNAMIC SYSTEM DEFINITION ---
// The logic for dynamic systems is also vastly simplified.
const manaRegenSystemLogic = `
    // Get component classes once from the world for efficiency.
    const ManaClass = world.getComponentClassByName('Mana');
    const PositionClass = world.getComponentClassByName('Position');

    // 'entities' is the iterable QueryResult.
    // The system author only needs to know about this simple loop.
    for (const { components } of entities) {
        const mana = components.get(ManaClass);
        const pos = components.get(PositionClass);

        // It's good practice to ensure components exist before using them.
        if (mana && pos) {
            if (mana.value < mana.max) {
                mana.value += 5 * deltaTime;
            }
            // Just a little effect to show it's working
            pos.y += Math.sin(mana.value / 10) * 0.1;
        }
    }
`;

const DynamicManaSystem = class extends System {
    constructor() {
        super(['Mana', Position]);
        // The signature of the new Function must match what System.update provides.
        this.execute = new Function('entities', 'world', 'deltaTime', manaRegenSystemLogic);
    }
}

// --- REGISTERING SYSTEMS ---
console.log('\n--- Registering Systems ---');
const systemsToRegister = [new RenderSystem(), new DynamicManaSystem()];
for (const system of systemsToRegister) {
    world.registerSystem(system);
}


// --- CREATING ENTITIES ---
console.log('\n--- Creating Entities ---');
const playerEntity = world.createEntity();
world.addComponent(playerEntity, new Player());
world.addComponent(playerEntity, new Position(10, 10));
world.addComponent(playerEntity, new Renderable('P', 'cyan'));
world.addComponent(playerEntity, 'Mana');
world.addComponent(playerEntity, 'Health', { value: 95 });

const staticEntity = world.createEntity();
world.addComponent(staticEntity, new Position(25, 25));
world.addComponent(staticEntity, new Renderable('X', 'red'));

// --- RUNNING THE SIMULATION ---
console.log('\n--- Starting Simulation Loop ---');
world.update(0.16); // Run a single frame


// --- SERIALIZATION DEMO ---
function runSerializationDemo() {
    console.log('\n\n--- Running Serialization Demo ---');

    // 1. Serialize the current world state to a JSON object
    const worldState = world.toJSON();
    const jsonString = JSON.stringify(worldState, null, 2);
    console.log('Serialized World State:');
    // console.log(jsonString); // Keep this commented for cleaner output unless debugging

    // 2. Create a new world from the serialized state
    console.log('\n--- Deserializing into a new World ---');

    const staticComponents = [Position, Renderable, Player];

    const newWorld = World.fromJSON(worldState, {
        systems: systemsToRegister,
        staticComponents: staticComponents
    });

    // 3. Run an update on the new world to prove it was restored
    console.log('\n--- Running first update on the NEW (deserialized) world ---');
    newWorld.update(0.16);
    console.log('--- Serialization Demo Complete ---');
}


// Run the demo after a short delay
setTimeout(() => {
    runSerializationDemo();
}, 500);
