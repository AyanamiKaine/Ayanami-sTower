CREATE TABLE IF NOT EXISTS GoodCategory (
    Name TEXT PRIMARY KEY,
    Description TEXT,
    TransportModifier REAL DEFAULT 1.0, -- Affects transport costs
    StorageModifier REAL DEFAULT 1.0,   -- Affects storage costs
    TaxCategory TEXT DEFAULT 'standard' -- For taxation systems
);

CREATE INDEX IF NOT EXISTS idx_good_category ON Good(Category);
