/**
 * This file creates a simple HTTP server using Bun's native APIs.
 * It serves a static HTML file and provides API endpoints to interact
 * with a text-based adventure game, including sending debug information.
 */

// --- Game Imports ---
import { Game } from "./src/Game.js";
import { Query, Rule, Criteria, Operator } from "sfpm-js";

// --- Game Initialization ---
const game = new Game({});
const gameState = new Map([
    ["playerLocation", "mars"],
    ["talkingTo", "marena"],
    ["dialogStage", "intro"],
    ["knowsMerena", false],
]);

class Player {}

let player = game.world.createEntity().set(Player);

const gameRules = [
    new Rule(
        [
            new Criteria("playerLocation", "mars", Operator.Equal),
            new Criteria("talkingTo", "marena", Operator.Equal),
            new Criteria("dialogStage", "intro", Operator.Equal),
            new Criteria(
                "knowsMerena",
                (knowsMerena) => knowsMerena === false,
                Operator.Predicate
            ),
        ],
        (g) => {
            g.currentPlayerOptions = new Map([
                [
                    1,
                    {
                        text: "[TALK] Who are you?",
                        action: () => {
                            gameState.set("knowsMerena", true);
                            gameState.set("dialogStage", "introduction_done");
                            console.log(
                                "Game state updated: Player now knows Marena."
                            );
                        },
                    },
                ],
                [
                    2,
                    {
                        text: "[LEAVE] I'm leaving",
                        action: () => {
                            gameState.set("talkingTo", null);
                            console.log(
                                "Game state updated: Player left the conversation."
                            );
                        },
                    },
                ],
            ]);
        }
    ),
    new Rule(
        [
            new Criteria("playerLocation", "mars", Operator.Equal),
            new Criteria("talkingTo", "marena", Operator.Equal),
            new Criteria("dialogStage", "introduction_done", Operator.Equal),
        ],
        (g) => {
            g.currentPlayerOptions = new Map([
                [
                    1,
                    {
                        text: "[TALK] Hey Marena, how are you today?",
                        action: () => {
                            /* Further dialog could be implemented here */
                        },
                    },
                ],
                [
                    2,
                    {
                        text: "[ATTACK] Marena, your time has come!",
                        action: () => {
                            /* Combat logic could be implemented here */
                        },
                    },
                ],
                [
                    3,
                    {
                        text: "[LEAVE] I am going, see you soon.",
                        action: () => {
                            gameState.set("talkingTo", null);
                        },
                    },
                ],
            ]);
        }
    ),
];

game.currentGameRules = gameRules;

/**
 * Gathers the current game options and debug info.
 * @returns {Object} An object containing player options and debug data.
 */
function getGameData() {
    const query = new Query(gameState);
    query.match(game.currentGameRules, game);

    const options = Array.from(game.currentPlayerOptions, ([id, option]) => ({
        id,
        text: option.text,
    }));

    const debug = {
        tick: game.currentTick,
        // Convert the gameState Map to a plain object for JSON serialization
        gameState: Object.fromEntries(gameState),
    };

    return { options, debug };
}

// --- Bun Server ---
const server = Bun.serve({
    port: 3000,
    async fetch(req) {
        const url = new URL(req.url);
        console.log(`Received ${req.method} request for: ${url.pathname}`);

        // --- API Routes for the Game ---
        if (url.pathname === "/api/game" && req.method === "GET") {
            const gameData = getGameData();
            return new Response(JSON.stringify(gameData), {
                headers: { "Content-Type": "application/json" },
            });
        }

        if (url.pathname === "/api/action" && req.method === "POST") {
            try {
                const { optionId } = await req.json();
                if (typeof optionId !== "number") {
                    return new Response(
                        JSON.stringify({
                            error: "Invalid 'optionId' provided.",
                        }),
                        { status: 400 }
                    );
                }

                game.selectPlayerOption(optionId);
                game.tick();

                const newGameData = getGameData();
                return new Response(JSON.stringify(newGameData), {
                    headers: { "Content-Type": "application/json" },
                });
            } catch (error) {
                console.error("Error processing action:", error);
                return new Response(
                    JSON.stringify({ error: "Failed to process action." }),
                    { status: 400 }
                );
            }
        }

        // --- Static File Serving ---
        if (url.pathname === "/") {
            try {
                const htmlFile = Bun.file("./index.html");
                return new Response(htmlFile, {
                    headers: { "Content-Type": "text/html" },
                });
            } catch (error) {
                console.error("Error reading index.html:", error);
                return new Response("Internal Server Error", { status: 500 });
            }
        }

        return new Response("Page Not Found", { status: 404 });
    },

    error(error) {
        console.error("Server error:", error);
        return new Response("An unexpected error occurred", { status: 500 });
    },
});

console.log(`Bun server running! Listening on http://localhost:${server.port}`);
console.log(
    "Game endpoints available at /api/game (GET) and /api/action (POST)"
);
