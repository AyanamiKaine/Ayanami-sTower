import { ComponentStorage } from "./ComponentStorage.js";
import { Entity } from "./Entity.js";
import { Query } from "./Query.js";
export class World {
    constructor() {
        this.entities = [];
        this.componentStorages = [];
        this.nextComponentId = 0;
        this.componentTypeMap = new Map();

        // NEW: Track component changes for query cache invalidation
        this._componentChangeVersion = 0;
        this._activeQueries = new Set();
    }

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
        const componentId = this.getComponentId(component.constructor);

        if (!this.componentStorages[componentId]) {
            this.componentStorages[componentId] = new ComponentStorage();
        }

        this.componentStorages[componentId].set(entity, component);
        this._componentChangeVersion++;

        // Optionally invalidate query caches
        // this._invalidateQueryCaches();
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
        const q = new Query(this);
        this._activeQueries.add(q);
        return q;
    }

    // NEW: Batch operations for better performance
    batchSet(operations) {
        for (const { entity, component } of operations) {
            const componentId = this.getComponentId(component.constructor);
            if (!this.componentStorages[componentId]) {
                this.componentStorages[componentId] = new ComponentStorage();
            }
            this.componentStorages[componentId].set(entity, component);
        }
        this._componentChangeVersion++;
    }

    // NEW: Get performance stats
    getPerformanceStats() {
        const storageStats = this.componentStorages.map((storage, id) => ({
            componentId: id,
            size: storage ? storage.size : 0,
            capacity: storage ? storage.dense.length : 0
        })).filter(stat => stat.size > 0);

        return {
            totalEntities: this.entities.length,
            componentTypes: this.nextComponentId,
            storages: storageStats,
            totalComponents: storageStats.reduce((sum, stat) => sum + stat.size, 0),
            changeVersion: this._componentChangeVersion
        };
    }
}