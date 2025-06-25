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
}
