-- Junction table for Character-Trait many-to-many relationship

CREATE TABLE IF NOT EXISTS CharacterTrait (
    CharacterId INTEGER NOT NULL,
    TraitId INTEGER NOT NULL,
        
    PRIMARY KEY (CharacterId, TraitId),
    FOREIGN KEY (CharacterId) REFERENCES Character(EntityId) ON DELETE CASCADE,
    FOREIGN KEY (TraitId) REFERENCES Trait(EntityId) ON DELETE CASCADE
);

-- Indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_character_trait_character ON CharacterTrait(CharacterId);
CREATE INDEX IF NOT EXISTS idx_character_trait_trait ON CharacterTrait(TraitId);

-- Example queries:

-- Find all traits for a specific character:
/*
SELECT t.Name, t.Description, t.Category, ct.TraitLevel, ct.Source
FROM Character c
JOIN CharacterTrait ct ON c.EntityId = ct.CharacterId
JOIN Trait t ON ct.TraitId = t.EntityId
WHERE c.EntityId = ?;
*/

-- Find all characters with a specific trait:
/*
SELECT c.Name, c.Age, ct.TraitLevel
FROM Trait t
JOIN CharacterTrait ct ON t.EntityId = ct.TraitId
JOIN Character c ON ct.CharacterId = c.EntityId
WHERE t.Name = 'Brave';
*/