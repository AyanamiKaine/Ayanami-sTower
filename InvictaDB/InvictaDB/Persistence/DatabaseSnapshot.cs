using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvictaDB.Persistence;

/// <summary>
/// Represents a serializable snapshot of the database state.
/// This is the format used for saving/loading game state.
/// </summary>
public class DatabaseSnapshot
{
    /// <summary>
    /// The serialized state data (table name -> serialized entries).
    /// </summary>
    public Dictionary<string, JsonElement> State { get; set; } = [];

    /// <summary>
    /// Type mappings for tables (table name -> full type name).
    /// </summary>
    public Dictionary<string, string> TableTypes { get; set; } = [];

    /// <summary>
    /// Metadata about the snapshot.
    /// </summary>
    public SnapshotMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about a database snapshot.
/// </summary>
public class SnapshotMetadata
{
    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional game turn/tick number.
    /// </summary>
    public long? Turn { get; set; }

    /// <summary>
    /// Optional description or save name.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version of the save format.
    /// </summary>
    public int Version { get; set; } = 1;
}
