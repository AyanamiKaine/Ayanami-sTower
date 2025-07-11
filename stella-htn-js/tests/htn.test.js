import { test, expect, describe } from "bun:test";
import {
    CompoundTask,
    Planner,
    PrimitiveTask,
    WorldStateProxy,
    PlanningError,
    PlanningTimeoutError,
    TaskNotFoundError,
    PlanExecutor,
} from "../main"; // Assuming your updated library is in main.js

describe("HTN Planner Library", () => {
    // --- Test Setup ---
    // Updated to use structuredClone for performance and correctness.
    const createWorldStateProxy = (initialState) => {
        let state = structuredClone(initialState);
        return new WorldStateProxy({
            getState: (key) => (key ? state[key] : state),
            setState: (key, value) => (state[key] = value),
            clone: () => createWorldStateProxy(state), // structuredClone is handled inside
            incrementState: (key, value = 1) =>
                (state[key] = (state[key] || 0) + value),
            generateCacheKey: () => JSON.stringify(state), // Keep for simplicity in tests
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
        const patrolPointA = new PrimitiveTask("Patrol to A", {
            effects: (ws) => ws.set("at", "A"),
        });
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

        test("should interrupt a plan and allow for replanning", () => {
            const planner = new Planner();
            planner.registerTask(patrolGoal);
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

            const executor = new PlanExecutor(
                initialResult.plan,
                initialResult.context
            );

            // 2. World changes: an enemy appears!
            const worldAfterChange = createWorldStateProxy({
                at: "A",
                enemyVisible: true,
            });
            const monitor = (ws) => {
                if (ws.get("enemyVisible")) {
                    return { interrupted: true, reason: "ENEMY_SPOTTED" };
                }
                return { interrupted: false };
            };

            // 3. Executor's tick is interrupted by the monitor
            const tickResult = executor.tick(monitor, worldAfterChange);
            expect(tickResult.interrupted).toBe(true);
            expect(tickResult.reason).toBe("ENEMY_SPOTTED");

            // 4. Replanning with the new world state produces an attack plan
            const replanResult = planner.findPlan({
                tasks: reactGoal,
                worldState: worldAfterChange,
            });
            expect(replanResult.plan[0].name).toBe("Attack Enemy");
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
            // The final context should not be tainted because the branch that tainted it failed and was rolled back.
            expect(result.context.tainted).toBe(false);
        });
    });
});
