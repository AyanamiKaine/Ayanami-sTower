using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Manages the storage for a single type of component using a sparse set.
/// A sparse set allows for $O(1)$ access, insertion, and removal of components for any entity.
/// </summary>
/// <typeparam name="T">The type of component to store.</typeparam>
public class ComponentStorage<T> : IComponentStorage
{
    /// <summary>
    /// A tightly packed array that stores the actual component data. This is the "dense" part of the sparse set.
    /// Iterating over this array is cache-friendly.
    /// </summary>
    private T[] _components;

    /// <summary>
    /// A parallel array to `_components` that stores the Entity for each component.
    /// This allows us to know which entity owns the component at a given dense index.
    /// </summary>
    private Entity[] _entities;

    /// <summary>
    /// Indexed by the entity's ID. It stores the index into the `_components` array (the dense index).
    /// A value of -1 indicates that the entity does not have this component. This is the "sparse" part of the set.
    /// </summary>
    private readonly int[] _sparse;

    /// <summary>
    /// Gets the current number of components stored.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the current capacity of the dense arrays. This will grow automatically.
    /// </summary>
    public int DenseCapacity => _components.Length;

    /// <summary>
    /// Gets the maximum number of entities supported by this storage.
    /// </summary>
    public uint MaxEntities { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentStorage{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">The initial number of components the storage can hold before resizing.</param>
    /// <param name="maxEntities">The maximum number of entities the world can have. This defines the size of the sparse array.</param>
    public ComponentStorage(int initialCapacity = 256, uint maxEntities = 100_000)
    {
        // Use `int` for capacity and count to align with C# array lengths and common practices.
        _components = new T[initialCapacity];
        _entities = new Entity[initialCapacity];
        MaxEntities = maxEntities;

        // The sparse array's size is fixed by maxEntities.
        _sparse = new int[maxEntities];
        // It's crucial to initialize sparse with a sentinel value (-1) to indicate "no component".
        Array.Fill(_sparse, -1);
    }

    /// <summary>
    /// Checks if a given entity has a component in this storage.
    /// This is an $O(1)$ operation.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns><c>true</c> if the entity has the component; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Entity entity)
    {
        uint sparseIndex = entity.Id;
        // Ensure the entity ID is within the bounds of the sparse array.
        if (sparseIndex >= MaxEntities) return false;

        int denseIndex = _sparse[sparseIndex];

        // A valid entry must:
        // 1. Not be the sentinel value (-1).
        // 2. Point to a valid index within the current count of dense elements.
        // 3. The stored entity must match on Id and Generation. We intentionally
        //    do not require the World reference to be identical because entity
        //    handles can be reconstructed (e.g., created from World state or via
        //    REST/UI) while still referring to the same logical entity. Requiring
        //    World equality caused false negatives after remove/re-add of tag components.
        if (denseIndex == -1 || denseIndex >= Count) return false;
        var stored = _entities[denseIndex];
        return stored.Id == entity.Id && stored.Generation == entity.Generation;
    }

    /// <summary>
    /// Sets or replaces a component for a given entity.
    /// This is an $O(1)$ operation (amortized, due to potential resizing).
    /// </summary>
    /// <param name="entity">The entity to which the component will be attached.</param>
    /// <param name="component">The component data to set.</param>
    public void Set(Entity entity, T component)
    {
        if (Has(entity))
        {
            // If the entity already has this component, just replace the data.
            int localDenseIndex = _sparse[entity.Id];
            _components[localDenseIndex] = component;
            return;
        }

        // The entity does not have the component, so add a new one.
        // First, check if our dense arrays are full.
        if (Count == DenseCapacity)
        {
            // If so, double their size.
            int newCapacity = Count == 0 ? 4 : Count * 2; // Start with 4 if empty.
            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _entities, newCapacity);
        }

        int denseIndex = Count;
        uint sparseIndex = entity.Id;

        // Place the new component and its entity at the end of the dense arrays.
        _components[denseIndex] = component;
        _entities[denseIndex] = entity;

        // Update the sparse array to point to the new location in the dense array.
        _sparse[sparseIndex] = denseIndex;

        Count++;
    }

    /// <summary>
    /// Gets a copy of the component for a given entity. This is safe for concurrent reads.
    /// </summary>
    /// <param name="entity">The entity whose component to retrieve.</param>
    /// <returns>A copy of the component data for the entity.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have a component of this type.</exception>
    public T Get(Entity entity)
    {
        // This implicitly dereferences the ref, creating a copy.
        return GetMut(entity);
    }

    /// <summary>
    /// Gets a mutable reference to the component for a given entity for direct, in-place modification.
    /// </summary>
    /// <param name="entity">The entity whose component to retrieve.</param>
    /// <returns>A mutable reference to the component data for the entity.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have a component of this type.</exception>
    public ref T GetMut(Entity entity)
    {
        if (!Has(entity))
        {
            throw new KeyNotFoundException($"Entity {entity} does not have a component of type {typeof(T).Name}.");
        }
        int denseIndex = _sparse[entity.Id];
        return ref _components[denseIndex];
    }

    /// <summary>
    /// Removes a component from an entity.
    /// This is an $O(1)$ "swap and pop" operation.
    /// </summary>
    /// <param name="entity">The entity from which to remove the component.</param>
    public void Remove(Entity entity)
    {
        if (!Has(entity)) return;

        // Get the indices for the entity we want to remove.
        uint sparseIndexToRemove = entity.Id;
        int denseIndexToRemove = _sparse[sparseIndexToRemove];

        // Get the info for the *last* element in the dense array.
        int lastDenseIndex = Count - 1;
        Entity lastEntity = _entities[lastDenseIndex];

        // --- The "Swap and Pop" ---
        // 1. Move the last element into the position of the element we are removing.
        _components[denseIndexToRemove] = _components[lastDenseIndex];
        _entities[denseIndexToRemove] = lastEntity;

        // 2. Update the sparse array for the moved entity to point to its new dense index.
        _sparse[lastEntity.Id] = denseIndexToRemove;

        // 3. Invalidate the sparse entry for the removed entity.
        _sparse[sparseIndexToRemove] = -1;

        // 4. Clear the now-unused last element to allow the GC to collect it if it's a reference type.
        _components[lastDenseIndex] = default!;
        _entities[lastDenseIndex] = default; // or Entity.Null

        Count--;
    }

    /// <summary>
    /// Gets an enumerable collection of all entities that have a component in this storage.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Entity}"/> that can be iterated over.</returns>
    public IEnumerable<Entity> GetEntities()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return _entities[i];
        }
    }

    /// <summary>
    /// Gets a read-only span of the tightly-packed components.
    /// Useful for high-performance, cache-friendly iteration in systems.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> over the components.</returns>
    public ReadOnlySpan<T> GetComponentsSpan()
    {
        return new ReadOnlySpan<T>(_components, 0, Count);
    }

    /// <summary>
    /// Gets the data associated with an entity as an object.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public object? GetDataAsObject(Entity entity)
    {
        // We can't just call Get(entity) because it might throw if the entity
        // doesn't have the component in this specific storage.
        if (Has(entity))
        {
            // Boxing occurs here, which is acceptable for a debug/inspection API.
            return Get(entity);
        }
        return null;
    }
}