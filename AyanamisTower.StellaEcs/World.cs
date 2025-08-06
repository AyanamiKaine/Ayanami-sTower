using System;
using System.Collections.Generic;
using System.Linq;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// The main class that manages all entities, components, and systems.
/// It acts as the central hub for all ECS operations.
/// </summary>
public class World
{
    private readonly uint _maxEntities;
    private uint _nextEntityId;

    /// <summary>
    /// A queue of recycled entity IDs. When an entity is destroyed, its ID is added here.
    /// </summary>
    private readonly Queue<uint> _freeIds = new();

    /// <summary>
    /// Stores the generation for each entity ID. This is used to invalidate old entity handles.
    /// </summary>
    private readonly int[] _generations;

    /// <summary>
    /// Stores all component storages, keyed by their component type.
    /// </summary>
    private readonly Dictionary<Type, IComponentStorage> _storages = [];
    private readonly List<ISystem> _systems = [];
    private readonly Dictionary<string, IEntityFunction> _functions = [];

    /// <summary>
    /// Creates a default world with the max allowed entity number of 5000:
    /// DESIGN NOTE: Why is a low number of entites good? Imagine this
    /// You have 1 million entities in a world and want to find out which entities
    /// have the enemy tag component. You would have to traverse 1 million entity ids 
    /// to find out if they have it. While iterating is quite fast you will notice that 
    /// most entities do not share that many component together. In essenece that means when 
    /// iterating over entities and checking if they have a component the common default case
    /// will be that they dont have said component. A solution for that could be using an archetype ecs
    /// but that usually results in many fragemented tables. A good solution is using different ecs worlds.
    /// And not just one.
    /// </summary>
    /// <param name="maxEntities"></param>
    public World(uint maxEntities = 5000)
    {
        _maxEntities = maxEntities;
        _generations = new int[(int)maxEntities];
        // Initialize all generations to 1. A generation of 0 can be considered invalid.
        Array.Fill(_generations, 1);
    }

    /// <summary>
    /// Creates a new entity or recycles a destroyed one.
    /// </summary>
    /// <returns>A valid <see cref="Entity"/> handle.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the maximum number of entities is reached.</exception>
    public Entity CreateEntity()
    {
        uint id;
        if (_freeIds.Count > 0)
        {
            // Recycle an old ID.
            id = _freeIds.Dequeue();
        }
        else
        {
            // Use a new ID if no recycled IDs are available.
            if (_nextEntityId >= _maxEntities)
            {
                throw new InvalidOperationException($"Maximum number of entities ({_maxEntities}) reached.");
            }
            id = _nextEntityId;
            _nextEntityId++;
        }

        // Return the entity with the current generation for that ID.
        return new Entity(id, _generations[id], this);
    }

    /// <summary>
    /// Destroys an entity, making its handle invalid and removing all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    public void DestroyEntity(Entity entity)
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");

        // Remove all components associated with this entity.
        foreach (var storage in _storages.Values)
        {
            storage.Remove(entity);
        }

        // Invalidate the entity handle by incrementing the generation.
        // The next time this ID is used, it will have a new generation.
        _generations[entity.Id]++;

        // Add the ID to the free list for recycling.
        _freeIds.Enqueue(entity.Id);
    }

    /// <summary>
    /// Checks if an entity handle is currently valid (i.e., it has not been destroyed).
    /// </summary>
    /// <param name="entity">The entity handle to check.</param>
    /// <returns>True if the entity is valid, false otherwise.</returns>
    public bool IsEntityValid(Entity entity)
    {
        return entity.Id < _nextEntityId && _generations[entity.Id] == entity.Generation;
    }

    /// <summary>
    /// Sets or replaces a component for an entity.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="entity">The entity to which the component is attached.</param>
    /// <param name="component">The component instance.</param>
    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        var storage = GetOrCreateStorage<T>();
        storage.Set(entity, component);
    }

    /// <summary>
    /// Gets a copy of an entity's component. This is safe for concurrent reads.
    /// To apply changes, you must call SetComponent() with the modified copy.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="entity">The entity whose component to get.</param>
    /// <returns>A copy of the component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have the component.</exception>
    public T GetComponent<T>(Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");

        if (_storages.TryGetValue(typeof(T), out var storage))
        {
            return ((ComponentStorage<T>)storage).Get(entity);
        }
        throw new KeyNotFoundException($"Entity {entity} does not have component of type {typeof(T).Name} because the storage does not exist.");
    }

    /// <summary>
    /// Gets a mutable reference to an entity's component for direct, in-place modification.
    /// This is faster but not safe for concurrent writes without external locking.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="entity">The entity whose component to get.</param>
    /// <returns>A mutable reference to the component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity does not have the component.</exception>
    public ref T GetMutComponent<T>(Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        if (_storages.TryGetValue(typeof(T), out var storage))
        {
            return ref ((ComponentStorage<T>)storage).GetMut(entity);
        }
        throw new KeyNotFoundException($"Entity {entity} does not have component of type {typeof(T).Name} because the storage does not exist.");
    }

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent<T>(Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        if (_storages.TryGetValue(typeof(T), out var storage))
        {
            return storage.Has(entity);
        }
        return false;
    }

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="entity">The entity from which to remove the component.</param>
    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        if (_storages.TryGetValue(typeof(T), out var storage))
        {
            storage.Remove(entity);
        }
    }

    /// <summary>
    /// Finds all entities that have all of the specified component types.
    /// </summary>
    /// <param name="types">The component types to query for.</param>
    /// <returns>A lazily-evaluated sequence of entities that match the query.</returns>
    public IEnumerable<Entity> Query(params Type[] types)
    {
        if (types.Length == 0)
        {
            return [];
        }

        var relevantStorages = new List<IComponentStorage>(types.Length);
        foreach (var type in types)
        {
            if (_storages.TryGetValue(type, out var storage))
            {
                relevantStorages.Add(storage);
            }
            else
            {
                // If any of the required component storages don't even exist, no entities can match.
                return [];
            }
        }

        // Optimization: Find the smallest storage to iterate over.
        var smallestStorage = relevantStorages.MinBy(s => s.Count);

        if (smallestStorage == null) return [];

        // We only need to check against the *other* storages.
        var otherStorages = relevantStorages.Where(s => s != smallestStorage);

        // Filter the entities from the smallest storage.
        return smallestStorage.GetEntities().Where(entity => otherStorages.All(s => s.Has(entity)));
    }

    /// <summary>
    /// Gets the existing storage for a component type, or creates a new one if it doesn't exist.
    /// </summary>
    private ComponentStorage<T> GetOrCreateStorage<T>() where T : struct
    {
        var type = typeof(T);
        if (!_storages.TryGetValue(type, out var storage))
        {
            storage = new ComponentStorage<T>(initialCapacity: 256, maxEntities: _maxEntities);
            _storages[type] = storage;
        }
        return (ComponentStorage<T>)storage;
    }

    /// <summary>
    /// Adds a new system to the world.
    /// </summary>
    public void RegisterSystem(ISystem system)
    {
        _systems.Add(system);
        // Console.WriteLine($"[World] Registered System: {system.GetType().Name}");
    }

    /// <summary>
    /// The main update loop for the world. This will execute all registered systems.
    /// </summary>
    public void Update(float deltaTime)
    {
        foreach (var system in _systems)
        {
            system.Update(this, deltaTime);
        }
    }

    /// <summary>
    /// Registers a named function with the world.
    /// </summary>
    /// <param name="functionName">The public name to be used for invoking the function.</param>
    /// <param name="function">The object that implements the function's logic.</param>
    public void RegisterFunction(string functionName, IEntityFunction function)
    {
        if (_functions.ContainsKey(functionName))
        {
            // Console.WriteLine($"[Warning] Overwriting existing function: {functionName}");
        }
        _functions[functionName] = function;
        // Console.WriteLine($"[World] Registered Function: {functionName}");
    }

    /// <summary>
    /// Invokes a registered function by name on a specific entity.
    /// </summary>
    /// <param name="entity">The target entity for the function.</param>
    /// <param name="functionName">The name of the function to call.</param>
    /// <param name="parameters">A variable list of parameters to pass to the function.</param>
    public void InvokeFunction(Entity entity, string functionName, params object[] parameters)
    {
        if (!IsEntityValid(entity))
        {
            Console.WriteLine($"[Error] Cannot invoke function '{functionName}' on an invalid entity.");
            return;
        }

        if (_functions.TryGetValue(functionName, out var function))
        {
            try
            {
                function.Execute(entity, this, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Exception while executing function '{functionName}': {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[Error] Attempted to invoke unknown function: '{functionName}'");
        }
    }
}