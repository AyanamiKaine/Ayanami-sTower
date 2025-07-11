import {
    CompoundTask,
    PlanExecutor,
    Planner,
    PrimitiveTask,
    WorldStateProxy,
} from "./main";

// Helper for adding delays in the command line execution
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

// 1. Define Primitive Tasks (The hero's basic actions)
const getSword = new PrimitiveTask("Get Sword", {
    conditions: (ws) => ws.get("heroLocation") === "village",
    effects: (ws) => ws.set("hasSword", true),
    operator: () => console.log("✅ Hero acquired the Blade of Legends!"),
});
const getShield = new PrimitiveTask("Get Shield", {
    conditions: (ws) => ws.get("heroLocation") === "village",
    effects: (ws) => ws.set("hasShield", true),
    operator: () => console.log("✅ Hero acquired the Aegis of Valor!"),
});
const travelToCave = new PrimitiveTask("Travel to Cave", {
    effects: (ws) => ws.set("heroLocation", "cave"),
    operator: () => console.log("🏃 Hero travels to the Whispering Cave..."),
});
const findKey = new PrimitiveTask("Find Key", {
    conditions: (ws) => ws.get("heroLocation") === "cave",
    effects: (ws) => ws.set("hasKey", true),
    operator: () => console.log("🔑 Hero found the Dragon's Crest Key!"),
});
const travelToLair = new PrimitiveTask("Travel to Lair", {
    effects: (ws) => ws.set("heroLocation", "lair"),
    operator: () => console.log("🏃 Hero travels to the Dragon's Lair..."),
});
const unlockLair = new PrimitiveTask("Unlock Lair", {
    conditions: (ws) => ws.get("heroLocation") === "lair" && ws.get("hasKey"),
    effects: (ws) => ws.set("lairUnlocked", true),
    operator: () => console.log("🔓 The great door to the lair creaks open!"),
});
const fightDragon = new PrimitiveTask("Fight Dragon", {
    conditions: (ws) =>
        ws.get("lairUnlocked") && ws.get("hasSword") && ws.get("hasShield"),
    effects: (ws) => ws.set("dragonIsAngry", true),
    operator: () =>
        console.log("⚔️ Hero engages the mighty dragon in fierce combat!"),
});
const deliverFinalBlow = new PrimitiveTask("Deliver Final Blow", {
    conditions: (ws) => ws.get("dragonIsAngry"),
    effects: (ws) => ws.set("dragonDefeated", true),
    operator: () =>
        console.log(
            "💥 With a final, heroic strike, the dragon is vanquished!"
        ),
});

// 2. Define Compound Tasks (High-level goals)
const getEquipped = new CompoundTask("Get Equipped", [
    { name: "Get Sword and Shield", subtasks: [getSword, getShield] },
]);
const enterLair = new CompoundTask("Enter Lair", [
    {
        name: "Find key and unlock",
        subtasks: [travelToCave, findKey, travelToLair, unlockLair],
    },
]);
const defeatDragon = new CompoundTask("Defeat the Dragon", [
    {
        name: "Default Strategy",
        subtasks: [getEquipped, enterLair, fightDragon, deliverFinalBlow],
    },
]);

// --- SIMULATION LOGIC ---
async function main() {
    console.log("📜 A new hero's quest begins...\n");

    // 3. Setup Planner and World State
    const planner = new Planner();
    planner.setLogCallback((type, message) => {
        // We can optionally log planner-specific messages
        // console.log(`[PLANNER-${type.toUpperCase()}] ${message}`);
    });

    // Register all tasks so the planner knows about them
    [
        getSword,
        getShield,
        travelToCave,
        findKey,
        travelToLair,
        unlockLair,
        fightDragon,
        deliverFinalBlow,
        getEquipped,
        enterLair,
        defeatDragon,
    ].forEach((t) => planner.registerTask(t));

    // Define the initial state of the world
    const initialWorldState = {
        hasSword: false,
        hasShield: false,
        hasKey: false,
        lairUnlocked: false,
        dragonIsAngry: false,
        dragonDefeated: false,
        heroLocation: "village",
    };

    // A factory function to create a proxy. This makes cloning much cleaner.
    const createProxy = (stateObject) => {
        return new WorldStateProxy({
            getState: (key) => (key ? stateObject[key] : stateObject),
            setState: (key, value) => (stateObject[key] = value),
            clone: () => createProxy(structuredClone(stateObject)),
        });
    };

    const worldStateProxy = createProxy(initialWorldState);

    // 4. Find a Plan
    console.log("🤔 The planner is thinking of a grand strategy...");
    const result = planner.findPlan({
        tasks: defeatDragon,
        worldState: worldStateProxy,
    });

    if (!result || result.plan.length === 0) {
        console.log(
            "\n❌ The planner could not devise a strategy. The kingdom is doomed."
        );
        return;
    }

    console.log("\n✨ A plan has been forged!");
    console.log("------------------------------------");
    result.plan.forEach((task, i) => {
        console.log(`${i + 1}. ${task.name}`);
    });
    console.log("------------------------------------");

    // 5. Execute the Plan
    console.log("\n⚔️ The hero begins their adventure!\n");
    const executor = new PlanExecutor(result.plan, result.context);

    while (!executor.isDone()) {
        await sleep(1000); // Wait 1 second between actions
        executor.tick();
    }

    console.log("\n🏆 QUEST COMPLETE! The hero is victorious!");
}

// Run the main simulation function
main();
