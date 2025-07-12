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
        updateObject,
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
        this._updateObject = updateObject;
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

    updateObject(id, newProperties) {
        if (this._updateObject) {
            this._updateObject(id, newProperties);
        } else {
            const objects = this.get("worldObjects");
            if (!Array.isArray(objects)) {
                console.warn(
                    `WorldStateProxy: 'worldObjects' is not an array. Cannot update object.`
                );
                return;
            }
            const objectIndex = objects.findIndex((o) => o.id === id);
            if (objectIndex !== -1) {
                objects[objectIndex] = {
                    ...objects[objectIndex],
                    ...newProperties,
                };
                this.set("worldObjects", objects);
            }
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
 * A specialized PrimitiveTask for handling Smart Object interactions.
 * It holds a reference to the specific object instance it interacts with.
 */
export class SmartObjectTask extends PrimitiveTask {
    constructor(name, smartObject, logic) {
        super(name, {
            // The logic functions are now closures that capture the specific `smartObject`
            conditions: (ws, ctx) => logic.conditions(ws, ctx, smartObject),
            effects: (ws, ctx) => logic.effects(ws, ctx, smartObject),
            operator: (ctx) => {
                // Automatically add the target object to the context for the operator
                const operatorContext = { ...ctx, smartObject };
                logic.operator(operatorContext);
            },
        });
        this.smartObject = smartObject;
        this.actionType = logic.actionType; // Store the generic action type for reference
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
                return {
                    method,
                    index: i,
                };
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
            frameBudget: 4, // Default to 4ms for planning per frame/tick.
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

    _findBacktrackPoint(decompositionHistory) {
        while (decompositionHistory.length > 0) {
            const lastDecomp = decompositionHistory.pop();
            const {
                compoundTask,
                lastMethodIndex,
                worldStateSnapshot,
                contextSnapshot,
                planState,
                remainingTasks,
            } = lastDecomp;

            for (
                let i = lastMethodIndex + 1;
                i < compoundTask.methods.length;
                i++
            ) {
                const nextMethod = compoundTask.methods[i];
                if (
                    !nextMethod.conditions ||
                    nextMethod.conditions(worldStateSnapshot, contextSnapshot)
                ) {
                    this.log(
                        "info",
                        `Found backtrack point. Trying method '${nextMethod.name}' for ${compoundTask.name}.`
                    );
                    decompositionHistory.push({
                        ...lastDecomp,
                        lastMethodIndex: i,
                    });
                    return {
                        worldState: worldStateSnapshot,
                        context: contextSnapshot,
                        planState: planState,
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
        return null;
    }

    async *findPlan({ tasks, worldState, context = {} }) {
        const overallStartTime = performance.now();
        let frameStartTime = overallStartTime;

        // FIX: Initialize metrics object for every run
        this.metrics = {
            iterations: 0,
            backtrackCount: 0,
            planningTime: 0,
            cacheHit: false,
        };

        TaskDiscoverer.discoverAndRegister(this, worldState);

        const cacheKey =
            this.config.enablePlanCaching && tasks instanceof Task
                ? `${tasks.name}:${worldState.getCacheKey()}`
                : null;

        if (cacheKey && this.planCache.has(cacheKey)) {
            this.metrics.cacheHit = true;
            this.metrics.planningTime = performance.now() - overallStartTime;
            // Yield once to indicate completion, then return the cached value
            yield {
                status: "completed",
                fromCache: true,
            };
            return this.planCache.get(cacheKey);
        }

        this.log("info", `Starting incremental planning...`);

        let tasksToProcess = Array.isArray(tasks) ? [...tasks] : [tasks];
        let workingWorldState = worldState.clone();
        let workingContext = structuredClone(context);
        let finalPlan = [];
        const decompositionHistory = [];

        while (tasksToProcess.length > 0) {
            this.metrics.iterations++;

            if (this.metrics.iterations > this.config.maxIterations) {
                // FIX: Throw timeout error correctly from within the generator
                throw new PlanningTimeoutError(
                    `Planning timed out after ${this.config.maxIterations} iterations.`
                );
            }

            if (this.metrics.iterations % 20 === 0) {
                const now = performance.now();
                if (now - frameStartTime > this.config.frameBudget) {
                    this.log(
                        "info",
                        `Frame budget of ${this.config.frameBudget}ms exceeded. Yielding...`
                    );
                    yield {
                        status: "running",
                        iterations: this.metrics.iterations,
                    };
                    await new Promise((resolve) => setTimeout(resolve, 0));
                    frameStartTime = performance.now();
                }
            }

            let currentTaskOrName = tasksToProcess.shift();
            let currentTask = null;

            // FIX: Handle task lookup when task is specified by name (string)
            if (typeof currentTaskOrName === "string") {
                currentTask = this.taskRegistry[currentTaskOrName];
                if (!currentTask) {
                    throw new TaskNotFoundError(currentTaskOrName);
                }
            } else {
                currentTask = currentTaskOrName;
            }

            if (currentTask.isPrimitive) {
                if (currentTask.conditions(workingWorldState, workingContext)) {
                    currentTask.effects(workingWorldState, workingContext);
                    finalPlan.push(currentTask);
                } else {
                    this.log(
                        "fail",
                        `Conditions FAILED for ${currentTask.name}. Backtracking...`
                    );
                    this.metrics.backtrackCount++;
                    const backtrackState =
                        this._findBacktrackPoint(decompositionHistory);
                    if (backtrackState) {
                        workingWorldState = backtrackState.worldState.clone();
                        workingContext = structuredClone(
                            backtrackState.context
                        );
                        finalPlan = [...backtrackState.planState];
                        tasksToProcess = backtrackState.tasksToProcess;
                    } else {
                        this.log("fail", "Backtracking failed. No plan found.");
                        return null;
                    }
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
                        worldStateSnapshot: workingWorldState.clone(),
                        contextSnapshot: structuredClone(workingContext),
                        planState: [...finalPlan],
                        remainingTasks: [...tasksToProcess],
                    });
                    tasksToProcess.unshift(...method.subtasks);
                } else {
                    this.log(
                        "fail",
                        `No applicable method found for ${currentTask.name}. Backtracking...`
                    );
                    this.metrics.backtrackCount++;
                    const backtrackState =
                        this._findBacktrackPoint(decompositionHistory);
                    if (backtrackState) {
                        workingWorldState = backtrackState.worldState.clone();
                        workingContext = structuredClone(
                            backtrackState.context
                        );
                        finalPlan = [...backtrackState.planState];
                        tasksToProcess = backtrackState.tasksToProcess;
                    } else {
                        this.log("fail", "Backtracking failed. No plan found.");
                        return null;
                    }
                }
            }
        }

        this.metrics.planningTime = performance.now() - overallStartTime;
        const result = {
            plan: finalPlan,
            context: workingContext,
        };
        if (cacheKey) this.planCache.set(cacheKey, result);
        return result;
    }
}

/**
 * Manages interruption policies and prevents infinite re-planning loops.
 */
export class InterruptionManager {
    constructor(config = {}) {
        this.config = {
            maxReplanAttempts: 3,
            cooldownDuration: 1000, // ms
            backoffMultiplier: 2,
            enableCooldown: true,
            ...config,
        };

        this.replanHistory = new Map(); // taskName -> { count, lastAttempt, cooldownUntil }
        this.activeInterruptors = new Map(); // interruptorId -> InterruptorState
        this.logCallback = () => {};
    }

    setLogCallback(callback) {
        this.logCallback = callback;
    }

    log(type, message, data = {}) {
        this.logCallback(type, message, data);
    }

    registerInterruptor(id, interruptor) {
        this.activeInterruptors.set(id, {
            ...interruptor,
            id,
            lastTriggered: 0,
            triggerCount: 0,
        });
    }

    unregisterInterruptor(id) {
        this.activeInterruptors.delete(id);
    }

    canReplan(taskName) {
        const now = Date.now();
        const history = this.replanHistory.get(taskName);

        if (!history) return true;

        if (this.config.enableCooldown && now < history.cooldownUntil) {
            this.log(
                "warn",
                `Replan blocked for ${taskName}: still in cooldown`,
                {
                    remainingCooldown: history.cooldownUntil - now,
                }
            );
            return false;
        }

        if (history.count >= this.config.maxReplanAttempts) {
            this.log(
                "warn",
                `Replan blocked for ${taskName}: max attempts exceeded`,
                {
                    attempts: history.count,
                    maxAttempts: this.config.maxReplanAttempts,
                }
            );
            return false;
        }

        return true;
    }

    recordReplanAttempt(taskName) {
        const now = Date.now();
        const history = this.replanHistory.get(taskName) || {
            count: 0,
            lastAttempt: 0,
            cooldownUntil: 0,
        };

        history.count++;
        history.lastAttempt = now;

        if (this.config.enableCooldown) {
            const cooldownDuration =
                this.config.cooldownDuration *
                Math.pow(this.config.backoffMultiplier, history.count - 1);
            history.cooldownUntil = now + cooldownDuration;
        }

        this.replanHistory.set(taskName, history);

        this.log("info", `Recorded replan attempt for ${taskName}`, {
            attempt: history.count,
            cooldownUntil: history.cooldownUntil,
        });
    }

    resetReplanHistory(taskName) {
        this.replanHistory.delete(taskName);
        this.log("info", `Reset replan history for ${taskName}`);
    }

    checkInterruptions(worldState, context, currentTask) {
        const now = Date.now();

        for (const [id, interruptor] of this.activeInterruptors) {
            try {
                const result = interruptor.check(
                    worldState,
                    context,
                    currentTask
                );

                if (result && result.interrupted) {
                    interruptor.lastTriggered = now;
                    interruptor.triggerCount++;

                    this.log("info", `Interruption triggered by ${id}`, {
                        reason: result.reason,
                        triggerCount: interruptor.triggerCount,
                    });

                    return {
                        ...result,
                        interruptorId: id,
                        timestamp: now,
                    };
                }
            } catch (error) {
                this.log("error", `Interruptor ${id} threw an error:`, error);
            }
        }

        return null;
    }

    cleanup(maxAge = 300000) {
        const now = Date.now();
        const cutoff = now - maxAge;

        for (const [taskName, history] of this.replanHistory) {
            if (history.lastAttempt < cutoff) {
                this.replanHistory.delete(taskName);
            }
        }
    }
}

/**
 * Central repository for the logic of smart actions.
 */
export const smartActionLibrary = {};

/**
 * Discovers tasks from smart objects in the world and registers them with the planner.
 */
export class TaskDiscoverer {
    static discoverAndRegister(planner, worldState) {
        const worldObjects = worldState.get("worldObjects") || [];
        let discoveredCount = 0;

        for (const obj of worldObjects) {
            if (!obj.smartLink || !obj.smartLink.actionType) continue;

            const actionType = obj.smartLink.actionType;
            const actionLogic = smartActionLibrary[actionType];

            if (actionLogic) {
                const taskName = `${actionType}_${obj.id}`;
                // Avoid re-registering the same task instance
                if (planner.taskRegistry[taskName]) continue;

                const newTask = new SmartObjectTask(taskName, obj, {
                    ...actionLogic,
                    actionType,
                });
                planner.registerTask(newTask);
                discoveredCount++;
            }
        }
        return discoveredCount;
    }
}

/**
 * Plan executor with sophisticated interruption handling and automatic replanning.
 */
export class PlanExecutor {
    constructor(plan, context, options = {}) {
        this.plan = [...plan];
        this.context = context;
        this.currentIndex = 0;
        this.interruptionManager =
            options.interruptionManager || new InterruptionManager();
        this.planner = options.planner;
        this.replanFn = options.replanFn;
        this.rootTask = options.rootTask;
        this.logCallback = options.logCallback || (() => {});

        this.state = "running"; // 'running', 'interrupted', 'completed', 'failed'
        this.lastInterruption = null;
        this.executionStartTime = Date.now();

        this.interruptionManager.setLogCallback(this.logCallback);
    }

    isDone() {
        return (
            this.currentIndex >= this.plan.length || this.state === "completed"
        );
    }

    isFailed() {
        return this.state === "failed";
    }

    isInterrupted() {
        return this.state === "interrupted";
    }

    async tick(worldState, options = {}) {
        if (this.isDone()) {
            if (this.state !== "completed") {
                this.state = "completed";
                this.logCallback(
                    "success",
                    "Plan execution completed successfully."
                );
            }
            return {
                status: "completed",
                task: null,
                executionTime: Date.now() - this.executionStartTime,
            };
        }

        if (this.isFailed()) {
            return {
                status: "failed",
                task: null,
                reason: this.lastInterruption,
            };
        }

        const currentTask = this.plan[this.currentIndex];

        const interruption = this.interruptionManager.checkInterruptions(
            worldState,
            this.context,
            currentTask
        );

        if (interruption) {
            return this.handleInterruption(interruption, worldState, options);
        }

        try {
            if (
                currentTask.conditions &&
                !currentTask.conditions(worldState, this.context)
            ) {
                const conditionFailure = {
                    interrupted: true,
                    reason: "preconditions_failed",
                    taskName: currentTask.name,
                    message: `Preconditions failed for ${currentTask.name} just before execution.`,
                };
                return this.handleInterruption(
                    conditionFailure,
                    worldState,
                    options
                );
            }

            currentTask.executeOperator(this.context);

            if (currentTask.effects) {
                currentTask.effects(worldState, this.context);
            }

            this.currentIndex++;

            if (this.isDone()) {
                this.state = "completed";
                this.logCallback(
                    "success",
                    "Plan execution completed successfully."
                );
                return {
                    status: "completed",
                    task: currentTask,
                    progress: 1,
                    executionTime: Date.now() - this.executionStartTime,
                };
            }

            return {
                status: "executing",
                task: currentTask,
                progress: this.currentIndex / this.plan.length,
            };
        } catch (error) {
            this.logCallback(
                "error",
                `Task execution failed: ${currentTask.name}`,
                error
            );
            const executionFailure = {
                interrupted: true,
                reason: "execution_failed",
                taskName: currentTask.name,
                error: error.message,
            };
            return this.handleInterruption(
                executionFailure,
                worldState,
                options
            );
        }
    }

    async handleInterruption(interruption, worldState, options = {}) {
        this.state = "interrupted";
        this.lastInterruption = interruption;
        this.logCallback("warn", "Plan interrupted", interruption);

        if (!options.enableReplanning || !this.replanFn || !this.rootTask) {
            this.state = "failed";
            return {
                status: "failed",
                interruption,
                reason: "replanning_not_available",
            };
        }

        const taskName = this.rootTask.name || "unknown_root_task";
        if (!this.interruptionManager.canReplan(taskName)) {
            this.state = "failed";
            return {
                status: "failed",
                interruption,
                reason: "replan_cooldown_or_max_attempts",
            };
        }

        try {
            this.interruptionManager.recordReplanAttempt(taskName);
            const replanGenerator = this.replanFn({
                tasks: this.rootTask,
                worldState: worldState,
                context: this.context,
            });

            // FIX: This is a simplified helper to run the generator to completion for the executor.
            const runGenerator = async () => {
                let lastValue;
                while (true) {
                    const { value, done } = await replanGenerator.next();
                    if (done) {
                        return value;
                    }
                    lastValue = value;
                }
            };
            const replanResult = await runGenerator();

            if (replanResult && replanResult.plan.length > 0) {
                this.logCallback(
                    "success",
                    "Replanning successful. New plan generated."
                );
                this.plan = replanResult.plan;
                this.context = replanResult.context;
                this.reset(); // Reset executor state for the new plan
                this.interruptionManager.resetReplanHistory(taskName);
                return {
                    status: "replanned",
                    interruption,
                    newPlan: this.plan,
                };
            } else {
                this.state = "failed";
                this.logCallback(
                    "error",
                    "Replanning failed: No new plan could be found."
                );
                return {
                    status: "failed",
                    interruption,
                    reason: "replan_failed_to_find_plan",
                };
            }
        } catch (error) {
            this.state = "failed";
            this.logCallback(
                "error",
                "Replanning failed with a critical error.",
                error
            );
            return {
                status: "failed",
                interruption,
                reason: "replanning_error",
                error,
            };
        }
    }

    reset() {
        this.currentIndex = 0;
        this.state = "running";
        this.lastInterruption = null;
        this.executionStartTime = Date.now();
    }

    getStats() {
        return {
            totalTasks: this.plan.length,
            completedTasks: this.currentIndex,
            progress:
                this.plan.length > 0 ? this.currentIndex / this.plan.length : 0,
            state: this.state,
            executionTime: Date.now() - this.executionStartTime,
            lastInterruption: this.lastInterruption,
        };
    }
}
