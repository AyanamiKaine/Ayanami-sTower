-- A modifier represents either a flat or percentage increase or degress this could be a CanMarry modifier, or Fertility increase by 10%.
-- maybe we split it into 4 tables, negative flat/percentage and positive flat/percentage
CREATE TABLE IF NOT EXISTS Modifier (
    EntityId INTEGER NOT NULL UNIQUE, 
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);