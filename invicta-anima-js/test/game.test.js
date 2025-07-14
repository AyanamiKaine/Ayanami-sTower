import { test, expect, describe } from "bun:test";
import { Criteria, Operator, Rule } from "sfpm-js";
import { Game } from "../src/Game";
import { Query } from "sfpm-js";

describe("Game", () => {
    test("Progressing the game world should increase the current tick count", () => {
        const game = new Game();
        game.Tick();

        expect(game.currentTick).toBe(1);
    });

    test("Can select player options", () => {
        const game = new Game();
        const gameRules = [];
        let gameState = new Map([
            ["playerLocation", "mars"],
            ["talkingTo", "marena"],
            ["dialogStage", "intro"],
            ["knowsMerena", false],
        ]);
        const query = new Query(gameState);
        let playerOption1Selected = false;
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
                                action: (world) => {},
                            },
                        ],
                        [
                            2,
                            {
                                text: "[ATTACK] Marena your time has come",
                                action: (world) => {},
                            },
                        ],
                        [
                            3,
                            {
                                text: "[LEAVE] I am going, see you soon",
                                action: (world) => {},
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
                                action: (world) => {
                                    playerOption1Selected = true;
                                },
                            },
                        ],
                        [
                            2,
                            {
                                text: "[LEAVE] I'm leaving",
                                action: (world) => {},
                            },
                        ],
                    ]);
                }
            )
        );

        query.match(gameRules, game);
        expect(game.currentPlayerOptions.size).toBe(2);
        game.selectPlayerOption(1);
        expect(playerOption1Selected).toBe(true);
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
                                action: (world) => {},
                            },
                        ],
                        [
                            2,
                            {
                                text: "[ATTACK] Marena your time has come",
                                action: (world) => {},
                            },
                        ],
                        [
                            3,
                            {
                                text: "[LEAVE] I am going, see you soon",
                                action: (world) => {},
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
                                action: (world) => {},
                            },
                        ],
                        [
                            2,
                            {
                                text: "[LEAVE] I'm leaving",
                                action: (world) => {},
                            },
                        ],
                    ]);
                }
            )
        );

        query.match(gameRules, game);

        expect(game.currentPlayerOptions.size).toBe(2);
    });
});
