export class Query {
    constructor(world) {
        this.world = world;
        this._with = [];
        this._without = [];
        this._optional = [];
        this._predicates = [];

        // Enhanced caching
        this._componentMappings = null;
        this._combinedPredicate = null;
        this._predicatesDirty = true;
        this._intersectionCache = null;
        this._excludedCache = null;
        this._cacheValid = false;
    }

    with(componentClass) {
        this._with.push(componentClass);
        this._invalidateCache();
        return this;
    }

    without(componentClass) {
        this._without.push(componentClass);
        this._invalidateCache();
        return this;
    }

    optional(componentClass) {
        this._optional.push(componentClass);
        this._invalidateCache();
        return this;
    }

    where(predicate) {
        this._predicates.push(predicate);
        this._predicatesDirty = true;
        return this;
    }

    _invalidateCache() {
        this._componentMappings = null;
        this._predicatesDirty = true;
        this._cacheValid = false;
        this._intersectionCache = null;
        this._excludedCache = null;
    }

    _getComponentMappings() {
        if (!this._componentMappings) {
            this._componentMappings = {
                with: this._with.map(c => ({
                    class: c,
                    name: c.name.toLowerCase(),
                    id: this.world.getComponentId(c)
                })),
                optional: this._optional.map(c => ({
                    class: c,
                    name: c.name.toLowerCase(),
                    id: this.world.getComponentId(c)
                }))
            };
        }
        return this._componentMappings;
    }

    _getCombinedPredicate() {
        if (this._predicatesDirty) {
            if (this._predicates.length === 0) {
                this._combinedPredicate = null;
            } else if (this._predicates.length === 1) {
                this._combinedPredicate = this._predicates[0];
            } else {
                this._combinedPredicate = (result) => this._predicates.every(p => p(result));
            }
            this._predicatesDirty = false;
        }
        return this._combinedPredicate;
    }

    // OPTIMIZED: Much faster intersection using Set operations
    _getEntityIntersection() {
        if (this._cacheValid && this._intersectionCache) {
            return this._intersectionCache;
        }

        if (this._with.length === 0) {
            this._intersectionCache = new Set();
            return this._intersectionCache;
        }

        // Get storages sorted by size (smallest first)
        const storageData = this._with
            .map(c => {
                const id = this.world.getComponentId(c);
                return {
                    storage: this.world.componentStorages[id],
                    class: c,
                    id: id
                };
            })
            .filter(data => data.storage && data.storage.size > 0)
            .sort((a, b) => a.storage.size - b.storage.size);

        if (storageData.length !== this._with.length) {
            this._intersectionCache = new Set();
            return this._intersectionCache;
        }

        // Start with smallest set
        let intersection = storageData[0].storage.getEntityIdSet();

        // Intersect with each subsequent set
        for (let i = 1; i < storageData.length && intersection.size > 0; i++) {
            const currentSet = storageData[i].storage.getEntityIdSet();
            const newIntersection = new Set();

            // Always iterate over the smaller set
            const [smaller, larger] = intersection.size < currentSet.size
                ? [intersection, currentSet]
                : [currentSet, intersection];

            for (const entityId of smaller) {
                if (larger.has(entityId)) {
                    newIntersection.add(entityId);
                }
            }
            intersection = newIntersection;
        }

        this._intersectionCache = intersection;
        return this._intersectionCache;
    }

    // OPTIMIZED: Cache excluded entities
    _getExcludedEntities() {
        if (this._cacheValid && this._excludedCache) {
            return this._excludedCache;
        }

        const excluded = new Set();
        for (const componentClass of this._without) {
            const componentId = this.world.componentTypeMap.get(componentClass);
            if (componentId !== undefined) {
                const storage = this.world.componentStorages[componentId];
                if (storage) {
                    const entityIdSet = storage.getEntityIdSet();
                    for (const entityId of entityIdSet) {
                        excluded.add(entityId);
                    }
                }
            }
        }

        this._excludedCache = excluded;
        return this._excludedCache;
    }

    // OPTIMIZED: Batch component lookups
    *_optimizedGeneralQuery() {
        const mappings = this._getComponentMappings();
        const predicate = this._getCombinedPredicate();
        const candidates = this._getEntityIntersection();
        const excluded = this._without.length > 0 ? this._getExcludedEntities() : null;

        // Pre-fetch storages for batch lookups
        const withStorages = mappings.with.map(m => ({
            name: m.name,
            storage: this.world.componentStorages[m.id]
        }));

        const optionalStorages = mappings.optional.map(m => ({
            name: m.name,
            storage: this.world.componentStorages[m.id]
        }));

        for (const entityId of candidates) {
            if (excluded && excluded.has(entityId)) continue;

            const entity = this.world.entities[entityId];
            const result = { entity };

            // Batch lookup required components
            let hasAll = true;
            for (const { name, storage } of withStorages) {
                const component = storage.get(entity);
                if (component === undefined) {
                    hasAll = false;
                    break;
                }
                result[name] = component;
            }

            if (!hasAll) continue;

            // Batch lookup optional components
            for (const { name, storage } of optionalStorages) {
                result[name] = storage ? storage.get(entity) : undefined;
            }

            if (predicate && !predicate(result)) continue;
            yield result;
        }
    }

    // OPTIMIZED: Single component query with better caching
    *_optimizedSingleComponentQuery() {
        const componentClass = this._with[0];
        const componentId = this.world.getComponentId(componentClass);
        const storage = this.world.componentStorages[componentId];

        if (!storage) return;

        const componentName = componentClass.name.toLowerCase();
        const mappings = this._getComponentMappings();

        // Pre-fetch optional storages
        const optionalStorages = mappings.optional.map(m => ({
            name: m.name,
            storage: this.world.componentStorages[m.id]
        }));

        // Use direct iteration over dense arrays for better cache locality
        for (let i = 0; i < storage.size; i++) {
            const entityId = storage.entityIdMap[i];
            const entity = this.world.entities[entityId];
            const component = storage.dense[i];

            const result = { entity };
            result[componentName] = component;

            // Batch lookup optional components
            for (const { name, storage: optStorage } of optionalStorages) {
                result[name] = optStorage ? optStorage.get(entity) : undefined;
            }

            yield result;
        }
    }

    *[Symbol.iterator]() {
        if (this._with.length === 0) {
            console.warn("Query must have at least one 'with' component to be efficient.");
            return;
        }

        // Mark cache as valid for this iteration
        this._cacheValid = true;

        try {
            if (this._without.length === 0 && this._optional.length === 0 && this._predicates.length === 0) {
                // Simple case - use optimized single component query
                if (this._with.length === 1) {
                    yield* this._optimizedSingleComponentQuery();
                } else {
                    yield* this._optimizedGeneralQuery();
                }
            } else {
                yield* this._optimizedGeneralQuery();
            }
        } finally {
            // Don't invalidate cache immediately - let it live for potential reuse
        }
    }

    // NEW: Method to manually refresh cache when entities/components change significantly
    refreshCache() {
        this._invalidateCache();
        return this;
    }

    estimateResultCount() {
        if (this._with.length === 0) return 0;
        const candidates = this._getEntityIntersection();
        const excluded = this._without.length > 0 ? this._getExcludedEntities() : null;
        let estimate = candidates.size;
        if (excluded) {
            for (const entityId of candidates) {
                if (excluded.has(entityId)) estimate--;
            }
        }
        return Math.max(0, estimate);
    }
}