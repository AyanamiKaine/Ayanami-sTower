CREATE TABLE IF NOT EXISTS Population (
    EntityId INTEGER NOT NULL UNIQUE, 
    consciousness REAL DEFAULT 0.0,         -- Political awareness (0-10)
    militancy REAL DEFAULT 0.0,             -- Revolutionary tendency (0-10)
    literacy REAL DEFAULT 0.0,              -- Education level (0-1)
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);