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

// Helper function to run the async generator planner to completion.
// This simplifies all the tests that need to get a final plan.
const runPlannerToCompletion = async (planner, options) => {
    const generator = planner.findPlan(options);
    let result = null;
    while (true) {
        const { value, done } = await generator.next();
        if (done) {
            result = value;
            break;
        }
    }
    return result;
};

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
            const proxy = createWorldStateProxy({
                a: 1,
            });
            expect(proxy.get("a")).toBe(1);
            proxy.set("b", 2);
            expect(proxy.get("b")).toBe(2);
            expect(proxy.get().b).toBe(2);
        });

        test("clone should create a deep, independent copy", () => {
            const originalProxy = createWorldStateProxy({
                nested: {
                    value: 10,
                },
            });
            const clonedProxy = originalProxy.clone();

            clonedProxy.set("nested", {
                value: 20,
            });

            expect(originalProxy.get("nested").value).toBe(10);
            expect(clonedProxy.get("nested").value).toBe(20);
        });
    });

    describe("Core Planning Logic", () => {
        test("should create a simple plan successfully", async () => {
            const planner = new Planner();
            const getMaterial = new PrimitiveTask("Get Material", {
                effects: (ws) => ws.set("hasMaterial", true),
            });
            const craftItem = new PrimitiveTask("Craft Item", {
                conditions: (ws) => ws.get("hasMaterial"),
            });
            const rootTask = new CompoundTask("Create Item", [
                {
                    name: "Default",
                    subtasks: [getMaterial, craftItem],
                },
            ]);

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({
                    hasMaterial: false,
                }),
            });

            expect(result.plan.length).toBe(2);
            expect(result.plan[0].name).toBe("Get Material");
            expect(result.plan[1].name).toBe("Craft Item");
        });

        test("should return null when no plan is possible", async () => {
            const planner = new Planner();
            const impossibleTask = new PrimitiveTask("Impossible", {
                conditions: () => false,
            });
            const rootTask = new CompoundTask("Root", [
                {
                    name: "Default",
                    subtasks: [impossibleTask],
                },
            ]);

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });

            expect(result).toBeNull();
        });
    });

    describe("Method Priorities and Backtracking", () => {
        test("should choose the highest priority method first", async () => {
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

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });

            expect(result.plan[0].name).toBe("High");
        });

        test("should backtrack to a lower priority method if the high priority one fails", async () => {
            const planner = new Planner();
            const succeed = new PrimitiveTask("Succeed");
            const fail = new PrimitiveTask("Fail", {
                conditions: () => false,
            });
            const rootTask = new CompoundTask("Root", [
                {
                    name: "Low Prio Success",
                    priority: 1,
                    subtasks: [succeed],
                },
                {
                    name: "High Prio Fail",
                    priority: 10,
                    subtasks: [fail],
                },
            ]);

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
            });

            expect(result.plan.length).toBe(1);
            expect(result.plan[0].name).toBe("Succeed");
            expect(planner.getMetrics().backtrackCount).toBe(1);
        });
    });

    describe("Task Parameters (Context)", () => {
        test("should pass and modify context between tasks", async () => {
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
                {
                    name: "Default",
                    subtasks: [findTarget, moveToTarget],
                },
            ]);

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
                context: {},
            });

            expect(result.context.targetId).toBe("enemy_1");
        });
    });

    describe("Plan Executor and Replanning", () => {
        test("should interrupt a plan and allow for replanning", async () => {
            const planner = new Planner();
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
                {
                    name: "Patrol",
                    priority: 1,
                    subtasks: [patrolGoal],
                },
            ]);

            planner.registerTask(patrolGoal);
            planner.registerTask(patrolPointB);
            planner.registerTask(attackEnemy);

            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });

            const initialResult = await runPlannerToCompletion(planner, {
                tasks: reactGoal,
                worldState: initialWorld,
            });
            expect(initialResult.plan[0].name).toBe("Patrol to B");

            const interruptionManager = new InterruptionManager();
            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (ws) =>
                    ws.get("enemyVisible")
                        ? {
                              interrupted: true,
                              reason: "ENEMY_SPOTTED",
                          }
                        : null,
            });

            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context,
                {
                    rootTask: reactGoal,
                    interruptionManager,
                    replanFn: (opts) => planner.findPlan(opts),
                }
            );

            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            const tickResult = await executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            expect(tickResult.status).toBe("replanned");
            expect(tickResult.interruption.reason).toBe("ENEMY_SPOTTED");
            expect(executor.plan[0].name).toBe("Attack Enemy");
        });
    });

    describe("Advanced Error and Edge Case Handling", () => {
        test("should throw TaskNotFoundError for unregistered task", async () => {
            const planner = new Planner();
            const rootTask = new CompoundTask("Root", [
                {
                    name: "Default",
                    subtasks: ["MissingTask"],
                },
            ]);

            await expect(
                runPlannerToCompletion(planner, {
                    tasks: rootTask,
                    worldState: createWorldStateProxy({}),
                })
            ).rejects.toThrow(TaskNotFoundError);
        });

        test("should throw PlanningTimeoutError when maxIterations is exceeded", async () => {
            const planner = new Planner({
                maxIterations: 5,
            });
            const infiniteTask = new CompoundTask("Infinite");
            infiniteTask.methods.push({
                name: "Loop",
                subtasks: [infiniteTask],
            });
            planner.registerTask(infiniteTask);

            await expect(
                runPlannerToCompletion(planner, {
                    tasks: infiniteTask,
                    worldState: createWorldStateProxy({}),
                })
            ).rejects.toThrow(PlanningTimeoutError);
        });

        test("should correctly use and invalidate the plan cache", async () => {
            const planner = new Planner();
            const rootTask = new CompoundTask("Root", [
                {
                    name: "Default",
                    subtasks: [new PrimitiveTask("Task")],
                },
            ]);
            const worldState1 = createWorldStateProxy({
                key: "value1",
            });
            const worldState2 = createWorldStateProxy({
                key: "value2",
            });

            await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: worldState1,
            });
            expect(planner.getMetrics().cacheHit).toBe(false);

            await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: worldState1,
            });
            expect(planner.getMetrics().cacheHit).toBe(true);

            await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: worldState2,
            });
            expect(planner.getMetrics().cacheHit).toBe(false);
        });

        test("should maintain context integrity after backtracking", async () => {
            const planner = new Planner();
            const setCtx = new PrimitiveTask("SetCtx", {
                effects: (ws, ctx) => {
                    ctx.tainted = true;
                },
            });
            const fail = new PrimitiveTask("Fail", {
                conditions: () => false,
            });
            const checkCtx = new PrimitiveTask("CheckCtx", {
                conditions: (ws, ctx) => ctx.tainted !== true,
            });
            const rootTask = new CompoundTask("Root", [
                {
                    name: "High Prio Fail",
                    priority: 10,
                    subtasks: [setCtx, fail],
                },
                {
                    name: "Low Prio Success",
                    priority: 1,
                    subtasks: [checkCtx],
                },
            ]);

            const result = await runPlannerToCompletion(planner, {
                tasks: rootTask,
                worldState: createWorldStateProxy({}),
                context: {
                    tainted: false,
                },
            });

            expect(result).not.toBeNull();
            expect(result.plan.length).toBe(1);
            expect(result.plan[0].name).toBe("CheckCtx");
            expect(result.context.tainted).toBe(false);
        });
    });

    describe("Smart Plan Executor and Replanning", () => {
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
            {
                name: "Patrol",
                priority: 1,
                subtasks: [patrolGoal],
            },
        ]);

        test("should automatically replan when an interruption occurs", async () => {
            const planner = new Planner();
            planner.registerTask(patrolGoal);
            planner.registerTask(attackEnemy);
            planner.registerTask(patrolPointA);
            planner.registerTask(patrolPointB);

            const interruptionManager = new InterruptionManager();
            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (worldState, context, currentTask) => {
                    if (
                        worldState.get("enemyVisible") &&
                        currentTask.name !== "Attack Enemy"
                    ) {
                        return {
                            interrupted: true,
                            reason: "ENEMY_SPOTTED",
                        };
                    }
                    return null;
                },
            });

            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });

            const initialResult = await runPlannerToCompletion(planner, {
                tasks: reactGoal,
                worldState: initialWorld,
            });
            expect(initialResult.plan[0].name).toBe("Patrol to B");

            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context,
                {
                    planner,
                    rootTask: reactGoal,
                    interruptionManager,
                    replanFn: (opts) => planner.findPlan(opts),
                }
            );

            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            const tickResult = await executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            expect(tickResult.status).toBe("replanned");
            expect(tickResult.interruption.reason).toBe("ENEMY_SPOTTED");
            expect(executor.plan[0].name).toBe("Attack Enemy");
            expect(executor.isDone()).toBe(false);
            expect(executor.isFailed()).toBe(false);

            const nextTickResult = await executor.tick(worldAfterChange, {
                enableReplanning: true,
            });
            expect(nextTickResult.status).toBe("completed");
            expect(nextTickResult.task.name).toBe("Attack Enemy");
            expect(worldAfterChange.get("enemyDefeated")).toBe(true);
        });

        test("should enter a failed state if replanning is not possible", async () => {
            const faultyPlanner = new Planner();
            faultyPlanner.registerTask(patrolGoal);
            faultyPlanner.registerTask(patrolPointB); // Register the primitive task used by patrol

            const reactGoalByName = new CompoundTask("React To World By Name", [
                {
                    name: "Attack",
                    priority: 100,
                    conditions: (ws) => ws.get("enemyVisible"),
                    subtasks: ["Attack Enemy"], // This task is NOT registered
                },
                {
                    name: "Patrol",
                    priority: 1,
                    subtasks: ["Patrol"],
                },
            ]);
            faultyPlanner.registerTask(reactGoalByName);

            const interruptionManager = new InterruptionManager();
            interruptionManager.registerInterruptor("enemy-spotter", {
                check: (ws) =>
                    ws.get("enemyVisible")
                        ? {
                              interrupted: true,
                              reason: "ENEMY_SPOTTED",
                          }
                        : null,
            });

            const initialWorld = createWorldStateProxy({
                at: "A",
                enemyVisible: false,
            });
            const initialResult = await runPlannerToCompletion(faultyPlanner, {
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
                    replanFn: (opts) => faultyPlanner.findPlan(opts),
                }
            );

            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });

            const tickResult = await executor.tick(worldAfterChange, {
                enableReplanning: true,
            });

            expect(tickResult.status).toBe("failed");
            expect(tickResult.reason).toBe("replanning_error");
            expect(executor.isFailed()).toBe(true);
        });
    });

    describe("Smart Object System", () => {
        smartActionLibrary.UseCover = {
            actionType: "UseCover",
            conditions: (ws, ctx, obj) => !obj.isOccupied,
            effects: (ws, ctx, obj) => {
                // This is a simulation, so we modify the object directly.
                // In a real scenario, you'd use ws.updateObject(obj.id, { isOccupied: true });
                obj.isOccupied = true;
                ws.set("agentInCover", ctx.agent.id);
            },
            operator: (ctx) => {},
        };

        let planner;
        let takeCoverTask;

        beforeEach(() => {
            planner = new Planner();
            takeCoverTask = new CompoundTask("TakeCover", [
                {
                    name: "Use Smart Object Cover",
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

        test("should throw TaskNotFoundError if the smart object task does not exist", async () => {
            const worldState = createWorldStateProxy({
                worldObjects: [],
            });

            await expect(
                runPlannerToCompletion(planner, {
                    tasks: takeCoverTask,
                    worldState: worldState,
                })
            ).rejects.toThrow(new TaskNotFoundError("UseCover_cover_point_1"));
        });

        test("should successfully create a plan using a discovered smart object task", async () => {
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

            // Discover tasks before planning
            TaskDiscoverer.discoverAndRegister(planner, worldState);

            const result = await runPlannerToCompletion(planner, {
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
        });

        test("should fail to find a plan if smart object conditions are not met", async () => {
            const worldState = createWorldStateProxy({
                worldObjects: [
                    {
                        id: "cover_point_1",
                        type: "Barrier",
                        isOccupied: true, // Condition will fail
                        smartLink: {
                            actionType: "UseCover",
                        },
                    },
                ],
            });

            // Discover tasks before planning
            TaskDiscoverer.discoverAndRegister(planner, worldState);

            const result = await runPlannerToCompletion(planner, {
                tasks: takeCoverTask,
                worldState,
                context: {},
            });

            expect(result).toBeNull();
        });
    });
});
