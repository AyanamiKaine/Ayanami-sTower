CREATE TABLE IF NOT EXISTS IdeologyConsumptionModifier (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    IdeologyId INTEGER NOT NULL,
    GoodId INTEGER NOT NULL,
    ConsumptionMultiplier REAL NOT NULL DEFAULT 1.0,
    PriorityModifier INTEGER DEFAULT 0,
    IsDiscouraged BOOLEAN NOT NULL DEFAULT FALSE, -- Ideologically discouraged
    IsPromoted BOOLEAN NOT NULL DEFAULT FALSE, -- Ideologically promoted
    Description TEXT, -- Ideological reasoning
    
    FOREIGN KEY (IdeologyId) REFERENCES Ideology(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (GoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(IdeologyId, GoodId),
    CHECK (ConsumptionMultiplier >= 0),
    CHECK (NOT (IsDiscouraged AND IsPromoted))
);