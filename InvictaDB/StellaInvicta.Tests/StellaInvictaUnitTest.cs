using InvictaDB;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests for Stella Invicta
/// </summary>
public class StellaInvictaUnitTest
{
    /// <summary>
    /// Game Creation Test
    /// </summary>
    [Fact]
    public void GameCreation()
    {

    }

    /// <summary>
    /// Date System Test
    /// </summary>
    [Fact]
    public void DateSystemTest()
    {
        var db = new InvictaDatabase();
        var dateSystem = new System.Date.DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);

        db = game.Init(db);

        var initialDate = db.GetSingleton<DateTime>();

        db = game.SimulateYear(db);
        var updatedDate = db.GetSingleton<DateTime>();
        Assert.Equal(initialDate.AddYears(1), updatedDate);
    }
}
