export class Query {
    /**
     * @param {import('./World').World} world The world instance to query against.
     */
    constructor(world) {
        this.world = world;
        this._with = [];
        this._without = [];
        this._optional = [];
        this._predicates = [];

        this._componentMappings = null;
        this._combinedPredicate = null;
        this._predicatesDirty = true;
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
    }

    _getComponentMappings() {
        if (!this._componentMappings) {
            this._componentMappings = {
                with: this._with.map(c => ({ class: c, name: c.name.toLowerCase() })),
                optional: this._optional.map(c => ({ class: c, name: c.name.toLowerCase() }))
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

    *_simpleWithQuery() {
        const mappings = this._getComponentMappings();
        const candidates = this._getEntityIntersection();

        for (const entityId of candidates) {
            const entity = this.world.entities[entityId];
            const result = { entity };

            for (const mapping of mappings.with) {
                result[mapping.name] = this.world.get(entity, mapping.class);
            }
            yield result;
        }
    }

    *_singleComponentQuery() {
        const componentClass = this._with[0];
        const componentId = this.world.getComponentId(componentClass);
        const storage = this.world.componentStorages[componentId];
        const componentName = componentClass.name.toLowerCase();

        if (!storage) return;

        for (const entityId of storage.entityIds()) {
            const entity = this.world.entities[entityId];
            const component = storage.get(entity);

            const result = { entity };
            result[componentName] = component;

            const mappings = this._getComponentMappings();
            for (const mapping of mappings.optional) {
                result[mapping.name] = this.world.get(entity, mapping.class);
            }
            yield result;
        }
    }

    _getEntityIntersection() {
        if (this._with.length === 0) return new Set();

        const storageData = this._with
            .map(c => ({
                // MODIFIED: Use world.getComponentId to get the storage
                storage: this.world.componentStorages[this.world.getComponentId(c)],
                class: c
            }))
            .filter(data => data.storage)
            .sort((a, b) => a.storage.size - b.storage.size);

        if (storageData.length !== this._with.length) {
            return new Set();
        }

        let candidates = new Set(storageData[0].storage.entityIds());

        for (let i = 1; i < storageData.length; i++) {
            const storage = storageData[i].storage;
            const newCandidates = new Set();
            for (const entityId of candidates) {
                const entity = this.world.entities[entityId];
                if (storage.has(entity)) {
                    newCandidates.add(entityId);
                }
            }
            candidates = newCandidates;
            if (candidates.size === 0) break;
        }
        return candidates;
    }

    _getExcludedEntities() {
        const excluded = new Set();
        for (const componentClass of this._without) {
            const componentId = this.world.componentTypeMap.get(componentClass);
            if (componentId !== undefined) {
                const storage = this.world.componentStorages[componentId];
                if (storage) {
                    for (const entityId of storage.entityIds()) {
                        excluded.add(entityId);
                    }
                }
            }
        }
        return excluded;
    }

    *_generalQuery() {
        const mappings = this._getComponentMappings();
        const predicate = this._getCombinedPredicate();
        const candidates = this._getEntityIntersection();
        const excluded = this._without.length > 0 ? this._getExcludedEntities() : null;

        for (const entityId of candidates) {
            if (excluded && excluded.has(entityId)) continue;
            const entity = this.world.entities[entityId];
            const result = { entity };
            for (const mapping of mappings.with) {
                result[mapping.name] = this.world.get(entity, mapping.class);
            }
            for (const mapping of mappings.optional) {
                result[mapping.name] = this.world.get(entity, mapping.class);
            }
            if (predicate && !predicate(result)) continue;
            yield result;
        }
    }

    *[Symbol.iterator]() {
        if (this._with.length === 0) {
            console.warn("Query must have at least one 'with' component to be efficient. Consider adding one.");
            return;
        }
        if (this._without.length === 0 && this._optional.length === 0 && this._predicates.length === 0) {
            yield* this._simpleWithQuery();
            return;
        }
        if (this._with.length === 1 && this._without.length === 0 && this._predicates.length === 0) {
            yield* this._singleComponentQuery();
            return;
        }
        yield* this._generalQuery();
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

    analyzePerformance() {
        const storages = this._with.map(c => {
            const id = this.world.componentTypeMap.get(c);
            return {
                component: c.name,
                size: id !== undefined ? this.world.componentStorages[id]?.size || 0 : 0
            };
        });

        return {
            withComponents: this._with.length,
            withoutComponents: this._without.length,
            optionalComponents: this._optional.length,
            predicates: this._predicates.length,
            storages: storages,
            smallestStorage: storages.length > 0 ? storages.reduce((min, s) => s.size < min.size ? s : min) : { component: 'N/A', size: 0 },
            estimatedResults: this.estimateResultCount(),
            queryPath: this._getQueryPath()
        };
    }

    _getQueryPath() {
        if (this._with.length === 0) return "invalid";
        if (this._without.length === 0 && this._optional.length === 0 && this._predicates.length === 0) {
            return "simple";
        }
        if (this._with.length === 1 && this._without.length === 0 && this._predicates.length === 0) {
            return "single-component";
        }
        return "general";
    }
}