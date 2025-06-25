import { test, expect, describe } from "bun:test";
import { Game } from "../src/game";
import Graph from "graphology";

/*
The question remains, should we remove all relationship mixins 
and should instead use a Graph datastructure to model them?
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

        const galaxyMap = new Graph();

        galaxyMap.addNode(sol.id, { entity: sol });
        galaxyMap.addNode(alphaCentauri.id, { entity: alphaCentauri });

        galaxyMap.addNode(sun.id, { entity: sun });
        galaxyMap.addNode(moon.id, { entity: moon });
        galaxyMap.addNode(earth.id, { entity: earth });
        galaxyMap.addNode(milkyWay.id, { entity: milkyWay });

        // A directed relationship: Earth -> orbits -> Sol
        galaxyMap.addDirectedEdge(earth.id, sol.id, { type: "orbits" });

        // An undirected (symmetric) relationship: Sol <-> connectedTo <-> Alpha Centauri
        // `addUndirectedEdge` handles the symmetry automatically.
        galaxyMap.addUndirectedEdge(sol.id, alphaCentauri.id, {
            type: "connectedTo",
        });
    }),
        test("Character Relationship Example", () => {
            // 1. Create your character entities
            const game = new Game();
            const eva = game.createEntity("Eva");
            const jax = game.createEntity("Jax");
            const kenji = game.createEntity("Kenji");

            // 2. Create the graph to store relationships
            const characterRelationships = new Graph();

            // 3. Add the characters as nodes
            characterRelationships.addNode(eva.id, { entity: eva });
            characterRelationships.addNode(jax.id, { entity: jax });
            characterRelationships.addNode(kenji.id, { entity: kenji });

            // 4. Model the relationships as directed edges with attributes

            // Eva hates Jax. The feeling is strong.
            characterRelationships.addDirectedEdge(eva.id, jax.id, {
                type: "social",
                status: "hates",
                sentiment: -0.9, // Very strong negative feeling
                isKnown: false, // Jax doesn't know... yet.
            });

            // Jax, however, sees Eva as a worthy rival. A different kind of negative relationship.
            characterRelationships.addDirectedEdge(jax.id, eva.id, {
                type: "social",
                status: "rivalry",
                sentiment: -0.5,
                isKnown: true,
            });

            // Eva trusts Kenji completely.
            characterRelationships.addDirectedEdge(eva.id, kenji.id, {
                type: "social",
                status: "trusts",
                sentiment: 1.0,
                isKnown: true,
            });

            // Kenji also trusts Eva. This makes the relationship mutual.
            characterRelationships.addDirectedEdge(kenji.id, eva.id, {
                type: "social",
                status: "trusts",
                sentiment: 1.0,
                isKnown: true,
            });

            expect(
                characterRelationships.getDirectedEdgeAttribute(
                    eva.id,
                    jax.id,
                    "status"
                ) === "hates"
            ).toBe(true);
        });
});
