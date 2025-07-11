// example.js

import {
    WorldStateProxy,
    PrimitiveTask,
    CompoundTask,
    Planner,
    InterruptionManager,
    PlanExecutor,
    InterruptorFactory,
} from "./main.js"; // Adjust the path if needed

// =============================================================================
// 1. SETUP THE WORLD
// =============================================================================

// Our "game world" is a simple JavaScript object.
// The planner will read from and write to this state.
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
    },
    lastKnownSoundLocation: null,
};

// The WorldStateProxy is the bridge between the planner and our game world.
// We provide functions for getting, setting, and cloning the state.
const createWorldStateProxy = (initialState) => {
    let state = structuredClone(initialState);
    return new WorldStateProxy({
        getState: (key) => (key ? state[key] : state),
        setState: (key, value) => {
            // A simple logger to see when the world changes
            console.log(
                `[WORLD] State changed: ${key} = ${JSON.stringify(value)}`
            );
            state[key] = value;
        },
        // structuredClone is a modern, fast way to deep-clone state.
        clone: () => createWorldStateProxy(state),
    });
};

const worldStateProxy = createWorldStateProxy(gameState);

// =============================================================================
// 2. DEFINE THE AI's "VOCABULARY" (TASKS)
// =============================================================================
// These are the basic actions the AI knows how to perform.

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
        ws.get("enemy").isVisible && !ws.get("enemy").isDefeated,
    // The operator is the "real" action in the game engine (e.g., play animation).
    // The effect is how it changes the world state for planning purposes.
    operator: (ctx) => {
        // In a real game, you might check for a hit here.
        // For this example, we'll just reduce the guard's health as if they are trading blows.
        ctx.guardTookDamage = (Math.random() * 25) | 0; // Random damage between 0-24
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
        // Let's say there's a 50% chance to defeat the enemy each turn
        if (Math.random() > 0.5) {
            ws.set("enemy", { ...ws.get("enemy"), isDefeated: true });
        }
    },
});

const fleeToSafety = new PrimitiveTask("Flee to Safety", {
    conditions: (ws) => !ws.get("guard").isFleeing,
    effects: (ws) => {
        const guard = ws.get("guard");
        ws.set("guard", { ...guard, position: "Safe Zone", isFleeing: true });
    },
});

// Now, we define high-level, compound tasks.
console.log("--- Defining AI Compound Tasks ---");

// A compound task for the patrol route.
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

// A compound task for how to react to a threat.
const respondToThreat = new CompoundTask("Respond to Threat", [
    {
        name: "Flee When Health Is Low",
        priority: 200, // Very high priority: survival first!
        conditions: (ws) => ws.get("guard").health < 40,
        subtasks: [fleeToSafety],
    },
    {
        name: "Engage Visible Enemy",
        priority: 100,
        conditions: (ws) => ws.get("enemy").isVisible,
        subtasks: [attackEnemy],
    },
]);

const standAlert = new PrimitiveTask("Stand Alert", {
    // This is a terminal task. The AI does nothing, having won.
    // In a real game, it might play an idle animation.
});

// This is the AI's "root" task. It defines the guard's overall behavior.
const guardBehavior = new CompoundTask("Guard Behavior", [
    {
        name: "Stand Alert After Victory",
        priority: 100, // This is the most important thing to do. If the enemy is beaten, we're done.
        conditions: (ws) =>
            ws.get("enemy").isDefeated && !ws.get("guard").isFleeing,
        subtasks: [standAlert],
    },
    {
        name: "Respond to Threat If Necessary",
        priority: 10, // Responding to threats is more important than patrolling.
        conditions: (ws) =>
            ws.get("enemy").isVisible && !ws.get("enemy").isDefeated,
        subtasks: ["Respond to Threat"],
    },
    {
        name: "Patrol If Safe",
        priority: 1, // The default, low-priority action.
        conditions: (ws) => !ws.get("enemy").isVisible,
        subtasks: ["Patrol Route"],
    },
]);

// =============================================================================
// 3. CONFIGURE THE PLANNER & INTERRUPTION MANAGER
// =============================================================================
console.log("--- Configuring Planner and Interruptions ---");

const planner = new Planner();

// Register all tasks so the planner knows about them, especially when referenced by name.
planner.registerTask(patrolRoute);
planner.registerTask(respondToThreat);
planner.registerTask(guardBehavior);
// Primitives can also be registered if needed, but are often used directly.

const interruptionManager = new InterruptionManager();

// Interruptor 1: A high-priority "Oh Crap!" moment when an enemy appears.
interruptionManager.registerInterruptor("enemy-spotted", {
    check: (worldState, context, currentTask) => {
        const enemy = worldState.get("enemy");

        // The enemy is only an "interruption" if it's a NEW and ACTIVE threat.
        // Do not interrupt if the threat is already handled (defeated) or
        // if we are already in the process of fleeing.
        if (enemy.isDefeated || worldState.get("guard").isFleeing) {
            return null; // Not an active threat, so no interruption.
        }

        // Only interrupt if the enemy is visible AND we are doing something
        // that isn't already a reaction to the enemy (like patrolling).
        const isUnaware = currentTask.name.includes("Patrol");
        if (enemy.isVisible && isUnaware) {
            return { interrupted: true, reason: "ENEMY_SPOTTED_WHILE_UNAWARE" };
        }

        return null; // No interruption needed.
    },
});

// Interruptor 2: A check to see if we should flee.
// We use the InterruptorFactory for a clean, reusable pattern.
const fleeInterruptor = InterruptorFactory.createTaskSpecificInterruptor(
    ["Attack Enemy"], // Only check this condition while we are actively attacking.
    (worldState) => {
        if (worldState.get("guard").health < 40) {
            return {
                interrupted: true,
                reason: "HEALTH_CRITICAL",
                message: `Health is ${
                    worldState.get("guard").health
                }, need to flee!`,
            };
        }
        return null;
    }
);
interruptionManager.registerInterruptor("health-critical", fleeInterruptor);

// =============================================================================
// 4. RUN THE SIMULATION
// =============================================================================

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
    // Minor tweak to use a different icon for 'completed'
    console.log(`[${icon[type] || "🤖"}] ${message}`, data);
}

async function runSimulation() {
    console.log("\n\n--- 🚀 STARTING AI SIMULATION ---");
    simpleLog("info", "Finding initial plan...");

    // --- Initial Planning ---
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

    // --- Setup the Executor ---
    let executor = new PlanExecutor(initialResult.plan, initialResult.context, { // Use 'let' so we can reassign it.
        planner,
        rootTask: guardBehavior,
        interruptionManager,
        logCallback: simpleLog,
    });

    // --- Simulation Loop ---
    const maxTicks = 5; // Increased ticks to see more behavior
    const world = worldStateProxy;

    for (let tickCount = 1; tickCount <= maxTicks; tickCount++) {
        await new Promise((resolve) => setTimeout(resolve, 500));
        console.log(`\n--- Tick ${tickCount} ---`);

        // --- WORLD EVENTS ---
        if (tickCount === 3) {
            simpleLog("warn", "WORLD EVENT: An enemy has appeared!");
            world.set("enemy", { ...world.get("enemy"), isVisible: true });
        }

        // --- THE NEW CONTINUOUS PLANNING LOGIC ---
        if (executor.isDone()) {
            if (executor.state === "failed") {
                simpleLog(
                    "error",
                    "Executor failed. Ending simulation.",
                    executor.lastInterruption
                );
                break;
            }

            // If the plan was completed, the AI should decide what to do next.
            if (executor.state === "completed") {
                simpleLog(
                    "completed",
                    "Plan finished. AI is thinking what to do next..."
                );

                const nextPlanResult = planner.findPlan({
                    tasks: guardBehavior,
                    worldState: world, // Use the current world state for the new plan
                });

                if (nextPlanResult && nextPlanResult.plan.length > 0) {
                    // If the AI decides on a new course of action, create a new executor for it.
                    simpleLog("success", "New plan found!", {
                        plan: nextPlanResult.plan.map((t) => t.name),
                    });
                    executor = new PlanExecutor(
                        nextPlanResult.plan,
                        nextPlanResult.context,
                        {
                            planner,
                            rootTask: guardBehavior,
                            interruptionManager,
                            logCallback: simpleLog,
                        }
                    );
                } else {
                    // If no plan is returned, the AI has no more valid actions.
                    simpleLog(
                        "info",
                        "AI has no further plans. Ending simulation."
                    );
                    break;
                }
            }
        }

        // If the AI is still executing its current plan, just tick.
        simpleLog("info", "Executor is ticking...");
        const tickResult = executor.tick(world, { enableReplanning: true });

        // Log the outcome of the tick unless it was a boring 'executing' status
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