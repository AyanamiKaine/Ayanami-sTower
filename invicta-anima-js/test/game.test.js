import { test, expect, describe } from "bun:test";
import { Criteria, Operator, Rule } from "sfpm-js";
import { Game } from "../src/Game";
import { Query } from "sfpm-js";
import { QueryResult } from "stella-ecs-js";
import { Planet } from "../src/components/Planet";
import { Name } from "../src/components/Name";
import { marenaDialogue } from "../src/data/marena_example_dialog";

class Player { }

describe("Game", () => {
    test("Progressing the game world should increase the current tick count", () => {
        const game = new Game();
        game.tick();

        expect(game.currentTick).toBe(1);
    });

    test("Get player entity", () => {
        const game = new Game();
        let player = game.world.createEntity().set(new Player());
        let queryResult = new QueryResult(game.world.query([Player]));

        expect(queryResult.count).toBe(1);
        queryResult.forEach(({ entity, components }) => {
            expect(components.has(Player)).toBe(true);
        });
    });

    test("Can use DialogueManager for a conversation", () => {
        const game = new Game();

        // 1. Load the dialogue graph into the manager
        game.dialogueManager.load(marenaDialogue);

        // 2. Set up initial game state
        game.gameState.set("playerLocation", "mars");
        game.gameState.set("knowsMarena", false);
        game.gameState.set("talkingTo", null);

        // 3. Define a RULE that TRIGGERS the conversation.
        // This is where your rule engine shines!
        const triggerMarenaTalkRule = new Rule(
            [
                new Criteria("playerLocation", "mars", Operator.Equal),
                new Criteria("interactTarget", "marena", Operator.Equal), // A new fact set when player clicks on her
            ],
            (game) => {
                // The payload's job is to START the dialogue.
                game.gameState.set("talkingTo", "marena");
                game.dialogueManager.startDialogue('marena_intro_start');
            }
        );

        // Pretend the player clicks on Marena
        game.gameState.set("interactTarget", "marena");
        const query = new Query(game.gameState);
        query.match([triggerMarenaTalkRule], game);

        // 4. Now that the dialogue has started, update the player options
        game.updateCurrentPlayerOptions();

        // We don't know Marena yet, so we should see 2 options: "Who are you?" and "Leave".
        expect(game.currentPlayerOptions.size).toBe(2);
        expect(game.currentPlayerOptions.get(1).text).toBe("[TALK] Who are you?");

        // 5. Player selects the first option
        game.selectPlayerOption(1);

        // The action should have set 'knowsMerena' to true
        expect(game.gameState.get('knowsMarena')).toBe(true);
        // The DialogueManager should have transitioned to the next node
        expect(game.dialogueManager.activeNode.id).toBe('marena_intro_reveal_name');

        // The options should have updated for the new node
        expect(game.currentPlayerOptions.size).toBe(2);
        expect(game.currentPlayerOptions.get(1).text).toBe("[TALK] I'm just a traveler.");
    });

    test("Fuzzy pattern matcher dialog example", () => {
        const game = new Game();
        const gameRules = [];
        let gameState = new Map([
            ["playerLocation", "mars"],
            ["talkingTo", "marena"],
            ["dialogStage", "intro"],
            ["knowsMerena", false],
        ]);
        const query = new Query(gameState);
        /*
        Based on the current state of the game, the player is given options what he can do.
        */
        gameRules.push(
            new Rule(
                [
                    new Criteria("playerLocation", "mars", Operator.Equal),
                    new Criteria("talkingTo", "marena", Operator.Equal),
                    new Criteria("dialogStage", "intro", Operator.Equal),
                ],
                (game) => {
                    game.currentPlayerOptions = new Map([
                        [
                            1,
                            {
                                text: "[TALK] Hey Marena how are you today?",
                                action: (world) => { },
                            },
                        ],
                        [
                            2,
                            {
                                text: "[ATTACK] Marena your time has come",
                                action: (world) => { },
                            },
                        ],
                        [
                            3,
                            {
                                text: "[LEAVE] I am going, see you soon",
                                action: (world) => { },
                            },
                        ],
                    ]);
                }
            ),
            new Rule(
                [
                    new Criteria("playerLocation", "mars", Operator.Equal),
                    new Criteria("talkingTo", "marena", Operator.Equal),
                    new Criteria("dialogStage", "intro", Operator.Equal),
                    new Criteria(
                        "knowsMerena",
                        (knowsMerena) => knowsMerena == false,
                        Operator.Predicate
                    ),
                ],
                (game) => {
                    game.currentPlayerOptions = new Map([
                        [
                            1,
                            {
                                text: "[TALK] Who are you?",
                                action: (world) => { },
                            },
                        ],
                        [
                            2,
                            {
                                text: "[LEAVE] I'm leaving",
                                action: (world) => { },
                            },
                        ],
                    ]);
                }
            )
        );

        query.match(gameRules, game);

        expect(game.currentPlayerOptions.size).toBe(2);
    });

    test("Creating a small example world", () => {
        const game = new Game();


        let krell = game.world
            .createEntity()
            .set(new Planet())
            .set(new Name("Krell"));

        expect(krell.get(Name).value).toBe("Krell")
    });
});
