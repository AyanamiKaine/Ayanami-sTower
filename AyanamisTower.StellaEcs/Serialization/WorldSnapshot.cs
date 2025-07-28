#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Collections.Generic;
using System.Text.Json;

namespace AyanamisTower.StellaEcs.Serialization
{
    /// <summary>
    /// A serializable representation of an entire World's state.
    /// </summary>
    public class WorldSnapshot
    {
        public int MaxEntities { get; set; }
        public int NextEntityId { get; set; }
        public int[] EntityGenerations { get; set; } = [];
        public Queue<int> RecycledEntityIds { get; set; } = new();

        public List<ComponentTypeInfo> ComponentTypes { get; set; } = [];
        public List<string> RelationshipTypes { get; set; } = [];

        public List<ComponentStorageSnapshot> ComponentData { get; set; } = [];
        public List<RelationshipStorageSnapshot> RelationshipData { get; set; } = [];
    }

    /// <summary>
    /// Serializable metadata for a component type, including its schema if it was created at runtime.
    /// </summary>
    public class ComponentTypeInfo
    {
        public string TypeName { get; set; } = "";
        public bool IsRuntimeDefined { get; set; }
        public Dictionary<string, string>? Fields { get; set; } // FieldName -> FieldType assembly-qualified name
    }

    /// <summary>
    /// A serializable snapshot of a single ComponentStorage.
    /// </summary>
    public class ComponentStorageSnapshot
    {
        public string ComponentTypeName { get; set; } = "";
        // Maps Entity ID to its component data, serialized as a JsonElement.
        public Dictionary<int, JsonElement> Components { get; set; } = [];
    }

    /// <summary>
    /// A serializable snapshot of a single RelationshipStorage.
    /// </summary>
    public class RelationshipStorageSnapshot
    {
        public string RelationshipTypeName { get; set; } = "";
        public List<RelationshipEntry> Relationships { get; set; } = [];
    }

    /// <summary>
    /// A serializable representation of a single relationship instance.
    /// </summary>
    public class RelationshipEntry
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        // Relationship data (if any), serialized as a JsonElement.
        public JsonElement Data { get; set; }
    }
}
