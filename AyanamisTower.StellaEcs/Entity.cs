using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a unique entity in the world using an ID and a generation.
/// The generation helps to invalidate handles to entities that have been destroyed.
/// This is a lightweight, immutable struct.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// A null/invalid entity handle.
    /// </summary>
    public static readonly Entity Null = new(0, 0, null);

    /// <summary>
    /// The raw integer ID of the entity. This corresponds to an index in the world's arrays.
    /// </summary>
    public readonly uint Id;

    /// <summary>
    /// The generation of the entity, which is incremented each time the ID is recycled.
    /// </summary>
    public readonly int Generation;

    /// <summary>
    /// A reference to the world this entity belongs to. This is necessary for the helper methods.
    /// </summary>
    private readonly World? _world;

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> struct.
    /// </summary>
    /// <param name="id">The entity's ID.</param>
    /// <param name="generation">The entity's generation.</param>
    /// <param name="world">The world the entity belongs to.</param>
    public Entity(uint id, int generation, World? world)
    {
        Id = id;
        Generation = generation;
        _world = world;
    }

    // --- New Helper Methods ---

    /// <summary>
    /// Sets or replaces a component for this entity.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="component">The component instance to set.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not associated with a world.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(T component) where T : struct
    {
        if (_world == null) throw new InvalidOperationException("Cannot set component on an entity that is not associated with a world.");
        _world.SetComponent(this, component);
    }

    /// <summary>
    /// Gets a copy of this entity's component. This is safe for concurrent reads.
    /// To apply changes to the component, you must call Set() with the modified copy.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>A copy of the component.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not associated with a world.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have the component.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get<T>() where T : struct
    {
        if (_world == null) throw new InvalidOperationException("Cannot get component from an entity that is not associated with a world.");
        // Assumes World.GetComponent<T> returns a copy.
        return _world.GetComponent<T>(this);
    }

    /// <summary>
    /// Gets a mutable reference to this entity's component for direct, in-place modification.
    /// This is faster for modification but is not safe for concurrent writes without external locking.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>A mutable reference to the component.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not associated with a world.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have the component.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetMut<T>() where T : struct
    {
        if (_world == null) throw new InvalidOperationException("Cannot get component from an entity that is not associated with a world.");
        // Assumes World has a corresponding GetMutComponent<T> that returns a ref.
        return ref _world.GetMutComponent<T>(this);
    }

    /// <summary>
    /// Checks if this entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct
    {
        // If the world is null or the entity is invalid, it can't have components.
        return _world != null && _world.HasComponent<T>(this);
    }

    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not associated with a world.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove<T>() where T : struct
    {
        if (_world == null) throw new InvalidOperationException("Cannot remove component from an entity that is not associated with a world.");
        _world.RemoveComponent<T>(this);
    }

    /// <summary>
    /// Destroys this entity, removing all its components and recycling its ID.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the entity is not associated with a world.</exception>
    public void Destroy()
    {
        if (_world == null) throw new InvalidOperationException("Cannot destroy an entity that is not associated with a world.");
        _world.DestroyEntity(this);
    }

    /// <summary>
    /// Checks if this entity handle is currently valid (i.e., it has not been destroyed).
    /// </summary>
    /// <returns>True if the entity is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return _world?.IsEntityValid(this) == true;
    }


    // --- Overloads and Implementation for Equality ---

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Entity"/>.
    /// </summary>
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    /// <summary>
    /// Determines whether the specified <see cref="Entity"/> is equal to the current <see cref="Entity"/>.
    /// </summary>
    public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation && _world == other._world;

    /// <summary>
    /// Returns a hash code for the current <see cref="Entity"/>.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Id, Generation, _world);

    /// <summary>
    /// Determines whether two <see cref="Entity"/> instances are equal.
    /// </summary>
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="Entity"/> instances are not equal.
    /// </summary>
    public static bool operator !=(Entity left, Entity right) => !(left == right);

    /// <summary>
    /// Returns a string that represents the current <see cref="Entity"/>.
    /// </summary>
    public override string ToString() => $"Entity(Id: {Id}, Gen: {Generation})";
}
