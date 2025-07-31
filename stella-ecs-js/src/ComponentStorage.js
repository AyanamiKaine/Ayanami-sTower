export class ComponentStorage {
    constructor(capacity = 100000) {
        this.dense = new Array(capacity);
        this.entityIdMap = new Array(capacity);
        this.sparse = [];
        this.size = 0;

        // NEW: Cached entity ID set for faster intersection
        this._entityIdSet = null;
        this._entityIdSetDirty = true;
    }

    has(entity) {
        const sparseIdx = this.sparse[entity.id];
        return (
            sparseIdx !== undefined &&
            sparseIdx < this.size && // Add bounds check
            this.entityIdMap[sparseIdx] === entity.id
        );
    }

    get(entity) {
        const sparseIdx = this.sparse[entity.id];
        if (sparseIdx !== undefined &&
            sparseIdx < this.size &&
            this.entityIdMap[sparseIdx] === entity.id) {
            return this.dense[sparseIdx];
        }
        return undefined;
    }

    set(entity, component) {
        const sparseIdx = this.sparse[entity.id];
        if (sparseIdx !== undefined &&
            sparseIdx < this.size &&
            this.entityIdMap[sparseIdx] === entity.id) {
            this.dense[sparseIdx] = component;
            return;
        }

        if (this.size >= this.dense.length) {
            console.error("Component storage is full.");
            return;
        }

        const denseIdx = this.size;
        this.dense[denseIdx] = component;
        this.entityIdMap[denseIdx] = entity.id;
        this.sparse[entity.id] = denseIdx;
        this.size++;
        this._entityIdSetDirty = true;
    }

    remove(entity) {
        const denseIdxToRemove = this.sparse[entity.id];
        if (denseIdxToRemove === undefined ||
            denseIdxToRemove >= this.size ||
            this.entityIdMap[denseIdxToRemove] !== entity.id) {
            return;
        }

        const lastDenseIdx = this.size - 1;
        const lastEntityId = this.entityIdMap[lastDenseIdx];

        this.dense[denseIdxToRemove] = this.dense[lastDenseIdx];
        this.entityIdMap[denseIdxToRemove] = lastEntityId;
        this.sparse[lastEntityId] = denseIdxToRemove;
        this.sparse[entity.id] = undefined;
        this.size--;
        this._entityIdSetDirty = true;
    }

    // NEW: Get cached entity ID set for faster intersections
    getEntityIdSet() {
        if (this._entityIdSetDirty || !this._entityIdSet) {
            this._entityIdSet = new Set();
            for (let i = 0; i < this.size; i++) {
                this._entityIdSet.add(this.entityIdMap[i]);
            }
            this._entityIdSetDirty = false;
        }
        return this._entityIdSet;
    }
    //####### Below here is just stuff to better enumerate on the component storage, its basically js specific and not relevant for the implementation of the component storage ########//

    *[Symbol.iterator]() {
        for (let i = 0; i < this.size; i++) {
            yield {
                entityId: this.entityIdMap[i],
                component: this.dense[i],
            };
        }
    }

    *components() {
        for (let i = 0; i < this.size; i++) {
            yield this.dense[i];
        }
    }

    *entityIds() {
        for (let i = 0; i < this.size; i++) {
            yield this.entityIdMap[i];
        }
    }

    *entries() {
        for (let i = 0; i < this.size; i++) {
            yield [this.entityIdMap[i], this.dense[i]];
        }
    }

    forEach(callback, thisArg) {
        for (let i = 0; i < this.size; i++) {
            callback.call(thisArg, this.dense[i], this.entityIdMap[i], this);
        }
    }

    map(callback, thisArg) {
        const results = [];
        for (let i = 0; i < this.size; i++) {
            results.push(
                callback.call(thisArg, this.dense[i], this.entityIdMap[i], this)
            );
        }
        return results;
    }

    filter(callback, thisArg) {
        const results = [];
        for (let i = 0; i < this.size; i++) {
            if (
                callback.call(thisArg, this.dense[i], this.entityIdMap[i], this)
            ) {
                results.push({
                    entityId: this.entityIdMap[i],
                    component: this.dense[i],
                });
            }
        }
        return results;
    }

    toArray() {
        const result = [];
        for (let i = 0; i < this.size; i++) {
            result.push({
                entityId: this.entityIdMap[i],
                component: this.dense[i],
            });
        }
        return result;
    }
}
