-- A feature attribute is used to enable specifc features, the mere presence in this tables mean that for the given entity the feature is enabled.

/*

Imagine you have a religion that has a specific can_communicate_with_void feature, that you want to enable.
We would use the entity id that represents the religion and add it to this table.


This systems allows to dynamically define new feature flags, that are completly decoupled, so we can mix and match features as we wish. 
Add new ones or even disable them.

Currently the table definition is not really correct. Because a FeatureAttribute needs to be defined as an entity and we need a way to say that an entity has many different attached feature attributes.
*/


CREATE TABLE IF NOT EXISTS FeatureAttribute (
    EntityId INTEGER NOT NULL UNIQUE, 
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);