namespace InvictaDB.Tests;

/// <summary>
/// Tests for the game world
/// </summary>
public class GameWorldUnitTest
{

    internal record GameDate(
        int Year = 1,
        int Month = 1,
        int Day = 1,
        int Hour = 0);

    /// <summary>
    /// Singleton Example test
    /// </summary>
    [Fact]
    public void SingletonExample()
    {
        var db = new InvictaDatabase();

        db = db.InsertSingleton("CurrentTick", 42);

        Assert.Equal(42, db.GetSingleton<int>("CurrentTick"));
    }

    /// <summary>
    /// Game Date Singleton Test
    /// </summary>
    [Fact]
    public void GameDateSingletonTest()
    {
        var db = new InvictaDatabase();

        var initialDate = new GameDate(Year: 1000, Month: 1, Day: 1, Hour: 0);
        db = db.InsertSingleton("GameDate", initialDate);

        var retrievedDate = db.GetSingleton<GameDate>("GameDate");

        Assert.Equal(initialDate, retrievedDate);
    }

    /// <summary>
    /// Immutable database test
    /// </summary>
    [Fact]
    public void ImmutableDatabaseTest()
    {
        var db1 = new InvictaDatabase();
        
        db1 = db1.InsertSingleton("Value", 10);

        var db2 = db1.InsertSingleton("Value", 20);

        Assert.Equal(10, db1.GetSingleton<int>("Value"));
        Assert.Equal(20, db2.GetSingleton<int>("Value"));
    }

}
