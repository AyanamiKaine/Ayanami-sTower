using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AyanamisTower.StellaEcs;


/*
TODO: Implement sqlite for storing relationships between entities.
While we can use the components to model relationships, we might want to have a more complex relationship between entities.
*/

// TODO: I would be nice to have much more meta data for plugins and the ecs world in general.
// For example, we could have a plugin version, author, description, etc.
// It would then be possible to have a plugin manager that can handle updates, dependencies, etc.
// Also we could have a http server that represents the world state and allows remote clients to interact with it.
// We could also have a web interface that allows users to interact with the world, view entities, components, systems, etc.
// We could see the world as a graph of entities, components, and systems. 

/// <summary>
/// The main class that manages all entities, components, and systems.
/// It acts as the central hub for all ECS operations.
/// </summary>
public class World
{
    private readonly ILogger _logger;
    /// <summary>
    /// The current tick of the world. This is incremented on each update by one.
    /// System can use it to determine the current frame or update cycle. Systems can determine if they want to run every tick or only every second, third and so on.
    /// </summary>
    public uint Tick { get; private set; }
    /// <summary>
    /// The delta time value provided to the most recent Update call.
    /// </summary>
    public float LastDeltaTime { get; private set; }
    /// <summary>
    /// Indicates whether the world is currently paused. When paused, Update() is a no-op.
    /// Use Step() to advance frames while paused.
    /// </summary>
    public bool IsPaused { get; private set; }
    private readonly uint _maxEntities;
    /// <summary>
    /// Entity ID counter. This is incremented each time a new entity is created.
    /// It starts at 1 because 0 is reserved for the Null entity.
    /// </summary>
    private uint _nextEntityId = 1;

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
    /// Stores dynamic components keyed by a string name. Each name maps to a dictionary from Entity to arbitrary data.
    /// Dynamic components trade performance for iteration speed and flexibility during prototyping.
    /// </summary>
    private readonly Dictionary<string, Dictionary<Entity, object?>> _dynamicComponents = new(StringComparer.Ordinal);

    /// <summary>
    /// Tracks the set of currently alive (valid) entity IDs.
    /// </summary>
    private readonly HashSet<uint> _activeEntityIds = new();

    // Note: REST API integration was removed from the core ECS to avoid leaking web concerns.

    // --- NEW: System Management ---
    private readonly List<ISystem> _unmanagedSystems = [];
    private bool _isSystemOrderDirty = true; // Flag to trigger sorting

    // --- NEW: System Group Lists ---
    // These lists will hold the final, sorted systems for execution.
    /// <summary>
    /// Initialization Phase: Systems that create entities, set up state (e.g., SpawnPlayerSystem).
    /// </summary>
    private List<ISystem> _initializationSystems = [];
    /// <summary>
    /// Simulation Phase: The main game logic systems (e.g., Input, AI, Physics, Movement)
    /// </summary>
    private List<ISystem> _simulationSystems = [];
    /// <summary>
    /// Systems that prepare data for rendering (e.g., Animation, Camera, UIRendering).
    /// </summary>
    private List<ISystem> _presentationSystems = [];

    /*
    Decoupling: A plugin can request a service by its interface (IPathfindingService) without needing a direct reference to the plugin that provides it (SuperPathfindingPlugin.dll).

    Extensibility: Any plugin can provide an implementation for a known service, allowing users to swap out, for example, a basic physics engine with a more advanced one just by changing plugins.

    Centralized Logic: Complex logic that doesn't fit neatly into an ECS system (like scene management, complex UI state, or pathfinding) can be encapsulated in a service.

    There is a chance that this will be replaced by a more complex service locator pattern in the future.
    This is a simple implementation that allows plugins to register and consume services.
    */

    // TODO: Implement a logger service that can be used by plugins to log messages.

    /// <summary>
    /// A Shared Service Hub (also known as a Service Locator) allows plugins to register and consume persistent, high-level services, enabling direct and powerful cross-plugin communication
    /// </summary>
    private readonly Dictionary<Type, object> _services = [];

    /*
    Idea used by https://github.com/MoonsideGames/MoonTools.ECS
    CHECK IT OUT!
    */

    /// <summary>
    /// Stores all message buses, keyed by their message type.
    /// </summary>
    private readonly Dictionary<Type, IMessageBus> _messageBuses = [];


    // --- NEW: Ownership Tracking ---
    private readonly Dictionary<string, IPlugin> _pluginsByPrefix = [];
    private readonly Dictionary<Type, IPlugin> _systemOwners = [];
    private readonly Dictionary<Type, IPlugin> _serviceOwners = [];
    private readonly Dictionary<Type, IPlugin> _componentOwners = [];

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
    /// <param name="logger">Optional logger; if not provided, a no-op logger is used.</param>
    public World(uint maxEntities = 5000, ILogger? logger = null)
    {
        _maxEntities = maxEntities;
        _generations = new int[(int)maxEntities];
        // Initialize all generations to 1. A generation of 0 can be considered invalid.
        Array.Fill(_generations, 1);
        _logger = logger ?? NullLogger.Instance;
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

        // Mark entity as alive and return the handle.
        _activeEntityIds.Add(id);
        _logger.LogTrace("Created entity {EntityId}", id);
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

        // Remove all dynamic components for this entity.
        foreach (var map in _dynamicComponents.Values)
        {
            map.Remove(entity);
        }

        // Mark as not alive.
        _activeEntityIds.Remove(entity.Id);

        // Invalidate the entity handle by incrementing the generation
        // (for future use if/when entity handles carry generation).
        _generations[entity.Id]++;

        // Add the ID to the free list for recycling.
        _freeIds.Enqueue(entity.Id);
        _logger.LogTrace("Destroyed entity {EntityId}", entity.Id);
    }

    /// <summary>
    /// Checks if an entity handle is currently valid (i.e., it has not been destroyed).
    /// </summary>
    /// <param name="entity">The entity handle to check.</param>
    /// <returns>True if the entity is valid, false otherwise.</returns>
    public bool IsEntityValid(Entity entity)
    {
        // Valid if alive and generation matches the world's current generation for this id.
        return _activeEntityIds.Contains(entity.Id) && _generations[entity.Id] == entity.Generation;
    }

    /// <summary>
    /// Registers a new component type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void RegisterComponent<T>() where T : struct
    {
        GetOrCreateStorage<T>();
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
    /// Finds all entities that have all of the specified dynamic component names.
    /// </summary>
    /// <param name="componentNames">The dynamic component names to query for.</param>
    /// <returns>A lazily-evaluated sequence of entities that match the query.</returns>
    public IEnumerable<Entity> QueryDynamic(params string[] componentNames)
    {
        if (componentNames == null || componentNames.Length == 0)
        {
            return [];
        }

        // Build a list of maps for the requested names; if any is missing, no matches.
        var maps = new List<Dictionary<Entity, object?>>(componentNames.Length);
        foreach (var name in componentNames)
        {
            if (!_dynamicComponents.TryGetValue(name, out var map))
            {
                return [];
            }
            maps.Add(map);
        }

        // Iterate the smallest map for efficiency.
        var smallest = maps.MinBy(m => m.Count);
        if (smallest == null || smallest.Count == 0) return [];

        var others = maps.Where(m => !ReferenceEquals(m, smallest)).ToArray();
        return smallest.Keys.Where(e => others.All(m => m.ContainsKey(e)));
    }

    /// <summary>
    /// Sets or replaces a dynamic component by name for an entity.
    /// </summary>
    /// <param name="entity">The target entity.</param>
    /// <param name="name">Dynamic component name.</param>
    /// <param name="data">Arbitrary data to associate.</param>
    public void SetDynamicComponent(Entity entity, string name, object? data)
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (!_dynamicComponents.TryGetValue(name, out var map))
        {
            map = new Dictionary<Entity, object?>();
            _dynamicComponents[name] = map;
        }
        map[entity] = data;
    }

    /// <summary>
    /// Returns true if the entity has a dynamic component with the given name.
    /// </summary>
    public bool HasDynamicComponent(Entity entity, string name)
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _dynamicComponents.TryGetValue(name, out var map) && map.ContainsKey(entity);
    }

    /// <summary>
    /// Removes a dynamic component by name from an entity. No-op if not present.
    /// </summary>
    public void RemoveDynamicComponent(Entity entity, string name)
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_dynamicComponents.TryGetValue(name, out var map))
        {
            map.Remove(entity);
        }
    }

    /// <summary>
    /// Gets the dynamic component value as object. Throws if not present.
    /// </summary>
    public object? GetDynamicComponent(Entity entity, string name)
    {
        if (!IsEntityValid(entity))
            throw new ArgumentException($"Entity {entity} is no longer valid");
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_dynamicComponents.TryGetValue(name, out var map) && map.TryGetValue(entity, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"Entity {entity} does not have dynamic component '{name}'.");
    }

    /// <summary>
    /// Gets the dynamic component value cast to T. Throws if not present or incompatible type.
    /// </summary>
    public T GetDynamicComponent<T>(Entity entity, string name)
    {
        var value = GetDynamicComponent(entity, name);
        return (T)value!;
    }

    /// <summary>
    /// Tries to get the dynamic component value cast to T.
    /// </summary>
    public bool TryGetDynamicComponent<T>(Entity entity, string name, out T value)
    {
        value = default!;
        if (!IsEntityValid(entity)) return false;
        if (string.IsNullOrEmpty(name)) return false;
        if (_dynamicComponents.TryGetValue(name, out var map) && map.TryGetValue(entity, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns all dynamic components attached to an entity as (name, data) pairs.
    /// </summary>
    public IEnumerable<(string name, object? data)> GetDynamicComponentsForEntity(Entity entity)
    {
        if (!IsEntityValid(entity)) yield break;
        foreach (var (name, map) in _dynamicComponents)
        {
            if (map.TryGetValue(entity, out var data))
            {
                yield return (name, data);
            }
        }
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
    /// Gets the existing message bus for a message type, or creates a new one if it doesn't exist.
    /// </summary>
    private MessageBus<T> GetOrCreateMessageBus<T>() where T : struct
    {
        var type = typeof(T);
        if (!_messageBuses.TryGetValue(type, out var bus))
        {
            bus = new MessageBus<T>();
            _messageBuses[type] = bus;
        }
        return (MessageBus<T>)bus;
    }

    /// <summary>
    /// Registers the owner of a specific component type.
    /// </summary>
    /// <param name="componentType"></param>
    /// <param name="owner"></param>
    public void RegisterComponentOwner(Type componentType, IPlugin owner)
    {
        _componentOwners[componentType] = owner;
    }

    /// <summary>
    /// Unregisters the owner of a specific component type.
    /// </summary>
    /// <param name="componentType"></param>
    public void UnregisterComponentOwner(Type componentType)
    {
        _componentOwners.Remove(componentType);
    }

    /// <summary>
    /// Unregisters a service of a specific type.
    /// </summary>
    public void UnregisterService<T>() where T : class
    {
        var serviceType = typeof(T);
        if (_services.Remove(serviceType))
        {
            _serviceOwners.Remove(serviceType);
        }
    }

    /// <summary>
    /// Removes a system by its name.
    /// </summary>
    public bool RemoveSystemByName(string systemName)
    {
        var systemToRemove = _systems.FirstOrDefault(s => s.Name == systemName);
        if (systemToRemove != null)
        {
            _systems.Remove(systemToRemove);
            _systemOwners.Remove(systemToRemove.GetType());
            _isSystemOrderDirty = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Disables a system by its unique name. Returns false if no such system exists.
    /// </summary>
    /// <param name="systemName">The system's Name property value.</param>
    public bool DisableSystemByName(string systemName)
    {
        var system = _systems.FirstOrDefault(s => s.Name == systemName);
        if (system == null) return false;
        system.Enabled = false;
        return true;
    }

    /// <summary>
    /// Enables a system by its unique name. Returns false if no such system exists.
    /// </summary>
    /// <param name="systemName">The system's Name property value.</param>
    public bool EnableSystemByName(string systemName)
    {
        var system = _systems.FirstOrDefault(s => s.Name == systemName);
        if (system == null) return false;
        system.Enabled = true;
        return true;
    }

    /// <summary>
    /// Registers a plugin instance for tracking and REST API access.
    /// This should be called when a plugin is loaded.
    /// </summary>
    /// <param name="plugin">The plugin instance to register.</param>
    public void RegisterPlugin(IPlugin plugin)
    {
        _pluginsByPrefix[plugin.Prefix] = plugin;
    }

    /// <summary>
    /// Unregisters a plugin from tracking.
    /// This should be called when a plugin is unloaded.
    /// </summary>
    /// <param name="plugin">The plugin instance to unregister.</param>
    public void UnregisterPlugin(IPlugin plugin)
    {
        _pluginsByPrefix.Remove(plugin.Prefix);
    }

    /// <summary>
    /// Adds a new system to the world. The execution order will be automatically resolved before the next update.
    /// </summary>
    public void RegisterSystem(ISystem system, IPlugin? owner = null)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (string.IsNullOrEmpty(system.Name) || _systems.Any(s => s.Name == system.Name))
        {
            throw new ArgumentException($"A system with the name '{system.Name}' is already registered or the name is invalid. System names must be unique and not empty.");
        }
        _unmanagedSystems.Add(system);
        _systems.Add(system);
        _isSystemOrderDirty = true;
        if (owner != null)
        {
            _systemOwners[system.GetType()] = owner;
        }
    }

    /// <summary>
    /// Sorts all registered systems and organizes them into their respective execution groups.
    /// This is called automatically by Update() when needed.
    /// </summary>
    public void SortAndGroupSystems()
    {
        if (!_isSystemOrderDirty) return;

        _logger.LogDebug("System order is dirty. Re-sorting and grouping systems...");

        // --- 1. Group systems by their [UpdateInGroup] attribute ---
        var systemsByGroup = new Dictionary<Type, List<ISystem>>
        {
            [typeof(InitializationSystemGroup)] = new List<ISystem>(),
            [typeof(SimulationSystemGroup)] = new List<ISystem>(),
            [typeof(PresentationSystemGroup)] = new List<ISystem>()
        };

        foreach (var system in _unmanagedSystems)
        {
            var groupAttr = system.GetType().GetCustomAttribute<UpdateInGroupAttribute>();
            var groupType = groupAttr?.TargetGroup ?? typeof(SimulationSystemGroup); // Default to Simulation

            if (systemsByGroup.TryGetValue(groupType, out var groupList))
            {
                groupList.Add(system);
            }
            else
            {
                // This could happen if a user defines a custom group but doesn't handle it in the World.
                _logger.LogWarning("System '{SystemName}' belongs to unhandled group '{GroupName}'. Placing in Simulation group.", system.Name, groupType.Name);
                systemsByGroup[typeof(SimulationSystemGroup)].Add(system);
            }
        }

        // --- 2. Sort each group individually and assign to the final lists ---
        try
        {
            _initializationSystems = SystemSorter.Sort(systemsByGroup[typeof(InitializationSystemGroup)]);
            _simulationSystems = SystemSorter.Sort(systemsByGroup[typeof(SimulationSystemGroup)]);
            _presentationSystems = SystemSorter.Sort(systemsByGroup[typeof(PresentationSystemGroup)]);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to sort systems");
            // In a real game, you might want to stop execution or enter a safe mode here.
            throw;
        }

        // The list is now clean.
        _isSystemOrderDirty = false;
        _logger.LogInformation("System sorting complete. Init={InitCount}, Sim={SimCount}, Pres={PresCount}", _initializationSystems.Count, _simulationSystems.Count, _presentationSystems.Count);
    }

    /// <summary>
    /// The main update loop for the world. It ensures systems are sorted by their dependencies before executing.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Remember the delta time even if paused.
        LastDeltaTime = deltaTime;

        if (IsPaused)
        {
            return; // No-op when paused
        }

        RunUpdate(deltaTime);
    }

    /// <summary>
    /// Advances the world by one step using the provided delta time regardless of the paused state.
    /// </summary>
    public void Step(float deltaTime)
    {
        // Preserve LastDeltaTime for telemetry.
        LastDeltaTime = deltaTime;
        RunUpdate(deltaTime);
    }

    /// <summary>
    /// Puts the world into a paused state. Update() becomes a no-op until resumed.
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// Resumes the world if it was paused.
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// Core update logic extracted so Step() can advance even when paused.
    /// </summary>
    private void RunUpdate(float deltaTime)
    {
        // If the system list has changed, re-sort and re-group everything.
        if (_isSystemOrderDirty)
        {
            SortAndGroupSystems();
        }

        // Execute systems in their guaranteed group order.
        // A temporary copy is used to prevent issues if a system modifies the list during iteration (e.g., via hot-reloading).
        foreach (var system in _initializationSystems.ToArray())
        {
            if (system.Enabled) system.Update(this, deltaTime);
        }

        foreach (var system in _simulationSystems.ToArray())
        {
            if (system.Enabled) system.Update(this, deltaTime);
        }

        foreach (var system in _presentationSystems.ToArray())
        {
            if (system.Enabled) system.Update(this, deltaTime);
        }

        Tick++;
        ClearAllMessages();
    }

    /// <summary>
    /// Publishes a message to the world. Any system can read this message during the current frame.
    /// All messages are cleared at the end of the frame.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    public void PublishMessage<T>(T message) where T : struct
    {
        var bus = GetOrCreateMessageBus<T>();
        bus.Publish(message);
    }

    /// <summary>
    /// Reads all messages of a specific type that have been published in the current frame.
    /// </summary>
    /// <typeparam name="T">The type of the message to read.</typeparam>
    /// <returns>A read-only list of messages. Returns an empty list if no messages of this type were published.</returns>
    public IReadOnlyList<T> ReadMessages<T>() where T : struct
    {
        var type = typeof(T);
        if (_messageBuses.TryGetValue(type, out var bus))
        {
            return ((MessageBus<T>)bus).GetMessages();
        }
        // Return a static empty list to avoid allocation
        return [];
    }

    /// <summary>
    /// Iterates through all message buses and clears them.
    /// </summary>
    private void ClearAllMessages()
    {
        foreach (var bus in _messageBuses.Values)
        {
            bus.Clear();
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
            _logger.LogError("Cannot invoke function '{FunctionName}' on an invalid entity.", functionName);
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
                _logger.LogError(ex, "Exception while executing function '{FunctionName}'", functionName);
            }
        }
        else
        {
            _logger.LogError("Attempted to invoke unknown function: '{FunctionName}'", functionName);
        }
    }

    /// <summary>
    /// Removes a system from the world.
    /// </summary>
    public void RemoveSystem<T>() where T : ISystem
    {
        int removedFromSystems = _systems.RemoveAll(s => s is T);
        int removedFromUnmanaged = _unmanagedSystems.RemoveAll(s => s is T);
        int removedCount = removedFromSystems + removedFromUnmanaged;
        if (removedCount > 0)
        {
            _logger.LogInformation("Removed {RemovedCount} systems of type {SystemType}", removedCount, typeof(T).Name);
            _isSystemOrderDirty = true;
        }
        else
        {
            _logger.LogDebug("No systems of type {SystemType} found to remove", typeof(T).Name);
        }
    }

    /// <summary>
    /// Removes a specific system instance from the world.
    /// </summary>
    public bool RemoveSystem(ISystem system)
    {
        if (system == null) return false;

        bool removed = _systems.Remove(system);
        removed |= _unmanagedSystems.Remove(system);
        if (removed)
        {
            _logger.LogInformation("Removed system instance: {SystemName}", system.Name);
            _isSystemOrderDirty = true;
        }
        else
        {
            _logger.LogDebug("System instance {SystemName} was not found in the systems list", system.Name);
        }
        return removed;
    }

    /// <summary>
    /// Disables a system of a specific type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DisableSystem<T>() where T : ISystem
    {
        foreach (var system in _systems.OfType<T>())
        {
            system.Enabled = false;
        }
    }

    /// <summary>
    /// Enables a system of a specific type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void EnableSystem<T>() where T : ISystem
    {
        foreach (var system in _systems.OfType<T>())
        {
            system.Enabled = true;
        }
    }


    /// <summary>
    /// Removes a registered function from the world.
    /// </summary>
    public void RemoveFunction(string functionName)
    {
        _functions.Remove(functionName);
    }

    /// <summary>
    /// Registers a service object that can be retrieved by other systems or plugins.
    /// Only one service of a given type can be registered.
    /// </summary>
    /// <typeparam name="T">The type (often an interface) to register the service under.</typeparam>
    /// <param name="service">The service instance.</param>
    /// <param name="owner">The plugin that owns this service, if applicable.</param>
    public void RegisterService<T>(T service, IPlugin? owner = null) where T : class
    {
        var serviceType = typeof(T);
        _services[serviceType] = service;
        if (owner != null)
        {
            _serviceOwners[serviceType] = owner;
        }
    }

    /// <summary>
    /// Retrieves a registered service.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no service of the given type is registered.</exception>
    public T GetService<T>() where T : class
    {
        var serviceType = typeof(T);
        if (!_services.TryGetValue(serviceType, out var service))
        {
            throw new KeyNotFoundException($"Service of type {serviceType.Name} has not been registered.");
        }
        return (T)service;
    }

    // TODO: Implement a way to clear all entities and components if needed.
    // TODO: Implement a way to serialize/deserialize the world state for saving/loading.

    /// <summary>
    /// Retrieves a collection of all currently active entities in the world.
    /// </summary>
    /// <returns>An IEnumerable of all valid entities.</returns>
    public IEnumerable<Entity> GetAllEntities()
    {
        // Enumerate only currently alive entities.
        foreach (var id in _activeEntityIds)
        {
            yield return new Entity(id, _generations[id], this);
        }
    }
    // --- Neutral read-only accessors for introspection (usable by tooling like REST API, editors, etc.) ---

    /// <summary>
    /// Maximum number of entities supported by this world.
    /// </summary>
    public uint MaxEntities => _maxEntities;

    /// <summary>
    /// Count of recycled entity IDs currently available.
    /// </summary>
    public int RecycledEntityIdCount => _freeIds.Count;

    /// <summary>
    /// Total number of registered systems.
    /// </summary>
    public int RegisteredSystemCount => _systems.Count;

    /// <summary>
    /// Total number of registered component types.
    /// </summary>
    public int RegisteredComponentTypeCount => _storages.Count;

    /// <summary>
    /// Snapshot of currently registered systems.
    /// </summary>
    public IEnumerable<ISystem> GetRegisteredSystems() => _systems.ToArray();

    /// <summary>
    /// Returns the owner plugin for a system type, if any.
    /// </summary>
    public IPlugin? GetSystemOwner(Type systemType)
    {
        _systemOwners.TryGetValue(systemType, out var owner);
        return owner;
    }

    /// <summary>
    /// Snapshot enumeration of registered services.
    /// </summary>
    public IEnumerable<(Type type, object instance)> GetRegisteredServices() => _services.Select(kvp => (kvp.Key, kvp.Value)).ToArray();

    /// <summary>
    /// Returns the owner plugin for a service type, if any.
    /// </summary>
    public IPlugin? GetServiceOwner(Type serviceType)
    {
        _serviceOwners.TryGetValue(serviceType, out var owner);
        return owner;
    }

    /// <summary>
    /// Snapshot of registered component types.
    /// </summary>
    public IEnumerable<Type> GetRegisteredComponentTypes() => _storages.Keys.ToArray();

    /// <summary>
    /// Returns the owner plugin for a component type, if any.
    /// </summary>
    public IPlugin? GetComponentOwner(Type componentType)
    {
        _componentOwners.TryGetValue(componentType, out var owner);
        return owner;
    }

    /// <summary>
    /// Snapshot of registered plugins.
    /// </summary>
    public IEnumerable<IPlugin> GetRegisteredPlugins() => _pluginsByPrefix.Values.ToArray();

    /// <summary>
    /// Tries to get a plugin by its prefix.
    /// </summary>
    public bool TryGetPluginByPrefix(string prefix, out IPlugin plugin)
    {
        return _pluginsByPrefix.TryGetValue(prefix, out plugin!);
    }

    /// <summary>
    /// Returns all components attached to an entity as (Type, boxedData) pairs.
    /// </summary>
    public IEnumerable<(Type type, object data)> GetComponentsForEntityAsObjects(Entity entity)
    {
        if (!IsEntityValid(entity)) yield break;
        foreach (var (type, storage) in _storages)
        {
            var data = storage.GetDataAsObject(entity);
            if (data != null)
            {
                yield return (type, data);
            }
        }
    }
}

#region Data Transfer Objects (DTOs) for Public API

/// <summary>
/// A DTO for exposing basic entity information, typically in a list.
/// </summary>
public class EntitySummaryDto
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public uint Id { get; set; }
    /// <summary>
    /// The generation of the entity
    /// </summary>
    public int Generation { get; set; }
    /// <summary>
    /// A direct link to the detailed view of this entity.
    /// </summary>
    public required string Url { get; set; }
}

/// <summary>
/// A DTO for exposing the full details of a single entity.
/// </summary>
public class EntityDetailDto
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public uint Id { get; set; }
    /// <summary>
    /// A direct link to the detailed view of this entity.
    /// </summary>
    public required List<ComponentInfoDto> Components { get; set; }
}

/// <summary>
/// A DTO for exposing the world's status.
/// </summary>
public class WorldStatusDto
{
    /// <summary>
    /// The maximum number of entities that can be created in this world.
    /// </summary>
    public uint MaxEntities { get; set; }
    /// <summary>
    /// The number of entity IDs that have been recycled and can be reused.
    /// </summary>
    public int RecycledEntityIds { get; set; }
    /// <summary>
    /// The number of systems that are currently registered in the world.
    /// </summary>
    public int RegisteredSystems { get; set; }
    /// <summary>
    /// The number of component types that are currently registered in the world.
    /// </summary>
    public int ComponentTypes { get; set; }
}

/// <summary>
/// A DTO for exposing system information.
/// </summary>
public class SystemInfoDto
{
    /// <summary>
    /// The name of the system.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Indicates whether the system is currently enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this system.
    /// </summary>
    public required string PluginOwner { get; set; }

}
/// <summary>
/// A DTO for exposing component information.
/// </summary>
public class ComponentInfoDto
{
    /// <summary>
    /// The name of the component type.
    /// </summary>
    public required string TypeName { get; set; }
    // We could add a 'Data' object here, but it requires serialization logic.
    /// <summary>
    /// The data associated with the component, if applicable.
    /// </summary>
    public object? Data { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this component.
    /// </summary>
    public string? PluginOwner { get; set; }

    /// <summary>
    /// Indicates whether this component entry represents a dynamic component (identified by name rather than type).
    /// </summary>
    public bool IsDynamic { get; set; }

}

/// <summary>
/// A DTO for exposing service information.
/// </summary>
public class ServiceInfoDto
{
    /// <summary>
    /// The full type name of the service, used for invoking methods.
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// A list of public methods available on the service.
    /// </summary>
    public required IEnumerable<string> Methods { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this service.
    /// </summary>
    public required string PluginOwner { get; set; }
}

/// <summary>
/// A DTO for exposing detailed plugin information.
/// </summary>
public class PluginDetailDto : PluginInfoDto
{
    /// <summary>
    /// A list of systems provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Systems { get; set; }
    /// <summary>
    /// A list of services provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Services { get; set; }
    /// <summary>
    /// A list of components provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Components { get; set; }
}

/// <summary>
/// A DTO for exposing plugin information.
/// </summary>
public class PluginInfoDto
{
    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The version of the plugin.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// The author of the plugin.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// A description of what the plugin does.
    /// </summary>
    public required string Description { get; set; }
    /// <summary>
    /// The unique prefix used for this plugin's systems and services.
    /// </summary>
    public required string Prefix { get; set; }
    /// <summary>
    /// The URL for accessing this plugin's API endpoints.
    /// </summary>
    public required string Url { get; set; }
}

#endregion
