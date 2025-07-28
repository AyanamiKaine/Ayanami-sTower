namespace AyanamisTower.StellaEcs;

/// <summary>
/// Defines a non-generic interface for a component storage,
/// allowing the World to manage storages of different component types.
/// </summary>
internal interface IComponentStorage
{
    /// <summary>
    /// Gets the current number of components stored in this storage.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks if the storage contains a component for the given entity ID.
    /// </summary>
    bool Has(int entityId);

    /// <summary>
    /// Removes a component for the given entity ID from the storage, if it exists.
    /// </summary>
    void Remove(int entityId);
    /// <summary>
    /// Gets a read-only span of the packed entity IDs for efficient iteration.
    /// </summary>
    ReadOnlySpan<int> PackedEntities { get; }
}
