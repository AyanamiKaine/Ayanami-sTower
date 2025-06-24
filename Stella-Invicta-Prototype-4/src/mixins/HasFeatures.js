import { featureDefinition } from "./FeatureDefinition.js";
import { hasTraits } from "./HasTraits.js"; // We need to be aware of the hasTraits mixin.

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
     * Checks if this entity possesses a specific feature, either directly
     * or through one of its traits.
     * @param {object|string} featureIdentifier - The feature entity object or its unique string key.
     * @returns {boolean}
     */
    entity.hasFeature = function (featureIdentifier) {
        // Step 1: Check for the feature directly on this entity.
        if (typeof featureIdentifier === "string") {
            for (const feature of this.features) {
                if (feature.key === featureIdentifier) {
                    return true; // Found it directly.
                }
            }
        } else if (this.features.has(featureIdentifier)) {
            return true; // Found it directly.
        }

        // Step 2: If not found, check for the feature within any traits this entity possesses.
        // This requires the entity to also have the `hasTraits` mixin.
        if (
            this.has &&
            typeof this.has === "function" &&
            this.has(hasTraits) &&
            this.getTraits
        ) {
            for (const trait of this.getTraits()) {
                // If the trait can have features and has the one we're looking for...
                if (
                    trait.hasFeature &&
                    typeof trait.hasFeature === "function" &&
                    trait.hasFeature(featureIdentifier)
                ) {
                    return true; // Found it on a trait!
                }
            }
        }

        // If we've checked everywhere and haven't found it.
        return false;
    };

    /**
     * Gets all feature entities associated with this entity.
     * @returns {object[]} An array of feature entities.
     */
    entity.getFeatures = function () {
        return Array.from(this.features);
    };
};
