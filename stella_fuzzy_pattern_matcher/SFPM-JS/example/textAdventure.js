import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { Query } from "../src/Query.js";

/**
 * Represents the distinct locations in the game.
 * In C#, this was a class with static members. In JavaScript,
 * a simple frozen object serves the same purpose, providing
 * immutable, unique location identifiers.
 */
const Location = Object.freeze({
    DungeonCell: { name: "Dungeon Cell" },
    Hallway: { name: "Hallway" },
    GuardRoom: { name: "Guard Room" },
    Outside: { name: "Outside" },
});

/**
 * A simple Levenshtein distance function to find the similarity between two strings.
 * This helps in detecting typos.
 * @param {string} s1 The first string.
 * @param {string} s2 The second string.
 * @returns {number} The number of edits to get from s1 to s2.
 */
const getLevenshteinDistance = (s1, s2) => {
    s1 = s1.toLowerCase();
    s2 = s2.toLowerCase();

    const costs = [];
    for (let i = 0; i <= s1.length; i++) {
        let lastValue = i;
        for (let j = 0; j <= s2.length; j++) {
            if (i === 0) {
                costs[j] = j;
            } else {
                if (j > 0) {
                    let newValue = costs[j - 1];
                    if (s1.charAt(i - 1) !== s2.charAt(j - 1)) {
                        newValue =
                            Math.min(Math.min(newValue, lastValue), costs[j]) +
                            1;
                    }
                    costs[j - 1] = lastValue;
                    lastValue = newValue;
                }
            }
        }
        if (i > 0) costs[s2.length] = lastValue;
    }
    return costs[s2.length];
};

/**
 * The main class that drives the game simulation.
 * It manages the world state, game rules, and the main game loop.
 */
class GameSimulation {
    constructor() {
        // _worldState holds all the dynamic facts about the game world.
        // A JavaScript Map is the direct equivalent of the C# Dictionary used here.
        this._worldState = new Map();

        // _gameRules will contain all the logic of our game, defined as Rule objects.
        this._gameRules = [];

        // The Query object is the interface to our rule-matching engine.
        // It's initialized with the world state, which it will match rules against.
        this._query = new Query(this._worldState);

        // A simple flag to control the main game loop.
        this._gameOver = false;
    }

    /**
     * Initializes the world state with all the starting values for our game.
     */
    setupWorld() {
        console.log("Setting up world...");
        this._worldState.set("PlayerLocation", Location.DungeonCell);
        this._worldState.set("HasRustyKey", false);
        this._worldState.set("CellDoorUnlocked", false);
        this._worldState.set("GuardIsDrowsy", true); // New initial state for the guard
        this._worldState.set("GuardIsAwake", false); // The guard is not fully awake initially
        this._worldState.set("HasRock", false);
        this._worldState.set("HasStaleBread", false); // New item
        this._worldState.set("HasMurkyWater", false); // New item
        this._worldState.set("HasSoggyBread", false); // New "crafted" item
        this._worldState.set("HasMainKey", false);
        this._worldState.set("GameCommand", ""); // This fact will be updated with player input.
    }

    /**
     * Defines all the interactive logic of the game as a collection of rules.
     * Each rule is a combination of conditions (Criteria) and an action (Payload).
     */
    setupRules() {
        console.log("Defining game logic as rules...");

        // --- META COMMANDS ---
        this._gameRules.push(
            new Rule(
                [new Criteria("GameCommand", "quit", Operator.Equal)],
                () => {
                    console.log("\nThanks for playing!");
                    this._gameOver = true;
                }
            )
        );

        this._gameRules.push(
            new Rule(
                [new Criteria("GameCommand", "help", Operator.Equal)],
                () =>
                    console.log(
                        "\nAvailable commands: look, take [item], use [item], drink [item], unlock door, throw [item], go [direction], quit."
                    )
            )
        );

        // --- LOOK RULES (Context-sensitive descriptions) ---
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "look", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                ],
                () =>
                    console.log(
                        "\nYou are in a damp, cold dungeon cell. A heavy wooden door is to the north. In the corner, you see a glint of metal on a small pile of straw, next to a crust of stale bread. A loose rock sits by the wall."
                    )
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "look", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () =>
                    console.log(
                        "\nYou are in a narrow stone hallway. Your cell is to the south. The hallway continues to the east, where you see a wooden door. A steady drip of murky water falls from a crack in the ceiling into a small puddle. To the west is a heavy iron door leading outside."
                    )
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "look", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.GuardRoom,
                        Operator.Equal
                    ),
                    new Criteria("GuardIsDrowsy", true, Operator.Equal),
                ],
                () =>
                    console.log(
                        "\nYou peek into the guard room. A burly guard is nodding off at a table, but his eyes occasionally snap open. He looks hungry. A large iron key hangs on a hook on the far wall."
                    )
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "look", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.GuardRoom,
                        Operator.Equal
                    ),
                    new Criteria("GuardIsAwake", true, Operator.Equal),
                ],
                () =>
                    console.log(
                        "\nYou peek into the guard room. A burly guard is sitting at a table, watching you carefully. A large iron key hangs on a hook on the far wall."
                    )
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "look", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.GuardRoom,
                        Operator.Equal
                    ),
                    new Criteria("GuardIsAwake", false, Operator.Equal),
                    new Criteria("GuardIsDrowsy", false, Operator.Equal),
                ],
                () =>
                    console.log(
                        "\nYou are in the guard room. The guard is slumped over the table, fast asleep, an empty plate in front of him. A large iron key hangs on a hook on the wall."
                    )
            )
        );

        // --- ITEM INTERACTION RULES ---
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take bread", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log("\nYou pick up the crust of stale bread.");
                    this._worldState.set("HasStaleBread", true);
                }
            )
        );
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take water", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log(
                        "\nYou cup your hands and scoop up some murky water."
                    );
                    this._worldState.set("HasMurkyWater", true);
                }
            )
        );
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "drink water", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log(
                        "\nYou stoop and slurp some water from the puddle. It tastes of dirt and regret, but the drip from the ceiling continues to replenish it."
                    );
                }
            )
        );
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "use bread", Operator.Equal),
                    new Criteria("HasStaleBread", true, Operator.Equal),
                    new Criteria("HasMurkyWater", true, Operator.Equal),
                ],
                () => {
                    console.log(
                        "\nYou combine the bread and water to make a disgusting, but tempting, soggy bread ball."
                    );
                    this._worldState.set("HasSoggyBread", true);
                    this._worldState.set("HasStaleBread", false);
                    this._worldState.set("HasMurkyWater", false);
                }
            )
        );
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "throw bread", Operator.Equal),
                    new Criteria("HasSoggyBread", true, Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log(
                        "\nYou toss the soggy bread into the guard room. The drowsy guard's eyes light up. He snatches the bread, devours it in two bites, and promptly slumps over, fast asleep."
                    );
                    this._worldState.set("GuardIsDrowsy", false);
                    this._worldState.set("GuardIsAwake", false);
                    this._worldState.set("HasSoggyBread", false);
                },
                "Feed Guard",
                5
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take key", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log(
                        "\nYou search the straw and find a small, rusty key."
                    );
                    this._worldState.set("HasRustyKey", true);
                },
                "Take Rusty Key",
                10
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take key", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.GuardRoom,
                        Operator.Equal
                    ),
                    new Criteria("GuardIsAwake", false, Operator.Equal),
                    new Criteria("GuardIsDrowsy", false, Operator.Equal),
                ],
                () => {
                    console.log(
                        "\nYou sneak over and quietly lift the main key from the hook."
                    );
                    this._worldState.set("HasMainKey", true);
                },
                "Take Main Key (sleeping guard)",
                10
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take key", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.GuardRoom,
                        Operator.Equal
                    ),
                    new Criteria("GuardIsDrowsy", true, Operator.Equal),
                ],
                () => {
                    console.log(
                        "\nYou reach for the key, but the guard stirs and his eyes snap open. 'Thief!' he shouts, raising his axe. You have failed."
                    );
                    this._gameOver = true;
                },
                "Take Main Key (drowsy guard)",
                10
            )
        ); // GAME OVER

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "take rock", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log("\nYou pick up a fist-sized rock.");
                    this._worldState.set("HasRock", true);
                }
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "throw rock", Operator.Equal),
                    new Criteria("HasRock", true, Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () => {
                    console.log(
                        "\nYou throw the rock down the hallway. It makes a loud clatter. The drowsy guard is startled and becomes fully alert!"
                    );
                    this._worldState.set("GuardIsDrowsy", false);
                    this._worldState.set("GuardIsAwake", true); // Making noise makes things worse!
                    this._worldState.set("HasRock", false);
                },
                "Throw Rock",
                5
            )
        );

        // --- DOOR AND MOVEMENT RULES ---
        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "unlock door", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                    new Criteria("HasRustyKey", true, Operator.Equal),
                ],
                () => {
                    console.log(
                        "\nThe rusty key fits the lock perfectly. With a loud *clunk*, the cell door unlocks."
                    );
                    this._worldState.set("CellDoorUnlocked", true);
                },
                "Unlock Cell Door",
                10
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "go north", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                    new Criteria("CellDoorUnlocked", true, Operator.Equal),
                ],
                () => {
                    console.log("\nYou open the door and step into a hallway.");
                    this._worldState.set("PlayerLocation", Location.Hallway);
                },
                "Go North from Cell",
                10
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "go north", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.DungeonCell,
                        Operator.Equal
                    ),
                ],
                () => console.log("\nThe door is locked.")
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "go east", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () => this._worldState.set("PlayerLocation", Location.GuardRoom)
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "go south", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () =>
                    this._worldState.set("PlayerLocation", Location.DungeonCell)
            )
        );

        this._gameRules.push(
            new Rule(
                [
                    new Criteria("GameCommand", "go west", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                    new Criteria("HasMainKey", true, Operator.Equal),
                ],
                () => {
                    console.log(
                        "\nThe main key slides into the heavy iron door. With a great effort, you turn it and the door swings open to reveal the moonlit forest. You are free!"
                    );
                    this._worldState.set("PlayerLocation", Location.Outside);
                    this._gameOver = true;
                },
                "Escape!",
                10
            )
        ); // WIN!

        this._gameRules.push(
            new Rule(
                [
                    /*
                    Do you notice something?
                    We do not write defensive logic, like does not have key.

                    Why? 

                    Because we write instead rules that are more specific like, hasIronKey
                    and the most specific key is always selected first. Sorcery implements 
                    them https://youtu.be/HZft_U4Fc-U?si=RPYTQuu1hqJtoN43&t=1368 
                    (Narrative Sorcery: Coherent Storytelling in an Open World)

                    I dont like that because we should write rules so they depend only the information
                    they really need.
                    */
                    new Criteria("GameCommand", "go west", Operator.Equal),
                    new Criteria(
                        "PlayerLocation",
                        Location.Hallway,
                        Operator.Equal
                    ),
                ],
                () =>
                    console.log(
                        "\nA heavy iron door blocks your way. It looks like it needs a large, important key."
                    )
            )
        );

        // --- FALLBACK RULE ---
        // This rule acts as a catch-all for any command that doesn't match a more specific rule.
        this._gameRules.push(
            new Rule(
                [
                    new Criteria(
                        "GameCommand",
                        (cmd) => typeof cmd === "string" && cmd.length > 0,
                        Operator.Predicate
                    ),
                ],
                () => {
                    const command = this._worldState.get("GameCommand");
                    const validCommands = [
                        "look",
                        "take key",
                        "take bread",
                        "take water",
                        "take rock",
                        "drink water",
                        "use bread",
                        "throw rock",
                        "throw bread",
                        "unlock door",
                        "go north",
                        "go south",
                        "go east",
                        "go west",
                        "help",
                        "quit",
                    ];

                    // If the command is valid but no rule was triggered, it means the context was wrong.
                    if (validCommands.includes(command)) {
                        console.log("\nYou can't do that right now.");
                        return; // Exit the payload early.
                    }

                    // If the command is not in the valid list, check for typos.
                    let bestMatch = null;
                    let minDistance = 3; // Only suggest if the typo is 1 or 2 letters off.

                    for (const validCmd of validCommands) {
                        const distance = getLevenshteinDistance(
                            command,
                            validCmd
                        );
                        if (distance < minDistance) {
                            minDistance = distance;
                            bestMatch = validCmd;
                        }
                    }

                    if (bestMatch) {
                        console.log(`\nDid you mean "${bestMatch}"?`);
                    } else {
                        console.log("\nI don't understand that command.");
                    }
                },
                "Default Fallback",
                -1 // A negative priority ensures it's chosen last.
            )
        );
    }

    /**
     * Starts and manages the main game loop.
     */
    async run() {
        this.setupWorld();
        this.setupRules();

        console.log("\n--- Welcome to Dungeon Escape! ---");
        console.log("Type 'help' for a list of commands.");

        // Automatically "look" around at the start of the game.
        this._worldState.set("GameCommand", "look");
        this._query.match(this._gameRules);

        // This loop will continuously read from the console for player input.
        while (!this._gameOver) {
            process.stdout.write("\n> ");
            for await (const line of console) {
                const input = line.toLowerCase().trim();
                if (input) {
                    this._worldState.set("GameCommand", input);
                    this._query.match(this._gameRules);
                }
                if (this._gameOver) {
                    break;
                }
                process.stdout.write("\n> ");
            }
        }
    }
}

// --- Entry Point ---
// To run the game, we create an instance of the simulation and call run().
const game = new GameSimulation();
game.run();
