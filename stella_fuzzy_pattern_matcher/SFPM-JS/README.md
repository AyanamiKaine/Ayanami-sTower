*AI Generated README*
# **sfpm-js**
**sfpm-js** is a lightweight, dependency-free, forward-chaining inference engine written in modern JavaScript. It is designed for managing complex state and logic in a declarative way, making it ideal for game development (especially for AI and quest/dialog systems), interactive narratives, simulations, and other dynamic applications.

The core principle is simple: you define a set of Rules, provide the current state as a collection of Facts, and the engine finds and executes the most specific rule that matches the current situation.

## **Core Concepts**

The system is built around a few key ideas:

* **Facts:** Simple key-value pairs that represent the current state of your world or application (e.g., PlayerLocation: 'Dungeon', HasKey: false). The collection of all current facts is managed by a FactSource.  
* **Criteria:** The building blocks of rules. A single criterion is a condition that checks a fact against a value (e.g., "is the PlayerLocation fact equal to 'Dungeon'?").  
* **Rules:** A rule is a collection of Criteria and a payload (a function to execute). A rule is considered a match only if **all** of its criteria are met by the current set of facts.  
* **Query:** An object that holds the current facts and provides a simple interface to trigger the rule-matching process.  
* **Specificity-Based Matching:** When multiple rules match the current facts, the engine automatically selects the **most specific** one to execute. Specificity is determined by the number of criteria in a rule—more criteria means more specific. If multiple rules have the same highest specificity, one is chosen at random.

## **Installation**

You can install the package using npm or your favorite package manager like bun or yarn.
```bash
npm install sfpm-js
```
Or with Bun:
```bash
bun add sfpm-js
```
## **Usage & API**

Here’s a step-by-step guide to using sfpm-js.

### **1\. Import the necessary classes**
```js
import { Query } from './src/Query.js';  
import { Rule } from './src/Rule.js';  
import { Criteria, Operator } from './src/Criteria.js';
```
### **2\. Create a Query object with initial facts**

The Query object holds the state of your application. You can initialize it with a Map of facts.
```js
// The world state is a collection of facts.  
const worldState = new Map([  
    ['playerHealth', 80],  
    ['playerLocation', 'Forest'],  
    ['isRaining', true],  
    ['hasMagicSword', false],  
]);

// The Query object is our interface to the rule engine.  
const query = new Query(worldState);
```
### **3\. Define your Rules**

Rules consist of criteria and a payload. The Operator enum provides different ways to compare fact values.

* Operator.Equal  
* Operator.NotEqual  
* Operator.GreaterThan  
* Operator.LessThan  
* Operator.GreaterThanOrEqual  
* Operator.LessThanOrEqual  
* Operator.Predicate (for custom logic via a function)
```js
// This array will hold all the logic of our application.  
const gameRules = [];

// Rule 1: A general rule for being in the forest.  
gameRules.push(new Rule(  
    [  
        new Criteria('playerLocation', 'Forest', Operator.Equal)  
    ],  
    () => {  
        console.log("It's a bit dark in the forest.");  
    }  
));

// Rule 2: A more specific rule for when it's raining in the forest.  
// This rule will be chosen over Rule 1 if it's raining because it has more criteria.  
gameRules.push(new Rule(  
    [  
        new Criteria('playerLocation', 'Forest', Operator.Equal),  
        new Criteria('isRaining', true, Operator.Equal)  
    ],  
    () => {  
        console.log("The forest is dark and wet from the rain. You should find shelter.");  
    },  
    "Rainy Forest Rule" // Optional name for debugging  
));

// Rule 3: A rule that uses a predicate for a custom check.  
// This rule is even more specific.  
gameRules.push(new Rule(  
    [  
        new Criteria('playerLocation', 'Forest', Operator.Equal),  
        new Criteria('isRaining', true, Operator.Equal),  
        new Criteria('playerHealth', (health) => health \< 50, Operator.Predicate)  
    ],  
    () => {  
        console.log("You're cold, wet, and injured. Your chances don't look good.");  
    }  
));

// Rule 4: A rule that modifies the world state.  
gameRules.push(new Rule(  
    [  
        new Criteria('playerLocation', 'Forest', Operator.Equal),  
        new Criteria('hasMagicSword', false, Operator.Equal)  
    ],  
    () => {  
        console.log("You find a glowing sword stuck in a stone! You pull it free.");  
        // The payload can modify the facts for subsequent matches.  
        query.add('hasMagicSword', true);  
    },  
    "Find Sword Rule",  
    10 // A higher priority can break ties among rules with the same specificity.  
));
```
### **4\. Run the Matcher**

Call query.match() with your rules to find and execute the best matching rule.
```js
console.log("--- First Turn \---");  
query.match(gameRules);  
// Expected Output: "You find a glowing sword stuck in a stone\! You pull it free."  
// Why? "Find Sword Rule" (2 criteria) and "Rainy Forest Rule" (2 criteria) both match.  
// "Find Sword Rule" has a higher priority (10 vs 0), so it wins.

console.log("\\n--- Second Turn \---");  
// The state has changed (\`hasMagicSword\` is now true). Let's match again.  
query.match(gameRules);  
// Expected Output: "The forest is dark and wet from the rain. You should find shelter."  
// Why? "Find Sword Rule" no longer matches. "Rainy Forest Rule" (2 criteria) is now the most specific valid rule.

// Now let's change the health to trigger the most specific rule.  
console.log("\\n--- Third Turn (Health is low) \---");  
query.add('playerHealth', 40);  
query.match(gameRules);  
// Expected Output: "You're cold, wet, and injured. Your chances don't look good."  
// Why? The rule with 3 criteria now matches and is the most specific.
```
## **Examples**

This repository includes more advanced examples in the /example directory:

* textAdventure.js: A complete, playable mini text adventure game that showcases how to structure game logic, locations, and item interactions using sfpm-js.  
* dialog.js: A sophisticated dialog system built on top of the engine. It demonstrates how to create branching conversations and manage character memory with a declarative API.

## **Running Tests**

The project uses bun:test for testing. To run the test suite:

bun test

## **Benchmarking**

A benchmark suite using mitata is included to measure the performance of rule evaluation and matching under various conditions.

To run the benchmarks:

bun run ./bench/sfpm.bench.js

## **Contributing**

Contributions are welcome\! If you find a bug or have a feature request, please open an issue. If you'd like to contribute code, please fork the repository and submit a pull request.

## **License**

This project is licensed under the MIT License. See the [LICENSE](https://www.google.com/search?q=LICENSE) file for details.