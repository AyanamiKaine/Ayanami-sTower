export class Entity {
    constructor(name = "", parent, children = []) {
        this.name = name;

        // We only want to add a valid entity to the children
        if (parent instanceof Entity) {
            parent.children.push(this);
            this.parent = parent;
        }

        this.children = children;
        this.behaviors = new Set(); // A Set to track applied mixins.
    }

    /**
     * Applies a behavior (mixin) to the entity instance.
     * @param {function} behavior - The mixin function (e.g., hasHealth).
     * @param  {...any} args - The arguments to pass to the mixin (e.g., initial health).
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
