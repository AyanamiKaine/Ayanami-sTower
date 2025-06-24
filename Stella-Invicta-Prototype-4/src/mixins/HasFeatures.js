/**
 * A mixin that gives an entity the ability to possess a collection of features.
 * This can be applied to characters, traits, cultures, or any other entity
 * that needs to have specific capabilities.
 *
 * @param {object} entity - The entity to give feature-holding capabilities.
 */
export const hasFeatures = (entity) => {
    // Initialize the property directly on the entity.
    entity.features = new Set();

    /**
     * Adds a feature entity.
     * @param {object} featureEntity - The entity (which must have the 'featureDefinition' mixin) to add.
     */
    entity.addFeature = function (featureEntity) {
        if (!featureEntity || !featureEntity.isFeatureDefinition) {
            console.warn(
                `Cannot add feature to '${this.name}': The provided object is not a valid feature definition.`
            );
            return;
        }
        this.features.add(featureEntity);
    };

    /**
     * Removes a feature entity from this entity.
     * @param {object} featureEntity - The feature entity to remove.
     */
    entity.removeFeature = function (featureEntity) {
        this.features.delete(featureEntity);
    };

    /**
     * Checks if this entity possesses a specific feature.
     * This is a powerful helper that can check by the feature entity object or by its unique string key.
     * @param {object|string} featureIdentifier - The feature entity object or its string key.
     * @returns {boolean}
     */
    entity.hasFeature = function (featureIdentifier) {
        if (typeof featureIdentifier === "string") {
            for (const feature of this.features) {
                if (feature.key === featureIdentifier) {
                    return true;
                }
            }
            return false;
        }
        return this.features.has(featureIdentifier);
    };

    /**
     * Gets all feature entities associated with this entity.
     * @returns {object[]} An array of feature entities.
     */
    entity.getFeatures = function () {
        return Array.from(this.features);
    };
};
