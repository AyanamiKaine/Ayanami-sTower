using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// A non-generic interface for ComponentStorage, allowing different storage types 
/// to be managed by the World in a single collection.
/// </summary>
public interface IComponentStorage
{
    /// <summary>
    /// Gets the number of components in this storage.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks if a given entity has a component in this storage.
    /// </summary>
    bool Has(Entity entity);

    /// <summary>
    /// Removes a component from an entity, if it exists.
    /// </summary>
    void Remove(Entity entity);

    /// <summary>
    /// Gets an enumerable collection of all entities within this storage.
    /// </summary>
    IEnumerable<Entity> GetEntities();
    /// <summary>
    /// Gets the data associated with an entity as an object.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    object? GetDataAsObject(Entity entity);

}
