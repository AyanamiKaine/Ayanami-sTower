using InvictaDB;

namespace StellaInvicta;

/// <summary>
/// Represents a snapshot with metadata for milestone saves.
/// </summary>
/// <param name="Database">The database state.</param>
/// <param name="GameDate">The in-game date when the snapshot was taken.</param>
/// <param name="Tick">The tick number when the snapshot was taken.</param>
/// <param name="CreatedAt">Real-world time when the snapshot was created.</param>
public record GameSnapshot(
    InvictaDatabase Database,
    DateTime GameDate,
    long Tick,
    DateTimeOffset CreatedAt);

/// <summary>
/// Represents the game state and logic.
/// </summary>
public class Game
{
    private readonly Dictionary<string, ISystem> _systems = [];
    private bool _initialized;

    // Undo/Redo history stacks
    private readonly Stack<InvictaDatabase> _undoHistory = new();
    private readonly Stack<InvictaDatabase> _redoHistory = new();
    private int _maxHistorySize = 100; // Configurable limit to prevent memory bloat

    // Milestone snapshots (keyed by "YYYY-MM" for monthly, "YYYY" for yearly)
    private readonly Dictionary<string, GameSnapshot> _monthlySnapshots = [];
    private readonly Dictionary<string, GameSnapshot> _yearlySnapshots = [];
    private int _maxMonthlySnapshots = 24;  // Keep 2 years of monthly snapshots by default
    private int _maxYearlySnapshots = 100;  // Keep 100 years of yearly snapshots by default

    /// <summary>
    /// Indicates whether the game has been initialized.
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Gets or sets the maximum number of states to keep in history.
    /// Set to 0 for unlimited (use with caution).
    /// </summary>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set => _maxHistorySize = value >= 0 ? value : 0;
    }

    /// <summary>
    /// Gets or sets the maximum number of monthly snapshots to keep.
    /// Set to 0 for unlimited.
    /// </summary>
    public int MaxMonthlySnapshots
    {
        get => _maxMonthlySnapshots;
        set => _maxMonthlySnapshots = value >= 0 ? value : 0;
    }

    /// <summary>
    /// Gets or sets the maximum number of yearly snapshots to keep.
    /// Set to 0 for unlimited.
    /// </summary>
    public int MaxYearlySnapshots
    {
        get => _maxYearlySnapshots;
        set => _maxYearlySnapshots = value >= 0 ? value : 0;
    }

    /// <summary>
    /// Gets whether an undo operation is available.
    /// </summary>
    public bool CanUndo => _undoHistory.Count > 0;

    /// <summary>
    /// Gets whether a redo operation is available.
    /// </summary>
    public bool CanRedo => _redoHistory.Count > 0;

    /// <summary>
    /// Gets the number of states in the undo history.
    /// </summary>
    public int UndoCount => _undoHistory.Count;

    /// <summary>
    /// Gets the number of states in the redo history.
    /// </summary>
    public int RedoCount => _redoHistory.Count;

    /// <summary>
    /// Gets the number of monthly snapshots stored.
    /// </summary>
    public int MonthlySnapshotCount => _monthlySnapshots.Count;

    /// <summary>
    /// Gets the number of yearly snapshots stored.
    /// </summary>
    public int YearlySnapshotCount => _yearlySnapshots.Count;

    #region Milestone Snapshots

    /// <summary>
    /// Creates a monthly snapshot key from a date.
    /// </summary>
    private static string GetMonthlyKey(DateTime date) => $"{date.Year:D4}-{date.Month:D2}";

    /// <summary>
    /// Creates a yearly snapshot key from a date.
    /// </summary>
    private static string GetYearlyKey(DateTime date) => $"{date.Year:D4}";

    /// <summary>
    /// Saves a monthly snapshot. Called automatically at month boundaries.
    /// </summary>
    /// <param name="db">The database state to snapshot.</param>
    private void SaveMonthlySnapshot(InvictaDatabase db)
    {
        var gameDate = db.GetSingleton<DateTime>();
        var tick = db.GetSingleton<long>("CurrentTick");
        var key = GetMonthlyKey(gameDate);

        // Only save if we don't already have this month
        if (!_monthlySnapshots.ContainsKey(key))
        {
            _monthlySnapshots[key] = new GameSnapshot(db, gameDate, tick, DateTimeOffset.UtcNow);
            TrimMonthlySnapshots();
        }
    }

    /// <summary>
    /// Saves a yearly snapshot. Called automatically at year boundaries.
    /// </summary>
    /// <param name="db">The database state to snapshot.</param>
    private void SaveYearlySnapshot(InvictaDatabase db)
    {
        var gameDate = db.GetSingleton<DateTime>();
        var tick = db.GetSingleton<long>("CurrentTick");
        var key = GetYearlyKey(gameDate);

        // Only save if we don't already have this year
        if (!_yearlySnapshots.ContainsKey(key))
        {
            _yearlySnapshots[key] = new GameSnapshot(db, gameDate, tick, DateTimeOffset.UtcNow);
            TrimYearlySnapshots();
        }
    }

    /// <summary>
    /// Trims monthly snapshots to the maximum allowed.
    /// </summary>
    private void TrimMonthlySnapshots()
    {
        if (_maxMonthlySnapshots <= 0 || _monthlySnapshots.Count <= _maxMonthlySnapshots)
            return;

        // Remove oldest snapshots
        var keysToRemove = _monthlySnapshots
            .OrderBy(kvp => kvp.Key)
            .Take(_monthlySnapshots.Count - _maxMonthlySnapshots)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _monthlySnapshots.Remove(key);
        }
    }

    /// <summary>
    /// Trims yearly snapshots to the maximum allowed.
    /// </summary>
    private void TrimYearlySnapshots()
    {
        if (_maxYearlySnapshots <= 0 || _yearlySnapshots.Count <= _maxYearlySnapshots)
            return;

        // Remove oldest snapshots
        var keysToRemove = _yearlySnapshots
            .OrderBy(kvp => kvp.Key)
            .Take(_yearlySnapshots.Count - _maxYearlySnapshots)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _yearlySnapshots.Remove(key);
        }
    }

    /// <summary>
    /// Gets all available monthly snapshots, ordered by date.
    /// </summary>
    /// <returns>List of monthly snapshots with their keys.</returns>
    public IReadOnlyList<(string Key, GameSnapshot Snapshot)> GetMonthlySnapshots()
    {
        return _monthlySnapshots
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    /// <summary>
    /// Gets all available yearly snapshots, ordered by date.
    /// </summary>
    /// <returns>List of yearly snapshots with their keys.</returns>
    public IReadOnlyList<(string Key, GameSnapshot Snapshot)> GetYearlySnapshots()
    {
        return _yearlySnapshots
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    /// <summary>
    /// Gets a monthly snapshot by key (format: "YYYY-MM").
    /// </summary>
    /// <param name="key">The monthly key.</param>
    /// <returns>The snapshot, or null if not found.</returns>
    public GameSnapshot? GetMonthlySnapshot(string key)
    {
        return _monthlySnapshots.TryGetValue(key, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Gets a monthly snapshot by year and month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The snapshot, or null if not found.</returns>
    public GameSnapshot? GetMonthlySnapshot(int year, int month)
    {
        return GetMonthlySnapshot($"{year:D4}-{month:D2}");
    }

    /// <summary>
    /// Gets a yearly snapshot by key (format: "YYYY").
    /// </summary>
    /// <param name="key">The yearly key.</param>
    /// <returns>The snapshot, or null if not found.</returns>
    public GameSnapshot? GetYearlySnapshot(string key)
    {
        return _yearlySnapshots.TryGetValue(key, out var snapshot) ? snapshot : null;
    }

    /// <summary>
    /// Gets a yearly snapshot by year.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <returns>The snapshot, or null if not found.</returns>
    public GameSnapshot? GetYearlySnapshot(int year)
    {
        return GetYearlySnapshot($"{year:D4}");
    }

    /// <summary>
    /// Jumps to a monthly snapshot, saving current state to undo history.
    /// </summary>
    /// <param name="currentDb">The current database state.</param>
    /// <param name="year">The year to jump to.</param>
    /// <param name="month">The month to jump to.</param>
    /// <returns>The snapshot database, or current if not found.</returns>
    public InvictaDatabase JumpToMonth(InvictaDatabase currentDb, int year, int month)
    {
        var snapshot = GetMonthlySnapshot(year, month);
        if (snapshot == null)
            return currentDb;

        PushToHistory(currentDb);
        return snapshot.Database;
    }

    /// <summary>
    /// Jumps to a yearly snapshot, saving current state to undo history.
    /// </summary>
    /// <param name="currentDb">The current database state.</param>
    /// <param name="year">The year to jump to.</param>
    /// <returns>The snapshot database, or current if not found.</returns>
    public InvictaDatabase JumpToYear(InvictaDatabase currentDb, int year)
    {
        var snapshot = GetYearlySnapshot(year);
        if (snapshot == null)
            return currentDb;

        PushToHistory(currentDb);
        return snapshot.Database;
    }

    /// <summary>
    /// Clears all milestone snapshots.
    /// </summary>
    public void ClearSnapshots()
    {
        _monthlySnapshots.Clear();
        _yearlySnapshots.Clear();
    }

    /// <summary>
    /// Manually creates a monthly snapshot for the current state.
    /// </summary>
    /// <param name="db">The database to snapshot.</param>
    public void CreateMonthlySnapshot(InvictaDatabase db)
    {
        SaveMonthlySnapshot(db);
    }

    /// <summary>
    /// Manually creates a yearly snapshot for the current state.
    /// </summary>
    /// <param name="db">The database to snapshot.</param>
    public void CreateYearlySnapshot(InvictaDatabase db)
    {
        SaveYearlySnapshot(db);
    }

    #endregion

    #region Undo/Redo Operations

    /// <summary>
    /// Saves the current state to history before making changes.
    /// Call this before any operation that should be undoable.
    /// </summary>
    /// <param name="db">The current database state to save.</param>
    private void PushToHistory(InvictaDatabase db)
    {
        _undoHistory.Push(db);

        // Clear redo history when a new action is taken
        _redoHistory.Clear();

        // Trim history if it exceeds max size
        if (_maxHistorySize > 0)
        {
            TrimHistory();
        }
    }

    /// <summary>
    /// Trims the undo history to the maximum size.
    /// </summary>
    private void TrimHistory()
    {
        if (_maxHistorySize <= 0 || _undoHistory.Count <= _maxHistorySize)
            return;

        // Convert to array, keep only the most recent entries, rebuild stack
        var items = _undoHistory.ToArray();
        _undoHistory.Clear();

        // Stack is LIFO, so ToArray gives newest first
        // We want to keep the newest, so take from the start
        for (int i = Math.Min(items.Length - 1, _maxHistorySize - 1); i >= 0; i--)
        {
            _undoHistory.Push(items[i]);
        }
    }

    /// <summary>
    /// Undoes the last action, returning the previous database state.
    /// </summary>
    /// <param name="currentDb">The current database state (will be pushed to redo stack).</param>
    /// <returns>The previous database state, or the current state if no undo available.</returns>
    public InvictaDatabase Undo(InvictaDatabase currentDb)
    {
        if (_undoHistory.Count == 0)
            return currentDb;

        // Save current state to redo stack
        _redoHistory.Push(currentDb);

        // Return the previous state
        return _undoHistory.Pop();
    }

    /// <summary>
    /// Redoes a previously undone action, returning the next database state.
    /// </summary>
    /// <param name="currentDb">The current database state (will be pushed to undo stack).</param>
    /// <returns>The redone database state, or the current state if no redo available.</returns>
    public InvictaDatabase Redo(InvictaDatabase currentDb)
    {
        if (_redoHistory.Count == 0)
            return currentDb;

        // Save current state to undo stack
        _undoHistory.Push(currentDb);

        // Return the redone state
        return _redoHistory.Pop();
    }

    /// <summary>
    /// Clears all undo/redo history.
    /// Useful when loading a new game or to free memory.
    /// </summary>
    public void ClearHistory()
    {
        _undoHistory.Clear();
        _redoHistory.Clear();
    }

    /// <summary>
    /// Gets a snapshot of the history for debugging or UI display.
    /// Returns (undoCount, redoCount, oldestUndoTick, newestUndoTick).
    /// </summary>
    public (int UndoCount, int RedoCount) GetHistoryInfo()
    {
        return (_undoHistory.Count, _redoHistory.Count);
    }

    #endregion
    /// <summary>
    /// Initializes the game with the provided database.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public InvictaDatabase Init(InvictaDatabase db)
    {
        if (_initialized)
            return db;

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
    /// Saves a checkpoint to history before simulating.
    /// </summary>
    /// <param name="db">The current database state.</param>
    /// <param name="saveToHistory">Whether to save a checkpoint before simulating (default: true).</param>
    public InvictaDatabase SimulateDay(InvictaDatabase db, bool saveToHistory = true)
    {
        if (saveToHistory)
            PushToHistory(db);

        var database = db;
        for (int hour = 0; hour < 24; hour++)
        {
            database = SimulateHour(database);
        }
        return database;
    }

    /// <summary>
    /// Simulates a month in the game using the actual number of days in the current month.
    /// Saves a checkpoint to history before simulating.
    /// Automatically saves a monthly snapshot at the start of each month.
    /// </summary>
    /// <param name="db">The current database state.</param>
    /// <param name="saveToHistory">Whether to save a checkpoint before simulating (default: true).</param>
    /// <param name="saveSnapshots">Whether to save milestone snapshots (default: true).</param>
    public InvictaDatabase SimulateMonth(InvictaDatabase db, bool saveToHistory = true, bool saveSnapshots = true)
    {
        if (saveToHistory)
            PushToHistory(db);

        // Save monthly snapshot at the start of the month
        if (saveSnapshots)
            SaveMonthlySnapshot(db);

        var database = db;
        var currentDate = db.GetSingleton<DateTime>();
        int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
        for (int day = 0; day < daysInMonth; day++)
        {
            database = SimulateDay(database, saveToHistory: false); // Don't double-save
        }
        return database;
    }

    /// <summary>
    /// Simulates a year in the game (12 months with correct days per month, including leap years).
    /// Saves a checkpoint to history before simulating.
    /// Automatically saves yearly and monthly snapshots.
    /// </summary>
    /// <param name="db">The current database state.</param>
    /// <param name="saveToHistory">Whether to save a checkpoint before simulating (default: true).</param>
    /// <param name="saveSnapshots">Whether to save milestone snapshots (default: true).</param>
    public InvictaDatabase SimulateYear(InvictaDatabase db, bool saveToHistory = true, bool saveSnapshots = true)
    {
        if (saveToHistory)
            PushToHistory(db);

        // Save yearly snapshot at the start of the year
        if (saveSnapshots)
            SaveYearlySnapshot(db);

        var database = db;
        for (int month = 0; month < 12; month++)
        {
            // Save monthly snapshots but don't double-save to history
            database = SimulateMonth(database, saveToHistory: false, saveSnapshots: saveSnapshots);
        }
        return database;
    }

    #region Time Travel (Advanced Undo)

    /// <summary>
    /// Peeks at a previous state without modifying the current state or history.
    /// Useful for showing "what if" scenarios or history previews.
    /// </summary>
    /// <param name="stepsBack">How many steps back to look (1 = most recent undo state).</param>
    /// <returns>The historical state, or null if not available.</returns>
    public InvictaDatabase? PeekHistory(int stepsBack = 1)
    {
        if (stepsBack <= 0 || stepsBack > _undoHistory.Count)
            return null;

        var items = _undoHistory.ToArray();
        return items[stepsBack - 1]; // ToArray gives newest first
    }

    /// <summary>
    /// Jumps directly to a specific point in history, discarding intermediate states.
    /// All states between current and target become redo-able.
    /// </summary>
    /// <param name="currentDb">The current database state.</param>
    /// <param name="stepsBack">How many steps back to jump.</param>
    /// <returns>The historical state, or current if invalid.</returns>
    public InvictaDatabase JumpToHistory(InvictaDatabase currentDb, int stepsBack)
    {
        if (stepsBack <= 0 || stepsBack > _undoHistory.Count)
            return currentDb;

        // Push current state to redo
        _redoHistory.Push(currentDb);

        // Pop states and push to redo until we reach the target
        InvictaDatabase target = currentDb;
        for (int i = 0; i < stepsBack; i++)
        {
            target = _undoHistory.Pop();
            if (i < stepsBack - 1) // Don't push the target itself to redo
            {
                _redoHistory.Push(target);
            }
        }

        return target;
    }

    /// <summary>
    /// Creates a "branch" in time - saves current state and returns a copy of a historical state.
    /// The original timeline is preserved in history.
    /// Useful for "what if" experimentation.
    /// </summary>
    /// <param name="currentDb">The current database state.</param>
    /// <param name="stepsBack">How many steps back to branch from.</param>
    /// <returns>A tuple of (branchedState, originalTimelinePreserved). Returns (current, false) if invalid.</returns>
    public (InvictaDatabase BranchedState, bool Success) BranchFromHistory(InvictaDatabase currentDb, int stepsBack)
    {
        var historicalState = PeekHistory(stepsBack);
        if (historicalState == null)
            return (currentDb, false);

        // Save current state so player can return to it
        PushToHistory(currentDb);

        // Return the historical state - player can now diverge
        return (historicalState, true);
    }

    #endregion
}
