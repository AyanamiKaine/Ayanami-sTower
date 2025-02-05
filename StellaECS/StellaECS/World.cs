using Microsoft.Data.Sqlite;

namespace StellaECS;

/// <summary>
/// Represents the current database.
/// </summary>
public class World : IDisposable
{
    /// <summary>
    /// Gets the total number of entities in the world.
    /// </summary>
    public int Size
    {
        get
        {
            return GetTotalNumberOfEntities();
        }
    }
    private readonly SqliteConnection _connection;
    private bool _disposed;
    private readonly string _connectionString = string.Empty;
    private readonly List<ISystem> _systems = [];

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
        InitalizeDatabaseTables();
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
        InitalizeDatabaseTables();
    }

    /// <summary>
    /// Creates a new entity in the database.
    /// </summary>
    /// <returns>The ID of the newly created entity.</returns>
    public Entity CreateEntity()
    {
        const string statement =
        """
            INSERT INTO entities DEFAULT VALUES;
            SELECT last_insert_rowid();
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        var entityID = Convert.ToInt32(command.ExecuteScalar());
        return new Entity(entityID, this);
    }

    /// <summary>
    /// Gets the name of the specified entity.
    /// </summary>
    /// <param name="entity">The entity whose name is to be retrieved.</param>
    /// <returns>The name of the specified entity.</returns>
    public string GetEntityName(Entity entity)
    {
        const string statement =
        """
        SELECT name 
        FROM name_component 
        WHERE entity_id = @entityId
        """;

        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        command.Parameters.AddWithValue("@entityId", entity.ID);

        return command.ExecuteScalar()?.ToString() ?? "";
    }

    /// <summary>
    /// Creates a new entity in the database with a specified name.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <returns>The ID of the newly created entity.</returns>
    public Entity CreateEntity(string name)
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
            return new Entity(entityId, this);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Runs all systems exactly one time.
    /// </summary>
    public void Progress()
    {
        foreach (var system in _systems)
        {
            system.Run();
        }
    }

    /// <summary>
    /// Adds a system to the world.
    /// </summary>
    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
    }

    private void InitalizeDatabaseTables()
    {
        CreateEntities();
        CreateNameComponent();
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

    private void CreateEntities()
    {
        const string statement =
        """
            CREATE TABLE IF NOT EXISTS entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT
            );
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Creates the name component table in the database if it does not already exist.
    /// </summary>
    private void CreateNameComponent()
    {
        const string statement =
        """
            CREATE TABLE IF NOT EXISTS name_component (
                entity_id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                FOREIGN KEY (entity_id) REFERENCES entities(id)
            );
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        command.ExecuteNonQuery();
    }

    private int GetTotalNumberOfEntities()
    {
        const string statement = "SELECT COUNT(*) FROM entities";
        using var command = _connection.CreateCommand();
        command.CommandText = statement;
        return Convert.ToInt32(command.ExecuteScalar());
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