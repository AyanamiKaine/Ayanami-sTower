CREATE TABLE IF NOT EXISTS Culture (
    EntityId INTEGER NOT NULL UNIQUE, 
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);