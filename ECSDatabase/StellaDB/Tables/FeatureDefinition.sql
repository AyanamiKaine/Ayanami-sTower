CREATE TABLE IF NOT EXISTS FeatureDefinition (
    EntityId INTEGER NOT NULL UNIQUE,
    Key TEXT NOT NULL UNIQUE, -- e.g., 'CanMarry', 'CanCommunicateWithVoid'
    Description TEXT,
    Category TEXT, -- Optional grouping like 'social', 'supernatural', 'combat'
    
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);