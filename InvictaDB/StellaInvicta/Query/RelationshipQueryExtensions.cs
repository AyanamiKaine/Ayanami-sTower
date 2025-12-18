using InvictaDB;
using StellaInvicta.Data;

namespace StellaInvicta.Query;

/// <summary>
/// Extension methods for querying Relationship tables.
/// </summary>
public static class RelationshipQueryExtensions
{
    #region Basic Query Methods

    /// <summary>
    /// Gets all outgoing relationships from an entity.
    /// </summary>
    public static IEnumerable<Relationship> GetOutgoingRelationships(
        this InvictaDatabase db,
        string sourceId,
        string? sourceType = null)
    {
        var query = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == sourceId);

        if (sourceType != null)
            query = query.Where(r => r.Value.SourceType == sourceType);

        return query.Select(r => r.Value);
    }

    /// <summary>
    /// Gets all incoming relationships to an entity.
    /// </summary>
    public static IEnumerable<Relationship> GetIncomingRelationships(
        this InvictaDatabase db,
        string targetId,
        string? targetType = null)
    {
        var query = db.GetTable<Relationship>()
            .Where(r => r.Value.TargetId == targetId);

        if (targetType != null)
            query = query.Where(r => r.Value.TargetType == targetType);

        return query.Select(r => r.Value);
    }

    /// <summary>
    /// Gets all relationships (both directions) for an entity.
    /// </summary>
    public static IEnumerable<Relationship> GetAllRelationships(
        this InvictaDatabase db,
        string entityId,
        string? entityType = null)
    {
        var outgoing = db.GetOutgoingRelationships(entityId, entityType);
        var incoming = db.GetIncomingRelationships(entityId, entityType);
        return outgoing.Concat(incoming);
    }

    #endregion

    #region Filtered Query Methods

    /// <summary>
    /// Gets outgoing relationships of a specific type.
    /// </summary>
    public static IEnumerable<Relationship> GetRelationshipsOfType(
        this InvictaDatabase db,
        string sourceId,
        string relationshipTypeId)
    {
        return db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == sourceId
                     && r.Value.RelationshipTypeId == relationshipTypeId)
            .Select(r => r.Value);
    }

    /// <summary>
    /// Gets the IDs of entities related to source by a specific relationship type.
    /// </summary>
    public static IEnumerable<string> GetRelatedIds(
        this InvictaDatabase db,
        string sourceId,
        string relationshipTypeId)
    {
        return db.GetRelationshipsOfType(sourceId, relationshipTypeId)
            .Select(r => r.TargetId);
    }

    /// <summary>
    /// Gets relationships with positive strength (opinion > 0).
    /// </summary>
    public static IEnumerable<Relationship> GetPositiveRelationships(
        this InvictaDatabase db,
        string sourceId,
        string? relationshipTypeId = null)
    {
        var query = db.GetOutgoingRelationships(sourceId)
            .Where(r => r.Strength > 0);

        if (relationshipTypeId != null)
            query = query.Where(r => r.RelationshipTypeId == relationshipTypeId);

        return query;
    }

    /// <summary>
    /// Gets relationships with negative strength (opinion 0).
    /// </summary>
    public static IEnumerable<Relationship> GetNegativeRelationships(
        this InvictaDatabase db,
        string sourceId,
        string? relationshipTypeId = null)
    {
        var query = db.GetOutgoingRelationships(sourceId)
            .Where(r => r.Strength < 0);

        if (relationshipTypeId != null)
            query = query.Where(r => r.RelationshipTypeId == relationshipTypeId);

        return query;
    }

    /// <summary>
    /// Gets the relationship between two specific entities.
    /// </summary>
    public static Relationship? GetRelationship(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId)
    {
        return db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == sourceId
                     && r.Value.TargetId == targetId
                     && r.Value.RelationshipTypeId == relationshipTypeId)
            .Select(r => r.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a relationship exists between two entities.
    /// </summary>
    public static bool HasRelationship(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId)
    {
        return db.GetRelationship(sourceId, targetId, relationshipTypeId) != null;
    }

    /// <summary>
    /// Gets the strength of a relationship, or null if it doesn't exist.
    /// </summary>
    public static int? GetRelationshipStrength(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId)
    {
        return db.GetRelationship(sourceId, targetId, relationshipTypeId)?.Strength;
    }

    #endregion

    #region Character-Specific Methods

    /// <summary>
    /// Gets all character IDs that have a relationship with the source character.
    /// </summary>
    public static IEnumerable<string> GetRelatedCharacterIds(
        this InvictaDatabase db,
        string characterId,
        string relationshipTypeId)
    {
        return db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == characterId
                     && r.Value.SourceType == nameof(Character)
                     && r.Value.TargetType == nameof(Character)
                     && r.Value.RelationshipTypeId == relationshipTypeId)
            .Select(r => r.Value.TargetId);
    }

    /// <summary>
    /// Gets all Character objects related to the source character.
    /// </summary>
    public static IEnumerable<Character> GetRelatedCharacters(
        this InvictaDatabase db,
        string characterId,
        string relationshipTypeId)
    {
        var characterTable = db.GetTable<Character>();
        return db.GetRelatedCharacterIds(characterId, relationshipTypeId)
            .Where(id => characterTable.ContainsKey(id))
            .Select(id => characterTable[id]);
    }

    /// <summary>
    /// Finds mutual relationships between two characters (e.g., mutual friends).
    /// </summary>
    public static IEnumerable<string> GetMutualRelations(
        this InvictaDatabase db,
        string characterId1,
        string characterId2,
        string relationshipTypeId)
    {
        var char1Relations = db.GetRelatedCharacterIds(characterId1, relationshipTypeId).ToHashSet();
        var char2Relations = db.GetRelatedCharacterIds(characterId2, relationshipTypeId).ToHashSet();
        return char1Relations.Intersect(char2Relations);
    }

    /// <summary>
    /// Gets relationship counts grouped by type for a character.
    /// </summary>
    public static Dictionary<string, int> GetRelationshipCountsByType(
        this InvictaDatabase db,
        string characterId)
    {
        return db.GetOutgoingRelationships(characterId, nameof(Character))
            .GroupBy(r => r.RelationshipTypeId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets the average relationship strength by type for a character.
    /// </summary>
    public static Dictionary<string, double> GetAverageStrengthByType(
        this InvictaDatabase db,
        string characterId)
    {
        return db.GetOutgoingRelationships(characterId, nameof(Character))
            .GroupBy(r => r.RelationshipTypeId)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Strength));
    }

    #endregion

    #region Modification Methods

    /// <summary>
    /// Creates a character-to-character relationship.
    /// </summary>
    public static InvictaDatabase AddCharacterRelationship(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId,
        int strength = 0)
    {
        var rel = RelationshipExtensions.CharacterRelationship(sourceId, targetId, relationshipTypeId, strength);
        return db.Insert(rel.CompositeKey, rel);
    }

    /// <summary>
    /// Creates a symmetric relationship (both directions).
    /// </summary>
    public static InvictaDatabase AddSymmetricRelationship(
        this InvictaDatabase db,
        string entityId1,
        string entityId2,
        string entityType,
        string relationshipTypeId,
        int strength = 0)
    {
        var rel1 = new Relationship(entityId1, entityType, entityId2, entityType, relationshipTypeId, strength);
        var rel2 = new Relationship(entityId2, entityType, entityId1, entityType, relationshipTypeId, strength);

        return db.Insert(rel1.CompositeKey, rel1)
                 .Insert(rel2.CompositeKey, rel2);
    }

    /// <summary>
    /// Updates the strength of an existing relationship.
    /// </summary>
    public static InvictaDatabase UpdateRelationshipStrength(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId,
        int newStrength)
    {
        var existing = db.GetRelationship(sourceId, targetId, relationshipTypeId);
        if (existing == null)
            return db;

        var updated = existing with { Strength = newStrength };
        return db.Insert(updated.CompositeKey, updated);
    }

    /// <summary>
    /// Modifies the strength of an existing relationship by a delta.
    /// </summary>
    public static InvictaDatabase ModifyRelationshipStrength(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId,
        int delta,
        int minStrength = -100,
        int maxStrength = 100)
    {
        var existing = db.GetRelationship(sourceId, targetId, relationshipTypeId);
        if (existing == null)
            return db;

        var newStrength = Math.Clamp(existing.Strength + delta, minStrength, maxStrength);
        var updated = existing with { Strength = newStrength };
        return db.Insert(updated.CompositeKey, updated);
    }

    /// <summary>
    /// Removes a relationship between two entities.
    /// </summary>
    public static InvictaDatabase RemoveRelationship(
        this InvictaDatabase db,
        string sourceId,
        string sourceType,
        string targetId,
        string targetType,
        string relationshipTypeId)
    {
        var key = $"{sourceType}:{sourceId}→{relationshipTypeId}→{targetType}:{targetId}";
        return db.RemoveEntry<Relationship>(key);
    }

    /// <summary>
    /// Removes a character-to-character relationship.
    /// </summary>
    public static InvictaDatabase RemoveCharacterRelationship(
        this InvictaDatabase db,
        string sourceId,
        string targetId,
        string relationshipTypeId)
    {
        return db.RemoveRelationship(sourceId, nameof(Character), targetId, nameof(Character), relationshipTypeId);
    }

    /// <summary>
    /// Removes all outgoing relationships of a specific type from an entity.
    /// </summary>
    public static InvictaDatabase RemoveAllRelationshipsOfType(
        this InvictaDatabase db,
        string sourceId,
        string relationshipTypeId)
    {
        var keys = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == sourceId
                     && r.Value.RelationshipTypeId == relationshipTypeId)
            .Select(r => r.Key)
            .ToList();

        foreach (var key in keys)
        {
            db = db.RemoveEntry<Relationship>(key);
        }
        return db;
    }

    #endregion
}
