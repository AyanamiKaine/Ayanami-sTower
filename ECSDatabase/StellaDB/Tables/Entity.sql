CREATE TABLE IF NOT EXISTS Entity (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId INTEGER,
    OwnerId INTEGER,

    FOREIGN KEY (ParentId) REFERENCES Entity(Id)
        ON DELETE CASCADE
    FOREIGN KEY (OwnerId) REFERENCES Entity(Id) ON DELETE SET NULL
);

-- Add index for efficient ownership queries
CREATE INDEX IF NOT EXISTS idx_entity_owner ON Entity(OwnerId);

-- Find all entities owned by a specific owner (e.g., Nation with Id = 10):
/*
SELECT * FROM Entity WHERE OwnerId = 10;
*/

-- Find the owner of a specific entity:
/*
SELECT owner.* 
FROM Entity owned
JOIN Entity owner ON owned.OwnerId = owner.Id
WHERE owned.Id = ?;
*/

-- Find all star systems owned by a nation:
/*
SELECT ss.EntityId, e.*
FROM Entity e
JOIN StarSystem ss ON e.Id = ss.EntityId
WHERE e.OwnerId = ?; -- Nation's EntityId
*/