namespace AyanamisTower.StellaEcs;

/// <summary>
/// Stores components of type <typeparamref name="T"/> using a sparse set for efficient access and cache-friendly iteration.
/// </summary>
public class ComponentStorage<T>(int capacity, int universeSize) : IComponentStorage where T : struct // Using 'struct' is common for performance
{
    // The same arrays as before for the sparse set logic
    private readonly int[] _dense = new int[capacity];
    private readonly int[] _sparse = new int[universeSize];

    // The new parallel array to store the actual component data
    private readonly T[] _components = new T[capacity];

    /// <summary>
    /// Gets the current number of components stored in this storage.
    /// </summary>
    public int Count { get; private set; } = 0;
    /// <summary>
    /// Gets the maximum number of components that can be stored in this storage.
    /// </summary>
    public int Capacity { get; } = capacity;
    /// <summary>
    /// Gets the maximum number of entities supported by this storage.
    /// </summary>
    public int UniverseSize { get; } = universeSize;

    // Provides direct, cache-friendly access for systems
    /// <summary>
    /// Gets a read-only span of the packed component data for efficient iteration.
    /// </summary>
    public ReadOnlySpan<T> PackedComponents => new(_components, 0, Count);
    /// <summary>
    /// Gets a read-only span of the packed entity IDs for efficient iteration.
    /// </summary>
    public ReadOnlySpan<int> PackedEntities => new(_dense, 0, Count);

    /// <summary>
    /// Adds a component of type <typeparamref name="T"/> for the specified entity ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity to add the component for.</param>
    /// <param name="component">The component data to add.</param>
    public void Add(int entityId, T component)
    {
        if (Count >= Capacity || entityId < 0 || entityId >= UniverseSize || Has(entityId))
        {
            return;
        }

        // The logic is identical, we just add one more step
        _dense[Count] = entityId;
        _sparse[entityId] = Count;
        _components[Count] = component; // <-- The new step!

        Count++;
    }

    /// <summary>
    /// Removes the component of type <typeparamref name="T"/> for the specified entity ID, if it exists.
    /// </summary>
    /// <param name="entityId">The ID of the entity whose component should be removed.</param>
    public void Remove(int entityId)
    {
        if (!Has(entityId))
        {
            return;
        }

        int indexOfEntity = _sparse[entityId];
        int lastEntity = _dense[Count - 1];
        T lastComponent = _components[Count - 1]; // <-- Get the last component

        // Perform the swap on all parallel arrays
        _dense[indexOfEntity] = lastEntity;
        _components[indexOfEntity] = lastComponent; // <-- The new step!
        _sparse[lastEntity] = indexOfEntity;

        Count--;
    }

    /// <summary>
    /// Determines whether a component of type <typeparamref name="T"/> exists for the specified entity ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity to check for a component.</param>
    /// <returns>True if the component exists for the entity; otherwise, false.</returns>
    public bool Has(int entityId)
    {
        if (entityId < 0 || entityId >= UniverseSize) return false;
        int indexInDense = _sparse[entityId];
        return indexInDense < Count && _dense[indexInDense] == entityId;
    }

    /// <summary>
    /// Gets a reference to the component of type <typeparamref name="T"/> for the specified entity ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity whose component should be retrieved.</param>
    /// <returns>A reference to the component associated with the specified entity ID.</returns>
    public ref T GetComponent(int entityId)
    {
        // Note: This assumes you've already checked Has(entityId)
        // Returning by 'ref' is a high-performance pattern that avoids copying structs.
        return ref _components[_sparse[entityId]];
    }

    /// <summary>
    /// Adds a new component or updates an existing one for the specified entity.
    /// This provides a convenient "upsert" (update or insert) operation.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="component">The component data to set.</param>
    public void Set(int entityId, T component)
    {
        if (Has(entityId))
        {
            // Entity already has the component, so update it in-place.
            _components[_sparse[entityId]] = component;
        }
        else
        {
            // Entity doesn't have the component, so add it.
            // The Add method already contains all the necessary checks (capacity, bounds, etc.).
            Add(entityId, component);
        }
    }

    /// <summary>
    /// Gets a readonly reference to the component for the specified entity ID.
    /// This is the preferred method for safe, high-performance read-only access as it avoids copying the struct.
    /// <para><b>Warning:</b> For maximum performance, this method does not perform a `Has()` check. The caller is responsible for ensuring the entity has the component before calling; otherwise, behavior is undefined and may lead to data corruption.</para>
    /// </summary>
    /// <param name="entityId">The ID of the entity whose component should be retrieved.</param>
    /// <returns>A readonly reference to the component.</returns>
    public ref readonly T Get(int entityId)
    {
        // Note: This returns a readonly reference. The caller cannot modify the component.
        // It assumes you've already checked Has(entityId).
        return ref _components[_sparse[entityId]];
    }

    /// <summary>
    /// Gets a mutable reference to the component for the specified entity ID.
    /// This method allows for direct, in-place modification of the component data, which is highly efficient.
    /// <para><b>Warning:</b> For maximum performance, this method does not perform a `Has()` check. The caller is responsible for ensuring the entity has the component before calling; otherwise, behavior is undefined and may lead to data corruption.</para>
    /// </summary>
    /// <param name="entityId">The ID of the entity whose component should be retrieved for modification.</param>
    /// <returns>A mutable reference to the component.</returns>
    public ref T GetMutable(int entityId)
    {
        // Note: This returns a mutable reference, allowing in-place modification.
        // It assumes you've already checked Has(entityId).
        return ref _components[_sparse[entityId]];
    }

    /// <summary>
    /// Tries to get the component for a specified entity.
    /// This method is safer than `Get()` or `GetMutable()` as it performs all necessary checks, but it involves copying the component struct into the `out` parameter. It is ideal for situations where safety is preferred over absolute performance.
    /// </summary>
    /// <param name="entityId">The ID of the entity.</param>
    /// <param name="component">When this method returns, contains the component associated with the specified entity, if the component is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
    /// <returns>true if the entity has the component; otherwise, false.</returns>
    public bool TryGetValue(int entityId, out T component)
    {
        if (Has(entityId))
        {
            // This creates a copy of the struct.
            component = _components[_sparse[entityId]];
            return true;
        }

        component = default;
        return false;
    }


    // --- Runtime/Non-Generic API Implementation ---

    void IComponentStorage.SetAsObject(int entityId, object componentData)
    {
        // This will throw an InvalidCastException if the type is wrong, which is desired behavior.
        Set(entityId, (T)componentData);
    }

    object IComponentStorage.GetAsObject(int entityId)
    {
        // This will box the struct into an object, creating a copy.
        return Get(entityId);
    }

    /// <summary>
    /// Explicitly implements the IComponentStorage.Remove method.
    /// </summary>
    void IComponentStorage.Remove(int entityId)
    {
        // This will call the public Remove(int entityId) method of this class.
        Remove(entityId);
    }
}