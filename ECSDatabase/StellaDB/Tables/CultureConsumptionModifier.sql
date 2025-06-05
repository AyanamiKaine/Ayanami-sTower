CREATE TABLE IF NOT EXISTS CultureConsumptionModifier (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CultureId INTEGER NOT NULL,
    GoodId INTEGER NOT NULL,
    ConsumptionMultiplier REAL NOT NULL DEFAULT 1.0, -- 0.0 = forbidden, 1.0 = normal, >1.0 = preferred
    PriorityModifier INTEGER DEFAULT 0, -- -1 = less important, +1 = more important
    
    FOREIGN KEY (CultureId) REFERENCES Culture(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (GoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(CultureId, GoodId),
    CHECK (ConsumptionMultiplier >= 0)
);