// example.js (Complete and Corrected)

import {
    WorldStateProxy,
    PrimitiveTask,
    CompoundTask,
    Planner,
    InterruptionManager,
    PlanExecutor,
    InterruptorFactory,
} from "./main.js";

const smartActionLibrary = {
    VaultThroughWindow: {
        preconditions: (ws, obj) => !obj.isBroken,
        effects: (ws, obj) => {
            const guard = ws.get("guard");
            ws.set("guard", { ...guard, position: "Behind Window" });
            // Find the object in the actual world state to modify it
            const worldObj = ws
                .get("worldObjects")
                .find((o) => o.id === obj.id);
            if (worldObj) worldObj.isBroken = true;
        },
        operator: (ctx) =>
            console.log(
                `[OPERATOR] Playing animation to vault through ${ctx.smartObject.id}`
            ),
    },
    // You could add more smart actions here, like 'FlipTableForCover', etc.
};

// =============================================================================
// 1. SETUP THE WORLD
// =============================================================================
let gameState = {
    guard: {
        id: "guard_01",
        name: "Stompy",
        health: 100,
        position: "Point A",
        isFleeing: false,
    },
    enemy: {
        id: "enemy_01",
        name: "Goblin",
        isVisible: false,
        isDefeated: false,
        position: "Behind Window",
    },
    worldObjects: [
        {
            id: "window_01",
            type: "Window",
            isBroken: false,
            // The smartLink is now just simple data!
            smartLink: {
                actionType: "VaultThroughWindow",
            },
        },
    ],
};

const createWorldStateProxy = (initialState) => {
    let state = structuredClone(initialState);
    return new WorldStateProxy({
        getState: (key) => (key ? state[key] : state),
        setState: (key, value) => {
            console.log(
                `[WORLD] State changed: ${key} = ${JSON.stringify(value)}`
            );
            state[key] = value;
        },
        clone: () => createWorldStateProxy(state),
    });
};
const worldStateProxy = createWorldStateProxy(gameState);

// =============================================================================
// 2. DEFINE THE AI's "VOCABULARY" (TASKS)
// =============================================================================
console.log("--- Defining AI Primitives ---");
const patrolToPointA = new PrimitiveTask("Patrol to Point A", {
    effects: (ws) =>
        ws.set("guard", { ...ws.get("guard"), position: "Point A" }),
});
const patrolToPointB = new PrimitiveTask("Patrol to Point B", {
    effects: (ws) =>
        ws.set("guard", { ...ws.get("guard"), position: "Point B" }),
});
const attackEnemy = new PrimitiveTask("Attack Enemy", {
    conditions: (ws) =>
        ws.get("enemy").isVisible &&
        !ws.get("enemy").isDefeated &&
        ws.get("guard").position === ws.get("enemy").position,
    operator: (ctx) => {
        ctx.guardTookDamage = (Math.random() * 25) | 0;
    },
    effects: (ws, ctx) => {
        if (ctx.guardTookDamage) {
            const guard = ws.get("guard");
            ws.set("guard", {
                ...guard,
                health: guard.health - ctx.guardTookDamage,
            });
            delete ctx.guardTookDamage;
        }
        if (Math.random() > 0.5) {
            ws.set("enemy", { ...ws.get("enemy"), isDefeated: true });
        }
    },
});
const fleeToSafety = new PrimitiveTask("Flee to Safety", {
    conditions: (ws) => !ws.get("guard").isFleeing,
    effects: (ws) => {
        ws.set("guard", {
            ...ws.get("guard"),
            position: "Safe Zone",
            isFleeing: true,
        });
    },
});
const standAlert = new PrimitiveTask("Stand Alert", {});

// NO LONGER NEEDED: const useSmartObject = new PrimitiveTask(...)

console.log("--- Defining AI Compound Tasks ---");
const patrolRoute = new CompoundTask("Patrol Route", [
    {
        name: "Go to B",
        conditions: (ws) => ws.get("guard").position === "Point A",
        subtasks: [patrolToPointB],
    },
    {
        name: "Go to A",
        conditions: (ws) => ws.get("guard").position === "Point B",
        subtasks: [patrolToPointA],
    },
]);

const respondToThreat = new CompoundTask("Respond to Threat", [
    {
        name: "Flee When Health Is Low",
        priority: 200,
        conditions: (ws) => ws.get("guard").health < 40,
        subtasks: [fleeToSafety],
    },
    // This method now handles smart objects!
    {
        name: "Get In Range of Enemy",
        priority: 100,
        conditions: (ws) =>
            ws.get("guard").position !== ws.get("enemy").position,
        // The subtask is now the DYNAMICALLY discovered task name.
        subtasks: ["VaultThroughWindow"],
    },
    {
        name: "Engage Visible Enemy",
        priority: 50, // Lower priority than moving into range.
        conditions: (ws) => ws.get("enemy").isVisible,
        subtasks: [attackEnemy],
    },
]);

const guardBehavior = new CompoundTask("Guard Behavior", [
    {
        name: "Stand Alert After Victory",
        priority: 100,
        conditions: (ws) =>
            ws.get("enemy").isDefeated && !ws.get("guard").isFleeing,
        subtasks: [standAlert],
    },
    {
        name: "Respond to Threat If Necessary",
        priority: 10,
        conditions: (ws) =>
            ws.get("enemy").isVisible && !ws.get("enemy").isDefeated,
        subtasks: ["Respond to Threat"],
    },
    {
        name: "Patrol If Safe",
        priority: 1,
        conditions: (ws) => !ws.get("enemy").isVisible,
        subtasks: ["Patrol Route"],
    },
]);

// =============================================================================
// 3. DEFINE THE TASK DISCOVERY FUNCTION - REVISED
// =============================================================================

/**
 * Scans the world and creates unique, fully-formed PrimitiveTasks from Smart Objects.
 * This is simpler and more robust than a generic handler.
 */
function discoverTasksFromSmartObjects(worldState) {
    const discoveredTasks = [];
    const worldObjects = worldState.get("worldObjects") || [];

    for (const obj of worldObjects) {
        if (obj.smartLink && obj.smartLink.actionType) {
            const actionType = obj.smartLink.actionType;
            // Look up the logic in our new library
            const actionLogic = smartActionLibrary[actionType];

            if (actionLogic) {
                // Create a NEW, specific primitive task from the object's data
                const newAction = new PrimitiveTask(actionType, {
                    // Name the task after its type
                    // The functions now correctly reference the actionLogic and pass the specific object 'obj'
                    conditions: (ws, ctx) => actionLogic.preconditions(ws, obj),
                    effects: (ws, ctx) => actionLogic.effects(ws, obj),
                    operator: (ctx) => {
                        // We can add the specific object to the context for the operator if needed
                        ctx.smartObject = obj;
                        actionLogic.operator(ctx);
                    },
                });
                discoveredTasks.push(newAction);
            }
        }
    }
    return discoveredTasks;
}

// =============================================================================
// 4. CONFIGURE PLANNER AND RUN SIMULATION - REVISED
// =============================================================================
const planner = new Planner();
planner.registerTask(patrolRoute);
planner.registerTask(respondToThreat); // We must register this so it can be found by name.
planner.registerTask(guardBehavior);

// The rest of the file (InterruptionManager, runSimulation) can remain the same!
// The simulation loop already calls the discovery function. Now that the
// discovery function is more robust, the whole system will work.

// ... (paste the InterruptionManager and runSimulation function from the previous step here)
function simpleLog(type, message, data = {}) {
    const icon = {
        success: "✅",
        info: "ℹ️",
        warn: "⚠️",
        error: "❌",
        executing: "▶️",
        replanned: "🔄",
        completed: "🏁",
    };
    console.log(`[${icon[type] || "🤖"}] ${message}`, data);
}
async function runSimulation() {
    console.log("\n\n--- 🚀 STARTING AI SIMULATION ---");
    simpleLog("info", "Finding initial plan...");
    const initialResult = planner.findPlan({
        tasks: guardBehavior,
        worldState: worldStateProxy,
        context: {},
    });
    if (!initialResult) {
        simpleLog("error", "Could not find any initial plan!");
        return;
    }
    simpleLog("success", "Initial plan found!", {
        plan: initialResult.plan.map((t) => t.name),
    });

    let executor = new PlanExecutor(initialResult.plan, initialResult.context, {
        planner,
        rootTask: guardBehavior,
        interruptionManager: new InterruptionManager(),
        logCallback: simpleLog,
    });

    const maxTicks = 10;
    const world = worldStateProxy;

    for (let tickCount = 1; tickCount <= maxTicks; tickCount++) {
        await new Promise((resolve) => setTimeout(resolve, 500));
        console.log(`\n--- Tick ${tickCount} ---`);

        if (tickCount === 2) {
            // Made the enemy appear sooner to see the new logic
            simpleLog("warn", "WORLD EVENT: An enemy has appeared!");
            world.set("enemy", { ...world.get("enemy"), isVisible: true });
        }

        if (executor.isDone()) {
            if (executor.state === "failed") {
                simpleLog(
                    "error",
                    "Executor failed. Ending simulation.",
                    executor.lastInterruption
                );
                break;
            }
            if (executor.state === "completed") {
                simpleLog(
                    "completed",
                    "Plan finished. AI is thinking what to do next..."
                );

                const discoveredTasks = discoverTasksFromSmartObjects(world);
                if (discoveredTasks.length > 0) {
                    simpleLog("info", "Discovered new smart object actions!", {
                        actions: discoveredTasks.map((t) => t.name),
                    });
                }
                for (const task of discoveredTasks) {
                    planner.registerTask(task);
                }

                const nextPlanResult = planner.findPlan({
                    tasks: guardBehavior,
                    worldState: world,
                });

                if (nextPlanResult && nextPlanResult.plan.length > 0) {
                    const newPlanName = nextPlanResult.plan[0].name;
                    if (newPlanName === "Stand Alert") {
                        simpleLog(
                            "info",
                            "AI has entered a terminal 'Stand Alert' state. Ending simulation."
                        );
                        executor.tick(world);
                        break;
                    }

                    simpleLog("success", "New plan found!", {
                        plan: nextPlanResult.plan.map((t) => t.name),
                    });
                    executor = new PlanExecutor(
                        nextPlanResult.plan,
                        nextPlanResult.context,
                        {
                            planner,
                            rootTask: guardBehavior,
                            interruptionManager: new InterruptionManager(),
                            logCallback: simpleLog,
                        }
                    );
                } else {
                    simpleLog(
                        "info",
                        "AI has no further plans. Ending simulation."
                    );
                    break;
                }
            }
        }

        simpleLog("info", "Executor is ticking...");
        const tickResult = executor.tick(world, { enableReplanning: true });

        if (tickResult.status !== "executing") {
            simpleLog(
                tickResult.status,
                `Tick complete. Status: ${tickResult.status}`,
                tickResult
            );
        }
    }

    simpleLog("info", "--- 🏁 SIMULATION ENDED ---");
    simpleLog("info", "Final World State:", world.get());
}

runSimulation();
