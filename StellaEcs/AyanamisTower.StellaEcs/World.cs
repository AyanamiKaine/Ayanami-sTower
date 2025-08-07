using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

    private object? _apiServerInstance;

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
        // PRE-UPDATE HOOK
        foreach (var system in _systems)
        {
            if (system.Enabled && system is IPreUpdateSystem preSystem)
            {
                preSystem.PreUpdate(this, deltaTime);
            }
        }

        // MAIN UPDATE
        foreach (var system in _systems)
        {
            if (!system.Enabled) continue;
            system.Update(this, deltaTime);
        }

        // POST-UPDATE HOOK
        foreach (var system in _systems)
        {
            if (system.Enabled && system is IPostUpdateSystem postSystem)
            {
                postSystem.PostUpdate(this, deltaTime);
            }
        }

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
        _systems.RemoveAll(s => s is T);
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
    public void RegisterService<T>(T service) where T : class
    {
        var serviceType = typeof(T);
        if (_services.ContainsKey(serviceType))
        {
            Console.WriteLine($"[Warning] Service of type {serviceType.Name} is already registered. Overwriting.");
        }
        _services[serviceType] = service;
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
    public WorldStatus GetWorldStatus()
    {
        return new WorldStatus
        {
            MaxEntities = _maxEntities,
            RecycledEntityIds = _freeIds.Count,
            RegisteredSystems = _systems.Count,
            ComponentTypes = _storages.Count
        };
    }

    /// <summary>
    /// Gets a list of all registered systems and their current state.
    /// </summary>
    public IEnumerable<SystemInfo> GetSystems()
    {
        return _systems.Select(s => new SystemInfo
        {
            Name = s.GetType().Name,
            Enabled = s.Enabled
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
            var entity = new Entity(i, _generations[i], this);
            if (IsEntityValid(entity))
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets information about all components attached to a specific entity.
    /// </summary>
    /// <param name="entity">The entity to inspect.</param>
    /// <returns>A list of component information objects.</returns>
    public IEnumerable<ComponentInfo> GetAllComponentsForEntity(Entity entity)
    {
        if (!IsEntityValid(entity))
        {
            return [];
        }

        var components = new List<ComponentInfo>();
        foreach (var (type, storage) in _storages)
        {
            if (storage.Has(entity))
            {
                // This part is tricky because we can't easily get the component
                // data without knowing its type. We can use reflection or add a
                // non-generic Get method to IComponentStorage.
                // For now, we'll just return the type name.
                components.Add(new ComponentInfo { TypeName = type.Name });
            }
        }
        return components;
    }


    /// <summary>
    /// Gets the names of all registered component types.
    /// </summary>
    public IEnumerable<string> GetComponentTypes()
    {
        return _storages.Keys.Select(t => t.Name);
    }
    #endregion
}

#region Data Transfer Objects (DTOs) for Public API

/// <summary>
/// A DTO for exposing the world's status.
/// </summary>
public class WorldStatus
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
public class SystemInfo
{
    /// <summary>
    /// The name of the system.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Indicates whether the system is currently enabled.
    /// </summary>
    public bool Enabled { get; set; }
}
/// <summary>
/// A DTO for exposing component information.
/// </summary>
public class ComponentInfo
{
    /// <summary>
    /// The name of the component type.
    /// </summary>
    public required string TypeName { get; set; }
    // We could add a 'Data' object here, but it requires serialization logic.
    // public object Data { get; set; }
}


#endregion