using InvictaDB;

namespace StellaInvicta;

/// <summary>
/// Represents the game state and logic.
/// </summary>
public class Game
{
    private readonly Dictionary<string, ISystem> _systems = [];
    private bool _initialized;
    /// <summary>
    /// Indicates whether the game has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized;
    /// <summary>
    /// Initializes the game with the provided database.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public InvictaDatabase Init(InvictaDatabase db)
    {
        db = InitializeSystems(db);
        _initialized = true;
        return db.InsertSingleton("CurrentTick", 0L);
    }

    /// <summary>
    /// Adds a system to the game.
    /// </summary>
    /// <param name="systemName">The name of the system.</param>
    /// <param name="system">The system instance.</param>
    public void AddSystem(string systemName, ISystem system)
    {
        _systems[systemName] = system;
    }
    /// <summary>
    /// Enables a system by name.
    /// </summary>
    /// <param name="systemName"></param>
    public void EnableSystem(string systemName)
    {
        if (_systems.TryGetValue(systemName, out var system))
        {
            system.Enabled = true;
        }
    }
    /// <summary>
    /// Disables a system by name.
    /// </summary>
    /// <param name="systemName"></param>
    public void DisableSystem(string systemName)
    {
        if (_systems.TryGetValue(systemName, out var system))
        {
            system.Enabled = false;
        }
    }
    /// <summary>
    /// Initializes all enabled systems. That are not yet initialized.
    /// You can safely call this multiple times; systems will only initialize once.
    /// </summary>
    /// <param name="db"></param>
    /// <returns>The updated database.</returns>
    public InvictaDatabase InitializeSystems(InvictaDatabase db)
    {
        foreach (var system in _systems.Values)
        {
            if (system.Enabled && !system.IsInitialized)
            {
                db = system.Initialize(db);
            }
        }

        _initialized = true;
        return db;
    }

    /// <summary>
    /// Runs a single tick of the game logic.
    /// </summary>
    public InvictaDatabase RunTick(InvictaDatabase db)
    {
        foreach (var system in _systems.Values)
        {
            if (system.Enabled)
            {
                db = system.Run(db);
            }
        }
        long currentTick = db.GetSingleton<long>("CurrentTick");
        return db.InsertSingleton("CurrentTick", currentTick + 1);
    }
    /// <summary>
    /// Simulates an hour in the game.
    /// </summary>
    public InvictaDatabase SimulateHour(InvictaDatabase db)
    {
        return RunTick(db);
    }
    /// <summary>
    /// Simulates a day in the game (24 hours).
    /// </summary>
    public InvictaDatabase SimulateDay(InvictaDatabase db)
    {
        var database = db;
        for (int hour = 0; hour < 24; hour++)
        {
            database = SimulateHour(database);
        }
        return database;
    }
    /// <summary>
    /// Simulates a month in the game using the actual number of days in the current month.
    /// </summary>
    public InvictaDatabase SimulateMonth(InvictaDatabase db)
    {
        var database = db;
        var currentDate = db.GetSingleton<DateTime>();
        int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
        for (int day = 0; day < daysInMonth; day++)
        {
            database = SimulateDay(database);
        }
        return database;
    }
    /// <summary>
    /// Simulates a year in the game (12 months with correct days per month, including leap years).
    /// </summary>
    public InvictaDatabase SimulateYear(InvictaDatabase db)
    {
        var database = db;
        for (int month = 0; month < 12; month++)
        {
            database = SimulateMonth(database);
        }
        return database;
    }
}
