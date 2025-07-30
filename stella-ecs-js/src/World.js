import { ComponentStorage } from "./ComponentStorage";
import { Entity } from "./Entity";

export class World {
    constructor() {
        this.entities = [];
        this.componentStorages = new Map();
    }

    createEntity() {
        const e = new Entity(this);
        e.id = this.entities.length;
        this.entities.push(e);
        return e;
    }

    set(entity, component) {
        const componentType = component.constructor.name;

        if (!this.componentStorages.has(componentType)) {
            const storage = new ComponentStorage();
            this.componentStorages.set(componentType, storage);
        }

        this.componentStorages.get(componentType).set(entity, component);
    }

    get(entity, componentClass) {
        const componentType = componentClass.name;
        const storage = this.componentStorages.get(componentType);

        if (!storage) {
            return undefined;
        }

        return storage.get(entity);
    }

    query() {}
}
