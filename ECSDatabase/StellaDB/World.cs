﻿using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace AyanamisTower.StellaDB;

/// <summary>
/// Represents the game world
/// </summary>
public class World : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteCompiler _compiler;
    private readonly QueryFactory _queryFactory; // Added for SqlKata
    private bool _disposed = false; // To detect redundant calls

    /// <summary>
    /// Name of the game
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Given the entity name get the entity id.
    /// </summary>
    public Dictionary<string, long> Entities = [];
    private readonly string _databaseFilePath = string.Empty;

    // Connection string for an in-memory SQLite database
    private readonly string _connectionString = string.Empty;

    /// <summary>
    /// When the world lives only in memory and not on disk it returns true.
    /// </summary>
    public bool InMemory { get; } = false;

    /// <summary>
    /// A cache mapping entity names to their IDs for quick lookups.
    /// </summary>
    private readonly Dictionary<string, long> _entityNameCache = [];

    /// <summary>
    /// Creates a database using a name
    /// </summary>
    /// <param name="name">Name of the current game</param>
    /// <param name="inMemory">Determines if the game should only be in memory</param>
    /// <param name="enabledOptimizations">Enables various pragma optimizations for sqlite3</param>
    public World(string name, bool inMemory = false, bool enabledOptimizations = true)
    {
        Name = name;
        InMemory = inMemory;
        _compiler = new SqliteCompiler();

        if (inMemory)
        {
            _connectionString = "Data Source=:memory:";
            _databaseFilePath = ":memory:"; // Indicate it's in-memory
            Console.WriteLine($"Initializing in-memory database for game: {Name}");
        }
        else
        {
            _databaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Name}.db");
            _connectionString = $"Data Source={_databaseFilePath}";
        }

        _connection = new SqliteConnection(_connectionString);
        _connection.Open();

        // Initialize QueryFactory
        _queryFactory = new QueryFactory(_connection, _compiler);

        if (enabledOptimizations)
            EnableOptimizations();

        try
        {
            CreateTables("Tables");
            LoadDataFromDirectory("Data");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // simply generates Plain Old Class Object based on the tables.
        // this is used to store tables directly in memory.
        // For directly accessing them, where performance is needed
        // Todo: Implement it so the generator runs before.
        //var generatedClasses = _connection.GenerateAllTables();
        //Console.WriteLine(generatedClasses);
    }

    /// <summary>
    /// Starts a new SqlKata query targeting a specific table.
    /// This allows fluent query building and execution directly by the user of the World object.
    /// Example: world.Query("Players").Where("Level", ">", 10).Get>Player>();
    /// </summary>
    /// <param name="tableName">The name of the table to query.</param>
    /// <returns>A SqlKata Query object associated with this world's database connection.</returns>
    public Query Query(string tableName)
    {
        return _queryFactory.Query(tableName);
    }

    /// <summary>
    /// Starts a new SqlKata query without an initial table.
    /// Useful for more complex queries, such as those starting with a Common Table Expression (CTE)
    /// or when the FROM clause is built dynamically.
    /// Example: world.Query().With("RegionalSales", ...).From("RegionalSales").Get();
    /// </summary>
    /// <returns>A SqlKata Query object associated with this world's database connection.</returns>
    public Query Query()
    {
        // This returns a new Query instance that is associated with the QueryFactory.
        // The user can then chain methods like .From(), .With(), etc.
        return _queryFactory.Query();
    }

    /// <summary>
    /// Creates a new entity and its associated name component. If the entity name already exists, it retrieves the existing one.
    /// </summary>
    public Entity Entity(string? name = null)
    {
        if (!string.IsNullOrEmpty(name) && _entityNameCache.TryGetValue(name, out long existingId))
        {
            return new Entity { Id = existingId, World = this };
        }

        using var transaction = _connection.BeginTransaction();
        try
        {
            // The fix is here: instead of new {}, we provide a non-empty object
            // by setting a nullable column to null. This satisfies SqlKata's requirement.
            long entityId = Query()
                .From("Entity")
                .InsertGetId<long>(new { ParentId = (long?)null });

            if (!string.IsNullOrEmpty(name))
            {
                Query("Name").Insert(new { EntityId = entityId, Value = name });
                _entityNameCache[name] = entityId; // Add to cache
            }

            transaction.Commit();

            return new Entity { Id = entityId, World = this };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Retrieves an entity by its name, using the cache for performance.
    /// </summary>
    public Entity? GetEntityByName(string name)
    {
        if (_entityNameCache.TryGetValue(name, out long id))
        {
            return new Entity { Id = id, World = this };
        }

        // If not in cache, query the database
        var result = Query("Name").Where("Value", name).FirstOrDefault();
        if (result != null)
        {
            long entityId = result.EntityId;
            _entityNameCache[name] = entityId; // Cache the result
            return new Entity { Id = entityId, World = this };
        }

        return null;
    }

    /// <summary>
    /// Iterates over all defined tables in the specified subfolder to create a database.
    /// The order is determined by 'SchemaOrder.json' within that subfolder.
    /// </summary>
    /// <param name="tablesSubFolder">The subfolder containing SQL files and SchemaOrder.json.</param>
    private void CreateTables(string tablesSubFolder)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string tablesFolderPath = Path.Combine(baseDirectory, tablesSubFolder);
        string schemaOrderFilePath = Path.Combine(tablesFolderPath, "SchemaOrder.json"); // Path to the JSON manifest

        Console.WriteLine($"Attempting to load schema order from: {schemaOrderFilePath}");
        List<string> orderedSqlFilenames = GetSchemaOrder(schemaOrderFilePath);

        if (orderedSqlFilenames == null || orderedSqlFilenames.Count == 0)
        {
            Console.WriteLine(
                "Warning: No schema files found or schema order is empty. No tables will be created."
            );
            return;
        }

        Console.WriteLine($"Found {orderedSqlFilenames.Count} SQL files to process in order.");

        // Use a transaction to ensure all tables are created or none are (atomicity)
        using var transaction = _connection.BeginTransaction();
        try
        {
            int fileCounter = 1;
            foreach (string sqlFileName in orderedSqlFilenames)
            {
                if (string.IsNullOrWhiteSpace(sqlFileName))
                {
                    Console.WriteLine(
                        $"Warning: Empty filename found in schema order at position {fileCounter}. Skipping."
                    );
                    fileCounter++;
                    continue;
                }

                string fullSqlFilePath =
                    Path.Combine(tablesFolderPath, sqlFileName.Trim()) + ".sql"; // Trim whitespace from filename
                Console.WriteLine(
                    $"Processing SQL file ({fileCounter}/{orderedSqlFilenames.Count}): {fullSqlFilePath}"
                );

                if (!File.Exists(fullSqlFilePath))
                {
                    Console.Error.WriteLine(
                        $"Error: SQL file '{sqlFileName}' not found at '{fullSqlFilePath}'. Aborting table creation."
                    );
                    transaction.Rollback(); // Rollback changes made so far
                    Console.WriteLine("Transaction rolled back due to missing SQL file.");
                    return; // Or throw an exception
                }

                string sqlScriptContent = File.ReadAllText(fullSqlFilePath);

                if (string.IsNullOrWhiteSpace(sqlScriptContent))
                {
                    Console.WriteLine(
                        $"Warning: SQL file '{sqlFileName}' is empty. Skipping execution."
                    );
                    fileCounter++;
                    continue;
                }

                using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction; // Assign the transaction to the command
                    command.CommandText = sqlScriptContent;
                    // ExecuteNonQuery is suitable for DDL statements like CREATE TABLE.
                    // Microsoft.Data.Sqlite will execute multiple statements in CommandText if separated by semicolons.
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Successfully executed SQL script: {sqlFileName}");
                }
                fileCounter++;
            }

            transaction.Commit(); // If all scripts executed successfully, commit the transaction
            Console.WriteLine("All schema tables created successfully. Transaction committed.");
        }
        catch (SqliteException ex)
        {
            Console.Error.WriteLine(
                $"SQLite Error during table creation: {ex.Message} (SQLite Error Code: {ex.SqliteErrorCode})"
            );
            Console.Error.WriteLine(
                "Error likely occurred while processing file being pointed to by the counter or the one before it if an earlier file had an issue not caught by basic checks."
            );
            try
            {
                transaction.Rollback();
                Console.WriteLine("Transaction rolled back due to SQLite error.");
            }
            catch (Exception rbEx)
            {
                Console.Error.WriteLine($"Error during transaction rollback: {rbEx.Message}");
            }
            throw;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine(
                $"I/O Error during table creation (e.g., reading SQL file): {ex.Message}"
            );
            try
            {
                transaction.Rollback();
                Console.WriteLine("Transaction rolled back due to I/O error.");
            }
            catch (Exception rbEx)
            {
                Console.Error.WriteLine($"Error during transaction rollback: {rbEx.Message}");
            }
            throw;
        }
        catch (Exception ex) // Catch-all for other unexpected errors
        {
            Console.Error.WriteLine(
                $"An unexpected error occurred during table creation: {ex.Message}"
            );
            try
            {
                transaction.Rollback();
                Console.WriteLine("Transaction rolled back due to an unexpected error.");
            }
            catch (Exception rbEx)
            {
                Console.Error.WriteLine($"Error during transaction rollback: {rbEx.Message}");
            }
            throw;
        }
    }

    private void EnableOptimizations()
    {
        const string optimizeDBCommand =
            @"
        -- Performance and setup PRAGMAs for in-memory database
            /*
            If the application running SQLite crashes, the data will be safe, but the database might become corrupted if the operating system crashes or the computer loses power before that data has been written to the disk surface. On the other hand, commits can be orders of magnitude faster with synchronous OFF.
            */
            PRAGMA synchronous = OFF;
            PRAGMA journal_mode = WAL;
            PRAGMA temp_store = MEMORY;
            PRAGMA locking_mode = NORMAL; 
            PRAGMA cache_size = -500000;     -- Suggest 500MB cache (adjust as needed)
            PRAGMA foreign_keys = ON;        -- Enforce foreign key constraints
            pragma page_size = 32768;
            PRAGMA mmap_size = 30000000000;
        ";

        using var command = _connection.CreateCommand();
        command.CommandText = optimizeDBCommand;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns a list of string in the order we want to create the tables.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static List<string> GetSchemaOrder(string filePath)
    {
        try
        {
            // 1. Read the entire file content as a string.
            //    File.ReadAllText opens the file, reads its contents, and closes the file.
            string jsonString = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.WriteLine(
                    $"Warning: The file '{filePath}' is empty or contains only whitespace."
                );
                return []; // Return an empty list
            }

            // 2. Deserialize the JSON string into a List<string>.
            //    The JsonSerializer will automatically preserve the order from the JSON array.
            List<string> schemaOrder = JsonSerializer.Deserialize<List<string>>(jsonString) ?? [];

            if (schemaOrder == null)
            {
                // This might happen if the JSON content is "null" literally,
                // or if deserialization results in null for some reason.
                Console.WriteLine(
                    $"Warning: Deserialization of '{filePath}' resulted in a null list. The JSON might be 'null' or malformed in a specific way."
                );
                return [];
            }

            return schemaOrder;
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: The schema order file was not found at '{filePath}'.");
            return []; // Or throw the exception, depending on desired behavior
        }
        catch (JsonException ex)
        {
            // This catches errors during JSON parsing/deserialization.
            Console.Error.WriteLine(
                $"Error: Could not parse the JSON file '{filePath}'. Details: {ex.Message}"
            );
            if (ex.LineNumber.HasValue && ex.BytePositionInLine.HasValue)
            {
                Console.Error.WriteLine(
                    $"Error occurred near Line: {ex.LineNumber + 1}, Position: {ex.BytePositionInLine + 1}"
                );
            }
            return []; // Or throw
        }
        catch (IOException ex)
        {
            // Catches other I/O errors, e.g., file access permissions.
            Console.Error.WriteLine(
                $"Error: An I/O error occurred while reading '{filePath}'. Details: {ex.Message}"
            );
            return []; // Or throw
        }
        catch (Exception ex) // Catch-all for any other unexpected errors
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return []; // Or throw
        }
    }

    /// <summary>
    /// Finds all XML files in a directory, consolidates their elements,
    /// and then calls the parser to load the data in multiple passes.
    /// </summary>
    /// <param name="relativeDirectoryPath">The relative path to the directory containing game data XML files.</param>
    public void LoadDataFromDirectory(string relativeDirectoryPath)
    {
        Console.WriteLine(
            $"Consolidating all game data from directory '{relativeDirectoryPath}'..."
        );
        string fullDirectoryPath = Path.Combine(AppContext.BaseDirectory, relativeDirectoryPath);

        if (!Directory.Exists(fullDirectoryPath))
        {
            Console.WriteLine(
                $"Warning: Data directory not found at '{fullDirectoryPath}'. No data will be loaded."
            );
            return;
        }

        // Step 1: Read all files and aggregate their elements into separate lists.
        var allEntityElements = new List<XElement>();
        var allRelationElements = new List<XElement>();

        foreach (string filePath in Directory.EnumerateFiles(fullDirectoryPath, "*.xml"))
        {
            try
            {
                XDocument doc = XDocument.Load(filePath);
                if (doc.Root == null)
                    continue;

                // Aggregate <Entity> elements
                allEntityElements.AddRange(doc.Root.Elements("Entity"));

                // THE FIX: Check if the root element itself is a relation container,
                // and also check for nested relation containers for flexibility.
                if (doc.Root.Name == "ManyToManyRelation")
                {
                    allRelationElements.AddRange(doc.Root.Elements());
                }
                else
                {
                    // Aggregate all children of any nested <ManyToManyRelation> elements
                    foreach (var relationRoot in doc.Root.Elements("ManyToManyRelation"))
                    {
                        allRelationElements.AddRange(relationRoot.Elements());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error reading or parsing XML file {Path.GetFileName(filePath)}: {ex.Message}"
                );
            }
        }
        Console.WriteLine(
            $"Consolidated {allEntityElements.Count} entities and {allRelationElements.Count} relations from all files."
        );

        // Step 2: Pass the consolidated list of entities to the parser.
        DataParser.ParseAndLoad(this, allEntityElements);

        Console.WriteLine("Finished loading all game data.");
    }

    /// <summary>
    /// Closes the connection to the underlying sql database(sqlite3)
    /// </summary>
    public void Close()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Diposes the world
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Release managed resources
            _connection?.Close(); // Important to close before dispose for SQLite
            _connection?.Dispose();
            // QueryFactory typically doesn't need explicit disposal unless it holds specific resources.
        }
        _disposed = true;
    }
}
