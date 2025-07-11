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

    /**
     * Registers an interruptor that can check for plan invalidation.
     * @param {string} id - Unique identifier for this interruptor
     * @param {object} interruptor - The interruptor configuration
     */
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

    /**
     * Checks if we can attempt to replan for a given task.
     * @param {string} taskName - The name of the task being replanned
     * @returns {boolean} Whether replanning is allowed
     */
    canReplan(taskName) {
        const now = Date.now();
        const history = this.replanHistory.get(taskName);

        if (!history) return true;

        // Check if we're still in cooldown
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

        // Check if we've exceeded max attempts
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

    /**
     * Records a replan attempt and updates cooldown.
     * @param {string} taskName - The name of the task being replanned
     */
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

    /**
     * Resets the replan history for a task (e.g., when a plan succeeds).
     * @param {string} taskName - The name of the task
     */
    resetReplanHistory(taskName) {
        this.replanHistory.delete(taskName);
        this.log("info", `Reset replan history for ${taskName}`);
    }

    /**
     * Checks all registered interruptors and returns the reason for interruption if any.
     * @param {WorldStateProxy} worldState - Current world state
     * @param {object} context - Current execution context
     * @param {PrimitiveTask} currentTask - The task being executed
     * @returns {object|null} Interruption result or null if no interruption
     */
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

    /**
     * Clears old replan history entries to prevent memory leaks.
     * @param {number} maxAge - Maximum age in milliseconds (default: 5 minutes)
     */
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
 * Plan executor with sophisticated interruption handling and automatic replanning.
 */
export class PlanExecutor {
    constructor(plan, context, options = {}) {
        this.plan = [...plan];
        this.context = context;
        this.currentIndex = 0;
        this.interruptionManager =
            options.interruptionManager || new InterruptionManager();
        this.planner = options.planner; // Reference to planner for replanning
        this.rootTask = options.rootTask; // Original task for replanning
        this.logCallback = options.logCallback || (() => {});

        // State management
        this.state = "running"; // 'running', 'interrupted', 'completed', 'failed'
        this.lastInterruption = null;
        this.executionStartTime = Date.now();

        // Set up logging
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

    /**
     * Executes the next step with intelligent interruption handling.
     * @param {WorldStateProxy} worldState - Current world state
     * @param {object} options - Execution options, e.g., { enableReplanning: true }
     * @returns {object} Execution result with a status like 'executing', 'completed', 'failed', 'replanned'.
     */
    tick(worldState, options = {}) {
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

        // Check for interruptions before executing
        const interruption = this.interruptionManager.checkInterruptions(
            worldState,
            this.context,
            currentTask
        );

        if (interruption) {
            return this.handleInterruption(interruption, worldState, options);
        }

        // Execute the task
        try {
            // Check primitive task conditions again right before execution, as the world might have changed.
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

            // It's crucial to apply the effects to the *actual* world state proxy, not a simulated one.
            if (currentTask.effects) {
                currentTask.effects(worldState, this.context);
            }

            this.currentIndex++;

            // Check if the plan is now complete after this step
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

    /**
     * Handles interruptions with automatic replanning if possible.
     * @param {object} interruption - The interruption details
     * @param {WorldStateProxy} worldState - Current world state
     * @param {object} options - Execution options
     * @returns {object} Handling result
     */
    handleInterruption(interruption, worldState, options = {}) {
        this.state = "interrupted";
        this.lastInterruption = interruption;

        this.logCallback("warn", "Plan interrupted", interruption);

        // If replanning is disabled or not possible, enter failed state
        if (!options.enableReplanning || !this.planner || !this.rootTask) {
            this.state = "failed";
            return {
                status: "failed",
                interruption,
                reason: "replanning_not_available",
            };
        }

        // Check if we are allowed to replan (cooldown, max attempts)
        const taskName = this.rootTask.name || "unknown_root_task";
        if (!this.interruptionManager.canReplan(taskName)) {
            this.state = "failed";
            return {
                status: "failed",
                interruption,
                reason: "replan_cooldown_or_max_attempts",
            };
        }

        // Attempt to replan
        try {
            this.interruptionManager.recordReplanAttempt(taskName);

            const replanResult = this.planner.findPlan({
                tasks: this.rootTask,
                worldState: worldState, // Use the current, changed world state
                context: this.context,
            });

            if (replanResult && replanResult.plan.length > 0) {
                // Successfully replanned
                this.interruptionManager.resetReplanHistory(taskName);
                this.plan = [...replanResult.plan];
                this.context = replanResult.context;
                this.currentIndex = 0;
                this.state = "running";

                this.logCallback(
                    "success",
                    "Successfully replanned after interruption",
                    {
                        newPlanLength: this.plan.length,
                        reason: interruption.reason,
                    }
                );

                return {
                    status: "replanned",
                    interruption,
                    newPlan: this.plan,
                    newPlanLength: this.plan.length,
                };
            } else {
                // Replanning failed to find a new plan
                this.state = "failed";
                this.logCallback(
                    "error",
                    "Replanning failed to find a valid plan.",
                    { reason: interruption.reason }
                );
                return {
                    status: "failed",
                    interruption,
                    reason: "replanning_failed",
                };
            }
        } catch (error) {
            this.logCallback("error", "Replanning threw an error", error);
            this.state = "failed";
            return {
                status: "failed",
                interruption,
                reason: "replanning_error",
                error: error.message,
            };
        }
    }

    /**
     * Resets the executor state for a fresh start.
     */
    reset() {
        this.currentIndex = 0;
        this.state = "running";
        this.lastInterruption = null;
        this.executionStartTime = Date.now();
    }

    /**
     * Gets current execution statistics.
     * @returns {object} Execution stats
     */
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

/**
 * Utility class for creating common interruptors.
 */
export class InterruptorFactory {
    /**
     * Creates an interruptor that checks if a target entity still exists.
     * @param {string} targetKey - The key in context that holds the target
     * @param {string} entityCollection - The collection name in world state
     * @returns {object} Interruptor configuration
     */
    static createTargetExistsInterruptor(targetKey, entityCollection) {
        return {
            check: (worldState, context, currentTask) => {
                const target = context[targetKey];
                if (!target) return null;

                const entities = worldState.get(entityCollection) || [];
                const targetExists = entities.some((e) => e.id === target.id);

                if (!targetExists) {
                    return {
                        interrupted: true,
                        reason: "target_no_longer_exists",
                        targetId: target.id,
                        message: `Target ${target.id} no longer exists`,
                    };
                }
                return null;
            },
        };
    }

    /**
     * Creates an interruptor that checks if the agent is still alive/functional.
     * @param {string} agentKey - The key in context that holds the agent
     * @param {string} entityCollection - The collection name in world state
     * @returns {object} Interruptor configuration
     */
    static createAgentHealthInterruptor(agentKey, entityCollection) {
        return {
            check: (worldState, context, currentTask) => {
                const agent = context[agentKey];
                if (!agent) return null;

                const entities = worldState.get(entityCollection) || [];
                const currentAgent = entities.find((e) => e.id === agent.id);

                if (!currentAgent || currentAgent.health <= 0) {
                    return {
                        interrupted: true,
                        reason: "agent_dead",
                        agentId: agent.id,
                        message: `Agent ${agent.id} is dead or missing`,
                    };
                }
                return null;
            },
        };
    }

    /**
     * Creates an interruptor that checks if a resource is still available.
     * @param {string} resourceName - The name of the resource to check
     * @param {number} requiredAmount - The minimum amount required
     * @returns {object} Interruptor configuration
     */
    static createResourceInterruptor(resourceName, requiredAmount) {
        return {
            check: (worldState, context, currentTask) => {
                const currentAmount = worldState.get(resourceName) || 0;

                if (currentAmount < requiredAmount) {
                    return {
                        interrupted: true,
                        reason: "insufficient_resources",
                        resourceName,
                        required: requiredAmount,
                        available: currentAmount,
                        message: `Insufficient ${resourceName}: need ${requiredAmount}, have ${currentAmount}`,
                    };
                }
                return null;
            },
        };
    }

    /**
     * Creates an interruptor that checks if a task type should be interrupted.
     * @param {Array<string>} taskNames - Task names to monitor
     * @param {function} condition - Function that returns interruption if condition is met
     * @returns {object} Interruptor configuration
     */
    static createTaskSpecificInterruptor(taskNames, condition) {
        return {
            check: (worldState, context, currentTask) => {
                if (!taskNames.includes(currentTask.name)) {
                    return null;
                }

                return condition(worldState, context, currentTask);
            },
        };
    }

    /**
     * Creates an interruptor that triggers after a certain time limit.
     * @param {number} timeLimit - Time limit in milliseconds
     * @returns {object} Interruptor configuration
     */
    static createTimeoutInterruptor(timeLimit) {
        const startTime = Date.now();

        return {
            check: (worldState, context, currentTask) => {
                const elapsed = Date.now() - startTime;

                if (elapsed > timeLimit) {
                    return {
                        interrupted: true,
                        reason: "timeout",
                        timeLimit,
                        elapsed,
                        message: `Execution timeout: ${elapsed}ms > ${timeLimit}ms`,
                    };
                }
                return null;
            },
        };
    }
}
