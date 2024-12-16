using Flecs.NET.Core;

namespace Avalonia.Flecs.Scripting;

/// <summary>
/// Should be thrown when an entity is not found in the named entities dictonary.
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    public EntityNotFoundException() : base()
    {
    }
    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    /// <param name="message"></param>
    public EntityNotFoundException(string message) : base(message)
    { }

    /// <summary>
    /// Constructor for EntityNotFoundException
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public EntityNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
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
public class NamedEntities(World world)
{
    private Dictionary<string, Entity> _entities = [];
    private World _world = world;

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
                return _entities[name];
            }
            catch (KeyNotFoundException)
            {
                throw new EntityNotFoundException($"Entity with the name:{name} was not found in the named entities dictonary. If you didnt care if the entity was created here you can use the GetEntityCreateIfNotExist method instead. If you expected that the entity was created here you should check if the entity was created before trying to access it.");
            }
        }
        set
        {
            if (_entities.TryGetValue(name, out _))
            {
                _entities[name] = value;
            }
            else
            {
                _entities.Add(name, _world.Entity(name));
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
        if (_entities.TryGetValue(name, out var _))
        {
            return;
        }
        _entities[name].Destruct();
        _entities.Remove(name);
    }

    /// <summary>
    /// Clears all entities in the container.
    /// </summary>
    public void Clear()
    {
        foreach (var entity in _entities)
        {
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
            return entity;
        }
        else
        {
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
        _entities.Add(name, entity);
        return entity;
    }

    /// <summary>
    /// Checks if an entity exists by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Contains(string name)
    {
        return _entities.ContainsKey(name);
    }
}
