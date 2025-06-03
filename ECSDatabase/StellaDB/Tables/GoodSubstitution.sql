CREATE TABLE IF NOT EXISTS GoodSubstitution (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    GoodId INTEGER NOT NULL,
    SubstituteGoodId INTEGER NOT NULL,
    SubstitutionRate REAL NOT NULL DEFAULT 0.5, -- How effectively one can substitute for another (0-1)
    
    FOREIGN KEY (GoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    FOREIGN KEY (SubstituteGoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(GoodId, SubstituteGoodId),
    CHECK (SubstitutionRate > 0 AND SubstitutionRate <= 1),
    CHECK (GoodId != SubstituteGoodId)
);

CREATE INDEX IF NOT EXISTS idx_substitution_good ON GoodSubstitution(GoodId);
CREATE INDEX IF NOT EXISTS idx_substitution_substitute ON GoodSubstitution(SubstituteGoodId);
