using SqlKata.Execution;

namespace AyanamisTower.StellaDB;


/// <summary>
/// Represents an entity in the game world
/// </summary>
public class Entity
{
    private string? _cachedName;
    /// <summary>
    /// Id of the entity, its used to find attached component of the entity
    /// </summary>
    public required long Id { get; init; }
    /// <summary>
    /// World where the entity lives in
    /// </summary>
    public required World World { get; init; }
    /// <summary>
    /// Name of the entity (cached after first access)
    /// </summary>
    public string Name
    {
        get
        {
            _cachedName ??= World.Query("Name")
                                  .Where("EntityId", Id)
                                  .Select("Value")
                                  .FirstOrDefault<string>();
            return _cachedName;
        }
        set
        {
            World.Query("Name")
                .Where("EntityId", Id)
                .Update(new { Value = value });
            _cachedName = value;
        }
    }

    /// <summary>
    /// Id of the parent entity, null if it has no parent
    /// </summary>
    public long? ParentId
    {
        get
        {
            return World.Query("Entity")
                                  .Where("Id", Id)
                                  .Select("ParentId")
                                  .FirstOrDefault<long?>();
        }
        set
        {
            World.Query("Entity")
                .Where("Id", Id)
                .Update(new { ParentId = value });
        }
    }

    /// <summary>
    /// Determines whether the specified Entity is equal to the current Entity based on ID and World
    /// </summary>
    /// <param name="other">The Entity to compare with the current Entity</param>
    /// <returns>true if the specified Entity is equal to the current Entity; otherwise, false</returns>
    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Two entities are equal if they have the same ID and belong to the same World
        return Id == other.Id && ReferenceEquals(World, other.World);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Entity
    /// </summary>
    /// <param name="obj">The object to compare with the current Entity</param>
    /// <returns>true if the specified object is equal to the current Entity; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity);
    }

    /// <summary>
    /// Returns a hash code for the current Entity based on its ID and World
    /// </summary>
    /// <returns>A hash code for the current Entity</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, World);
    }

    /// <summary>
    /// Determines whether two Entity instances are equal
    /// </summary>
    /// <param name="left">The first Entity to compare</param>
    /// <param name="right">The second Entity to compare</param>
    /// <returns>true if the Entity instances are equal; otherwise, false</returns>
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two Entity instances are not equal
    /// </summary>
    /// <param name="left">The first Entity to compare</param>
    /// <param name="right">The second Entity to compare</param>
    /// <returns>true if the Entity instances are not equal; otherwise, false</returns>
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a string representation of the Entity
    /// </summary>
    /// <returns>A string that represents the current Entity</returns>
    public override string ToString()
    {
        return $"Entity[Id={Id}, Name=\"{Name}\"]";
    }
}