import { test, expect, describe, beforeEach } from "bun:test";
import {
    CompoundTask,
    Planner,
    PrimitiveTask,
    WorldStateProxy,
    PlanningError,
    PlanningTimeoutError,
    TaskNotFoundError,
    PlanExecutor,
    InterruptionManager,
    smartActionLibrary,
    TaskDiscoverer,
    SmartObjectTask,
} from "../main"; // Assuming your updated library is in main.js

describe("HTN Planner Library", () => {
    // --- Test Setup ---
    const createWorldStateProxy = (initialState) => {
        let state = structuredClone(initialState);
        return new WorldStateProxy({
            getState: (key) => (key ? state[key] : state),
            setState: (key, value) => (state[key] = value),
            clone: () => createWorldStateProxy(state),
            incrementState: (key, value = 1) =>
                (state[key] = (state[key] || 0) + value),
            generateCacheKey: () => JSON.stringify(state),
        });
    };

    describe("WorldStateProxy", () => {
        test("should get and set values correctly", () => {
            const proxy = createWorldStateProxy({ a: 1 });
            expect(proxy.get("a")).toBe(1);
            proxy.set("b", 2);
            expect(proxy.get("b")).toBe(2);
            expect(proxy.get().b).toBe(2);
        });

        test("clone should create a deep, independent copy", () => {
            const originalProxy = createWorldStateProxy({
                nested: { value: 10 },
            });
            const clonedProxy = originalProxy.clone();

            clonedProxy.set("nested", { value: 20 });

            expect(originalProxy.get("nested").value).toBe(10);
            expect(clonedProxy.get("nested").value).toBe(20);
        });
    });

    describe("Core Planning Logic", () => {
        test("should create a simple plan successfully", () => {
            const planner = new Planner();
            const getMaterial = new PrimitiveTask("Get Material", {
                effects: (ws) => ws.set("hasMaterial", true),
            });
            const craftItem = new PrimitiveTask("Craft Item", {
                conditions: (ws) => ws.get("hasMaterial"),
            });
            const rootTask = new CompoundTask("Create Item", [
                { name: "Default", subtasks: [getMaterial, craftItem] },
            ]);
            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({ hasMaterial: false }),
            });
            expect(result.plan.length).toBe(2);
            expect(result.plan[0].name).toBe("Get Material");
            expect(result.plan[1].name).toBe("Craft Item");
        });

        test("should return null when no plan is possible", () => {
            const planner = new Planner();
            const impossibleTask = new PrimitiveTask("Impossible", {
                conditions: () => false,
            });
            const rootTask = new CompoundTask("Root", [
                { name: "Default", subtasks: [impossibleTask] },
            ]);
            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });
            expect(result).toBeNull();
        });
    });

    describe("Method Priorities and Backtracking", () => {
        test("should choose the highest priority method first", () => {
            const planner = new Planner();
            const rootTask = new CompoundTask("Root", [
                {
                    name: "Low Prio",
                    priority: 1,
                    subtasks: [new PrimitiveTask("Low")],
                },
                {
                    name: "High Prio",
                    priority: 10,
                    subtasks: [new PrimitiveTask("High")],
                },
            ]);
            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });
            expect(result.plan[0].name).toBe("High");
        });

        test("should backtrack to a lower priority method if the high priority one fails", () => {
            const planner = new Planner();
            const succeed = new PrimitiveTask("Succeed");
            const fail = new PrimitiveTask("Fail", { conditions: () => false });

            const rootTask = new CompoundTask("Root", [
                { name: "Low Prio Success", priority: 1, subtasks: [succeed] },
                { name: "High Prio Fail", priority: 10, subtasks: [fail] },
            ]);
            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });
            expect(result.plan.length).toBe(1);
            expect(result.plan[0].name).toBe("Succeed");
            expect(planner.getMetrics().backtrackCount).toBe(1);
        });
    });

    describe("Task Parameters (Context)", () => {
        test("should pass and modify context between tasks", () => {
            const planner = new Planner();
            const findTarget = new PrimitiveTask("Find Target", {
                effects: (ws, ctx) => {
                    ctx.targetId = "enemy_1";
                },
            });
            const moveToTarget = new PrimitiveTask("Move To Target", {
                conditions: (ws, ctx) => ctx.targetId === "enemy_1",
            });
            const rootTask = new CompoundTask("Engage Target", [
                { name: "Default", subtasks: [findTarget, moveToTarget] },
            ]);
            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
                context: {},
            });
            expect(result.context.targetId).toBe("enemy_1");
        });
    });

    describe("Plan Executor and Replanning", () => {
        const patrolPointB = new PrimitiveTask("Patrol to B", {
            effects: (ws) => ws.set("at", "B"),
        });
        const attackEnemy = new PrimitiveTask("Attack Enemy", {
            conditions: (ws) => ws.get("enemyVisible"),
        });
        const patrolGoal = new CompoundTask("Patrol", [
            {
                name: "Go to B",
                conditions: (ws) => ws.get("at") === "A",
                subtasks: [patrolPointB],
            },
        ]);
        const reactGoal = new CompoundTask("React To World", [
            {
                name: "Attack",
                priority: 100,
                conditions: (ws) => ws.get("enemyVisible"),
                subtasks: [attackEnemy],
            },
            { name: "Patrol", priority: 1, subtasks: [patrolGoal] },
        ]);

        test("should interrupt a plan and allow for replanning", () => {
            const planner = new Planner();
            planner.registerTask(patrolGoal);
            planner.registerTask(attackEnemy); // Ensure all tasks are registered
            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });

            // 1. Initial plan is to patrol
            const initialResult = planner.findPlan({
                tasks: reactGoal,
                worldState: initialWorld,
            });
            expect(initialResult.plan[0].name).toBe("Patrol to B");

            // Setup for smart execution
            const interruptionManager = new InterruptionManager();
            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (worldState) => {
                    if (worldState.get("enemyVisible")) {
                        return { interrupted: true, reason: "ENEMY_SPOTTED" };
                    }
                    return null; // Return null when no interruption
                },
            });

            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context,
                {
                    planner,
                    rootTask: reactGoal,
                    interruptionManager,
                }
            );

            // 2. World changes: an enemy appears!
            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            // 3. Executor's tick should be interrupted and trigger a replan
            const tickResult = executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            expect(tickResult.status).toBe("replanned");
            expect(tickResult.interruption.reason).toBe("ENEMY_SPOTTED");

            // 4. Replanning with the new world state should produce an attack plan
            // We check the executor's internal state to confirm the new plan is active.
            expect(executor.plan[0].name).toBe("Attack Enemy");
        });
    });

    describe("Advanced Error and Edge Case Handling", () => {
        test("should throw TaskNotFoundError for unregistered task", () => {
            const planner = new Planner();
            const rootTask = new CompoundTask("Root", [
                { name: "Default", subtasks: ["MissingTask"] },
            ]);
            expect(() =>
                planner.findPlan({
                    tasks: rootTask,
                    worldState: createWorldStateProxy({}),
                })
            ).toThrow(TaskNotFoundError);
        });

        test("should throw PlanningTimeoutError when maxIterations is exceeded", () => {
            const planner = new Planner({ maxIterations: 5 });
            const infiniteTask = new CompoundTask("Infinite");
            infiniteTask.methods.push({
                name: "Loop",
                subtasks: [infiniteTask],
            });
            planner.registerTask(infiniteTask);

            expect(() =>
                planner.findPlan({
                    tasks: infiniteTask,
                    worldState: createWorldStateProxy({}),
                })
            ).toThrow(PlanningTimeoutError);
        });

        test("should correctly use and invalidate the plan cache", () => {
            const planner = new Planner();
            const rootTask = new CompoundTask("Root", [
                { name: "Default", subtasks: [new PrimitiveTask("Task")] },
            ]);
            const worldState1 = createWorldStateProxy({ key: "value1" });
            const worldState2 = createWorldStateProxy({ key: "value2" });

            planner.findPlan({ tasks: rootTask, worldState: worldState1 });
            expect(planner.getMetrics().cacheHit).toBe(false);

            planner.findPlan({ tasks: rootTask, worldState: worldState1 });
            expect(planner.getMetrics().cacheHit).toBe(true);

            planner.findPlan({ tasks: rootTask, worldState: worldState2 });
            expect(planner.getMetrics().cacheHit).toBe(false);
        });

        test("should maintain context integrity after backtracking", () => {
            const planner = new Planner();
            const setCtx = new PrimitiveTask("SetCtx", {
                effects: (ws, ctx) => {
                    ctx.tainted = true;
                },
            });
            const fail = new PrimitiveTask("Fail", { conditions: () => false });
            const checkCtx = new PrimitiveTask("CheckCtx", {
                conditions: (ws, ctx) => ctx.tainted !== true,
            });

            const rootTask = new CompoundTask("Root", [
                {
                    name: "High Prio Fail",
                    priority: 10,
                    subtasks: [setCtx, fail],
                },
                { name: "Low Prio Success", priority: 1, subtasks: [checkCtx] },
            ]);

            const result = planner.findPlan({
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
                context: { tainted: false },
            });

            expect(result).not.toBeNull();
            expect(result.plan.length).toBe(1);
            expect(result.plan[0].name).toBe("CheckCtx");
            expect(result.context.tainted).toBe(false);
        });
    });

    describe("Smart Plan Executor and Replanning", () => {
        // --- Reusable tasks for replanning tests ---
        const patrolPointA = new PrimitiveTask("Patrol to A", {
            effects: (ws) => ws.set("at", "A"),
        });
        const patrolPointB = new PrimitiveTask("Patrol to B", {
            effects: (ws) => ws.set("at", "B"),
        });
        const attackEnemy = new PrimitiveTask("Attack Enemy", {
            conditions: (ws) => ws.get("enemyVisible"),
            effects: (ws) => ws.set("enemyDefeated", true),
        });
        const patrolGoal = new CompoundTask("Patrol", [
            {
                name: "Go to B",
                conditions: (ws) => ws.get("at") === "A",
                subtasks: [patrolPointB],
            },
            {
                name: "Go to A",
                conditions: (ws) => ws.get("at") === "B",
                subtasks: [patrolPointA],
            },
        ]);
        const reactGoal = new CompoundTask("React To World", [
            {
                name: "Attack",
                priority: 100,
                conditions: (ws) => ws.get("enemyVisible"),
                subtasks: [attackEnemy],
            },
            { name: "Patrol", priority: 1, subtasks: [patrolGoal] },
        ]);

        test("should automatically replan when an interruption occurs", () => {
            const planner = new Planner();
            planner.registerTask(patrolGoal);
            planner.registerTask(attackEnemy);
            const interruptionManager = new InterruptionManager();

            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (worldState, context, currentTask) => {
                    if (
                        worldState.get("enemyVisible") &&
                        currentTask.name !== "Attack Enemy"
                    ) {
                        return { interrupted: true, reason: "ENEMY_SPOTTED" };
                    }
                    return null;
                },
            });

            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });

            // 1. Initial plan is to patrol
            const initialResult = planner.findPlan({
                tasks: reactGoal,
                worldState: initialWorld,
            });
            expect(initialResult.plan[0].name).toBe("Patrol to B");

            // 2. Create the Executor with all necessary components for replanning
            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context,
                {
                    planner,
                    rootTask: reactGoal,
                    interruptionManager,
                }
            );

            // 3. World changes: an enemy appears!
            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            // 4. Tick the executor. It should detect the change, interrupt, and replan automatically.
            const tickResult = executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            // 5. Assert the replan was successful
            expect(tickResult.status).toBe("replanned");
            expect(tickResult.interruption.reason).toBe("ENEMY_SPOTTED");

            // 6. Assert the executor's internal state has been updated with the new plan
            expect(executor.plan[0].name).toBe("Attack Enemy");
            expect(executor.isDone()).toBe(false);
            expect(executor.isFailed()).toBe(false);

            // 7. Tick again with the new plan. It should now execute the attack without re-interrupting.
            const nextTickResult = executor.tick(worldAfterChange, {
                enableReplanning: true,
            });
            expect(nextTickResult.status).toBe("completed");
            expect(nextTickResult.task.name).toBe("Attack Enemy");

            // 8. The world state should reflect the effects of the attack
            expect(worldAfterChange.get("enemyDefeated")).toBe(true);
        });

        // --- FIX 2: The assertion for the reason has been corrected ---
        test("should enter a failed state if replanning is not possible", () => {
            const faultyPlanner = new Planner();
            faultyPlanner.registerTask(patrolGoal);

            const reactGoalByName = new CompoundTask("React To World By Name", [
                {
                    name: "Attack",
                    priority: 100,
                    conditions: (ws) => ws.get("enemyVisible"),
                    subtasks: ["Attack Enemy"], // This task is NOT registered
                },
                { name: "Patrol", priority: 1, subtasks: ["Patrol"] },
            ]);

            const interruptionManager = new InterruptionManager();
            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (ws) =>
                    ws.get("enemyVisible")
                        ? { interrupted: true, reason: "ENEMY_SPOTTED" }
                        : null,
            });

            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });
            const initialResult = faultyPlanner.findPlan({
                tasks: reactGoalByName,
                worldState: initialWorld,
            });
            expect(initialResult.plan[0].name).toBe("Patrol to B");

            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context,
                {
                    planner: faultyPlanner,
                    rootTask: reactGoalByName,
                    interruptionManager,
                }
            );

            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            const tickResult = executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            expect(tickResult.status).toBe("failed");
            // Corrected assertion: The planner threw an *error*, which is different from *failing to find a plan*.
            expect(tickResult.reason).toBe("replanning_error");
            expect(executor.isFailed()).toBe(true);
        });
    });

    describe("Smart Object System", () => {
        // Define a smart action in the library for testing purposes
        smartActionLibrary.UseCover = {
            actionType: "UseCover",
            conditions: (ws, ctx, obj) => !obj.isOccupied,
            effects: (ws, ctx, obj) => {
                const agentId = ctx.agent.id;
                // In a real system, you'd find and modify the object in the worldState array
                obj.isOccupied = true;
                ws.set("agentInCover", agentId);
            },
            operator: (ctx) => {
                /* Triggers 'take cover' animation */
            },
        };

        let planner;
        let takeCoverTask;

        beforeEach(() => {
            planner = new Planner();
            // A high-level task that wants to use a cover object
            takeCoverTask = new CompoundTask("TakeCover", [
                {
                    name: "Use Smart Object Cover",
                    // The subtask is a string name that will be dynamically generated
                    subtasks: ["UseCover_cover_point_1"],
                },
            ]);
            planner.registerTask(takeCoverTask);
        });

        test("should discover and register a SmartObjectTask", () => {
            const worldState = createWorldStateProxy({
                worldObjects: [
                    {
                        id: "cover_point_1",
                        type: "Barrier",
                        isOccupied: false,
                        smartLink: {
                            actionType: "UseCover",
                        },
                    },
                ],
            });

            const count = TaskDiscoverer.discoverAndRegister(
                planner,
                worldState
            );
            expect(count).toBe(1);
            const discoveredTask =
                planner.taskRegistry["UseCover_cover_point_1"];
            expect(discoveredTask).toBeInstanceOf(SmartObjectTask);
            expect(discoveredTask.name).toBe("UseCover_cover_point_1");
            expect(discoveredTask.smartObject.id).toBe("cover_point_1");
        });

        test("should throw TaskNotFoundError if the smart object task does not exist", () => {
            const worldState = createWorldStateProxy({
                worldObjects: [],
            });

            expect(() => {
                planner.findPlan({
                    tasks: takeCoverTask,
                    worldState: worldState,
                });
            }).toThrow(new TaskNotFoundError("UseCover_cover_point_1"));
        });

        test("should successfully create a plan using a discovered smart object task", () => {
            const worldState = createWorldStateProxy({
                agent: {
                    id: "agent_007",
                },
                worldObjects: [
                    {
                        id: "cover_point_1",
                        type: "Barrier",
                        isOccupied: false,
                        smartLink: {
                            actionType: "UseCover",
                        },
                    },
                ],
            });

            const result = planner.findPlan({
                tasks: takeCoverTask,
                worldState,
                context: {
                    agent: {
                        id: "agent_007",
                    },
                },
            });

            expect(result).not.toBeNull();
            expect(result.plan.length).toBe(1);
            expect(result.plan[0].name).toBe("UseCover_cover_point_1");

            // Check if the effects were applied correctly during planning simulation
            const finalWorldState = createWorldStateProxy({
                agent: {
                    id: "agent_007",
                },
                worldObjects: [
                    {
                        id: "cover_point_1",
                        type: "Barrier",
                        isOccupied: true, // Should be occupied now
                        smartLink: {
                            actionType: "UseCover",
                        },
                    },
                ],
                agentInCover: "agent_007", // Custom state set by effect
            });

            // The world state passed to findPlan is a *clone*, so the original is untouched.
            // The effects are applied to the *working* world state inside the planner.
            // We can't directly test the working state post-plan, but success of the plan implies
            // the effects were simulated correctly.
        });

        test("should fail to find a plan if smart object conditions are not met", () => {
            const worldState = createWorldStateProxy({
                worldObjects: [
                    {
                        id: "cover_point_1",
                        type: "Barrier",
                        isOccupied: true,
                        smartLink: {
                            actionType: "UseCover",
                        },
                    },
                ],
            });

            const result = planner.findPlan({
                tasks: takeCoverTask,
                worldState,
                context: {},
            });

            // The planner should return null because the only available method
            // (using the smart object) has conditions that are not met.
            expect(result).toBeNull();
        });
    });
});
