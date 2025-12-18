using AyanamisTower.SFPM;
using InvictaDB.PatternMatching;

namespace InvictaDB.Tests;

#pragma warning disable CS1591

/// <summary>
/// Unit tests demonstrating how to use the fuzzy pattern matcher with InvictaDB.
/// </summary>
public class PatternMatchingUnitTest
{
    // Test entity types
    public record Character(string Id, string Name, int Age, string Faction, int Strength, bool IsAlive);
    public record Ship(string Id, string Name, int CrewSize, string Type, int HullIntegrity);
    public record GameState(int CurrentYear, string CurrentSeason, bool IsAtWar);

    #region Basic Entity Matching

    [Fact]
    public void AsFactSource_EntityProperties_CanBeQueried()
    {
        // Arrange - Create a character entity
        var character = new Character("char1", "Marcus", 35, "Empire", 75, true);
        var factSource = character.AsFactSource();

        // Act - Query properties as facts
        var hasAge = factSource.TryGetFact<int>("Age", out var age);
        var hasName = factSource.TryGetFact<string>("Name", out var name);
        var hasFaction = factSource.TryGetFact<string>("Faction", out var faction);

        // Assert
        Assert.True(hasAge);
        Assert.Equal(35, age);
        Assert.True(hasName);
        Assert.Equal("Marcus", name);
        Assert.True(hasFaction);
        Assert.Equal("Empire", faction);
    }

    [Fact]
    public void AsFactSource_MissingProperty_ReturnsFalse()
    {
        var character = new Character("char1", "Marcus", 35, "Empire", 75, true);
        var factSource = character.AsFactSource();

        var hasNonExistent = factSource.TryGetFact<string>("NonExistentProperty", out var value);

        Assert.False(hasNonExistent);
        Assert.Null(value);
    }

    #endregion

    #region Rule Evaluation Against Entities

    [Fact]
    public void Rule_EvaluatesAgainstEntity_MatchesWhenCriteriaMet()
    {
        // Arrange
        var character = new Character("char1", "Marcus", 35, "Empire", 75, true);
        var factSource = character.AsFactSource();

        var adultRule = new Rule(
            [new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual)],
            () => { },
            "AdultRule"
        );

        // Act
        var (matched, criteriaCount) = adultRule.Evaluate(factSource);

        // Assert
        Assert.True(matched);
        Assert.Equal(1, criteriaCount);
    }

    [Fact]
    public void Rule_EvaluatesAgainstEntity_FailsWhenCriteriaNotMet()
    {
        // Arrange
        var youngCharacter = new Character("char1", "Tim", 12, "Empire", 30, true);
        var factSource = youngCharacter.AsFactSource();

        var adultRule = new Rule(
            [new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual)],
            () => { },
            "AdultRule"
        );

        // Act
        var (matched, _) = adultRule.Evaluate(factSource);

        // Assert
        Assert.False(matched);
    }

    [Fact]
    public void Rule_WithMultipleCriteria_AllMustMatch()
    {
        // Arrange
        var veteran = new Character("char1", "Marcus", 45, "Empire", 90, true);
        var factSource = veteran.AsFactSource();

        var veteranRule = new Rule(
            [
                new Criteria<int>("Age", 40, Operator.GreaterThanOrEqual),
                new Criteria<int>("Strength", 80, Operator.GreaterThanOrEqual),
                new Criteria<bool>("IsAlive", true, Operator.Equal)
            ],
            () => { },
            "VeteranWarriorRule"
        );

        // Act
        var (matched, criteriaCount) = veteranRule.Evaluate(factSource);

        // Assert
        Assert.True(matched);
        Assert.Equal(3, criteriaCount);
    }

    [Fact]
    public void Rule_WithPredicateCriteria_SupportsCustomLogic()
    {
        // Arrange
        var character = new Character("char1", "Marcus", 35, "Empire", 75, true);
        var factSource = character.AsFactSource();

        // Custom predicate to check if name starts with 'M'
        var nameStartsWithM = new Rule(
            [new Criteria<string>("Name", name => name.StartsWith("M"), "StartsWithM")],
            () => { },
            "NameStartsWithMRule"
        );

        // Act
        var (matched, _) = nameStartsWithM.Evaluate(factSource);

        // Assert
        Assert.True(matched);
    }

    #endregion

    #region Database Table Matching

    [Fact]
    public void FindMatching_ReturnsEntitiesThatMatchAnyRule()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Young Tim", 12, "Rebels", 30, true))
            .Insert("c3", new Character("c3", "Elder Sara", 60, "Empire", 50, true))
            .Insert("c4", new Character("c4", "Teen Alex", 17, "Neutral", 45, true));

        var adultRule = new Rule(
            [new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual)],
            () => { },
            "AdultRule"
        );

        var rules = new List<Rule> { adultRule };

        // Act
        var adults = rules.FindMatching(db.GetTable<Character>()).ToList();

        // Assert
        Assert.Equal(2, adults.Count);
        Assert.Contains(adults, c => c.Name == "Marcus");
        Assert.Contains(adults, c => c.Name == "Elder Sara");
    }

    [Fact]
    public void FindMatching_WithMultipleRules_ReturnsEntitiesMatchingAny()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Young Tim", 12, "Rebels", 30, true))
            .Insert("c3", new Character("c3", "Elder Sara", 60, "Empire", 50, true));

        var empireRule = new Rule(
            [new Criteria<string>("Faction", "Empire", Operator.Equal)],
            () => { },
            "EmpireRule"
        );

        var youngRule = new Rule(
            [new Criteria<int>("Age", 15, Operator.LessThan)],
            () => { },
            "YoungRule"
        );

        var rules = new List<Rule> { empireRule, youngRule };

        // Act - Should match Empire members OR young characters
        var matched = rules.FindMatching(db.GetTable<Character>()).ToList();

        // Assert - All three match: Marcus (Empire), Tim (young), Sara (Empire)
        Assert.Equal(3, matched.Count);
    }

    [Fact]
    public void FindMatchingAll_RequiresAllRulesToMatch()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Young Tim", 12, "Empire", 30, true))
            .Insert("c3", new Character("c3", "Elder Sara", 60, "Rebels", 50, true));

        var empireRule = new Rule(
            [new Criteria<string>("Faction", "Empire", Operator.Equal)],
            () => { },
            "EmpireRule"
        );

        var adultRule = new Rule(
            [new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual)],
            () => { },
            "AdultRule"
        );

        var rules = new List<Rule> { empireRule, adultRule };

        // Act - Must match BOTH Empire AND Adult
        var matched = rules.FindMatchingAll(db.GetTable<Character>()).ToList();

        // Assert - Only Marcus is Empire AND adult
        Assert.Single(matched);
        Assert.Equal("Marcus", matched[0].Name);
    }

    [Fact]
    public void CountMatching_ReturnsCorrectCount()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Young Tim", 12, "Rebels", 30, true))
            .Insert("c3", new Character("c3", "Elder Sara", 60, "Empire", 50, true))
            .Insert("c4", new Character("c4", "Dead Joe", 40, "Empire", 80, false));

        var aliveRule = new Rule(
            [new Criteria<bool>("IsAlive", true, Operator.Equal)],
            () => { },
            "AliveRule"
        );

        // Act
        var count = new List<Rule> { aliveRule }.CountMatching(db.GetTable<Character>());

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void FindFirstMatching_ReturnsFirstMatch()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Sara", 60, "Empire", 50, true));

        var elderRule = new Rule(
            [new Criteria<int>("Age", 50, Operator.GreaterThanOrEqual)],
            () => { },
            "ElderRule"
        );

        // Act
        var elder = new List<Rule> { elderRule }.FindFirstMatching(db.GetTable<Character>());

        // Assert
        Assert.NotNull(elder);
        Assert.Equal("Sara", elder.Name);
    }

    #endregion

    #region EntityMatcher (Optimized Reusable Matcher)

    [Fact]
    public void EntityMatcher_Matches_ChecksSingleEntity()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new([new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual)], () => { }, "AdultRule")
        };

        var matcher = rules.CreateMatcher();
        var adult = new Character("c1", "Marcus", 35, "Empire", 75, true);
        var child = new Character("c2", "Tim", 10, "Empire", 20, true);

        // Act & Assert
        Assert.True(matcher.Matches(adult));
        Assert.False(matcher.Matches(child));
    }

    [Fact]
    public void EntityMatcher_Filter_ReturnsMatchingEntities()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Tim", 12, "Rebels", 30, true))
            .Insert("c3", new Character("c3", "Sara", 60, "Empire", 50, true));

        var matcher = new List<Rule>
        {
            new([new Criteria<string>("Faction", "Empire", Operator.Equal)], () => { }, "EmpireRule")
        }.CreateMatcher();

        // Act
        var empireMembers = matcher.Filter(db.GetTable<Character>()).ToList();

        // Assert
        Assert.Equal(2, empireMembers.Count);
        Assert.All(empireMembers, c => Assert.Equal("Empire", c.Faction));
    }

    [Fact]
    public void EntityMatcher_Partition_SplitsEntities()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Tim", 12, "Rebels", 30, true))
            .Insert("c3", new Character("c3", "Sara", 60, "Empire", 50, true));

        var matcher = new List<Rule>
        {
            new([new Criteria<int>("Strength", 50, Operator.GreaterThanOrEqual)], () => { }, "StrongRule")
        }.CreateMatcher();

        // Act
        var (strong, weak) = matcher.Partition(db.GetTable<Character>());

        // Assert
        Assert.Equal(2, strong.Count); // Marcus (75), Sara (50)
        Assert.Single(weak);           // Tim (30)
        Assert.Equal("Tim", weak[0].Name);
    }

    [Fact]
    public void EntityMatcher_GetBestMatch_ReturnsMostSpecificRule()
    {
        // Arrange - Create rules with different specificity
        var generalRule = new Rule(
            [new Criteria<bool>("IsAlive", true, Operator.Equal)],
            () => { },
            "AliveRule"
        );

        var specificRule = new Rule(
            [
                new Criteria<bool>("IsAlive", true, Operator.Equal),
                new Criteria<string>("Faction", "Empire", Operator.Equal),
                new Criteria<int>("Age", 30, Operator.GreaterThanOrEqual)
            ],
            () => { },
            "AliveEmpireVeteranRule"
        );

        var matcher = new List<Rule> { generalRule, specificRule }.CreateMatcher();
        var character = new Character("c1", "Marcus", 35, "Empire", 75, true);

        // Act
        var bestMatch = matcher.GetBestMatch(character);

        // Assert - Should return the more specific rule
        Assert.NotNull(bestMatch);
        Assert.Equal("AliveEmpireVeteranRule", bestMatch.Name);
    }

    #endregion

    #region Database Context Integration

    [Fact]
    public void AsFactSourceWithContext_CombinesEntityAndSingletons()
    {
        // Arrange - Database with game state singleton
        var db = new InvictaDatabase()
            .InsertSingleton("GameState", new GameState(2450, "Summer", true));

        var character = new Character("c1", "Marcus", 35, "Empire", 75, true);
        var factSource = character.AsFactSourceWithContext(db);

        // Act - Can query both entity properties and singleton properties
        var hasAge = factSource.TryGetFact<int>("Age", out var age);
        var hasGameState = factSource.TryGetFact<GameState>("GameState", out var gameState);

        // Assert
        Assert.True(hasAge);
        Assert.Equal(35, age);
        Assert.True(hasGameState);
        Assert.Equal(2450, gameState!.CurrentYear);
        Assert.True(gameState.IsAtWar);
    }

    [Fact]
    public void DatabaseFactSource_QueriesSingletons()
    {
        // Arrange
        var db = new InvictaDatabase()
            .InsertSingleton("GameState", new GameState(2450, "Summer", true))
            .InsertSingleton("PlayerFaction", "Empire");

        var factSource = db.AsFactSource();

        // Act
        var hasGameState = factSource.TryGetFact<GameState>("GameState", out var gameState);
        var hasFaction = factSource.TryGetFact<string>("PlayerFaction", out var faction);

        // Assert
        Assert.True(hasGameState);
        Assert.Equal(2450, gameState!.CurrentYear);
        Assert.True(hasFaction);
        Assert.Equal("Empire", faction);
    }

    [Fact]
    public void MatchAll_WithContext_ExecutesPayloadsWithDatabaseAccess()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .InsertSingleton("GameState", new GameState(2450, "Summer", true))
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Sara", 40, "Empire", 60, true));

        var executionCount = 0;

        // Rule that checks entity property AND could access game state via context
        var empireRule = new Rule(
            [new Criteria<string>("Faction", "Empire", Operator.Equal)],
            () => executionCount++,
            "EmpireRule"
        );

        // Act
        new List<Rule> { empireRule }.MatchAll(db.GetTable<Character>(), context: db);

        // Assert - Payload executed for each matching entity
        Assert.Equal(2, executionCount);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ComplexScenario_FindRecruitableSoldiers()
    {
        // Arrange - Game database with various characters
        var db = new InvictaDatabase()
            .RegisterTable<Character>()
            .InsertSingleton("GameState", new GameState(2450, "Summer", false))
            .Insert("c1", new Character("c1", "Marcus", 35, "Empire", 75, true))
            .Insert("c2", new Character("c2", "Tim", 12, "Neutral", 30, true))
            .Insert("c3", new Character("c3", "Sara", 28, "Neutral", 65, true))
            .Insert("c4", new Character("c4", "Dead Joe", 40, "Neutral", 80, false))
            .Insert("c5", new Character("c5", "Old Bob", 70, "Neutral", 40, true));

        // Recruitable: Alive, Neutral, Adult (18+), Not too old (<60), Strong enough (50+)
        var recruitableRule = new Rule(
            [
                new Criteria<bool>("IsAlive", true, Operator.Equal),
                new Criteria<string>("Faction", "Neutral", Operator.Equal),
                new Criteria<int>("Age", 18, Operator.GreaterThanOrEqual),
                new Criteria<int>("Age", 60, Operator.LessThan),
                new Criteria<int>("Strength", 50, Operator.GreaterThanOrEqual)
            ],
            () => { },
            "RecruitableRule"
        );

        // Act
        var recruitable = new List<Rule> { recruitableRule }
            .FindMatching(db.GetTable<Character>())
            .ToList();

        // Assert - Only Sara matches all criteria
        Assert.Single(recruitable);
        Assert.Equal("Sara", recruitable[0].Name);
    }

    [Fact]
    public void ComplexScenario_PrioritizedEventSystem()
    {
        // Arrange - Simulate an event system that picks the best dialogue
        var character = new Character("c1", "Marcus", 35, "Empire", 75, true);
        string? selectedDialogue = null;

        // Multiple rules with different specificity - most specific should win
        var rules = new List<Rule>
        {
            new(
                [new Criteria<bool>("IsAlive", true, Operator.Equal)],
                () => selectedDialogue = "Hello, traveler.",
                "GenericGreeting"
            ),
            new(
                [
                    new Criteria<bool>("IsAlive", true, Operator.Equal),
                    new Criteria<string>("Faction", "Empire", Operator.Equal)
                ],
                () => selectedDialogue = "Hail, fellow citizen of the Empire!",
                "EmpireGreeting"
            ),
            new(
                [
                    new Criteria<bool>("IsAlive", true, Operator.Equal),
                    new Criteria<string>("Faction", "Empire", Operator.Equal),
                    new Criteria<int>("Strength", 70, Operator.GreaterThanOrEqual)
                ],
                () => selectedDialogue = "A mighty warrior of the Empire! Well met!",
                "EmpireWarriorGreeting"
            )
        };

        // Act - Match should execute the most specific rule's payload
        rules.Match(character.AsFactSource());

        // Assert - Most specific matching rule wins
        Assert.Equal("A mighty warrior of the Empire! Well met!", selectedDialogue);
    }

    [Fact]
    public void ComplexScenario_FleetManagement()
    {
        // Arrange - Fleet database
        var db = new InvictaDatabase()
            .RegisterTable<Ship>()
            .Insert("s1", new Ship("s1", "HMS Victory", 800, "Battleship", 95))
            .Insert("s2", new Ship("s2", "Scout Alpha", 20, "Scout", 100))
            .Insert("s3", new Ship("s3", "Carrier One", 2000, "Carrier", 60))
            .Insert("s4", new Ship("s4", "Damaged Cruiser", 300, "Cruiser", 25));

        // Find ships that need repair
        var needsRepairRule = new Rule(
            [new Criteria<int>("HullIntegrity", 50, Operator.LessThan)],
            () => { },
            "NeedsRepairRule"
        );

        // Find combat-ready capital ships
        var combatReadyCapitalRule = new Rule(
            [
                new Criteria<int>("HullIntegrity", 80, Operator.GreaterThanOrEqual),
                new Criteria<int>("CrewSize", 500, Operator.GreaterThanOrEqual)
            ],
            () => { },
            "CombatReadyCapitalRule"
        );

        // Act
        var needsRepair = new List<Rule> { needsRepairRule }
            .FindMatching(db.GetTable<Ship>())
            .ToList();

        var combatReady = new List<Rule> { combatReadyCapitalRule }
            .FindMatching(db.GetTable<Ship>())
            .ToList();

        // Assert
        Assert.Single(needsRepair);
        Assert.Equal("Damaged Cruiser", needsRepair[0].Name);

        Assert.Single(combatReady);
        Assert.Equal("HMS Victory", combatReady[0].Name);
    }

    #endregion
}
