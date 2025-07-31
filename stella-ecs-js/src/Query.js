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
    }

    /**
     * Specifies that the query should only include entities that have the given component.
     * @param {Function} componentClass The component class.
     * @returns {Query} The query instance for chaining.
     */
    with(componentClass) {
        this._with.push(componentClass);
        return this;
    }

    /**
     * Specifies that the query should exclude entities that have the given component.
     * @param {Function} componentClass The component class.
     * @returns {Query} The query instance for chaining.
     */
    without(componentClass) {
        this._without.push(componentClass);
        return this;
    }

    /**
     * Specifies a component that is optional. The component will be included in the query result if the entity has it.
     * @param {Function} componentClass The component class.
     * @returns {Query} The query instance for chaining.
     */
    optional(componentClass) {
        this._optional.push(componentClass);
        return this;
    }

    /**
     * Adds a predicate function to filter the results.
     * The predicate will receive an object with the entity and its components.
     * @param {Function} predicate A function that returns true if the entity should be included.
     * @returns {Query} The query instance for chaining.
     */
    where(predicate) {
        this._predicates.push(predicate);
        return this;
    }

    /**
     * Makes the Query object iterable.
     * Executes the query and returns an iterable object that yields the results.
     * Each result is an object containing the entity and the requested components.
     * The component keys are the lowercase names of the component classes.
     * e.g., { entity, position, velocity } for Position and Velocity components.
     * @returns {Iterable<{entity: import('./Entity').Entity, [key: string]: any}>}
     */
    *[Symbol.iterator]() {
        // Ensure there's at least one 'with' component to iterate over.
        if (this._with.length === 0) {
            console.warn(
                "Query must have at least one 'with' component to be efficient. Consider adding one."
            );
            // Note: A full scan could be implemented here, but it's generally inefficient and bad practice.
            // For now, we'll return an empty iterator.
            return;
        }

        // Get all component storages required by the query.
        const withStorages = this._with
            .map((c) => this.world.componentStorages.get(c.name))
            .filter(Boolean);

        // If a required storage doesn't exist, the query can't return anything.
        if (withStorages.length !== this._with.length) {
            return;
        }

        // Find the smallest component storage to iterate over for efficiency.
        let smallestStorage = withStorages[0];
        for (let i = 1; i < withStorages.length; i++) {
            if (withStorages[i].size < smallestStorage.size) {
                smallestStorage = withStorages[i];
            }
        }

        // Iterate over the entities in the smallest storage.
        for (const entityId of smallestStorage.entityIds()) {
            const entity = this.world.entities[entityId];

            // 1. Check 'with' conditions: Does the entity have all required components?
            let hasAllWith = true;
            for (const componentClass of this._with) {
                if (
                    !this.world.componentStorages
                        .get(componentClass.name)
                        ?.has(entity)
                ) {
                    hasAllWith = false;
                    break;
                }
            }
            if (!hasAllWith) continue;

            // 2. Check 'without' conditions: Does the entity have any excluded components?
            let hasAnyWithout = false;
            for (const componentClass of this._without) {
                if (
                    this.world.componentStorages
                        .get(componentClass.name)
                        ?.has(entity)
                ) {
                    hasAnyWithout = true;
                    break;
                }
            }
            if (hasAnyWithout) continue;

            // 3. If all checks pass so far, build the result object.
            const result = { entity };

            // Add 'with' components to the result.
            for (const componentClass of this._with) {
                const componentName = componentClass.name.toLowerCase();
                result[componentName] = this.world.get(entity, componentClass);
            }

            // Add 'optional' components to the result (will be undefined if not present).
            for (const componentClass of this._optional) {
                const componentName = componentClass.name.toLowerCase();
                result[componentName] = this.world.get(entity, componentClass);
            }

            // 4. Apply 'where' predicates.
            let passesPredicates = true;
            for (const predicate of this._predicates) {
                if (!predicate(result)) {
                    passesPredicates = false;
                    break;
                }
            }
            if (!passesPredicates) continue;

            // 5. Yield the final result.
            yield result;
        }
    }
}
