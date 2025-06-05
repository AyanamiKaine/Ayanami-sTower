CREATE TABLE IF NOT EXISTS Population (
    EntityId INTEGER NOT NULL UNIQUE, 
    consciousness REAL DEFAULT 0.0,         -- Political awareness (0-10)
    militancy REAL DEFAULT 0.0,             -- Revolutionary tendency (0-10)
    literacy REAL DEFAULT 0.0,              -- Education level (0-1)
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);

-- Example query to calculate actual consumption for a population
/*
-- Calculate final consumption for a specific population
WITH PopulationConsumption AS (
    SELECT 
        p.Id as PopulationId,
        p.Size,
        p.WealthLevel,
        ptc.GoodId,
        ptc.BaseConsumption,
        ptc.Priority,
        g.Category,
        
        -- Apply cultural modifier
        COALESCE(ccm.ConsumptionMultiplier, 1.0) as CultureMultiplier,
        COALESCE(ccm.PriorityModifier, 0) as CulturePriorityMod,
        
        -- Apply religious modifier
        COALESCE(rcm.ConsumptionMultiplier, 1.0) as ReligionMultiplier,
        COALESCE(rcm.PriorityModifier, 0) as ReligionPriorityMod,
        COALESCE(rcm.IsForbidden, FALSE) as IsForbidden,
        
        -- Apply ideological modifier  
        COALESCE(icm.ConsumptionMultiplier, 1.0) as IdeologyMultiplier,
        COALESCE(icm.PriorityModifier, 0) as IdeologyPriorityMod,
        
        
    FROM Population p
    JOIN PopTypeConsumption ptc ON p.PopTypeId = ptc.PopTypeId
    JOIN Good g ON ptc.GoodId = g.Id
    LEFT JOIN CultureConsumptionModifier ccm ON p.CultureId = ccm.CultureId AND ptc.GoodId = ccm.GoodId
    LEFT JOIN ReligionConsumptionModifier rcm ON p.ReligionId = rcm.ReligionId AND ptc.GoodId = rcm.GoodId
    LEFT JOIN IdeologyConsumptionModifier icm ON p.IdeologyId = icm.IdeologyId AND ptc.GoodId = icm.GoodId
    WHERE p.Id = ? -- Specific population ID
)
SELECT 
    PopulationId,
    GoodId,
    Size,
    CASE 
        WHEN IsForbidden THEN 0.0
        WHEN WealthLevel < AccessibilityThreshold THEN 0.0
        ELSE (
            BaseConsumption * 
            Size * 
            POWER(WealthLevel, WealthScaling) *
            CultureMultiplier * 
            ReligionMultiplier * 
            IdeologyMultiplier
        )
    END as FinalConsumption,
    (Priority + CulturePriorityMod + ReligionPriorityMod + IdeologyPriorityMod) as FinalPriority
FROM PopulationConsumption
ORDER BY FinalPriority DESC, FinalConsumption DESC;
*/