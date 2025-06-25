import Graph from "graphology";
import { Entity } from "./Entity.js";

export class Game {
    constructor() {
        this.entities = [];
        // Non hierarchically entity relationships are modeled using graph
        this.relationships = new Graph();
    }

    /**
     * Creates a new Entity and adds it to the game.
     * @param {string} name - The name for the new entity.
     * @returns {Entity} The newly created entity.
     */
    createEntity(name) {
        // Now we pass an object to the Entity constructor.
        // The keys of the object are the "named arguments".
        const entity = new Entity({
            name: name,
            id: this.entities.length + 1,
        });

        this.entities.push(entity);
        this.relationships.addNode(entity.id, { entity: entity });
        return entity;
    }

    /**
     * Adds a directed (one-way) relationship between two entities.
     * @param {Entity} source - The entity where the relationship originates.
     * @param {Entity} target - The entity the relationship points to.
     * @param {object} [attributes={}] - An object describing the relationship (e.g., {type: 'social', status: 'hates'}).
     */
    addOneWayRelationship(source, target, attributes = {}) {
        if (!source || !target) return;
        this.relationships.addDirectedEdge(source.id, target.id, attributes);
    }

    /**
     * Adds an undirected (two-way, symmetrical) relationship between two entities.
     * @param {Entity} source - One of the entities in the relationship.
     * @param {Entity} target - The other entity in the relationship.
     * @param {object} [attributes={}] - An object describing the relationship (e.g., {type: 'physical', connection: 'jump-gate'}).
     */
    addSymmetricRelationship(source, target, attributes = {}) {
        if (!source || !target) return;
        this.relationships.addUndirectedEdge(source.id, target.id, attributes);
    }
}
