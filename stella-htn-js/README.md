# **stella-htn-js**

AI GENERATED README

A flexible and powerful Hierarchical Task Network (HTN) planner for creating complex and emergent AI behaviors in JavaScript. This library is designed for game developers and AI enthusiasts looking for a goal-oriented planning system that is easy to integrate and extend.

## **Features**

* **Hierarchical Planning**: Define complex behaviors by decomposing them into smaller, manageable tasks.  
* **Dynamic & Flexible**: Plans are generated at runtime, allowing AI to adapt to a changing world.  
* **Smart Objects**: A system for creating context-aware interactions with world objects.  
* **Plan Interruption & Replanning**: Robustly handle unexpected events and replan on the fly.  
* **Performance-Oriented**: Designed with performance in mind, including features like plan caching and an incremental planner.  
* **ESM Native**: Built as a native ES Module.

## **Installation**

You can install stella-htn-js via npm or your favorite package manager:
```bash
npm install stella-htn-js
```
Or with bun:
```bash
bun add stella-htn-js
```
## **Core Concepts**

stella-htn-js is based on a few core concepts:

* **Tasks**: The building blocks of behavior.  
  * **PrimitiveTask**: Represents a single, executable action that directly affects the world state (e.g., Attack, MoveTo).  
  * **CompoundTask**: Represents a high-level goal that is achieved by completing a sequence of sub-tasks (e.g., EngageEnemy). It contains multiple *methods*, which are different ways to accomplish the goal.  
* **Planner**: The "brain" of the system. It takes a high-level task and a world state, and finds a sequence of primitive tasks (a "plan") to achieve it.  
* **WorldStateProxy**: A flexible interface that allows the planner to interact with your game's world state, no matter how it's structured.  
* **PlanExecutor**: Executes a plan step-by-step and handles interruptions.

## **Quick Start**

Here's a simple example of how to set up and run the planner.

### **1. Define Your Tasks**

First, define the primitive and compound tasks that your AI can perform.
```js
import { PrimitiveTask, CompoundTask } from 'stella-htn-js';

// A primitive task to attack an enemy  
const attackEnemy = new PrimitiveTask('AttackEnemy', {  
    conditions: (ws) => ws.get('agentHasWeapon') && ws.get('enemyIsVisible'),  
    effects: (ws) => ws.set('enemyDefeated', true),  
    operator: () => console.log("AI is attacking the enemy!")  
});

// A compound task to get a weapon  
const getWeapon = new CompoundTask('GetWeapon', [  
    {  
        name: 'Pick up a nearby weapon',  
        conditions: (ws) => !ws.get('agentHasWeapon'),  
        subtasks: ['PickupWeapon_scattergun_01'] // This would be a SmartObjectTask  
    }  
]);

// A high-level goal to engage an enemy  
const engageEnemy = new CompoundTask('EngageEnemy', [  
    {  
        name: 'Attack if ready',  
        priority: 10,  
        conditions: (ws) => ws.get('agentHasWeapon'),  
        subtasks: [attackEnemy]  
    },  
    {  
        name: 'Get a weapon first',  
        priority: 5,  
        subtasks: [getWeapon, 'EngageEnemy'] // Recursively call this task  
    }  
]);
```
### **2. Set Up the World State**

The planner needs to know about the current state of the world. WorldStateProxy lets you connect your game's state.
```js
import { WorldStateProxy } from 'stella-htn-js';

let gameState = {  
    agentHasWeapon: false,  
    enemyIsVisible: true,  
    enemyDefeated: false,  
    // ... other game state  
};

const worldState = new WorldStateProxy({  
    getState: (key) => (key ? gameState[key] : gameState),  
    setState: (key, value) => (gameState[key] = value),  
    clone: () => {  
        const clonedState = structuredClone(gameState);  
        return new WorldStateProxy({  
            getState: (key) => (key ? clonedState[key] : clonedState),  
            setState: (key, value) => (clonedState[key] = value),  
            clone: () => { /*...recursive clone setup...*/ }  
        });  
    }  
});
```
### **3. Find a Plan**

Now, create a Planner instance, register your tasks, and find a plan.
```js
import { Planner } from 'stella-htn-js';

const planner = new Planner();  
planner.registerTask(attackEnemy);  
planner.registerTask(getWeapon);  
planner.registerTask(engageEnemy);  
// ... register other tasks

const planResult = await runPlannerToCompletion(planner, {  
    tasks: engageEnemy,  
    worldState: worldState,  
    context: { agentId: 'agent_01' }  
});

if (planResult) {  
    console.log('Plan found:', planResult.plan.map(task => task.name));  
    // Now you can execute this plan!  
} else {  
    console.log('No plan found.');  
}

// Helper to run the async generator planner  
async function runPlannerToCompletion(planner, options) {  
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
}
```
## **API Reference**

### **Planner**

The main class for finding plans.

* `new Planner(config)`: Creates a new planner.  
  * `config.maxIterations`: Max planning steps.  
  * `config.enablePlanCaching`: Caches plans for given states.  
* `registerTask(task)`: Registers a task with the planner.  
* `findPlan({ tasks, worldState, context })`: Asynchronously finds a plan. Returns a generator.

### **PlanExecutor**

Executes a plan and handles interruptions.

* `new PlanExecutor(plan, context, options)`  
* `tick(worldState)`: Executes the next step of the plan.  
* `isDone()`: Returns true if the plan is complete.

### **InterruptionManager**

Manages conditions that can interrupt a plan.

* `registerInterruptor(id, interruptor)`: Adds a condition that can interrupt the plan.

## **Contributing**

Contributions are welcome! Please feel free to submit a pull request or open an issue.

## **License**

This project is licensed under the MIT License. See the LICENSE file for details.