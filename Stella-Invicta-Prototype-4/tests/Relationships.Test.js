import { test, expect, describe } from "bun:test";
import { Game } from "../src/game";
import Graph from "graphology";

/*
    The question remains, should we remove all relationship mixins 
    and should instead use a Graph datastructure to model them?

    Simple hierarchly relationships can be put in mixins like parent and 
    orbits, complex non-hierarchily relationships should be put into a graph
    those are, character relationships, isAllied with, etc. Everything that 
    would form a complex web should be a graph.
    */

describe("Relationship Example", () => {
    test("GalaxyMap Relationship Example", () => {
        const game = new Game();

        const milkyWay = game.createEntity("Milky Way");

        const sol = game.createEntity("Sol");
        const alphaCentauri = game.createEntity("Alpha Centauri");

        const sun = game.createEntity("Sun");
        const earth = game.createEntity("Earth");
        const moon = game.createEntity("Moon");

        // A directed relationship: Earth -> orbits -> Sol
        game.addOneWayRelationship(earth, sol, { type: "orbits" });

        // An undirected (symmetric) relationship: Sol <-> connectedTo <-> Alpha Centauri
        // `addUndirectedEdge` handles the symmetry automatically.
        game.addSymmetricRelationship(sol, alphaCentauri, {
            type: "connectedTo",
        });
    }),
        test("Character Relationship Example", () => {
            const game = new Game();

            const sol = game.createEntity("Sol");
            const alphaCentauri = game.createEntity("Alpha Centauri");
            const earth = game.createEntity("Earth");

            const eva = game.createEntity("Eva");
            const jax = game.createEntity("Jax");

            // --- 2. Use the new helper methods to add relationships ---

            // Physical, undirected relationship for a jump-gate
            game.addSymmetricRelationship(sol, alphaCentauri, {
                type: "physical",
                connection: "jump-gate",
            });

            // Social, directed relationship for Eva hating Jax
            game.addOneWayRelationship(eva, jax, {
                type: "social",
                status: "hates",
                sentiment: -0.9,
            });

            // Hierarchical, directed relationship for orbits
            game.addOneWayRelationship(earth, sol, {
                type: "physical",
                status: "orbits",
            });

            // --- 3. Query the relationships directly through the graph instance ---
            // Note: For advanced querying, accessing the graph directly is still powerful.

            // Check the jump-gate
            // Because it's undirected, the order doesn't matter.
            expect(game.relationships.hasEdge(sol.id, alphaCentauri.id)).toBe(
                true
            );
            expect(game.relationships.hasEdge(alphaCentauri.id, sol.id)).toBe(
                true
            );

            // Check the social relationship
            const evaHatesJax =
                game.relationships.getDirectedEdgeAttribute(
                    eva.id,
                    jax.id,
                    "status"
                ) === "hates";
            expect(evaHatesJax).toBe(true);

            // Check that the reverse is not true
            const jaxHatesEva = game.relationships.hasDirectedEdge(
                jax.id,
                eva.id
            );
            expect(jaxHatesEva).toBe(false);
        });
});
