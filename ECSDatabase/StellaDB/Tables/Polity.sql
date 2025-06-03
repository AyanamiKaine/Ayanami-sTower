-- "A polity is a group of people with a collective identity,
-- who are organized by some form of political institutionalized 
-- social relations, and have a capacity to mobilize resources"

-- Why the name polity instead of lets say faction ?
-- It much better captures the diversity of complex political structures. 
-- https://en.wikipedia.org/wiki/Polity

CREATE TABLE IF NOT EXISTS Polity (
    EntityId INTEGER NOT NULL UNIQUE, 
    SeatOfPowerLocationID INTEGER,     -- Foreign Key to a entity that represents a location.
    Abbreviation VARCHAR(50),          -- e.g., 'GER', 'USA', 'CIS', 'FO'
    LeaderTitle VARCHAR(500), -- e.g., 'Emperor', 'Chancellor', 'Supreme Leader', 'Grand Moff', 'Vigo', 'Ascendant', "Council of Justice"
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
    FOREIGN KEY (EntityId) REFERENCES Entity(Id)
);