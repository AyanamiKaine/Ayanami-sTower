CREATE TABLE IF NOT EXISTS RecipeInput (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecipeId INTEGER NOT NULL,
    InputGoodId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    IsOptional BOOLEAN NOT NULL DEFAULT FALSE, -- Can production continue without this?
    EfficiencyImpact REAL DEFAULT 1.0, -- Impact on efficiency if missing (0-1)
    
    FOREIGN KEY (RecipeId) REFERENCES ProductionRecipe(Id) ON DELETE CASCADE,
    FOREIGN KEY (InputGoodId) REFERENCES Good(Id) ON DELETE CASCADE,
    UNIQUE(RecipeId, InputGoodId),
    CHECK (Quantity > 0),
    CHECK (EfficiencyImpact >= 0 AND EfficiencyImpact <= 1)
);

CREATE INDEX IF NOT EXISTS idx_recipe_output ON ProductionRecipe(OutputGoodId);

CREATE INDEX IF NOT EXISTS idx_recipe_input_recipe ON RecipeInput(RecipeId);
CREATE INDEX IF NOT EXISTS idx_recipe_input_good ON RecipeInput(InputGoodId);