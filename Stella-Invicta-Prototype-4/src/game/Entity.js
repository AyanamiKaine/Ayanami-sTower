/**
 * Represents an object in the game world.
 * It uses an object-based constructor for named parameters and supports mixins for behavior.
 */
export class Entity {
    /**
     * @param {object} [config={}] - The configuration object for the entity.
     * @param {string} [config.name=""] - The name of the entity.
     * @param {Entity} [config.parent=null] - The parent entity.
     * @param {Entity[]} [config.children=[]] - An array of child entities.
     * @param {number} [config.id=0] - A unique identifier.
     */
    constructor({ name = "", parent = null, children = [], id = 0 } = {}) {
        this.name = name;
        this.id = id;
        this.parent = parent;
        this.children = children;

        /**
         * A Set to track applied behavior functions (mixins).
         * @type {Set<Function>}
         */
        this.behaviors = new Set();

        /**
         * A Map to track the properties and methods added by each behavior.
         * The key is the behavior function, and the value is an array of property names (strings).
         * @private
         * @type {Map<Function, string[]>}
         */
        this._behaviorProps = new Map();

        // We only want to add a valid entity to the parent's children
        if (this.parent instanceof Entity) {
            this.parent.children.push(this);
        }
    }

    /**
     * Applies a behavior (mixin) to the entity instance.
     * This method tracks all properties and methods added by the behavior so they can be removed later.
     * @param {function} behavior - The mixin function (e.g., hasHealth).
     * @param {...any} args - The arguments to pass to the mixin (e.g., initial health).
     * @returns {Entity} The entity instance for chaining.
     */
    with(behavior, ...args) {
        if (this.has(behavior)) {
            console.warn(
                `Behavior '${behavior.name}' has already been added to entity '${this.name}'.`
            );
            return this;
        }

        // Add the behavior to the set immediately to prevent recursive calls
        // from re-entering for the same behavior. This acts as a lock.
        this.behaviors.add(behavior);

        // Get all property keys on the entity before applying the new behavior.
        const beforeKeys = new Set(Object.keys(this));

        // Apply the behavior. The behavior function might add properties directly to the entity
        // and/or return an object of methods to be assigned.
        const behaviorMethods = behavior(this, ...args);
        if (typeof behaviorMethods === "object" && behaviorMethods !== null) {
            Object.assign(this, behaviorMethods);
        }

        // Get all property keys after applying the behavior.
        const afterKeys = new Set(Object.keys(this));

        // Determine which new properties were added.
        const newKeys = [...afterKeys].filter((key) => !beforeKeys.has(key));

        // Store the list of new properties, associating them with the behavior.
        this._behaviorProps.set(behavior, newKeys);

        return this;
    }

    /**
     * Removes a behavior (mixin) from the entity instance.
     * It removes all properties and methods that were added by that behavior.
     * @param {function} behavior - The mixin function to remove.
     * @returns {Entity} The entity instance for chaining.
     */
    without(behavior) {
        if (!this.has(behavior)) {
            console.warn(
                `Cannot remove behavior '${behavior.name}' from entity '${this.name}' because it was never added.`
            );
            return this;
        }

        // Get the list of properties that this behavior added.
        const propsToRemove = this._behaviorProps.get(behavior);

        if (propsToRemove) {
            // Delete each property from the entity instance.
            for (const prop of propsToRemove) {
                delete this[prop];
            }
        }

        // Clean up tracking maps.
        this._behaviorProps.delete(behavior);
        this.behaviors.delete(behavior);

        return this;
    }

    /**
     * Checks if a behavior has been applied to this entity.
     * @param {function} behavior - The mixin function to check for.
     * @returns {boolean} True if the entity has the behavior, false otherwise.
     */
    has(behavior) {
        return this.behaviors.has(behavior);
    }

    /**
     * Returns an array of strings with the names of all applied behaviors.
     * @returns {string[]} An array of behavior names.
     */
    getBehaviors() {
        return Array.from(this.behaviors, (behavior) => behavior.name);
    }

    /**
     * Removes all behaviors from the entity.
     * @returns {Entity} The entity instance for chaining.
     */
    clearBehaviors() {
        // We create a copy of the behaviors set because the `without` method will be modifying the original set during iteration.
        const behaviorsToRemove = new Set(this.behaviors);
        for (const behavior of behaviorsToRemove) {
            this.without(behavior);
        }
        return this;
    }
}
