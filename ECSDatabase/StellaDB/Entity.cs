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
            _cachedName ??= World
                .Query("Name")
                .Where("EntityId", Id)
                .Select("Value")
                .FirstOrDefault<string>();
            return _cachedName;
        }
        set
        {
            World.Query("Name").Where("EntityId", Id).Update(new { Value = value });
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
            return World.Query("Entity").Where("Id", Id).Select("ParentId").FirstOrDefault<long?>();
        }
        set { World.Query("Entity").Where("Id", Id).Update(new { ParentId = value }); }
    }

    /// <summary>
    /// Updates a component on this entity, an entity must already have this component attached
    /// other wise an error will be thrown. Keep in Mind that writing your own update query is often much more efficient
    /// because know at best what data you actually need and what to update. These methods are for convience
    /// to improve the iteration speed when developing. For performance prefer writing your own queries.
    /// </summary>
    /// <param name="componentName">Name of the component table</param>
    /// <param name="data">Component data as anonymous object</param>
    /// <returns>This entity for method chaining</returns>
    public Entity Update(string componentName, object data)
    {
        World.Query(componentName).Where("EntityId", Id).Update(data);

        return this;
    }

    /// <summary>
    /// Adds a component to an entity
    /// </summary>
    /// <param name="componentName"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public Entity Add(string componentName, object data)
    {
        // Insert new component
        World.Query(componentName).Insert(data);
        return this;
    }

    /// <summary>
    /// Adds a component for an entity, used for identifier components, that only have one field
    /// the entity id, so we have a star table where each entity id in the table says, is a star.
    /// These methods are for convience
    /// to improve the iteration speed when developing. For performance prefer writing your own queries.
    /// </summary>
    /// <returns></returns>
    public Entity Add(string componentName)
    {
        World.Query(componentName).Insert(new { EntityId = Id });
        return this;
    }

    /// <summary>
    /// Checks if entity has a specific component. These methods are for convience
    /// to improve the iteration speed when developing. For performance prefer writing your own queries.
    /// </summary>
    /// <param name="componentName">Name of the component table</param>
    /// <returns>True if component exists</returns>
    public bool Has(string componentName)
    {
        var count = World.Query(componentName).Where("EntityId", Id).Count<int>();
        return count > 0;
    }

    /// <summary>
    /// Gets a component as a dynamic object. These methods are for convience
    /// to improve the iteration speed when developing. For performance prefer writing your own queries.
    /// </summary>
    /// <param name="componentName">Name of the component table</param>
    /// <returns>Component data or null if not found</returns>
    public dynamic? Get(string componentName)
    {
        return World.Query(componentName).Where("EntityId", Id).FirstOrDefault();
    }

    /// <summary>
    /// Gets a component as a strongly-typed object. These methods are for convience
    /// to improve the iteration speed when developing. For performance prefer writing your own queries.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <returns>Component instance or null if not found</returns>
    public T Get<T>()
    {
        var componentName = typeof(T).Name;

        return World.Query(componentName).Where("EntityId", Id).FirstOrDefault<T>();
    }

    /// <summary>
    /// Determines whether the specified Entity is equal to the current Entity based on ID and World
    /// </summary>
    /// <param name="other">The Entity to compare with the current Entity</param>
    /// <returns>true if the specified Entity is equal to the current Entity; otherwise, false</returns>
    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

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
        if (left is null)
            return right is null;
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
