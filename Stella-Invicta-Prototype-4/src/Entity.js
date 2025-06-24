/**
 * Represents an object in the game world.
 * It uses an object-based constructor for named parameters.
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
        this.behaviors = new Set(); // A Set to track applied mixins.

        // We only want to add a valid entity to the parent's children
        if (this.parent instanceof Entity) {
            this.parent.children.push(this);
        }
    }

    /**
     * Applies a behavior (mixin) to the entity instance.
     * @param {function} behavior - The mixin function (e.g., hasHealth).
     * @param {...any} args - The arguments to pass to the mixin (e.g., initial health).
     */
    with(behavior, ...args) {
        this.behaviors.add(behavior); // Track the applied behavior.
        const behaviorMethods = behavior(this, ...args);
        Object.assign(this, behaviorMethods);
        return this;
    }

    /**
     * Checks if a behavior has been applied to this entity.
     * @param {function} behavior - The mixin function to check for.
     * @returns {boolean}
     */
    has(behavior) {
        return this.behaviors.has(behavior);
    }
}