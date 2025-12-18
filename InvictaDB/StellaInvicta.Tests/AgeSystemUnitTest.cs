using InvictaDB;
using StellaInvicta;
using StellaInvicta.Data;
using StellaInvicta.System.Age;
using StellaInvicta.System.Date;
using static InvictaDB.Messaging.GameMessages;
using static StellaInvicta.System.Age.AgeSystem;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests for AgeSystem functionality.
/// </summary>
public class AgeSystemUnitTest
{
    private static Character CreateCharacter(string name, int age, DateTime birthDate) =>
        new(name, age, 10, 10, 10, 10, birthDate);

    private static (Game game, InvictaDatabase db) SetupGameWithSystems()
    {
        var db = new InvictaDatabase();
        var game = new Game();
        var dateSystem = new DateSystem();
        var ageSystem = new AgeSystem();

        game.AddSystem(dateSystem.Name, dateSystem);
        game.AddSystem(ageSystem.Name, ageSystem);

        db = game.Init(db);
        return (game, db);
    }

    /// <summary>
    /// AgeSystem should initialize and register Character table.
    /// </summary>
    [Fact]
    public void AgeSystem_Initialize_RegistersCharacterTable()
    {
        var db = new InvictaDatabase();
        var ageSystem = new AgeSystem();

        db = ageSystem.Initialize(db);

        Assert.True(ageSystem.IsInitialized);
        Assert.True(db.TableExists<Character>());
    }

    /// <summary>
    /// AgeSystem should not fail if Character table already exists.
    /// </summary>
    [Fact]
    public void AgeSystem_Initialize_DoesNotFailIfTableExists()
    {
        var db = new InvictaDatabase().RegisterTable<Character>();
        var ageSystem = new AgeSystem();

        db = ageSystem.Initialize(db);

        Assert.True(ageSystem.IsInitialized);
        Assert.True(db.TableExists<Character>());
    }

    /// <summary>
    /// Character should age when their birthday arrives.
    /// </summary>
    [Fact]
    public void AgeSystem_CharacterAgesOnBirthday()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 2
        db = db.InsertSingleton(new DateTime(2, 1, 1, 0, 0, 0));

        // Create a character born on Jan 2, Year 1, currently Age 0 (almost 1)
        var character = CreateCharacter("TestChar", 0, new DateTime(1, 1, 2));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate one day - should trigger birthday on Jan 2
        db = game.SimulateDay(db);

        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(1, updatedCharacter.Age); // Character should now be 1 year old
    }

    /// <summary>
    /// Character should not age before their birthday.
    /// </summary>
    [Fact]
    public void AgeSystem_CharacterDoesNotAgeBeforeBirthday()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 2
        db = db.InsertSingleton(new DateTime(2, 1, 1, 0, 0, 0));

        // Create a character born on Jan 15, Year 1, Age 0 (almost 1)
        var character = CreateCharacter("TestChar", 0, new DateTime(1, 1, 15));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate 10 days (Jan 1-10) - birthday hasn't arrived yet
        for (int i = 0; i < 10; i++)
        {
            db = game.SimulateDay(db);
        }

        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(0, updatedCharacter.Age); // Character should still be 0
    }

    /// <summary>
    /// Character should age correctly over multiple years.
    /// </summary>
    [Fact]
    public void AgeSystem_CharacterAgesOverMultipleYears()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 2
        db = db.InsertSingleton(new DateTime(2, 1, 1, 0, 0, 0));

        // Create a character born on March 15, Year 1, Age 0
        // (Using a date that's not Jan 1 to properly test year boundary)
        var character = CreateCharacter("TestChar", 0, new DateTime(1, 3, 15));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate 3 years - character should age on each March 15
        for (int year = 0; year < 3; year++)
        {
            db = game.SimulateYear(db);
        }

        // Born Year 1, started Year 2, now Year 5 (Jan 1)
        // Had birthdays on March 15 of Years 2, 3, 4 = 3 birthdays
        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(3, updatedCharacter.Age);
    }

    /// <summary>
    /// Multiple characters should age independently based on their birth dates.
    /// </summary>
    [Fact]
    public void AgeSystem_MultipleCharactersAgeIndependently()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 22 (so characters are the ages we specify)
        db = db.InsertSingleton(new DateTime(22, 1, 1, 0, 0, 0));

        // Character A: born Jan 5, Year 2, currently Age 19 (turns 20 on Jan 5)
        var charA = CreateCharacter("Alice", 19, new DateTime(2, 1, 5));
        // Character B: born Jan 10, Year 2, currently Age 19 (turns 20 on Jan 10)
        var charB = CreateCharacter("Bob", 19, new DateTime(2, 1, 10));
        // Character C: born Feb 1, Year 2, currently Age 19 (turns 20 on Feb 1)
        var charC = CreateCharacter("Charlie", 19, new DateTime(2, 2, 1));

        db = db.Insert("alice", charA);
        db = db.Insert("bob", charB);
        db = db.Insert("charlie", charC);
        db = db.ClearMessages();

        // Simulate 7 days (Jan 1-7) - only Alice should have had birthday
        for (int i = 0; i < 7; i++)
        {
            db = game.SimulateDay(db);
        }

        Assert.Equal(20, db.GetEntry<Character>("alice").Age);   // Had birthday on Jan 5
        Assert.Equal(19, db.GetEntry<Character>("bob").Age);     // Birthday is Jan 10
        Assert.Equal(19, db.GetEntry<Character>("charlie").Age); // Birthday is Feb 1
    }

    /// <summary>
    /// AgeSystem should send CharacterBirthday message when character ages.
    /// </summary>
    [Fact]
    public void AgeSystem_SendsBirthdayMessage()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 27
        db = db.InsertSingleton(new DateTime(27, 1, 1, 0, 0, 0));

        // Character born Jan 2, Year 2, currently Age 24 (turns 25 on Jan 2)
        var character = CreateCharacter("TestChar", 24, new DateTime(2, 1, 2));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate one day - birthday on Jan 2
        db = game.SimulateDay(db);

        var birthdayMessages = db.Messages.GetMessages<CharacterBirthday>().ToList();
        Assert.Single(birthdayMessages);

        var payload = birthdayMessages[0].GetPayload<CharacterBirthday>();
        Assert.NotNull(payload);
        Assert.Equal("testchar", payload.CharacterId);
        Assert.Equal("TestChar", payload.CharacterName);
        Assert.Equal(25, payload.NewAge);
    }

    /// <summary>
    /// AgeSystem should handle leap year birthdays correctly.
    /// Characters born on Feb 29 should age on Feb 29 in leap years.
    /// </summary>
    [Fact]
    public void AgeSystem_HandlesLeapYearBirthday()
    {
        var (game, db) = SetupGameWithSystems();

        // Start on Year 8, which is also a leap year (Year 4 was birth year)
        db = db.InsertSingleton(new DateTime(8, 1, 1, 0, 0, 0));

        // Create a character born on Feb 29, Year 4 (leap year), currently Age 3
        var character = CreateCharacter("LeapBaby", 3, new DateTime(4, 2, 29));
        db = db.Insert("leapbaby", character);
        db = db.ClearMessages();

        // Simulate until March (60 days in a leap year: Jan=31, Feb=29)
        for (int i = 0; i < 60; i++)
        {
            db = game.SimulateDay(db);
        }

        var currentDate = db.GetSingleton<DateTime>();
        Assert.Equal(3, currentDate.Month); // Should be in March

        var updatedCharacter = db.GetEntry<Character>("leapbaby");
        Assert.Equal(4, updatedCharacter.Age); // Should have aged on Feb 29
    }

    /// <summary>
    /// AgeSystem should not process if there are no NewDay messages.
    /// </summary>
    [Fact]
    public void AgeSystem_NoProcessingWithoutNewDayMessage()
    {
        var db = new InvictaDatabase();
        var ageSystem = new AgeSystem();

        db = ageSystem.Initialize(db);

        var character = CreateCharacter("TestChar", 25, new DateTime(1, 1, 1));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Run the age system directly without any NewDay message
        db = ageSystem.Run(db);

        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(25, updatedCharacter.Age); // Age unchanged
    }

    /// <summary>
    /// AgeSystem should handle empty character table gracefully.
    /// </summary>
    [Fact]
    public void AgeSystem_HandlesEmptyCharacterTable()
    {
        var (game, db) = SetupGameWithSystems();
        db = db.ClearMessages();

        // Simulate a day with no characters - should not throw
        var exception = Record.Exception(() => db = game.SimulateDay(db));
        Assert.Null(exception);
    }

    /// <summary>
    /// Character's age should be calculated correctly when birthday hasn't occurred yet this year.
    /// </summary>
    [Fact]
    public void AgeSystem_CorrectAgeCalculationBeforeBirthdayThisYear()
    {
        var (game, db) = SetupGameWithSystems();

        // Start game on Jan 1, Year 3
        db = db.InsertSingleton(new DateTime(3, 1, 1, 0, 0, 0));

        // Character born Dec 15, Year 1, currently Age 1 (birthday was in Year 2)
        var character = CreateCharacter("TestChar", 1, new DateTime(1, 12, 15));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate 30 days (still in January/February) - no birthday yet this year
        for (int i = 0; i < 30; i++)
        {
            db = game.SimulateDay(db);
        }

        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(1, updatedCharacter.Age); // Still 1, birthday is in December
    }

    /// <summary>
    /// Character should age correctly when simulating across year boundary.
    /// </summary>
    [Fact]
    public void AgeSystem_AgesCorrectlyAcrossYearBoundary()
    {
        var (game, db) = SetupGameWithSystems();

        // Start on Jan 1, Year 2
        db = db.InsertSingleton(new DateTime(2, 1, 1, 0, 0, 0));

        // Character born on Dec 25, Year 1, currently Age 0 (turns 1 on Dec 25, Year 2)
        var character = CreateCharacter("TestChar", 0, new DateTime(1, 12, 25));
        db = db.Insert("testchar", character);
        db = db.ClearMessages();

        // Simulate a full year - should have birthday on Dec 25
        db = game.SimulateYear(db);

        var updatedCharacter = db.GetEntry<Character>("testchar");
        Assert.Equal(1, updatedCharacter.Age);

        // Verify we're now in Year 3
        var currentDate = db.GetSingleton<DateTime>();
        Assert.Equal(3, currentDate.Year);
    }

    /// <summary>
    /// Multiple birthdays on the same day should all be processed.
    /// </summary>
    [Fact]
    public void AgeSystem_ProcessesMultipleBirthdaysOnSameDay()
    {
        var (game, db) = SetupGameWithSystems();

        // Start on Jan 1, Year 22
        db = db.InsertSingleton(new DateTime(22, 1, 1, 0, 0, 0));

        // Three characters all born on Jan 2 in different years
        var char1 = CreateCharacter("Alice", 19, new DateTime(2, 1, 2));    // Turns 20
        var char2 = CreateCharacter("Bob", 29, new DateTime(2, 1, 2));      // Turns 30 (different birth year implied by age)
        var char3 = CreateCharacter("Charlie", 39, new DateTime(2, 1, 2));  // Turns 40

        db = db.Insert("alice", char1);
        db = db.Insert("bob", char2);
        db = db.Insert("charlie", char3);
        db = db.ClearMessages();

        // Simulate one day - all three should have birthdays
        db = game.SimulateDay(db);

        Assert.Equal(20, db.GetEntry<Character>("alice").Age);
        Assert.Equal(20, db.GetEntry<Character>("bob").Age);     // All born same year, so same new age
        Assert.Equal(20, db.GetEntry<Character>("charlie").Age); // All born same year, so same new age

        // Should have 3 birthday messages
        var birthdayMessages = db.Messages.GetMessages<CharacterBirthday>().ToList();
        Assert.Equal(3, birthdayMessages.Count);
    }
}
