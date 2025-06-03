-- A feature attribute is used to enable specifc features, the mere presence in this tables mean that for the given entity the feature is enabled.

/*

Imagine you have a religion that has a specific can_communicate_with_void feature, that you want to enable.
We would use the entity id that represents the religion and add it to this table. Or for characters can_marry,
so we could dynamicaly add and remove abilities.


This systems allows to dynamically define new feature flags, that are completly decoupled, so we can mix and match features as we wish. 
Add new ones or even disable them.

*/
-- Junction table to assign features to entities
-- This is the core table that says "Entity X has Feature Y"
CREATE TABLE IF NOT EXISTS EntityFeature (
    EntityId INTEGER NOT NULL, -- The entity that has the feature
    FeatureId INTEGER NOT NULL, -- References FeatureDefinition.EntityId
    
    PRIMARY KEY (EntityId, FeatureId),
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE,
    FOREIGN KEY(FeatureId) REFERENCES FeatureDefinition(EntityId) ON DELETE CASCADE
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_entity_feature_entity ON EntityFeature(EntityId);
CREATE INDEX IF NOT EXISTS idx_entity_feature_feature ON EntityFeature(FeatureId);