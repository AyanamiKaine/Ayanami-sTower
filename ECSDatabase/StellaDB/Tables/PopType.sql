CREATE TABLE IF NOT EXISTS PopType (
    EntityId INTEGER NOT NULL UNIQUE, 
    Key TEXT NOT NULL UNIQUE, -- 'officers', 'workers', 'artisans', 'aristocrats', etc.
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);