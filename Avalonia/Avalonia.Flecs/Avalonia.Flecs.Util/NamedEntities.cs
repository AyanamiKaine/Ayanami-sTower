using System.Collections;
using System.Reflection;
using Flecs.NET.Core;
using NLog;

namespace Avalonia.Flecs.Util;

/// <summary>
/// Should be thrown when an entity is not found in the named entities dictonary.
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    public EntityNotFoundException()
        : base() { }

    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    /// <param name="message"></param>
    public EntityNotFoundException(string message)
        : base(message) { }

    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}

/*
I would really like to enhance the idea of entities bringing them even more in object oriented design
more similar to small-talk.

Entities should be so much more, for example adding a description component would be quite nice.
*/

/// <summary>
/// A container for named entities. We simply give an entity
/// a specific name we can easily get it. It ensures that if
/// the entity is not found it will be created otherwise if the
/// entity already exists in the world we simply return it.
/// Its used for convenience sake so we can easily refrence entities
/// by their given name instead of their path.
/// </summary>
/// <param name="world"></param>
public class NamedEntities(World world) : IEnumerable<Entity>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Dictionary<string, Entity> _entities = [];
    private World _world = world;

    /// <summary>
    /// Event triggered when an entity is added to the NamedEntities container.
    /// </summary>
    public event Action<Entity, string>? OnEntityAdded;

    /// <summary>
    /// Gets or sets an entity by name. If an entity
    /// is already defined by name it will be replaced
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Entity this[string name]
    {
        get
        {
            try
            {
                Logger.Debug("Getting entity with name: {EntityName}", name);
                return _entities[name];
            }
            catch (KeyNotFoundException)
            {
                Logger.Error("Entity with name: {EntityName} not found", name);
                throw new EntityNotFoundException(
                    $"Entity with the name:{name} was not found in the named entities dictonary. If you didnt care if the entity was created here you can use the GetEntityCreateIfNotExist method instead. If you expected that the entity was created here you should check if the entity was created before trying to access it."
                );
            }
        }
        set
        {
            if (_entities.TryGetValue(name, out var existingEntity))
            {
                Logger.Info(
                    "Replacing existing entity with name: {EntityName}, ID: {EntityId}",
                    name,
                    existingEntity.ToString()
                );
                _entities[name] = value;
            }
            else
            {
                Logger.Info("Creating new entity with name: {EntityName}", name);
                _entities.Add(name, _world.Entity(name));
                OnEntityAdded?.Invoke(value, name); // Invoke the event when a new entity is added
            }
            _entities[name] = value;
        }
    }

    /// <summary>
    /// Removes an entity from the container and destroys it.
    /// </summary>
    /// <param name="name"></param>
    public void Remove(string name)
    {
        if (!_entities.TryGetValue(name, out var entity))
        {
            Logger.Debug("Attempted to remove non-existent entity: {EntityName}", name);
            return;
        }
        Logger.Info("Removing entity: {EntityName}, ID: {EntityId}", name, entity.ToString());
        _entities[name].Destruct();
        _entities.Remove(name);
    }

    /// <summary>
    /// Clears all entities in the container.
    /// </summary>
    public void Clear()
    {
        Logger.Info("Clearing all entities. Count: {EntityCount}", _entities.Count);
        foreach (var entity in _entities)
        {
            Logger.Debug(
                "Destructing entity: {EntityName}, ID: {EntityId}",
                entity.Key,
                entity.Value.ToString()
            );
            entity.Value.Destruct();
        }
        _entities.Clear();
    }

    /// <summary>
    /// Gets an entity by name. If the entity is not found
    /// it gets created.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Entity GetEntityCreateIfNotExist(string name)
    {
        if (_entities.TryGetValue(name, out var entity))
        {
            Logger.Debug(
                "Retrieved existing entity: {EntityName}, ID: {EntityId}",
                name,
                entity.ToString()
            );
            return entity;
        }
        else
        {
            Logger.Info("Entity not found, creating new entity with name: {EntityName}", name);
            return Create(name);
        }
    }

    /// <summary>
    /// Creates an entity by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Entity Create(string name)
    {
        var entity = _world.Entity(name);
        Logger.Info("Created new entity: {EntityName}, ID: {EntityId}", name, entity.ToString());
        _entities.Add(name, entity);
        OnEntityAdded?.Invoke(entity, name); // Invoke the event when a new entity is added
        return entity;
    }

    /// <summary>
    /// Creates a nameless entity, its default name
    /// will be its id given by flecs ecs. Usually just a number.
    /// Use this if you intent to use an entity only in a local context
    /// where you created it, or when you only want to use it in queries.
    /// </summary>
    /// <returns></returns>
    public Entity Create()
    {
        var entity = _world.Entity();
        var entityId = entity.ToString();
        Logger.Info("Created new unnamed entity, ID: {EntityId}", entityId);
        _entities.Add(entityId, entity);
        OnEntityAdded?.Invoke(entity, entityId); // Invoke the event when a new entity is added
        return entity;
    }

    /// <summary>
    /// Checks if an entity exists by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Contains(string name)
    {
        var exists = _entities.ContainsKey(name);
        Logger.Debug("Checking if entity exists: {EntityName}, Result: {Exists}", name, exists);
        return exists;
    }

    /// <inheritdoc/>
    public IEnumerator<Entity> GetEnumerator()
    {
        Logger.Debug("Enumerating {EntityCount} entities", _entities.Count);
        return _entities.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// While attached event handlers are not removed from the avalonia objects of entities
/// and this results in a memory leak. This class provides a method to clear all events.
/// BUT for now the memory leak only happens when scripts rerun old entities are destroyed
/// but there event handlers are not removed and the avalonia objects like a button
/// are not garbage collected. The result is a memory leak that is rather small
/// So small right now that it is not worth the effort to fix it.
/// The code below represent a way to possible fix it but it is not worth the effort
/// at least for now.
/// </summary>
internal static class EventCleaner
{
    /// <summary>
    /// Clears all events on an object
    /// </summary>
    /// <param name="obj"></param>
    internal static void ClearAllEvents(object obj)
    {
        if (obj == null)
            return;

        Type type = obj.GetType();
        EventInfo[] events = type.GetEvents(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        foreach (EventInfo eventInfo in events)
        {
            FieldInfo? fieldInfo = null;

            //Try to get the event field directly
            fieldInfo = type.GetField(
                eventInfo.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
            //If the field is not found directly, it might be a compiler-generated field (e.g., for auto-implemented events)
            if (fieldInfo == null)
            {
                fieldInfo = GetCompilerGeneratedField(type, eventInfo.Name);
            }
            if (fieldInfo == null)
                continue;

            try
            {
                fieldInfo.SetValue(obj, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error clearing event {eventInfo.Name} on {type.Name}: {ex.Message}"
                );
            }
        }
    }

    private static FieldInfo? GetCompilerGeneratedField(Type type, string eventName)
    {
        //For compiler-generated fields, the name is typically in the format <EventName>k__BackingField
        string backingFieldName = $"<{eventName}>k__BackingField";
        return type.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
