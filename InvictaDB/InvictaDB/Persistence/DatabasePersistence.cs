using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;

namespace InvictaDB.Persistence;

/// <summary>
/// Provides serialization and persistence capabilities for InvictaDatabase.
/// Since the database is immutable, snapshots can be safely serialized on background threads.
/// </summary>
public static class DatabasePersistence
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    #region Synchronous Operations

    /// <summary>
    /// Creates a snapshot of the database that can be serialized.
    /// </summary>
    /// <param name="db">The database to snapshot.</param>
    /// <param name="turn">Optional turn number for metadata.</param>
    /// <param name="description">Optional description for the save.</param>
    /// <returns>A serializable snapshot.</returns>
    public static DatabaseSnapshot CreateSnapshot(
        this InvictaDatabase db,
        long? turn = null,
        string? description = null)
    {
        var snapshot = new DatabaseSnapshot
        {
            Metadata = new SnapshotMetadata
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Turn = turn,
                Description = description
            }
        };

        // Serialize each key-value pair in the database
        foreach (var kvp in db)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            var valueType = value.GetType();

            // Store the type information
            snapshot.TableTypes[key] = valueType.AssemblyQualifiedName ?? valueType.FullName ?? valueType.Name;

            // Serialize the value to JsonElement
            var json = JsonSerializer.SerializeToElement(value, valueType, DefaultOptions);
            snapshot.State[key] = json;
        }

        return snapshot;
    }

    /// <summary>
    /// Serializes a database snapshot to JSON string.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>JSON string representation.</returns>
    public static string SerializeSnapshot(DatabaseSnapshot snapshot, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(snapshot, options ?? DefaultOptions);
    }

    /// <summary>
    /// Serializes a database directly to JSON string.
    /// </summary>
    /// <param name="db">The database to serialize.</param>
    /// <param name="turn">Optional turn number for metadata.</param>
    /// <param name="description">Optional description for the save.</param>
    /// <returns>JSON string representation.</returns>
    public static string Serialize(
        this InvictaDatabase db,
        long? turn = null,
        string? description = null)
    {
        var snapshot = db.CreateSnapshot(turn, description);
        return SerializeSnapshot(snapshot);
    }

    /// <summary>
    /// Saves a database to a file synchronously.
    /// </summary>
    /// <param name="db">The database to save.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="turn">Optional turn number for metadata.</param>
    /// <param name="description">Optional description for the save.</param>
    public static void SaveToFile(
        this InvictaDatabase db,
        string filePath,
        long? turn = null,
        string? description = null)
    {
        var json = db.Serialize(turn, description);
        File.WriteAllText(filePath, json);
    }

    #endregion

    #region Asynchronous Operations (Zero-Lag Autosave)

    /// <summary>
    /// Saves a database to a file asynchronously on a background thread.
    /// Since the database is immutable, the game can continue while this runs.
    /// This is the "zero-lag autosave" feature.
    /// </summary>
    /// <param name="db">The database snapshot to save (immutable, safe to use from another thread).</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="turn">Optional turn number for metadata.</param>
    /// <param name="description">Optional description for the save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the save is done.</returns>
    public static Task SaveToFileAsync(
        this InvictaDatabase db,
        string filePath,
        long? turn = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // Capture the immutable database reference - safe to use from any thread
        var snapshot = db.CreateSnapshot(turn, description);

        return Task.Run(async () =>
        {
            var json = SerializeSnapshot(snapshot);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Starts a background save operation and returns immediately.
    /// The game can continue without waiting. Use the returned task to check completion if needed.
    /// </summary>
    /// <param name="db">The database snapshot to save.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="turn">Optional turn number for metadata.</param>
    /// <param name="description">Optional description for the save.</param>
    /// <param name="onComplete">Optional callback when save completes.</param>
    /// <param name="onError">Optional callback if save fails.</param>
    /// <returns>A task representing the background save operation.</returns>
    public static Task FireAndForgetSaveAsync(
        this InvictaDatabase db,
        string filePath,
        long? turn = null,
        string? description = null,
        Action? onComplete = null,
        Action<Exception>? onError = null)
    {
        var task = db.SaveToFileAsync(filePath, turn, description);

        // Handle completion callbacks without blocking
        task.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                onError?.Invoke(t.Exception.InnerException ?? t.Exception);
            }
            else if (t.IsCompletedSuccessfully)
            {
                onComplete?.Invoke();
            }
        }, TaskScheduler.Default);

        return task;
    }

    #endregion

    #region Loading Operations

    /// <summary>
    /// Deserializes a JSON string to a DatabaseSnapshot.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized snapshot.</returns>
    public static DatabaseSnapshot? DeserializeSnapshot(string json)
    {
        return JsonSerializer.Deserialize<DatabaseSnapshot>(json, DefaultOptions);
    }

    /// <summary>
    /// Loads a database from a snapshot, reconstructing the type registries.
    /// Note: The types must be available in the current assembly context.
    /// </summary>
    /// <param name="snapshot">The snapshot to load from.</param>
    /// <param name="typeResolver">Optional function to resolve types by name.</param>
    /// <returns>A new InvictaDatabase with the loaded state.</returns>
    public static InvictaDatabase LoadFromSnapshot(
        DatabaseSnapshot snapshot,
        Func<string, Type?>? typeResolver = null)
    {
        var db = new InvictaDatabase();

        foreach (var kvp in snapshot.State)
        {
            var key = kvp.Key;
            var jsonElement = kvp.Value;

            // Try to get the type
            Type? valueType = null;
            if (snapshot.TableTypes.TryGetValue(key, out var typeName))
            {
                valueType = typeResolver?.Invoke(typeName) ?? Type.GetType(typeName);
            }

            if (valueType == null)
            {
                // Skip entries we can't deserialize
                continue;
            }

            // Deserialize the value
            var value = jsonElement.Deserialize(valueType, DefaultOptions);
            if (value != null)
            {
                // Use reflection to call the appropriate registration/insert method
                // For tables (ImmutableDictionary types), we need special handling
                if (valueType.IsGenericType &&
                    valueType.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>))
                {
                    var entryType = valueType.GetGenericArguments()[1];

                    // Register the table
                    var registerMethod = typeof(InvictaDatabase)
                        .GetMethod(nameof(InvictaDatabase.RegisterTable), [typeof(string)])!
                        .MakeGenericMethod(entryType);
                    db = (InvictaDatabase)registerMethod.Invoke(db, [key])!;

                    // Insert each entry from the dictionary
                    var insertMethod = typeof(InvictaDatabase)
                        .GetMethod(nameof(InvictaDatabase.Insert))!
                        .MakeGenericMethod(entryType);

                    // Get the entries from the deserialized dictionary
                    var entries = (System.Collections.IEnumerable)value;
                    foreach (var entry in entries)
                    {
                        var entryKey = entry.GetType().GetProperty("Key")!.GetValue(entry) as string;
                        var entryValue = entry.GetType().GetProperty("Value")!.GetValue(entry);
                        if (entryKey != null && entryValue != null)
                        {
                            db = (InvictaDatabase)insertMethod.Invoke(db, [entryKey, entryValue])!;
                        }
                    }
                }
                else
                {
                    // It's a singleton
                    var insertSingletonMethod = typeof(InvictaDatabase)
                        .GetMethod(nameof(InvictaDatabase.InsertSingleton), [typeof(string), valueType.MakeByRefType().GetElementType() ?? valueType])!
                        .MakeGenericMethod(valueType);
                    db = (InvictaDatabase)insertSingletonMethod.Invoke(db, [key, value])!;
                }
            }
        }

        return db.ClearEvents(); // Clear the events generated during load
    }

    /// <summary>
    /// Loads a database from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="typeResolver">Optional function to resolve types by name.</param>
    /// <returns>A new InvictaDatabase with the loaded state.</returns>
    public static InvictaDatabase LoadFromJson(string json, Func<string, Type?>? typeResolver = null)
    {
        var snapshot = DeserializeSnapshot(json);
        if (snapshot == null)
        {
            throw new InvalidOperationException("Failed to deserialize database snapshot.");
        }
        return LoadFromSnapshot(snapshot, typeResolver);
    }

    /// <summary>
    /// Loads a database from a file.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="typeResolver">Optional function to resolve types by name.</param>
    /// <returns>A new InvictaDatabase with the loaded state.</returns>
    public static InvictaDatabase LoadFromFile(string filePath, Func<string, Type?>? typeResolver = null)
    {
        var json = File.ReadAllText(filePath);
        return LoadFromJson(json, typeResolver);
    }

    /// <summary>
    /// Loads a database from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="typeResolver">Optional function to resolve types by name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new InvictaDatabase with the loaded state.</returns>
    public static async Task<InvictaDatabase> LoadFromFileAsync(
        string filePath,
        Func<string, Type?>? typeResolver = null,
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return LoadFromJson(json, typeResolver);
    }

    #endregion
}
