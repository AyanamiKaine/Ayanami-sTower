/*
Used to indicate that an entity is located at another entity. For example
Imagine a character entity that is located at a province entity. This relation
is helpful when we destroy a planet to also "terminate" all entities that are located
at the planet
*/

CREATE TABLE IF NOT EXISTS LocatedAt (
    EntityId1 INTEGER NOT NULL,
    EntityId2 INTEGER NOT NULL,
    
    PRIMARY KEY (EntityId1, EntityId2),
    FOREIGN KEY (EntityId1) REFERENCES Entity(Id) ON DELETE CASCADE,
    FOREIGN KEY (EntityId2) REFERENCES Entity(Id) ON DELETE CASCADE,
    
    CHECK (EntityId1 < EntityId2)
);

-- Indexes for efficient querying (for Option 1)
CREATE INDEX IF NOT EXISTS idx_connected_to_entity1 ON LocatedAt(EntityId1);
CREATE INDEX IF NOT EXISTS idx_connected_to_entity2 ON LocatedAt(EntityId2);