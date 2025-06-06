CREATE TABLE IF NOT EXISTS ConnectedTo (
    EntityId1 INTEGER NOT NULL,
    EntityId2 INTEGER NOT NULL,    
    PRIMARY KEY (EntityId1, EntityId2),
    FOREIGN KEY (EntityId1) REFERENCES Entity(Id) ON DELETE CASCADE,
    FOREIGN KEY (EntityId2) REFERENCES Entity(Id) ON DELETE CASCADE,
    
    CHECK (EntityId1 < EntityId2)
);

-- Indexes for efficient querying (for Option 1)
CREATE INDEX IF NOT EXISTS idx_connected_to_entity1 ON ConnectedTo(EntityId1);
CREATE INDEX IF NOT EXISTS idx_connected_to_entity2 ON ConnectedTo(EntityId2);

-- Example queries for Option 1:
-- To find all systems connected to a specific system (e.g., EntityId = 5):
/*
SELECT 
    CASE 
        WHEN EntityId1 = 5 THEN EntityId2 
        ELSE EntityId1 
    END as ConnectedSystemId,
FROM ConnectedTo 
WHERE EntityId1 = 5 OR EntityId2 = 5;
*/

-- To check if two systems are connected:
/*
SELECT COUNT(*) > 0 as IsConnected
FROM ConnectedTo 
WHERE (EntityId1 = ? AND EntityId2 = ?) 
   OR (EntityId1 = ? AND EntityId2 = ?)
   -- Note: with CHECK constraint, you only need to check the ordered pair
   -- WHERE (EntityId1 = MIN(?, ?) AND EntityId2 = MAX(?, ?))
*/


/*

Getting all systems that are connected to the sol system.

SELECT
    -- 3. Get the 'Value' (the name) from the Name table for each of the connected IDs.
    Name.Value AS ConnectedSystemName
FROM
    Name
WHERE
    -- This 'EntityId' is from the Name table.
    Name.EntityId IN (
        -- 2. This subquery produces a list of all entity IDs connected to 'Sol'.
        SELECT
            -- This CASE statement finds the ID of the "other" system in the pair.
            CASE
                WHEN CT.EntityId1 = (SELECT EntityId FROM Name WHERE Value = 'Sol')
                THEN CT.EntityId2
                ELSE CT.EntityId1
            END
        FROM
            ConnectedTo AS CT
        WHERE
            -- 1. Find all connection rows that involve Sol's ID.
            CT.EntityId1 = (SELECT EntityId FROM Name WHERE Value = 'Sol') OR
            CT.EntityId2 = (SELECT EntityId FROM Name WHERE Value = 'Sol')
    );

*/