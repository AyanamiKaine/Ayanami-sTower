CREATE TABLE IF NOT EXISTS Prestige (
    EntityId INTEGER NOT NULL UNIQUE, 
    Value REAL NOT NULL,
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);