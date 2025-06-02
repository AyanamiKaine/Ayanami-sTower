CREATE TABLE IF NOT EXISTS ConnectedTo (
    SystemId1 INTEGER NOT NULL,
    SystemId2 INTEGER NOT NULL,
    Distance REAL,
    
    PRIMARY KEY (SystemId1, SystemId2),
    FOREIGN KEY (SystemId1) REFERENCES StarSystem(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (SystemId2) REFERENCES StarSystem(EntityId) ON DELETE CASCADE,
    
    CHECK (SystemId1 < SystemId2)
);

-- Indexes for efficient querying (for Option 1)
CREATE INDEX IF NOT EXISTS idx_connected_to_system1 ON ConnectedTo(SystemId1);
CREATE INDEX IF NOT EXISTS idx_connected_to_system2 ON ConnectedTo(SystemId2);

-- Example queries for Option 1:
-- To find all systems connected to a specific system (e.g., EntityId = 5):
/*
SELECT 
    CASE 
        WHEN SystemId1 = 5 THEN SystemId2 
        ELSE SystemId1 
    END as ConnectedSystemId,
    Distance,
    TravelTime,
    ConnectionType
FROM ConnectedTo 
WHERE SystemId1 = 5 OR SystemId2 = 5;
*/

-- To check if two systems are connected:
/*
SELECT COUNT(*) > 0 as IsConnected
FROM ConnectedTo 
WHERE (SystemId1 = ? AND SystemId2 = ?) 
   OR (SystemId1 = ? AND SystemId2 = ?)
   -- Note: with CHECK constraint, you only need to check the ordered pair
   -- WHERE (SystemId1 = MIN(?, ?) AND SystemId2 = MAX(?, ?))
*/