using Flecs.NET.Core;

namespace Avalonia.Flecs.Scripting;


/// <summary>
/// A container for named entities. We simply give an entity
/// a specific name we can easily get it. It ensures that if 
/// the entity is not found it will be created otherwise if the 
/// entity already exists in the world we simply return it.
/// </summary>
/// <param name="world"></param>
public class NamedEntities(World world)
{
    private Dictionary<string, Entity> _entities = new();
    private World _world = world;

    public Entity this[string name]
    {
        get
        {
            if (_entities.TryGetValue(name, out var entity))
            {
                return entity;
            }
            else
            {
                entity = _world.Entity(name);
                _entities.Add(name, entity);
                return entity;
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

    public void Remove(string name)
    {
        _entities.Remove(name);
    }

    public void Clear()
    {
        foreach (var entity in _entities)
        {
            entity.Value.Destruct();
        }
        _entities.Clear();
    }
}
