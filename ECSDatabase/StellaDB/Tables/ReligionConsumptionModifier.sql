CREATE TABLE IF NOT EXISTS ReligionConsumptionModifier (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ReligionId INTEGER NOT NULL,
    GoodId INTEGER NOT NULL,
    ConsumptionMultiplier REAL NOT NULL DEFAULT 1.0, -- 0.0 = forbidden/sin
    PriorityModifier INTEGER DEFAULT 0,
    IsForbidden BOOLEAN NOT NULL DEFAULT FALSE, -- Explicit prohibition
    IsRitualGood BOOLEAN NOT NULL DEFAULT FALSE, -- Required for religious practices
    Description TEXT, -- Religious reasoning
    
    FOREIGN KEY (ReligionId) REFERENCES Religion(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (GoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(ReligionId, GoodId),
    CHECK (ConsumptionMultiplier >= 0),
    CHECK (NOT (IsForbidden AND IsRitualGood)) -- Can't be both forbidden and required
);