using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

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
    /// <summary>
    /// The current tick of the world. This is incremented on each update by one.
    /// System can use it to determine the current frame or update cycle. Systems can determine if they want to run every tick or only every second, third and so on.
    /// </summary>
    public uint Tick { get; private set; }
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

    private object? _apiServerInstance;

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
        return new Entity(id, this);
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
        return entity.Id < _nextEntityId;
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

        Console.WriteLine("[World] System order is dirty. Re-sorting and grouping systems...");

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
                Console.WriteLine($"[Warning] System '{system.Name}' belongs to unhandled group '{groupType.Name}'. Placing in Simulation group.");
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
            Console.WriteLine($"[FATAL] Failed to sort systems: {ex.Message}");
            // In a real game, you might want to stop execution or enter a safe mode here.
            throw;
        }

        // The list is now clean.
        _isSystemOrderDirty = false;
        Console.WriteLine("[World] System sorting complete.");
    }

    /// <summary>
    /// The main update loop for the world. It ensures systems are sorted by their dependencies before executing.
    /// </summary>
    public void Update(float deltaTime)
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

    /// <summary>
    /// Removes a system from the world.
    /// </summary>
    public void RemoveSystem<T>() where T : ISystem
    {
        int removedCount = _systems.RemoveAll(s => s is T);
        if (removedCount > 0)
        {
            Console.WriteLine($"[World] Removed {removedCount} systems of type {typeof(T).Name}");
            _isSystemOrderDirty = true;
        }
        else
        {
            Console.WriteLine($"[World] No systems of type {typeof(T).Name} found to remove");
        }
    }

    /// <summary>
    /// Removes a specific system instance from the world.
    /// </summary>
    public bool RemoveSystem(ISystem system)
    {
        if (system == null) return false;

        bool removed = _systems.Remove(system);
        if (removed)
        {
            Console.WriteLine($"[World] Removed system instance: {system.Name}");
            _isSystemOrderDirty = true;
        }
        else
        {
            Console.WriteLine($"[World] System instance {system.Name} was not found in the systems list");
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

    #region Public API for REST/Remote Access

    /*
    It would be better if we would move the rest api functiona to extension methods.
    We currently dont do it because we need to add a new field/property to the World class.
    This is a temporary solution to allow remote access to the world state.
    But this is possible in dotnet 10, Currrently this is a problem because users
    can call the methods even though the plugin is not loaded.
    */

    /// <summary>
    /// Enables the REST API for the world by dynamically loading the API server assembly.
    /// The API provides remote access to inspect the world's state.
    /// </summary>
    /// <param name="url">The URL the API server should listen on.</param>
    public void EnableRestApi(string url = "http://localhost:5123")
    {
        try
        {
            // 1. Construct the full path to the API DLL.
            // This is more robust than relying on the default probing mechanism of Assembly.Load().
            const string apiDllName = "AyanamisTower.StellaEcs.RestAPI.dll";
            string apiDllPath = Path.Combine(AppContext.BaseDirectory, apiDllName);

            if (!File.Exists(apiDllPath))
            {
                // Throw the specific exception the catch block is looking for.
                throw new FileNotFoundException($"The API assembly was not found at the expected path: {apiDllPath}", apiDllName);
            }

            // 2. Dynamically load the API assembly from its specific path.
            var apiAssembly = Assembly.LoadFrom(apiDllPath);

            // 3. Find the RestApiServer type.
            var apiServerType = apiAssembly.GetType("AyanamisTower.StellaEcs.Api.RestApiServer") ?? throw new InvalidOperationException("Could not find RestApiServer type in the API assembly.");

            // 4. Get a reference to the static Start method.
            var startMethod = apiServerType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("Could not find the 'Start' method on the RestApiServer.");

            // 5. Invoke the Start method, passing this world instance and the URL.
            startMethod.Invoke(null, [this, url]);

            _apiServerInstance = apiServerType; // Store a reference for stopping it later.
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"[Error] {ex.Message}. Please ensure '{ex.FileName}' is in the application's output directory.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to enable REST API: {ex.Message}, Check if your project has a refrence RestAPI project so its dependency are correctly loaded");
        }
    }

    /// <summary>
    /// Disables the REST API server if it is running.
    /// </summary>
    public async Task DisableRestApi()
    {
        if (_apiServerInstance is Type apiServerType)
        {
            var stopMethod = apiServerType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static);
            if (stopMethod?.Invoke(null, null) is Task stopTask)
            {
                await stopTask;
            }
            _apiServerInstance = null;
        }
        else
        {
            Console.WriteLine("REST API is not currently enabled.");
        }
    }

    /// <summary>
    /// Gets a snapshot of the world's current status.
    /// </summary>
    public WorldStatusDto GetWorldStatus()
    {
        return new WorldStatusDto
        {
            MaxEntities = _maxEntities,
            RecycledEntityIds = _freeIds.Count,
            RegisteredSystems = _systems.Count,
            ComponentTypes = _storages.Count
        };
    }

    /// <summary>
    /// Retrieves detailed information about a specific plugin.
    /// </summary>
    /// <param name="pluginPrefix"></param>
    /// <returns></returns>
    public PluginDetailDto? GetPluginDetails(string pluginPrefix)
    {
        if (!_pluginsByPrefix.TryGetValue(pluginPrefix, out var plugin))
        {
            return null;
        }

        return new PluginDetailDto
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Author = plugin.Author,
            Description = plugin.Description,
            Prefix = plugin.Prefix,
            Url = $"/api/plugins/{plugin.Prefix}",
            Systems = [.. plugin.ProvidedSystems.Select(t => $"{plugin.Prefix}.{t.Name}")],
            Services = [.. plugin.ProvidedServices.Select(t => t.FullName ?? t.Name)],
            Components = [.. plugin.ProvidedComponents.Select(t => t.Name)]
        };
    }

    /// <summary>
    /// Gets a list of all registered systems and their current state.
    /// </summary>
    public IEnumerable<SystemInfoDto> GetSystems()
    {
        return _systems.Select(s => new SystemInfoDto
        {
            Name = s.Name,
            Enabled = s.Enabled,
            PluginOwner = _systemOwners.TryGetValue(s.GetType(), out var p) ? p.Prefix : "World"
        });
    }
    /// <summary>
    /// Retrieves a collection of all currently active entities in the world.
    /// </summary>
    /// <returns>An IEnumerable of all valid entities.</returns>
    public IEnumerable<Entity> GetAllEntities()
    {
        // This is a simple but potentially slow way to get all entities.
        // It iterates all possible IDs and checks for validity.
        // For a more performant version, you might maintain a separate list of active entities.
        for (uint i = 0; i < _nextEntityId; i++)
        {
            var entity = new Entity(i, this);
            if (IsEntityValid(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets information and data for all components attached to a specific entity.
    /// </summary>
    /// <param name="entity">The entity to inspect.</param>
    /// <returns>A list of component information objects, including their data.</returns>
    public List<ComponentInfoDto> GetAllComponentsForEntity(Entity entity)
    {
        var components = new List<ComponentInfoDto>();
        if (!IsEntityValid(entity))
        {
            return components;
        }

        foreach (var (type, storage) in _storages)
        {
            // Use our new method to get the data!
            var data = storage.GetDataAsObject(entity);
            if (data != null)
            {
                components.Add(new ComponentInfoDto
                {
                    TypeName = type.Name,
                    Data = data
                });
            }
        }
        return components;
    }


    /// <summary>
    /// Gets the names of all registered component types.
    /// </summary>
    public IEnumerable<ComponentInfoDto> GetComponentTypes()
    {
        return _storages.Keys.Select(t => new ComponentInfoDto
        {
            TypeName = t.Name,
            PluginOwner = _componentOwners.TryGetValue(t, out var p) ? p.Prefix : "World",
            Data = null // Data is entity-specific, not relevant for a general listing
        });
    }
    /// <summary>
    /// Gets a list of all registered services and their public methods.
    /// </summary>
    public IEnumerable<ServiceInfoDto> GetServices()
    {
        return _services.Select(kvp => new ServiceInfoDto
        {
            TypeName = kvp.Key.FullName ?? kvp.Key.Name,
            Methods = kvp.Key.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => !m.IsSpecialName)
                         .Select(m => m.Name),
            PluginOwner = _serviceOwners.TryGetValue(kvp.Key, out var p) ? p.Prefix : "World"
        });
    }

    /// <summary>
    /// Gets a list of all loaded plugins with their information.
    /// </summary>
    public IEnumerable<PluginInfoDto> GetPlugins()
    {
        return _pluginsByPrefix.Values.Select(plugin => new PluginInfoDto
        {
            Name = plugin.Name,
            Version = plugin.Version,
            Author = plugin.Author,
            Description = plugin.Description,
            Prefix = plugin.Prefix,
            Url = $"/api/plugins/{plugin.Prefix}"
        });
    }

    /// <summary>
    /// Sets a component on an entity using dynamic type resolution from a type name and JSON data.
    /// This method is primarily intended for REST API usage.
    /// </summary>
    /// <param name="entity">The entity to attach the component to.</param>
    /// <param name="componentTypeName">The name of the component type.</param>
    /// <param name="componentData">The JSON data for the component.</param>
    /// <returns>True if the component was successfully set, false otherwise.</returns>
    public bool SetComponentFromJson(Entity entity, string componentTypeName, JsonElement componentData)
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is no longer valid");
        }

        // Find the component type by name across all loaded assemblies
        var componentType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == componentTypeName || type.FullName == componentTypeName) ?? throw new ArgumentException($"Component type '{componentTypeName}' not found. Available component types: {string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsValueType && t.Namespace?.Contains("Component") == true).Select(t => t.Name))}");
        if (!componentType.IsValueType)
        {
            throw new ArgumentException($"Component type '{componentTypeName}' must be a struct (value type).");
        }

        try
        {
            // Deserialize the JSON data to the component type
            var component = JsonSerializer.Deserialize(componentData, componentType, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            }) ?? throw new JsonException($"Failed to deserialize component data for type '{componentTypeName}'. JSON: {componentData.GetRawText()}");

            // Use reflection to call the generic SetComponent method
            var method = (typeof(World).GetMethod(nameof(SetComponent))?.MakeGenericMethod(componentType)) ?? throw new InvalidOperationException($"Could not create generic SetComponent method for type '{componentTypeName}'.");
            method.Invoke(this, [entity, component]);
            Console.WriteLine($"[World] Successfully set component '{componentTypeName}' on entity {entity}");
            return true;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"JSON deserialization failed for component '{componentTypeName}': {ex.Message}. JSON: {componentData.GetRawText()}", ex);
        }
        catch (Exception ex) when (ex.InnerException is JsonException jsonEx)
        {
            throw new ArgumentException($"JSON deserialization failed for component '{componentTypeName}': {jsonEx.Message}. JSON: {componentData.GetRawText()}", jsonEx);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set component '{componentTypeName}' on entity {entity}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Removes a component from an entity using dynamic type resolution from a type name.
    /// This method is primarily intended for REST API usage.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentTypeName">The name of the component type.</param>
    /// <returns>True if the component was successfully removed, false otherwise.</returns>
    public bool RemoveComponentByName(Entity entity, string componentTypeName)
    {
        if (!IsEntityValid(entity))
        {
            throw new ArgumentException($"Entity {entity} is no longer valid");
        }

        // Find the component type by name across all loaded assemblies
        var componentType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == componentTypeName || type.FullName == componentTypeName) ?? throw new ArgumentException($"Component type '{componentTypeName}' not found.");
        if (!componentType.IsValueType)
        {
            throw new ArgumentException($"Component type '{componentTypeName}' must be a struct (value type).");
        }

        try
        {
            // Use reflection to call the generic RemoveComponent method
            var method = (typeof(World).GetMethod(nameof(RemoveComponent))?.MakeGenericMethod(componentType)) ?? throw new InvalidOperationException($"Could not create generic RemoveComponent method for type '{componentTypeName}'.");
            method.Invoke(this, [entity]);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[World] Failed to remove component '{componentTypeName}' from entity {entity}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Invokes a method on a registered service by its type name.
    /// </summary>
    /// <param name="serviceTypeName">The full name of the service's type.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="parameters">A dictionary of parameter names and their values, typically from a JSON body.</param>
    /// <returns>The result of the method invocation.</returns>
    public object? InvokeServiceMethod(string serviceTypeName, string methodName, Dictionary<string, object> parameters)
    {
        // 1. Find the service type using its full name for accuracy.
        var serviceType = _services.Keys.FirstOrDefault(t => t.FullName == serviceTypeName) ?? throw new KeyNotFoundException($"Service of type '{serviceTypeName}' has not been registered.");

        // 2. Get the service instance from the dictionary.
        var serviceInstance = _services[serviceType];

        // 3. Find the method on the service's type.
        // This simple version finds the first public method with the given name.
        // A more advanced implementation could handle method overloading by inspecting parameter types.
        var method = serviceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) ?? throw new MissingMethodException(serviceTypeName, methodName);

        // 4. Prepare parameters for invocation.
        var methodParams = method.GetParameters();
        var invokeArgs = new object?[methodParams.Length];
        // Use a case-insensitive dictionary for friendlier API usage.
        var caseInsensitiveParams = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < methodParams.Length; i++)
        {
            var paramInfo = methodParams[i];
            if (paramInfo.Name != null &&
                caseInsensitiveParams.TryGetValue(paramInfo.Name, out var paramValue) &&
                paramValue is JsonElement jsonElement)
            {
                // The model binder gives us JsonElements. We need to deserialize them to the actual parameter type.
                invokeArgs[i] = jsonElement.Deserialize(paramInfo.ParameterType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else if (paramInfo.HasDefaultValue)
            {
                // If a parameter is missing from the request, use its default value if available.
                invokeArgs[i] = paramInfo.DefaultValue;
            }
            else
            {
                // If a required parameter is missing and has no default, throw an error.
                throw new ArgumentException($"Missing required parameter: '{paramInfo.Name}' for method '{methodName}'.");
            }
        }

        // 5. Invoke the method on the service instance with the prepared arguments.
        return method.Invoke(serviceInstance, invokeArgs);
    }
    #endregion
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
