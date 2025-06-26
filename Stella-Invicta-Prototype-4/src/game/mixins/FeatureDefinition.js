/**
 * A marker mixin that designates an entity as a "feature definition".
 * A feature represents a specific capability or property that can be granted
 * to other entities like traits, characters, cultures, etc.
 *
 * @param {object} entity - The entity to mark as a feature definition.
 * @param {string} [key=""] - A unique machine-readable key (e.g., 'CanMarry', 'IsTelepathic').
 * @param {string} [description=""] - A human-readable description of the feature.
 * @param {string} [category=""] - An optional category for grouping (e.g., 'social', 'combat').
 */
export const featureDefinition = (
    entity,
    key = "",
    description = "",
    category = ""
) => {
    entity.key = key;
    entity.description = description;
    entity.category = category;

    // A helper property to make it easy to identify entities with this mixin.
    entity.isFeatureDefinition = true;
};
