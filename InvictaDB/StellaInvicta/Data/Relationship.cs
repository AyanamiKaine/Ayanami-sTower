namespace StellaInvicta.Data;

/// <summary>
/// Represents a relationship between two entities.
/// This is a generic, decoupled relationship that can connect any entities.
/// </summary>
/// <param name="SourceId">The ID of the source entity (e.g., a character).</param>
/// <param name="SourceType">The type name of the source entity (e.g., "Character").</param>
/// <param name="TargetId">The ID of the target entity (e.g., another character).</param>
/// <param name="TargetType">The type name of the target entity (e.g., "Character").</param>
/// <param name="RelationshipTypeId">The ID of the relationship type.</param>
/// <param name="Strength">Optional strength/intensity of the relationship (-100 to 100).</param>
public record Relationship(
    string SourceId,
    string SourceType,
    string TargetId,
    string TargetType,
    string RelationshipTypeId,
    int Strength = 0
)
{
    /// <summary>
    /// Creates a composite key for this relationship.
    /// Use this as the ID when inserting into the database.
    /// </summary>
    public string CompositeKey => $"{SourceType}:{SourceId}→{RelationshipTypeId}→{TargetType}:{TargetId}";
}

/// <summary>
/// Extension methods for creating typed relationships.
/// </summary>
public static class RelationshipExtensions
{
    /// <summary>
    /// Creates a character-to-character relationship.
    /// </summary>
    public static Relationship CharacterRelationship(
        string sourceCharacterId,
        string targetCharacterId,
        string relationshipTypeId,
        int strength = 0) =>
        new(sourceCharacterId, nameof(Character), targetCharacterId, nameof(Character), relationshipTypeId, strength);

    /// <summary>
    /// Creates a character-to-location relationship.
    /// </summary>
    public static Relationship CharacterLocationRelationship(
        string characterId,
        string locationId,
        string relationshipTypeId,
        int strength = 0) =>
        new(characterId, nameof(Character), locationId, nameof(Location), relationshipTypeId, strength);
}
