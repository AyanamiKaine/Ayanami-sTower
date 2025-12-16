using InvictaDB;

namespace StellaInvicta;

/// <summary>
/// Represents the game world containing the database and other global systems.
/// </summary>
public static class GameWorld
{
    /// <summary>
    /// Creates an example game database.
    /// </summary>
    /// <returns></returns>
    public static InvictaDatabase ExampleGame()
    {

        InvictaDatabase db = [];
        db.InsertSingleton("CurrentTick", 0);
        db.InsertSingleton("GameDate", new DateTime(1, 1, 1, 0, 0, 0));
        return db;
    }

}