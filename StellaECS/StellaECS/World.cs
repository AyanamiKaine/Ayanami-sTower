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
    /// Creates a new entity in the database.
    /// </summary>
    /// <returns>The ID of the newly created entity.</returns>
    public int CreateEntity()
    {
        const string statement =
        """
            INSERT INTO entities DEFAULT VALUES;
            SELECT last_insert_rowid();
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>
    /// Creates a new entity in the database with a specified name.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <returns>The ID of the newly created entity.</returns>
    public int CreateEntity(string name)
    {
        using var transaction = _connection.BeginTransaction();
        try
        {
            // Create entity
            const string createEntityStatement =
            """
                INSERT INTO entities DEFAULT VALUES;
                SELECT last_insert_rowid();
                """;

            int entityId;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = createEntityStatement;
                command.Transaction = transaction;
                entityId = Convert.ToInt32(command.ExecuteScalar());
            }

            // Add name component
            const string addNameStatement =
            """
                INSERT INTO name_component (entity_id, name)
                VALUES (@entityId, @name);
                """;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = addNameStatement;
                command.Transaction = transaction;
                command.Parameters.AddWithValue("@entityId", entityId);
                command.Parameters.AddWithValue("@name", name);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return entityId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
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