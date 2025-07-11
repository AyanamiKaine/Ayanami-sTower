/**
 * Provides a generic interface to interact with the world state.
 * This class acts as a bridge between the planner and the user's
 * specific state management system (e.g., a simple object, an ECS).
 *
 * @performance
 * The `clone` function is the most performance-critical part of this class.
 * For production game use, it is HIGHLY recommended to provide an efficient,
 * specialized cloning function. The modern `structuredClone()` is an excellent
 * default choice, being much faster than `JSON.parse(JSON.stringify(state))`.
 */
export class WorldStateProxy {
    constructor({
        getState,
        setState,
        clone,
        incrementState,
        generateCacheKey,
    }) {
        if (
            typeof getState !== "function" ||
            typeof setState !== "function" ||
            typeof clone !== "function"
        ) {
            throw new Error(
                "WorldStateProxy requires 'getState', 'setState', and 'clone' functions."
            );
        }
        this._getState = getState;
        this._setState = setState;
        this._clone = clone;
        this._incrementState = incrementState;
        // Optional: A function to generate a unique key for the current state for caching.
        // If not provided, the planner will fall back to a slower stringify method.
        this._generateCacheKey = generateCacheKey;
    }

    get(key) {
        return this._getState(key);
    }
    set(key, value) {
        this._setState(key, value);
    }
    clone() {
        return this._clone();
    }

    increment(key, value = 1) {
        if (this._incrementState) {
            this._incrementState(key, value);
        } else {
            const currentValue = this.get(key);
            this.set(
                key,
                (typeof currentValue === "number" ? currentValue : 0) + value
            );
        }
    }

    getCacheKey() {
        if (this._generateCacheKey) {
            return this._generateCacheKey();
        }
        // Fallback for caching if no custom key generator is provided.
        // WARNING: This can be slow for large states.
        return JSON.stringify(this._getState());
    }
}

/**
 * Base class for all tasks in the HTN.
 */
export class Task {
    constructor(name) {
        this.name = name;
    }
}

/**
 * Represents a single, executable action in the world.
 */
export class PrimitiveTask extends Task {
    constructor(
        name,
        {
            conditions = () => true,
            effects = () => {},
            operator = () => {},
        } = {}
    ) {
        super(name);
        this.isPrimitive = true;
        this.conditions = conditions;
        this.effects = effects;
        this.operator = operator;
    }

    executeOperator(context) {
        // In a real game engine, this would trigger animations, sounds, etc.
        console.log(
            `Executing operator for: ${this.name} with context:`,
            context
        );
        this.operator(context);
    }
}

/**
 * Represents a high-level task that can be decomposed into smaller subtasks.
 * Each method is a potential way to accomplish the task.
 *
 * @typedef {object} Method
 * @property {string} name - The name of the method for debugging.
 * @property {number} [priority=0] - The priority of the method. Higher numbers are chosen first.
 * @property {function(WorldStateProxy, object): boolean} [conditions] - A function that must return true for this method to be applicable.
 * @property {Array<Task|string>} subtasks - An array of subtasks to be executed if this method is chosen.
 */
export class CompoundTask extends Task {
    /**
     * @param {string} name - The name of the compound task.
     * @param {Array<Method>} [methods=[]] - An array of methods.
     */
    constructor(name, methods = []) {
        super(name);
        this.isPrimitive = false;

        // Validate and normalize methods
        methods.forEach((m, i) => {
            if (!m.name || !Array.isArray(m.subtasks)) {
                throw new Error(
                    `Invalid method structure at index ${i} in CompoundTask '${name}'. Methods must have a 'name' and a 'subtasks' array.`
                );
            }
        });

        // Sort methods by priority ONCE on creation. This optimizes finding applicable methods.
        this.methods = methods
            .map((m) => ({
                ...m,
                priority: m.priority || 0,
            }))
            .sort((a, b) => b.priority - a.priority);
    }

    /**
     * Finds the first applicable method based on pre-sorted priority.
     * @param {WorldStateProxy} worldState - The current state of the world.
     * @param {object} context - The current planning context.
     * @returns {{method: Method, index: number}|null} The best applicable method and its index, or null.
     */
    findApplicableMethod(worldState, context) {
        for (let i = 0; i < this.methods.length; i++) {
            const method = this.methods[i];
            if (!method.conditions || method.conditions(worldState, context)) {
                return { method, index: i };
            }
        }
        return null;
    }
}

// --- Custom Error Types ---
export class PlanningError extends Error {
    constructor(message) {
        super(message);
        this.name = "PlanningError";
    }
}
export class TaskNotFoundError extends PlanningError {
    constructor(taskName) {
        super(`Task '${taskName}' not found in registry.`);
        this.name = "TaskNotFoundError";
        this.taskName = taskName;
    }
}
export class PlanningTimeoutError extends PlanningError {
    constructor(message) {
        super(message);
        this.name = "PlanningTimeoutError";
    }
}

/**
 * The main planner class that implements the forward-decomposition HTN algorithm.
 */
export class Planner {
    constructor(config = {}) {
        this.config = {
            maxIterations: 1000,
            maxTime: Infinity, // Milliseconds
            enablePlanCaching: true,
            ...config,
        };
        this.logCallback = () => {};
        this.taskRegistry = {};
        this.planCache = new Map();
        this.metrics = {};
    }

    registerTask(task) {
        this.taskRegistry[task.name] = task;
    }
    setLogCallback(callback) {
        this.logCallback = callback;
    }
    getMetrics() {
        return this.metrics;
    }
    clearCache() {
        this.planCache.clear();
    }
    log(type, message, data = {}) {
        this.logCallback(type, message, data);
    }

    /**
     * Finds the next point in the decomposition history to backtrack to.
     * This implementation is optimized to avoid re-filtering arrays by using the stored method index.
     * @param {Array<object>} decompositionHistory - The history of decompositions.
     * @returns {object|null} A state object to restore to, or null if no backtrack point is found.
     */
    _findBacktrackPoint(decompositionHistory) {
        while (decompositionHistory.length > 0) {
            const lastDecomp = decompositionHistory.pop();
            const {
                compoundTask,
                lastMethodIndex,
                worldState,
                context,
                planState,
                remainingTasks,
            } = lastDecomp;

            // Start searching for an alternative method from the index *after* the one that failed.
            for (
                let i = lastMethodIndex + 1;
                i < compoundTask.methods.length;
                i++
            ) {
                const nextMethod = compoundTask.methods[i];
                // Check if this new method is applicable in the state *before* the previous failed method was applied.
                if (
                    !nextMethod.conditions ||
                    nextMethod.conditions(worldState, context)
                ) {
                    this.log(
                        "info",
                        `Found backtrack point. Trying method '${nextMethod.name}' for ${compoundTask.name}.`
                    );

                    // We found a valid alternative. Push its state onto the history so we can backtrack from it later if needed.
                    decompositionHistory.push({
                        compoundTask,
                        lastMethodIndex: i,
                        worldState: worldState.clone(),
                        context: structuredClone(context),
                        planState: [...planState],
                        remainingTasks: [...remainingTasks],
                    });

                    // Return the state required for the planner to resume.
                    return {
                        worldState,
                        context,
                        planState,
                        tasksToProcess: [
                            ...nextMethod.subtasks,
                            ...remainingTasks,
                        ],
                    };
                }
            }
            this.log(
                "info",
                `No more methods to try for ${compoundTask.name}. Backtracking further...`
            );
        }
        return null; // No backtrack points left.
    }

    /**
     * Core planning logic. Can be used for initial planning or replanning.
     * @param {object} options - The options for finding a plan.
     * @param {Task | Array<Task|string>} options.tasks - The root task or initial task list.
     * @param {WorldStateProxy} options.worldState - The initial state of the world.
     * @param {object} [options.context={}] - The initial planning context.
     * @returns {object|null} A result object with the plan and final context, or null if no plan is found.
     */
    findPlan({ tasks, worldState, context = {} }) {
        const startTime = performance.now();
        let backtrackCount = 0;

        // 1. Check Plan Cache
        const isInitialPlan = tasks instanceof Task;
        const cacheKey =
            this.config.enablePlanCaching && isInitialPlan
                ? `${tasks.name}:${worldState.getCacheKey()}`
                : null;

        if (cacheKey && this.planCache.has(cacheKey)) {
            this.metrics = {
                planningTime: performance.now() - startTime,
                nodesExplored: 0,
                planLength: this.planCache.get(cacheKey).plan.length,
                backtrackCount: 0,
                cacheHit: true,
            };
            this.log("success", "Plan found in cache.", {
                metrics: this.metrics,
            });
            return this.planCache.get(cacheKey);
        }

        this.log("info", `Starting planning...`);

        let finalPlan = [];
        let tasksToProcess = Array.isArray(tasks) ? [...tasks] : [tasks];
        let workingWorldState = worldState.clone();
        let workingContext = structuredClone(context); // Use structuredClone for performance
        const decompositionHistory = [];
        let iterations = 0;

        while (tasksToProcess.length > 0) {
            // 2. Check Time and Iteration Limits
            if (++iterations > this.config.maxIterations)
                throw new PlanningTimeoutError(
                    `Max iterations (${this.config.maxIterations}) reached.`
                );
            if (performance.now() - startTime > this.config.maxTime)
                throw new PlanningTimeoutError(
                    `Planning time exceeded ${this.config.maxTime}ms.`
                );

            let currentTask = tasksToProcess.shift();

            if (typeof currentTask === "string") {
                const taskName = currentTask;
                currentTask = this.taskRegistry[taskName];
                if (!currentTask) throw new TaskNotFoundError(taskName);
            }

            try {
                if (currentTask.isPrimitive) {
                    if (
                        currentTask.conditions(
                            workingWorldState,
                            workingContext
                        )
                    ) {
                        currentTask.effects(workingWorldState, workingContext);
                        finalPlan.push(currentTask);
                    } else {
                        throw new PlanningError(
                            `Conditions FAILED for ${currentTask.name}.`
                        );
                    }
                } else {
                    // Compound Task
                    const result = currentTask.findApplicableMethod(
                        workingWorldState,
                        workingContext
                    );
                    if (result) {
                        const { method, index } = result;
                        decompositionHistory.push({
                            compoundTask: currentTask,
                            lastMethodIndex: index,
                            worldState: workingWorldState.clone(),
                            context: structuredClone(workingContext),
                            planState: [...finalPlan],
                            remainingTasks: [...tasksToProcess],
                        });
                        tasksToProcess.unshift(...method.subtasks);
                    } else {
                        throw new PlanningError(
                            `No applicable method found for ${currentTask.name}.`
                        );
                    }
                }
            } catch (e) {
                if (e instanceof TaskNotFoundError) throw e;

                this.log("fail", e.message + " Backtracking...");
                backtrackCount++;
                const backtrackPoint =
                    this._findBacktrackPoint(decompositionHistory);

                if (backtrackPoint) {
                    // Restore state from the successful backtrack point
                    workingWorldState = backtrackPoint.worldState;
                    workingContext = backtrackPoint.context;
                    finalPlan = [...backtrackPoint.planState];
                    tasksToProcess = [...backtrackPoint.tasksToProcess];
                } else {
                    this.log(
                        "fail",
                        "Backtracking failed at the root. No plan found."
                    );
                    return null;
                }
            }
        }

        const result = { plan: finalPlan, context: workingContext };

        this.metrics = {
            planningTime: performance.now() - startTime,
            nodesExplored: iterations,
            planLength: finalPlan.length,
            backtrackCount,
            cacheHit: false,
        };
        this.log("success", "Planning complete! Final plan generated.", {
            metrics: this.metrics,
        });

        if (cacheKey) this.planCache.set(cacheKey, result);

        return result;
    }
}

/**
 * Manages the step-by-step execution of a plan and monitors for interruptions.
 */
export class PlanExecutor {
    /**
     * @param {Array<PrimitiveTask>} plan - The plan to execute.
     * @param {object} context - The context for the plan.
     */
    constructor(plan, context) {
        this.plan = [...plan];
        this.context = context;
        this.currentIndex = 0;
    }

    isDone() {
        return this.currentIndex >= this.plan.length;
    }

    /**
     * Executes the next step of the plan.
     * @param {function(WorldStateProxy): (boolean|object)} [monitor] - A function to check for plan-invalidating world changes.
     * If it returns `true` or an object like `{interrupted: true}`, execution stops.
     * @param {WorldStateProxy} [worldState] - The current world state, required if a monitor is used.
     * @returns {{interrupted: boolean, task: PrimitiveTask|null, reason?: any}} - The result of the step.
     */
    tick(monitor, worldState) {
        if (this.isDone()) {
            return { interrupted: false, task: null };
        }

        if (monitor && worldState) {
            const monitorResult = monitor(worldState);
            const wasInterrupted =
                (typeof monitorResult === "boolean" &&
                    monitorResult === true) ||
                (monitorResult && monitorResult.interrupted);

            if (wasInterrupted) {
                this.log("PlanExecutor: Monitor triggered. Plan interrupted.");
                // If the monitor returns a detailed object, pass it through. Otherwise, create a default one.
                return typeof monitorResult === "object"
                    ? monitorResult
                    : { interrupted: true, task: null };
            }
        }

        const task = this.plan[this.currentIndex];
        task.executeOperator(this.context);
        this.currentIndex++;

        return { interrupted: false, task };
    }

    // Added a simple log method for consistency, can be overridden.
    log(message) {
        console.log(message);
    }
}
