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

    // --- Runtime/Non-Generic API Methods ---
    /// <summary>
    /// Adds a component to this entity using its runtime <see cref="Type"/>.
    /// This is an alias for <see cref="Set(Type, object)"/>.
    /// </summary>
    /// <param name="componentType">The runtime type of the component to add.</param>
    /// <param name="componentData">The component data as a boxed object. The object's type must match the component type.</param>
    public void Add(Type componentType, object componentData) => _world?.AddComponent(this, componentType, componentData);

    /// <summary>
    /// Removes a component from this entity using its runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="componentType">The runtime type of the component to remove.</param>
    public void Remove(Type componentType) => _world?.RemoveComponent(this, componentType);

    /// <summary>
    /// Checks if this entity has a specific component, using its runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="componentType">The runtime type of the component to check for.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    public bool Has(Type componentType) => _world?.HasComponent(this, componentType) ?? false;

    /// <summary>
    /// Gets a component from this entity as a boxed object, using its runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="componentType">The runtime type of the component to retrieve.</param>
    /// <returns>The component data as a boxed object.</returns>
    public object Get(Type componentType) => _world!.GetComponent(this, componentType);

    /// <summary>
    /// Adds a new component or updates an existing one for this entity, using its runtime <see cref="Type"/>.
    /// </summary>
    /// <param name="componentType">The runtime type of the component to set.</param>
    /// <param name="componentData">The component data as a boxed object. The object's type must match the component type.</param>
    public void Set(Type componentType, object componentData) => _world?.SetComponent(this, componentType, componentData);

    /// <summary>
    /// Adds a relationship of type <typeparamref name="T"/> from this entity to a target entity.
    /// If the relationship is bidirectional, the reverse relationship (target -> this) is also added.
    /// </summary>
    /// <typeparam name="T">The type of the relationship, which must implement <see cref="IRelationship"/>.</typeparam>
    /// <param name="target">The entity to which the relationship is directed.</param>
    public void AddRelationship<T>(Entity target) where T : struct, IRelationship
        => _world?.AddRelationship<T>(this, target);

    /// <summary>
    /// Removes a relationship of type <typeparamref name="T"/> from this entity to a target entity.
    /// If the relationship is bidirectional, the reverse relationship (target -> this) is also removed.
    /// </summary>
    /// <typeparam name="T">The type of the relationship, which must implement <see cref="IRelationship"/>.</typeparam>
    /// <param name="target">The entity from which the relationship is directed.</param>
    public void RemoveRelationship<T>(Entity target) where T : struct, IRelationship
        => _world?.RemoveRelationship<T>(this, target);

    /// <summary>
    /// Checks if a relationship of type <typeparamref name="T"/> exists from this entity to a target entity.
    /// </summary>
    /// <typeparam name="T">The type of the relationship, which must implement <see cref="IRelationship"/>.</typeparam>
    /// <param name="target">The target entity of the relationship.</param>
    /// <returns><c>true</c> if the relationship exists; otherwise, <c>false</c>.</returns>
    public bool HasRelationship<T>(Entity target) where T : struct, IRelationship
        => _world?.HasRelationship<T>(this, target) ?? false;

    /// <summary>
    /// Gets all entities that this entity has a relationship of type <typeparamref name="T"/> with.
    /// </summary>
    /// <typeparam name="T">The type of the relationship, which must implement <see cref="IRelationship"/>.</typeparam>
    /// <returns>An enumerable collection of target entities.</returns>
    public IEnumerable<Entity> GetRelationshipTargets<T>() where T : struct, IRelationship
        => _world?.GetRelationshipTargets<T>(this) ?? Enumerable.Empty<Entity>();


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
