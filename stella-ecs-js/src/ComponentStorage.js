export class ComponentStorage {
    /**
     * @param {number} capacity - The maximum number of components this storage can hold. By default 100_000
     */
    constructor(capacity = 100000) {
        this.dense = new Array(capacity);

        this.entityIdMap = new Array(capacity);

        this.sparse = [];

        this.size = 0;
    }

    has(entity) {
        const sparseIdx = this.sparse[entity.id];
        return (
            sparseIdx !== undefined && this.entityIdMap[sparseIdx] === entity.id
        );
    }

    get(entity) {
        if (!this.has(entity)) {
            return undefined;
        }
        const denseIdx = this.sparse[entity.id];
        return this.dense[denseIdx];
    }

    set(entity, component) {
        if (this.has(entity)) {
            const denseIdx = this.sparse[entity.id];
            this.dense[denseIdx] = component;
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
    }

    remove(entity) {
        if (!this.has(entity)) {
            return;
        }

        const denseIdxToRemove = this.sparse[entity.id];
        const lastDenseIdx = this.size - 1;
        const lastEntityId = this.entityIdMap[lastDenseIdx];

        this.dense[denseIdxToRemove] = this.dense[lastDenseIdx];
        this.entityIdMap[denseIdxToRemove] = this.entityIdMap[lastDenseIdx];
        this.sparse[lastEntityId] = denseIdxToRemove;
        this.sparse[entity.id] = undefined;
        this.size--;
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
