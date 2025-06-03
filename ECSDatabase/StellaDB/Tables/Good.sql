CREATE TABLE IF NOT EXISTS Good (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL UNIQUE,
    Category TEXT NOT NULL, -- 'raw_material', 'manufactured', 'luxury', 'military', 'food', etc.
    BaseWeight REAL NOT NULL DEFAULT 1.0, -- For transport cost calculations
    BaseVolume REAL NOT NULL DEFAULT 1.0, -- For storage calculations
    IsAbstract BOOLEAN NOT NULL DEFAULT FALSE, -- For service-type goods
    Description TEXT
);