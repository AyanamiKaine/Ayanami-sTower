import { World, System } from './index.js';

// --- STATIC (PRE-DEFINED) COMPONENTS ---
class Position { constructor(x = 0, y = 0) { this.x = x; this.y = y; } }
class Renderable { constructor(char = '?', color = 'white') { this.char = char; this.color = color; } }
class Player { }

// --- STATIC (PRE-DEFINED) SYSTEM ---
class RenderSystem extends System {
    constructor() {
        super([Position, Renderable]);
    }

    execute(world, archetypes, deltaTime) {
        console.log(`RenderSystem is processing ${archetypes.length} archetypes:`);

        for (const archetype of archetypes) {
            const positions = archetype.componentArrays.get(Position);
            const renderables = archetype.componentArrays.get(Renderable);

            for (let i = 0; i < archetype.entityList.length; i++) {
                const entityId = archetype.entityList[i];
                const pos = positions[i];
                const render = renderables[i];

                const isPlayer = world.getComponent(entityId, Player);
                const hasMana = world.getComponent(entityId, 'Mana');

                let playerTag = isPlayer ? '[PLAYER]' : '';
                let manaTag = hasMana ? `[MANA: ${hasMana.value.toFixed(0)}]` : '';

                console.log(`  -> ${render.char} ${playerTag} at (${pos.x.toFixed(2)}, ${pos.y.toFixed(2)}) ${manaTag}`);
            }
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
const manaRegenSystemLogic = `
    for (const archetype of archetypes) {
        const ManaClass = world.getComponentClassByName('Mana');
        const PositionClass = world.getComponentClassByName('Position');

        if (!ManaClass || !PositionClass) continue;

        const manas = archetype.componentArrays.get(ManaClass);
        const positions = archetype.componentArrays.get(PositionClass);

        for (let i = 0; i < archetype.entityList.length; i++) {
            const mana = manas[i];
            if (mana.value < mana.max) {
                mana.value += 5 * deltaTime;
            }
            const pos = positions[i];
            pos.y += Math.sin(mana.value / 10) * 0.1;
        }
    }
`;

const DynamicManaSystem = class extends System {
    constructor() {
        super(['Mana', Position]);
        this.execute = new Function('world', 'archetypes', 'deltaTime', manaRegenSystemLogic);
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
world.update(0);


// --- SERIALIZATION DEMO ---
function runSerializationDemo() {
    console.log('\n\n--- Running Serialization Demo ---');

    // 1. Serialize the current world state to a JSON object
    const worldState = world.toJSON();
    const jsonString = JSON.stringify(worldState, null, 2);
    console.log('Serialized World State:');
    console.log(jsonString);

    // 2. Create a new world from the serialized state
    console.log('\n--- Deserializing into a new World ---');

    // Define the list of static components used in your application
    const staticComponents = [Position, Renderable, Player];

    // Pass the systems and static components to the new world
    const newWorld = World.fromJSON(worldState, {
        systems: systemsToRegister,
        staticComponents: staticComponents
    });

    // 3. Run an update on the new world to prove it was restored
    console.log('\n--- Running first update on the NEW (deserialized) world ---');
    newWorld.update(0);
    console.log('--- Serialization Demo Complete ---');
}


// Run the demo and then exit
setTimeout(() => {
    runSerializationDemo();
    process.exit();
}, 1000);
