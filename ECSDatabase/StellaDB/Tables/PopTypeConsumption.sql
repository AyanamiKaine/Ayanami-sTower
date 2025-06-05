CREATE TABLE IF NOT EXISTS PopTypeConsumption (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PopTypeId INTEGER NOT NULL,
    GoodId INTEGER NOT NULL,
    BaseConsumption REAL NOT NULL DEFAULT 0.0, -- Base units consumed per pop per day/turn
    Priority INTEGER NOT NULL DEFAULT 1, -- 1=luxury, 2=normal, 3=necessity, 4=survival
    
    FOREIGN KEY (PopTypeId) REFERENCES PopType(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (GoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(PopTypeId, GoodId),
    CHECK (BaseConsumption >= 0),
    CHECK (Priority >= 1 AND Priority <= 4)
)