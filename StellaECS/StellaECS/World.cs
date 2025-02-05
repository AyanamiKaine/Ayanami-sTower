using Microsoft.Data.Sqlite;

namespace StellaECS;

/// <summary>
/// Represents the current database.
/// </summary>
public class World : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;
    private readonly string _connectionString = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <exception cref="InvalidOperationException">Thrown when a database file with the same name already exists.</exception>
    public World(string name)
    {
        var dbPath = $"{name}.db";
        if (File.Exists(dbPath))
        {
            throw new InvalidOperationException($"A database file '{dbPath}' already exists.");
        }

        _connectionString = $"Data Source={dbPath}.db";
        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        DatabaseOptimizations();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class with an in-memory database.
    /// Useful for temporary data that does not need to persit.
    /// </summary>
    public World()
    {
        const string connectionString = "Data Source=:memory:";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        DatabaseOptimizations();
    }

    /// <summary>
    /// Optimizes the database for local concurrent performance.
    /// </summary>
    private void DatabaseOptimizations()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $@"
                PRAGMA journal_mode=WAL;
                PRAGMA page_size = {32768};
                PRAGMA synchronous = normal;
                PRAGMA temp_store = memory;
                PRAGMA mmap_size = 30000000000;
                ";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the World and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the World.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizes an instance of the World class.
    /// </summary>
    ~World()
    {
        Dispose(false);
    }
}