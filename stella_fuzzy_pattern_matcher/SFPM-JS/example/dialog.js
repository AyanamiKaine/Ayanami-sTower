import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { Query } from "../src/Query.js";
import readline from "readline";
import { match } from "../src/RuleMatcher.js";
import { DictionaryFactSource } from "../src/FactSource.js"; // FIX: Import the FactSource adapter.

// The SFPM-JS library (Rule, Criteria, Query, match) is used as-is.
// We are building a new abstraction on top of it.

/**
 * The DialogEngine is a higher-level abstraction that simplifies creating dialog rules.
 * It takes declarative outcomes and translates them into the necessary imperative
 * state changes and rule-matching calls, automatically handling state consumption.
 */
class DialogEngine {
    constructor(worldState, gameRules, dialogManager) {
        this._worldState = worldState;
        this._gameRules = gameRules;
        this._dialogManager = dialogManager;
    }

    /**
     * A helper function to re-run the rule matching process.
     */
    _runQuery() {
        // FIX: Always wrap the worldState Map in a DictionaryFactSource before matching.
        match(this._gameRules, new DictionaryFactSource(this._worldState));
    }

    /**
     * Creates a rule with an automatically generated payload that handles state.
     * @param {Criteria[]} criteria - The conditions for the rule to fire.
     * @param {object} outcome - A declarative object describing the result.
     * @param {string} [name=''] - An optional name for the rule.
     * @param {number} [priority=0] - The priority of the rule.
     */
    addRule(criteria, outcome, name = "", priority = 0) {
        // The magic happens here. We generate a payload function that interprets
        // the declarative 'outcome' object.
        const payload = () => {
            // 1. Automatically consume the triggering facts ('option' or 'trigger').
            for (const c of criteria) {
                if (
                    c.factName === "option" ||
                    c.factName === "trigger" ||
                    c.factName === "command"
                ) {
                    this._worldState.delete(c.factName);
                }
            }

            // 2. Execute the declared outcome.
            if (outcome.text) {
                // The text can be a string or a function for dynamic text.
                const text =
                    typeof outcome.text === "function"
                        ? outcome.text(this._worldState)
                        : outcome.text;
                console.log(text);
            }
            if (outcome.setFacts) {
                for (const [key, value] of Object.entries(outcome.setFacts)) {
                    // Values can be functions for dynamic setting.
                    const val =
                        typeof value === "function"
                            ? value(this._worldState)
                            : value;
                    this._worldState.set(key, val);
                }
            }
            if (outcome.options) {
                // The options can also be a function to allow conditional options.
                const opts =
                    typeof outcome.options === "function"
                        ? outcome.options(this._worldState)
                        : outcome.options;
                this._dialogManager.options.clear();
                for (const [key, text] of Object.entries(opts)) {
                    this._dialogManager.options.set(key, text);
                }
            }
            if (outcome.onLeave) {
                console.log("\nYou end the conversation.");
                this._dialogManager.activeNPC = null;
            }

            // 3. If there's a next step, trigger it.
            if (outcome.nextTrigger) {
                this._worldState.set("trigger", outcome.nextTrigger);
                this._runQuery(); // Automatically continue the chain.
            }
        };

        this._gameRules.push(new Rule(criteria, payload, name, priority));
    }
}

/**
 * The main class that drives the dialog simulation.
 */
class DialogSimulation {
    constructor() {
        this._worldState = new Map();
        this._gameRules = [];
        this._dialogManager = { activeNPC: null, options: new Map() };
        // The engine is the new central piece for rule creation.
        this.engine = new DialogEngine(
            this._worldState,
            this._gameRules,
            this._dialogManager
        );
        this._gameOver = false;
    }

    setupWorld() {
        console.log("Setting up world and character memory...");
        this._worldState.set("knowsSecretPassword", false);
        this._worldState.set("player_name", "Captain");
        this._worldState.set("player_rep", 5);
        this._worldState.set("arcy_timesTalked", 0);
        this._worldState.set("barman_gaveRumor", false);
    }

    /**
     * Rules are now defined declaratively, which is much cleaner and safer.
     */
    setupRules() {
        console.log("Defining dialog logic as rules...");

        // --- Generic Rule to start conversations ---
        this.engine.addRule(
            [new Criteria("command", "talk", Operator.Equal)],
            {
                // The outcome dynamically sets the next trigger based on the NPC.
                setFacts: {
                    trigger: (ws) => `PopulateDialog_${ws.get("target_npc")}`,
                },
                nextTrigger: "run_query", // A non-empty string to kick off the query run.
            },
            "StartConversation"
        );

        // --- Arcy's Dialog Rules ---
        this.engine.addRule(
            [new Criteria("trigger", "PopulateDialog_Arcy", Operator.Equal)],
            {
                // Increment the counter and move to the next step.
                setFacts: {
                    arcy_timesTalked: (ws) => ws.get("arcy_timesTalked") + 1,
                },
                nextTrigger: "ShowGreeting_Arcy",
            },
            "HandleArcyEntry"
        );

        this.engine.addRule(
            [
                new Criteria("trigger", "ShowGreeting_Arcy", Operator.Equal),
                new Criteria("arcy_timesTalked", 2, Operator.LessThanOrEqual),
            ],
            {
                text: (ws) =>
                    `\n[Arcy]: Well hello there, ${ws.get(
                        "player_name"
                    )}. Good to see a fresh face.`,
                nextTrigger: "PopulateArcyOptions",
            },
            "ArcyGreeting_Normal"
        );

        this.engine.addRule(
            [
                new Criteria("trigger", "ShowGreeting_Arcy", Operator.Equal),
                new Criteria("arcy_timesTalked", 2, Operator.GreaterThan),
            ],
            {
                text: `\n[Arcy]: You again? Can't get enough of my charming personality, I see.`,
                nextTrigger: "PopulateArcyOptions",
            },
            "ArcyGreeting_Familiar"
        );

        this.engine.addRule(
            [new Criteria("trigger", "PopulateArcyOptions", Operator.Equal)],
            {
                options: {
                    arcy_ask_password: "I heard you know a secret password.",
                    arcy_ask_reputation: "What do you think of me?",
                    leave: "I have to go.",
                },
            },
            "ShowArcyOptions",
            -1
        );

        // --- Barman's Dialog Rules ---
        this.engine.addRule(
            [new Criteria("trigger", "PopulateDialog_Barman", Operator.Equal)],
            {
                text: "\n[Barman]: Wiping a glass, he grunts. 'What'll it be?'",
                // This logic is now cleanly inside the declarative outcome.
                options: (ws) => {
                    const opts = {
                        barman_ask_rumors: "Heard any rumors?",
                        leave: "Nothing right now.",
                    };
                    if (ws.get("knowsSecretPassword")) {
                        opts[
                            "barman_secret_menu"
                        ] = `'I've heard the sky is blue at midnight.'`;
                    }
                    return opts;
                },
            },
            "PopulateBarmanOptions"
        );

        // --- Player Choice Handling ---
        this.engine.addRule(
            [new Criteria("option", "arcy_ask_password", Operator.Equal)],
            {
                text: `\n[Arcy]: Leaning closer, she whispers, 'Tell the barman the sky is blue at midnight. See what he gives you.'`,
                setFacts: { knowsSecretPassword: true },
                nextTrigger: "PopulateArcyOptions",
            },
            "Response_AskArcyPassword"
        );

        this.engine.addRule(
            [new Criteria("option", "arcy_ask_reputation", Operator.Equal)],
            {
                text: (ws) => {
                    const rep = ws.get("player_rep");
                    if (rep > 8)
                        return "\n[Arcy]: You're a hero around here! Good to see you.";
                    if (rep < 3)
                        return "\n[Arcy]: I've heard things... and I'm keeping my eye on you.";
                    return "\n[Arcy]: You seem alright, I suppose.";
                },
                nextTrigger: "PopulateArcyOptions",
            },
            "Response_AskArcyReputation"
        );

        this.engine.addRule(
            [new Criteria("option", "barman_ask_rumors", Operator.Equal)],
            {
                text: (ws) =>
                    ws.get("barman_gaveRumor")
                        ? "\n[Barman]: I told you all I know."
                        : "\n[Barman]: Heard Arcy has a bit of a temper. Watch yourself.",
                setFacts: { barman_gaveRumor: true },
                nextTrigger: "PopulateDialog_Barman",
            },
            "Response_AskBarmanRumors"
        );

        this.engine.addRule(
            [new Criteria("option", "barman_secret_menu", Operator.Equal)],
            {
                text: "\n[Barman]: He raises an eyebrow, then slides a dusty bottle across the bar. 'On the house.' Your reputation increases.",
                setFacts: {
                    player_rep: (ws) => ws.get("player_rep") + 1,
                    knowsSecretPassword: false,
                },
                nextTrigger: "PopulateDialog_Barman",
            },
            "Response_BarmanSecret"
        );

        this.engine.addRule(
            [new Criteria("option", "leave", Operator.Equal)],
            { onLeave: true },
            "LeaveConversation"
        );
    }

    run() {
        this.setupWorld();
        this.setupRules();

        const rl = readline.createInterface({
            input: process.stdin,
            output: process.stdout,
        });

        const gameLoop = () => {
            if (this._gameOver) {
                console.log("\nYou leave the cantina.");
                rl.close();
                return;
            }

            if (!this._dialogManager.activeNPC) {
                console.log("\nYou see a few people around the cantina.");
                console.log("1: Talk to Arcy, the spacer.");
                console.log("2: Talk to the Barman.");
                console.log("3: Leave the cantina.");

                rl.question("\n> ", (answer) => {
                    const choice = parseInt(answer.trim(), 10);

                    if (choice === 1) {
                        this._worldState.set("target_npc", "Arcy");
                        this._worldState.set("command", "talk");
                    } else if (choice === 2) {
                        this._worldState.set("target_npc", "Barman");
                        this._worldState.set("command", "talk");
                    } else if (choice === 3) {
                        this._gameOver = true;
                    } else {
                        console.log("Invalid choice.");
                    }

                    if (choice === 1 || choice === 2) {
                        this._dialogManager.activeNPC =
                            this._worldState.get("target_npc");
                        // FIX: Always wrap the worldState Map in a DictionaryFactSource before matching.
                        match(
                            this._gameRules,
                            new DictionaryFactSource(this._worldState)
                        );
                    }
                    gameLoop();
                });
            } else {
                const optionKeys = Array.from(
                    this._dialogManager.options.keys()
                );
                if (optionKeys.length === 0) {
                    this._dialogManager.activeNPC = null;
                    gameLoop();
                    return;
                }

                console.log("\nWhat do you say?");
                optionKeys.forEach((key, index) => {
                    console.log(
                        `${index + 1}: ${this._dialogManager.options.get(key)}`
                    );
                });

                rl.question("\n> ", (answer) => {
                    const choice = parseInt(answer.trim(), 10) - 1;
                    if (choice >= 0 && choice < optionKeys.length) {
                        this._worldState.set("option", optionKeys[choice]);
                        // FIX: Always wrap the worldState Map in a DictionaryFactSource before matching.
                        match(
                            this._gameRules,
                            new DictionaryFactSource(this._worldState)
                        );
                    } else {
                        console.log("Invalid choice.");
                    }
                    gameLoop();
                });
            }
        };

        console.log("\n--- Welcome to the Star-themed Cantina ---");
        gameLoop();
    }
}

// --- Entry Point ---
const game = new DialogSimulation();
game.run();
