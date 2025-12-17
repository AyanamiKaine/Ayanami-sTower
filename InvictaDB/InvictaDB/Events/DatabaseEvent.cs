using System.Collections.Immutable;

namespace InvictaDB.Events;

/// <summary>
/// The type of database operation that triggered the event.
/// </summary>
public enum DatabaseEventType
{
    /// <summary>
    /// A new entry was inserted.
    /// </summary>
    Inserted,

    /// <summary>
    /// An existing entry was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// An entry was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// A singleton was inserted or updated.
    /// </summary>
    SingletonChanged,

    /// <summary>
    /// A table was registered.
    /// </summary>
    TableRegistered
}

/// <summary>
/// Represents an event that occurred in the database.
/// </summary>
/// <param name="EventType">The type of operation.</param>
/// <param name="EntityType">The type of entity affected.</param>
/// <param name="EntityId">The ID of the entity (null for singletons identified by type).</param>
/// <param name="TableName">The table name (null for singletons).</param>
/// <param name="OldValue">The previous value (null for inserts).</param>
/// <param name="NewValue">The new value (null for removes).</param>
/// <param name="Timestamp">When the event occurred.</param>
public record DatabaseEvent(
    DatabaseEventType EventType,
    Type EntityType,
    string? EntityId,
    string? TableName,
    object? OldValue,
    object? NewValue,
    long Timestamp)
{
    /// <summary>
    /// Creates an insert event.
    /// </summary>
    public static DatabaseEvent Insert<T>(string tableName, string id, T newValue) =>
        new(DatabaseEventType.Inserted, typeof(T), id, tableName, null, newValue, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Creates an update event.
    /// </summary>
    public static DatabaseEvent Update<T>(string tableName, string id, T? oldValue, T newValue) =>
        new(DatabaseEventType.Updated, typeof(T), id, tableName, oldValue, newValue, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Creates a remove event.
    /// </summary>
    public static DatabaseEvent Remove<T>(string? tableName, string id, T? oldValue) =>
        new(DatabaseEventType.Removed, typeof(T), id, tableName, oldValue, null, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Creates a singleton changed event.
    /// </summary>
    public static DatabaseEvent SingletonChanged<T>(string singletonId, T? oldValue, T newValue) =>
        new(DatabaseEventType.SingletonChanged, typeof(T), singletonId, null, oldValue, newValue, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Creates a table registered event.
    /// </summary>
    public static DatabaseEvent TableRegistered<T>(string tableName) =>
        new(DatabaseEventType.TableRegistered, typeof(T), null, tableName, null, null, DateTimeOffset.UtcNow.Ticks);
}

/// <summary>
/// Immutable collection of database events.
/// </summary>
public class DatabaseEventLog
{
    /// <summary>
    /// Empty event log.
    /// </summary>
    public static readonly DatabaseEventLog Empty = new(ImmutableList<DatabaseEvent>.Empty);

    private readonly ImmutableList<DatabaseEvent> _events;

    private DatabaseEventLog(ImmutableList<DatabaseEvent> events)
    {
        _events = events;
    }

    /// <summary>
    /// Gets all events in the log.
    /// </summary>
    public ImmutableList<DatabaseEvent> Events => _events;

    /// <summary>
    /// Gets the number of events in the log.
    /// </summary>
    public int Count => _events.Count;

    /// <summary>
    /// Adds an event to the log, returning a new log.
    /// </summary>
    public DatabaseEventLog Add(DatabaseEvent evt) =>
        new(_events.Add(evt));

    /// <summary>
    /// Adds multiple events to the log, returning a new log.
    /// </summary>
    public DatabaseEventLog AddRange(IEnumerable<DatabaseEvent> events) =>
        new(_events.AddRange(events));

    /// <summary>
    /// Clears all events, returning an empty log.
    /// </summary>
    public DatabaseEventLog Clear() => Empty;

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    public IEnumerable<DatabaseEvent> GetEvents(DatabaseEventType eventType) =>
        _events.Where(e => e.EventType == eventType);

    /// <summary>
    /// Gets events for a specific entity type.
    /// </summary>
    public IEnumerable<DatabaseEvent> GetEventsForType<T>() =>
        _events.Where(e => e.EntityType == typeof(T));

    /// <summary>
    /// Gets events for a specific entity.
    /// </summary>
    public IEnumerable<DatabaseEvent> GetEventsForEntity<T>(string id) =>
        _events.Where(e => e.EntityType == typeof(T) && e.EntityId == id);

    /// <summary>
    /// Gets events that occurred after a specific timestamp.
    /// </summary>
    public IEnumerable<DatabaseEvent> GetEventsSince(long timestamp) =>
        _events.Where(e => e.Timestamp > timestamp);
}
