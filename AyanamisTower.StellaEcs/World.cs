using System;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// The central class in the ECS framework that manages all entities and their components.
/// It handles the entity lifecycle and provides a unified API for component manipulation.
/// </summary>
public class World
{
    // --- Fields ---
    private readonly int _maxEntities;
    private int _nextEntityId = 0;

    // A queue for recycling entity IDs to keep the set of IDs compact.
    private readonly Queue<int> _recycledEntityIds = new();

    // The core of the World: A dictionary mapping a component Type to its storage object.
    private readonly Dictionary<Type, IComponentStorage> _componentStorages = [];
    /// <summary>
    /// Stores the current generation for each entity ID.
    /// </summary>
    private readonly int[] _entityGenerations;
    // --- Constructor ---

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    /// <param name="maxEntities">The maximum number of entities this world can support. By Default 1 Million</param>
    public World(int maxEntities = 1_000_000)
    {
        if (maxEntities <= 0) throw new ArgumentOutOfRangeException(nameof(maxEntities), "Maximum entities must be positive.");
        _maxEntities = maxEntities;
        // Initialize the generations array
        _entityGenerations = new int[maxEntities];
    }

    // --- Entity Lifecycle Management ---

    /// <summary>
    /// Creates a new entity and returns its unique ID.
    /// It will reuse a destroyed entity ID if one is available.
    /// </summary>
    /// <returns>The integer ID of the newly created entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the world has reached its maximum entity capacity.</exception>
    public Entity CreateEntity()
    {
        if (_recycledEntityIds.Count > 0)
        {
            int id = _recycledEntityIds.Dequeue();
            return new Entity(id, _entityGenerations[id], this);
        }

        if (_nextEntityId >= _maxEntities)
        {
            throw new InvalidOperationException("Cannot create new entity: World has reached maximum capacity.");
        }

        int newId = _nextEntityId++;
        // A brand new entity still starts with generation 1. This part is correct.
        _entityGenerations[newId] = 1;
        return new Entity(newId, _entityGenerations[newId], this);
    }

    /// <summary>
    /// Checks if an entity handle is "alive" and valid.
    /// A handle is alive if its generation matches the world's current generation for that ID.
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        // Check if the ID is valid and the generation matches.
        return entity.Id >= 0
            && entity.Id < _maxEntities
            && _entityGenerations[entity.Id] == entity.Generation;
    }

    /// <summary>
    /// Destroys an entity and removes all of its associated components from all storages.
    /// The entity's ID will be recycled for future use.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    public void DestroyEntity(Entity entity)
    {
        // Only destroy if the handle is valid and alive
        if (!IsAlive(entity)) return;

        // Remove all components
        foreach (var storage in _componentStorages.Values)
        {
            storage.Remove(entity.Id);
        }

        // Invalidate the handle by incrementing the generation and recycle the ID.
        _entityGenerations[entity.Id]++;
        _recycledEntityIds.Enqueue(entity.Id);
    }

    /// <summary>
    /// Gets a full Entity handle from a raw entity ID.
    /// This is used internally to reconstruct handles during queries.
    /// </summary>
    public Entity GetEntityFromId(int entityId)
    {
        if (entityId < 0 || entityId >= _maxEntities) return Entity.Null;
        return new Entity(entityId, _entityGenerations[entityId], this);
    }

    // --- Component Management ---

    /// <summary>
    /// Registers a component type with the world, creating its underlying storage.
    /// This must be called for each component type before it can be used.
    /// </summary>
    /// <typeparam name="T">The type of component to register.</typeparam>
    /// <param name="capacity">Optional: The maximum number of this component type to store. Defaults to the world's max entities.</param>
    public void RegisterComponent<T>(int? capacity = null) where T : struct
    {
        var componentType = typeof(T);
        if (_componentStorages.ContainsKey(componentType))
        {
            // Or throw an exception, depending on desired behavior
            return;
        }

        var storageCapacity = capacity ?? _maxEntities;
        var newStorage = new ComponentStorage<T>(storageCapacity, _maxEntities);
        _componentStorages.Add(componentType, newStorage);
    }

    /// <summary>
    /// Checks if a component type has been registered with the world.
    /// </summary>
    /// <param name="componentType">The component type to check.</param>
    /// <returns>True if the component type is registered; otherwise, false.</returns>
    public bool IsComponentRegistered(Type componentType)
    {
        return _componentStorages.ContainsKey(componentType);
    }

    /// <summary>
    /// Retrieves the underlying storage for a given component type.
    /// This is useful for systems that need to iterate over all components of a certain type.
    /// </summary>
    /// <typeparam name="T">The component type whose storage to retrieve.</typeparam>
    /// <returns>The ComponentStorage for the given type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the component type could not have been automatically registered.</exception>
    public ComponentStorage<T> GetStorage<T>() where T : struct
    {
        var componentType = typeof(T);
        if (!_componentStorages.TryGetValue(componentType, out var storage))
        {
            RegisterComponent<T>();

            if (!_componentStorages.TryGetValue(componentType, out var newlyCreatedStorage))
            {
                throw new InvalidOperationException($"Component type '{componentType.Name}' has not been registered. Even though this should have happened automatically you may need to Call RegisterComponent<{componentType.Name}>() manually first.");
            }
            return (ComponentStorage<T>)newlyCreatedStorage;
        }
        return (ComponentStorage<T>)storage;
    }

    /// <summary>
    /// Internal, non-generic version of GetStorage for the query system.
    /// </summary>
    internal IComponentStorage GetStorageUnsafe(Type componentType)
    {
        if (!_componentStorages.TryGetValue(componentType, out var storage))
        {
            throw new InvalidOperationException($"Component type '{componentType.Name}' has not been registered.");
        }
        return storage;
    }

    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    public void AddComponent<T>(Entity entity, T component) where T : struct
    {
        if (IsAlive(entity)) GetStorage<T>().Add(entity.Id, component);
    }
    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        if (IsAlive(entity)) GetStorage<T>().Remove(entity.Id);
    }
    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    public bool HasComponent<T>(Entity entity) where T : struct
    {
        return IsAlive(entity) && GetStorage<T>().Has(entity.Id);
    }
    /// <summary>
    /// Gets a readonly reference to an entity's component.
    /// </summary>
    public ref readonly T GetComponent<T>(Entity entity) where T : struct
    {
        if (!IsAlive(entity)) throw new InvalidOperationException("Attempted to get component from a dead entity.");
        return ref GetStorage<T>().Get(entity.Id);
    }
    /// <summary>
    /// Gets a mutable reference to an entity's component for in-place modification.
    /// </summary>
    public ref T GetComponentMutable<T>(Entity entity) where T : struct
    {
        if (!IsAlive(entity)) throw new InvalidOperationException("Attempted to get component from a dead entity.");
        return ref GetStorage<T>().GetMutable(entity.Id);
    }
    /// <summary>
    /// Adds a new component or updates an existing one for the specified entity.
    /// </summary>
    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        if (IsAlive(entity)) GetStorage<T>().Set(entity.Id, component);
    }

    // --- Runtime/Non-Generic API ---

    /// <summary>
    /// Registers a component type with the world at runtime, creating its underlying storage.
    /// This overload allows registration using a <see cref="Type"/> object.
    /// </summary>
    /// <param name="componentType">The type of component to register (must be a non-primitive struct).</param>
    /// <param name="capacity">Optional: The maximum number of this component type to store. Defaults to the world's max entities.</param>
    /// <exception cref="ArgumentException">Thrown if the component type is not a non-primitive struct.</exception>
    public void RegisterComponent(Type componentType, int? capacity = null)
    {
        if (!componentType.IsValueType || componentType.IsPrimitive)
        {
            throw new ArgumentException("Component type must be a non-primitive struct.", nameof(componentType));
        }
        if (_componentStorages.ContainsKey(componentType)) return;

        var storageType = typeof(ComponentStorage<>).MakeGenericType(componentType);
        var storageCapacity = capacity ?? _maxEntities;
        var newStorage = Activator.CreateInstance(storageType, storageCapacity, _maxEntities);
        _componentStorages.Add(componentType, (IComponentStorage)newStorage!);
    }

    /// <summary>
    /// Adds a component of the specified type to an entity using runtime type information.
    /// </summary>
    /// <param name="entity">The entity to which the component will be added.</param>
    /// <param name="componentType">The type of the component to add.</param>
    /// <param name="componentData">The component data to add.</param>
    public void AddComponent(Entity entity, Type componentType, object componentData) => SetComponent(entity, componentType, componentData);
    /// <summary>
    /// Removes a component of the specified type from an entity using runtime type information.
    /// </summary>
    /// <param name="entity">The entity from which the component will be removed.</param>
    /// <param name="componentType">The type of the component to remove.</param>
    public void RemoveComponent(Entity entity, Type componentType)
    {
        if (IsAlive(entity) && _componentStorages.TryGetValue(componentType, out var storage))
        {
            storage.Remove(entity.Id);
        }
    }
    /// <summary>
    /// Checks if an entity has a specific component of the given type using runtime type information.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentType">The type of the component to check for.</param>
    /// <returns>True if the entity has the specified component; otherwise, false.</returns>
    public bool HasComponent(Entity entity, Type componentType)
    {
        return IsAlive(entity) && _componentStorages.TryGetValue(componentType, out var storage) && storage.Has(entity.Id);
    }
    /// <summary>
    /// Gets the component of the specified type from an entity using runtime type information.
    /// </summary>
    /// <param name="entity">The entity from which to retrieve the component.</param>
    /// <param name="componentType">The type of the component to retrieve.</param>
    /// <returns>The component object if present; otherwise, throws an exception if the entity is not alive.</returns>
    public object GetComponent(Entity entity, Type componentType)
    {
        if (!IsAlive(entity)) throw new InvalidOperationException("Attempted to get component from a dead entity.");
        return GetStorageUnsafe(componentType).GetAsObject(entity.Id);
    }
    /// <summary>
    /// Adds a new component or updates an existing one for the specified entity using runtime type information.
    /// </summary>
    /// <param name="entity">The entity to which the component will be added or updated.</param>
    /// <param name="componentType">The type of the component to add or update.</param>
    /// <param name="componentData">The component data to set.</param>
    public void SetComponent(Entity entity, Type componentType, object componentData)
    {
        if (IsAlive(entity)) GetStorageUnsafe(componentType).SetAsObject(entity.Id, componentData);
    }

    /// <summary>
    /// Creates a new <see cref="QueryBuilder"/> for building and executing entity queries in this world.
    /// </summary>
    /// <returns>A new instance of <see cref="QueryBuilder"/> associated with this world.</returns>
    public QueryBuilder Query() => new(this);
}
