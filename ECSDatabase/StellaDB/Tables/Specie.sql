CREATE TABLE IF NOT EXISTS Specie (
    EntityId INTEGER NOT NULL UNIQUE, 
    BaseFertility REAL NOT NULL,
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);