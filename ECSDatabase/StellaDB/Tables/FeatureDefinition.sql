CREATE TABLE IF NOT EXISTS FeatureDefinition (
    EntityId INTEGER NOT NULL UNIQUE,
    Name TEXT NOT NULL UNIQUE, -- e.g., 'can_marry', 'can_communicate_with_void'
    Description TEXT,
    Category TEXT, -- Optional grouping like 'social', 'supernatural', 'combat'
    
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);