import { Criteria, Operator, Rule } from "sfpm-js";
import { Game } from "../src/Game";
import { Scenario } from "../src/Scenario";
import { test, expect, describe } from "bun:test";

describe("Scenario", () => {
    test("Creating an scenario", () => {
        const name = "Minority Report";
        const game = new Game();
        const scenarioRules = [
            new Rule(
                [
                    new Criteria("playerLocation", "mars", Operator.Equal),
                    new Criteria("talkingTo", "marena", Operator.Equal),
                    new Criteria("dialogStage", "intro", Operator.Equal),
                ],
                () => {
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
                () => {
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
            ),
        ];
        const scenario = new Scenario(name, game.world, scenarioRules);

        expect(scenario.name).toBe(name);
        expect(scenario.world).toBe(game.world);
        expect(scenario.rules).toBe(scenarioRules);

        game.AddScenario(scenario);
        expect(game.scenarios.size).toBe(1);
    });

    test("removing an scenario", () => {
        const name = "Minority Report";
        const game = new Game();
        const scenarioRules = [
            new Rule(
                [
                    new Criteria("playerLocation", "mars", Operator.Equal),
                    new Criteria("talkingTo", "marena", Operator.Equal),
                    new Criteria("dialogStage", "intro", Operator.Equal),
                ],
                () => {
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
                () => {
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
            ),
        ];
        const scenario = new Scenario(name, game.world, scenarioRules);

        expect(scenario.name).toBe(name);
        expect(scenario.world).toBe(game.world);
        expect(scenario.rules).toBe(scenarioRules);

        game.AddScenario(scenario);
        game.RemoveScenario(scenario);
        expect(game.scenarios.size).toBe(0);
    });
});
