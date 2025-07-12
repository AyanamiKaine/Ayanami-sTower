import {
    WorldStateProxy,
    CompoundTask,
    Planner,
    PlanExecutor,
    PrimitiveTask,
    InterruptionManager,
    smartActionLibrary, 
    SmartObjectTask,
    TaskDiscoverer,
} from "./main.js";

// =============================================================================
// 1. DEFINE SMART ACTION LOGIC
//    This is where you define the *behavior* of smart actions.
// =============================================================================
console.log("--- Defining Smart Action Logic ---");

smartActionLibrary.VaultThroughWindow = {
    // `obj` is the specific smart object instance from the world
    conditions: (ws, ctx, obj) => !obj.isBroken,
    effects: (ws, ctx, obj) => {
        // Update the agent's position
        ws.set("agentPosition", obj.endsAt);
        // Modify the state of the specific smart object in the world
        const windows = ws
            .get("worldObjects")
            .filter((o) => o.type === "Window");
        const windowToBreak = windows.find((w) => w.id === obj.id);
        if (windowToBreak) {
            windowToBreak.isBroken = true;
        }
    },
    operator: (ctx) => {
        console.log(
            `[OPERATOR] Agent ${ctx.agent.id} plays animation to vault through window ${ctx.smartObject.id}`
        );
    },
};

smartActionLibrary.PickupWeapon = {
    // Agent can only pick up the weapon if they don't have one and the weapon is available
    conditions: (ws, ctx, obj) => !ws.get("agentHasWeapon") && !obj.isEquipped,
    effects: (ws, ctx, obj) => {
        ws.set("agentHasWeapon", true);
        const weapons = ws
            .get("worldObjects")
            .filter((o) => o.type === "Weapon");
        const weaponToEquip = weapons.find((w) => w.id === obj.id);
        if (weaponToEquip) {
            weaponToEquip.isEquipped = true;
        }
    },
    operator: (ctx) => {
        console.log(
            `[OPERATOR] Agent ${ctx.agent.id} picks up weapon ${ctx.smartObject.id}`
        );
    },
};

// =============================================================================
// 2. SETUP THE WORLD
// =============================================================================
let gameState = {
    agentPosition: "Hallway",
    agentHasWeapon: false, // Agent starts without a weapon
    enemyVisible: false,
    enemyPosition: "Courtyard",
    enemyDefeated: false,
    worldObjects: [
        {
            id: "window_01",
            type: "Window",
            isBroken: false,
            endsAt: "Courtyard",
            smartLink: {
                actionType: "VaultThroughWindow",
            },
        },
        {
            // --- NEW WEAPON OBJECT ---
            id: "scattergun_01",
            type: "Weapon",
            isEquipped: false,
            smartLink: {
                actionType: "PickupWeapon",
            },
        },
    ],
};
const createWorldStateProxy = (initialState) => {
    let state = structuredClone(initialState);
    return new WorldStateProxy({
        getState: (key) => (key ? state[key] : state),
        setState: (key, value) => (state[key] = value),
        clone: () => createWorldStateProxy(state),
    });
};

// =============================================================================
// 3. DEFINE THE AI's TASKS
// =============================================================================

const attackEnemy = new PrimitiveTask("AttackEnemy", {
    conditions: (ws) => ws.get("agentPosition") === ws.get("enemyPosition"),
    effects: (ws) => ws.set("enemyDefeated", true),
    operator: () => console.log("[OPERATOR] Agent is attacking!"),
});

const patrol = new PrimitiveTask("Patrol", {
    operator: () => console.log("[OPERATOR] Agent is patrolling..."),
});

// This is the key part: The method's subtask refers to a *dynamically generated* task name.
const getInRange = new CompoundTask("GetInRange", [
    {
        name: "Vault through window to reach enemy",
        conditions: (ws) => ws.get("agentPosition") !== ws.get("enemyPosition"),
        // The planner will find the task named "VaultThroughWindow_window_01"
        // which was created by the TaskDiscoverer.
        subtasks: ["VaultThroughWindow_window_01"],
    },
]);

const rootBehavior = new CompoundTask("RootBehavior", [
    {
        name: "Get Weapon if Needed",
        priority: 20, // Highest priority
        conditions: (ws) => ws.get("enemyVisible") && !ws.get("agentHasWeapon"),
        subtasks: ["PickupWeapon_scattergun_01"], // Task name is dynamically generated
    },
    {
        name: "Engage Enemy",
        priority: 10,
        conditions: (ws) => ws.get("enemyVisible") && ws.get("agentHasWeapon"),
        subtasks: [getInRange, attackEnemy],
    },
    {
        name: "Patrol if Idle",
        priority: 1,
        subtasks: [patrol],
    },
]);
// =============================================================================
// 4. RUN THE PLANNER
// =============================================================================

async function runSimulation() {
    console.log("--- 🚀 STARTING SIMULATION ---");

    const planner = new Planner();
    planner.registerTask(attackEnemy);
    planner.registerTask(patrol);
    planner.registerTask(getInRange);
    planner.registerTask(rootBehavior);

    let worldState = createWorldStateProxy(gameState);
    let context = {
        agent: {
            id: "guard_1",
        },
    };

    console.log(
        "\n--- Scenario 1: Enemy is not visible. AI should patrol. ---"
    );
    let result1 = planner.findPlan({
        tasks: rootBehavior,
        worldState,
        context,
    });
    console.log(
        "Plan found:",
        result1?.plan.map((t) => t.name) // .plan.map expression expected ERROR
    ); // Expected: ['Patrol'] //  expected : ERROR

    console.log(
        "\n--- Scenario 2: Enemy becomes visible. AI should find a path and attack. ---"
    );
    worldState.set("enemyVisible", true);
    let result2 = planner.findPlan({
        tasks: rootBehavior,
        worldState,
        context,
    });
    console.log(
        "Plan found:",
        result2?.plan.map((t) => t.name) // .plan.map expression expected ERROR
    ); // Expected: ['VaultThroughWindow_window_01', 'AttackEnemy'] //  expected : ERROR

    if (result2) {
        console.log(
            "\n--- Executing Full Plan (Get Weapon -> Vault -> Attack) ---"
        );
        const executionWorld = createWorldStateProxy(
            structuredClone(gameState)
        );
        executionWorld.set("enemyVisible", true); // Make sure enemy is visible for execution

        const executor = new PlanExecutor(result2.plan, result2.context);
        while (!executor.isDone()) {
            // In a real game, each tick would take time. Here we just loop.
            executor.tick(executionWorld);
        }
        console.log("--- Execution Complete ---");
        console.log("Final World State:", executionWorld.get());
    }
}

runSimulation();
