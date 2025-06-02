-- why the name polity instead of faction ?
-- It much better captures the diversity of complex political structures. 
-- https://en.wikipedia.org/wiki/Polity

CREATE TABLE IF NOT EXISTS Polity (
    EntityId INTEGER NOT NULL UNIQUE, 
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);