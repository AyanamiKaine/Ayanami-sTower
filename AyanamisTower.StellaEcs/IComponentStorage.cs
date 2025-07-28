namespace AyanamisTower.StellaEcs;

/// <summary>
/// Defines a non-generic interface for a component storage,
/// allowing the World to manage storages of different component types.
/// </summary>
internal interface IComponentStorage
{
    /// <summary>
    /// Checks if the storage contains a component for the given entity ID.
    /// </summary>
    bool Has(int entityId);

    /// <summary>
    /// Removes a component for the given entity ID from the storage, if it exists.
    /// </summary>
    void Remove(int entityId);
}
