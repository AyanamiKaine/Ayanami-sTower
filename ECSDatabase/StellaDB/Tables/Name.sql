CREATE TABLE IF NOT EXISTS Name (
    EntityId INTEGER NOT NULL UNIQUE,
    Value TEXT UNIQUE,
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);