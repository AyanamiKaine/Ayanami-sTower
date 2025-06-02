CREATE TABLE IF NOT EXISTS Trait (
    EntityId INTEGER NOT NULL UNIQUE, 
    IsPositive BOOLEAN DEFAULT 1, -- Whether this is generally a positive or negative trait


    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);