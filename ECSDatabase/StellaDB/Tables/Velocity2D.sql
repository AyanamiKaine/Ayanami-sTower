CREATE TABLE IF NOT EXISTS Velocity2D (
    EntityId INTEGER NOT NULL UNIQUE, 
    X REAL NOT NULL,
    Y REAL NOT NULL,
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);