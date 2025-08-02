import { ComponentStorage } from "./ComponentStorage.js";
import { Entity } from "./Entity.js";
import { Query } from "./Query.js";

export class World {
    constructor() {
        this.entities = [];
        this.componentStorages = [];
        this.nextComponentId = 0;
        // Maps a component class constructor to its unique ID within this world.
        this.componentTypeMap = new Map();
    }

    /**
     * Gets the unique ID for a component class for this specific world instance.
     * If the component type is new to the world, it will be registered and assigned an ID.
     * @param {Function} componentClass The component class constructor.
     * @returns {number} The unique ID for the component type.
     */
    getComponentId(componentClass) {
        let id = this.componentTypeMap.get(componentClass);
        if (id === undefined) {
            id = this.nextComponentId++;
            this.componentTypeMap.set(componentClass, id);
        }
        return id;
    }

    createEntity() {
        const e = new Entity(this);
        e.id = this.entities.length;
        this.entities.push(e);
        return e;
    }

    set(entity, component) {
        // Get the world-specific ID for this component type.
        const componentId = this.getComponentId(component.constructor);

        if (!this.componentStorages[componentId]) {
            this.componentStorages[componentId] = new ComponentStorage();
        }
        this.componentStorages[componentId].set(entity, component);
    }

    get(entity, componentClass) {
        const componentId = this.componentTypeMap.get(componentClass);
        if (componentId === undefined) {
            return undefined;
        }

        const storage = this.componentStorages[componentId];
        return storage ? storage.get(entity) : undefined;
    }

    query() {
        return new Query(this);
    }
}
