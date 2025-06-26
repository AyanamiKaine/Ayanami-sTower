import { trait } from "./Trait.js";

/**
 * A mixin that gives an entity the ability to possess a collection of traits.
 * This is a general-purpose behavior that can be applied to any entity,
 * not just characters.
 *
 * This version of the mixin directly attaches methods to the entity,
 * which is a more robust pattern than returning them from a closure.
 *
 * @param {object} entity - The entity to give trait-holding capabilities.
 */
export const hasTraits = (entity) => {
    // Initialize the property directly on the entity.
    entity.traits = new Set();

    /**
     * Adds a trait entity.
     * Note: We use a classic function() definition here so that `this` refers
     * to the entity instance when the method is called (e.g., korriban.addTrait()).
     * @param {object} traitEntity - The entity (which must have the 'trait' mixin) to add.
     */
    entity.addTrait = function (traitEntity) {
        if (!traitEntity || !traitEntity.has(trait)) {
            console.warn(
                `Cannot add trait to '${this.name}': The provided object is not a valid entity with the 'trait' behavior.`
            );
            return;
        }
        this.traits.add(traitEntity);
    };

    /**
     * Removes a trait entity from this entity.
     * @param {object} traitEntity - The trait entity to remove.
     */
    entity.removeTrait = function (traitEntity) {
        this.traits.delete(traitEntity);
    };

    /**
     * Checks if this entity possesses a specific trait.
     * @param {object} traitEntity - The trait entity to check for.
     * @returns {boolean}
     */
    entity.hasTrait = function (traitEntity) {
        return this.traits.has(traitEntity);
    };

    /**
     * Gets all trait entities associated with this entity.
     * @returns {object[]} An array of trait entities.
     */
    entity.getTraits = function () {
        return Array.from(this.traits);
    };

    /**
     * A helper method to get only the positive traits.
     * @returns {object[]} An array of positive trait entities.
     */
    entity.getPositiveTraits = function () {
        return Array.from(this.traits).filter((t) => t.isPositive);
    };

    /**
     * A helper method to get only the negative traits.
     * @returns {object[]} An array of negative trait entities.
     */
    entity.getNegativeTraits = function () {
        return Array.from(this.traits).filter((t) => !t.isPositive);
    };

    // Since this mixin now directly modifies the entity, it doesn't need to return anything.
};
