using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a unique entity in the world using an ID and a generation.
/// The generation helps to invalidate handles to entities that have been destroyed.
/// This is a lightweight, immutable struct.
/// </summary>
public readonly struct Entity(int id, int generation, World? world) : IEquatable<Entity>
{
    /// <summary>
    /// A null/invalid entity handle.
    /// </summary>
    public static readonly Entity Null = new(-1, 0, null);

    /// <summary>
    /// The raw integer ID of the entity. This corresponds to an index in the world's arrays.
    /// </summary>
    public readonly int Id = id;

    /// <summary>
    /// The generation of the entity, which is incremented each time the ID is recycled.
    /// </summary>
    public readonly int Generation = generation;
    private readonly World? _world = world;

    /// <summary>
    /// Checks if this entity handle is "alive" and valid.
    /// A handle is alive if its generation matches the world's current generation for that ID.
    /// </summary>
    /// <returns>True if the entity is alive; otherwise, false.</returns>
    public bool IsAlive() => _world?.IsAlive(this) ?? false;

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    public void Add<T>(T component) where T : struct => _world?.AddComponent(this, component);

    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    public void Remove<T>() where T : struct => _world?.RemoveComponent<T>(this);

    /// <summary>
    /// Checks if this entity has a specific component.
    /// </summary>
    public bool Has<T>() where T : struct => _world?.HasComponent<T>(this) ?? false;

    /// <summary>
    /// Gets a readonly reference to this entity's component.
    /// </summary>
    public ref readonly T Get<T>() where T : struct => ref _world!.GetComponent<T>(this);

    /// <summary>
    /// Gets a mutable reference to this entity's component for in-place modification.
    /// </summary>
    public ref T GetMutable<T>() where T : struct => ref _world!.GetComponentMutable<T>(this);

    /// <summary>
    /// Adds a new component or updates an existing one for this entity.
    /// </summary>
    public void Set<T>(T component) where T : struct => _world?.SetComponent(this, component);

    /// <summary>
    /// Destroys this entity, removing all its components and recycling its ID.
    /// </summary>
    public void Destroy() => _world?.DestroyEntity(this);

    // --- Overloads and Implementation for Equality ---

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="Entity"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="Entity"/>.</param>
    /// <returns><c>true</c> if the specified object is an <see cref="Entity"/> and is equal to the current <see cref="Entity"/>; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);
    /// <summary>
    /// Determines whether the specified <see cref="Entity"/> is equal to the current <see cref="Entity"/>.
    /// </summary>
    /// <param name="other">The <see cref="Entity"/> to compare with the current <see cref="Entity"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="Entity"/> is equal to the current <see cref="Entity"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;
    /// <summary>
    /// Returns a hash code for the current <see cref="Entity"/>.
    /// </summary>
    /// <returns>A hash code for the current <see cref="Entity"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(Id, Generation);
    /// <summary>
    /// Determines whether two <see cref="Entity"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="Entity"/> to compare.</param>
    /// <param name="right">The second <see cref="Entity"/> to compare.</param>
    /// <returns><c>true</c> if the entities are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    /// <summary>
    /// Determines whether two <see cref="Entity"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="Entity"/> to compare.</param>
    /// <param name="right">The second <see cref="Entity"/> to compare.</param>
    /// <returns><c>true</c> if the entities are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Entity left, Entity right) => !(left == right);
    /// <summary>
    /// Returns a string that represents the current <see cref="Entity"/>.
    /// </summary>
    /// <returns>A string representation of the current <see cref="Entity"/>.</returns>
    public override string ToString() => $"Entity(Id: {Id}, Gen: {Generation})";
}
