using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
            LoadFeatureDefinitions();
            LoadGalaxies();
            //predefineGalaxies();
            //PredefinePolity();
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
    /// Creates an entity and returns it.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Entity Entity(string? name = null)
    {
        long entityId = -1;
        using var transaction = _connection.BeginTransaction();
        try
        {
            // 1. Create Entity to get ID
            using (var entityCmd = _connection.CreateCommand())
            {
                entityCmd.Transaction = transaction;
                entityCmd.CommandText = "INSERT INTO Entity DEFAULT VALUES; SELECT last_insert_rowid();";
                entityId = (long)entityCmd.ExecuteScalar()!;
            }

            // 2. Insert Name into EntityNames
            using (var nameCmd = _connection.CreateCommand())
            {
                nameCmd.Transaction = transaction;
                nameCmd.CommandText = "INSERT INTO Name (EntityId, Value) VALUES (@EntityId, @Value);";
                nameCmd.Parameters.AddWithValue("@EntityId", entityId);
                if (string.IsNullOrEmpty(name))
                {
                    nameCmd.Parameters.AddWithValue("@Value", DBNull.Value);
                }
                else
                {
                    nameCmd.Parameters.AddWithValue("@Value", name);
                }
                nameCmd.ExecuteNonQuery();
            }
            transaction.Commit();

            // We want a dictonary of entities where we can easy get them using their name. 
            if (!string.IsNullOrEmpty(name))
                Entities[name] = entityId;

            return new Entity()
            {
                Id = entityId,
                World = this,
            };
        }
        catch (SqliteException ex)
        {
            transaction.Rollback();
            Console.WriteLine($"{ex.Message}");
            Console.WriteLine($"Error: Entity name '{name}' already exists or another unique constraint failed. Entity not created.");
            throw; // Re-throw or handle more gracefully
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
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
            Console.WriteLine("Warning: No schema files found or schema order is empty. No tables will be created.");
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
                    Console.WriteLine($"Warning: Empty filename found in schema order at position {fileCounter}. Skipping.");
                    fileCounter++;
                    continue;
                }

                string fullSqlFilePath = Path.Combine(tablesFolderPath, sqlFileName.Trim()) + ".sql"; // Trim whitespace from filename
                Console.WriteLine($"Processing SQL file ({fileCounter}/{orderedSqlFilenames.Count}): {fullSqlFilePath}");

                if (!File.Exists(fullSqlFilePath))
                {
                    Console.Error.WriteLine($"Error: SQL file '{sqlFileName}' not found at '{fullSqlFilePath}'. Aborting table creation.");
                    transaction.Rollback(); // Rollback changes made so far
                    Console.WriteLine("Transaction rolled back due to missing SQL file.");
                    return; // Or throw an exception
                }

                string sqlScriptContent = File.ReadAllText(fullSqlFilePath);

                if (string.IsNullOrWhiteSpace(sqlScriptContent))
                {
                    Console.WriteLine($"Warning: SQL file '{sqlFileName}' is empty. Skipping execution.");
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
            Console.Error.WriteLine($"SQLite Error during table creation: {ex.Message} (SQLite Error Code: {ex.SqliteErrorCode})");
            Console.Error.WriteLine("Error likely occurred while processing file being pointed to by the counter or the one before it if an earlier file had an issue not caught by basic checks.");
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
            Console.Error.WriteLine($"I/O Error during table creation (e.g., reading SQL file): {ex.Message}");
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
            Console.Error.WriteLine($"An unexpected error occurred during table creation: {ex.Message}");
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
        const string optimizeDBCommand = @"
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
                Console.WriteLine($"Warning: The file '{filePath}' is empty or contains only whitespace.");
                return []; // Return an empty list
            }

            // 2. Deserialize the JSON string into a List<string>.
            //    The JsonSerializer will automatically preserve the order from the JSON array.
            List<string> schemaOrder = JsonSerializer.Deserialize<List<string>>(jsonString) ?? [];

            if (schemaOrder == null)
            {
                // This might happen if the JSON content is "null" literally,
                // or if deserialization results in null for some reason.
                Console.WriteLine($"Warning: Deserialization of '{filePath}' resulted in a null list. The JSON might be 'null' or malformed in a specific way.");
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
            Console.Error.WriteLine($"Error: Could not parse the JSON file '{filePath}'. Details: {ex.Message}");
            if (ex.LineNumber.HasValue && ex.BytePositionInLine.HasValue)
            {
                Console.Error.WriteLine($"Error occurred near Line: {ex.LineNumber + 1}, Position: {ex.BytePositionInLine + 1}");
            }
            return []; // Or throw
        }
        catch (IOException ex)
        {
            // Catches other I/O errors, e.g., file access permissions.
            Console.Error.WriteLine($"Error: An I/O error occurred while reading '{filePath}'. Details: {ex.Message}");
            return []; // Or throw
        }
        catch (Exception ex) // Catch-all for any other unexpected errors
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return []; // Or throw
        }
    }


    /// <summary>
    /// Each game world is populated with a set of predefined galaxies
    /// </summary>
    private void PredefineGalaxies()
    {
        PredefineAndromedaGalaxy();
        PredefineMilkyWayGalaxy();
    }

    /// <summary>
    /// Pre define political polities
    /// </summary>
    private void PredefinePolity()
    {
        DefineGrandExhangePolity();
    }

    private void DefineGrandExhangePolity()
    {
        // Capital world of the grandExchange
        var abacus = Entity("Abacus");
        Query("Planet").Insert(new
        {
            EntityId = abacus.Id
        });

        var grandExchange = Entity("Grand Exchange");
        Query("Polity").Insert(new
        {
            EntityId = grandExchange.Id,
            LeaderTitle = "Prime Arbiter",
            Abbreviation = "GE",
            SeatOfPowerLocationID = abacus.Id
        });
    }

    private void DefineVoidWardensPolity()
    {
        // Capital world of the Void Wardens9*
        var veridia = Entity("Veridia");
        Query("Planet").Insert(new
        {
            EntityId = veridia.Id
        });

        var grandExchange = Entity("Void Wardens");
        Query("Polity").Insert(new
        {
            EntityId = grandExchange.Id,
            LeaderTitle = "Grand Keeper",
            Abbreviation = "GK",
            SeatOfPowerLocationID = veridia.Id
        });
    }

    private void LoadFeatureDefinitions()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FeatureDefinition.xml");
        foreach (var key in DataParser.GetFeatureKeys(path))
        {
            Query("FeatureDefinition").Insert(new
            {
                EntityId = Entity(key ?? "").Id,
                Key = key
            });
        }
    }

    private void LoadGalaxies()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Galaxy.xml");
        foreach (var galaxyName in DataParser.GetGalaxyNames(path))
        {
            var e = Entity(galaxyName ?? "");
            Query("Galaxy").Insert(new { EntityId = e.Id });
        }
    }

    private void PredefineMilkyWayGalaxy()
    {
        //Defining an entity with a galaxy component
        var milkiWay = Entity("Milky Way");
        Query("Galaxy").Insert(new { EntityId = milkiWay.Id });

        var proximaCentauri = Entity("Proxima Centauri");
        Query("Entity").Where("Id", proximaCentauri.Id).Update(new { ParentId = milkiWay.Id });
        Query("StarSystem").Insert(new
        {
            EntityId = proximaCentauri.Id,
        });

        Query("Position3D").Insert(new
        {
            EntityId = proximaCentauri.Id,
            X = 5,
            Y = 2,
            Z = 3,
        });

        var proximaCentauriA = Entity("Proxima Centauri A");
        proximaCentauriA.ParentId = proximaCentauri.Id;
        Query("Star").Insert(new
        {
            EntityId = proximaCentauriA.Id,
        });

        var proximaCentauriB = Entity("Proxima Centauri B");
        Query("Entity").Where("Id", proximaCentauriB.Id).Update(new { ParentId = proximaCentauriA.Id });
        Query("Planet").Insert(new
        {
            EntityId = proximaCentauriB.Id,
        });

        var sirus = Entity("Sirius");
        Query("Entity").Where("Id", sirus.Id).Update(new { ParentId = milkiWay.Id });
        Query("StarSystem").Insert(new
        {
            EntityId = sirus.Id,
        });

        var sirusA = Entity("SiriusA");
        Query("Entity").Where("Id", sirusA.Id).Update(new { ParentId = sirus.Id });
        Query("Star").Insert(new
        {
            EntityId = sirusA.Id,
        });

        var sirusB = Entity("SiriusB");
        Query("Entity").Where("Id", sirusB.Id).Update(new { ParentId = sirus.Id });
        Query("Star").Insert(new
        {
            EntityId = sirusB.Id,
        });
    }

    private void PredefineAndromedaGalaxy()
    {
        //Defining an entity with a galaxy component
        var andromeda = Entity("Andromeda");
        Query("Galaxy").Insert(new { EntityId = andromeda.Id });
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
