using Flecs.NET.Core;

namespace Avalonia.Flecs.Scripting;


public class NamedEntities(World world)
{
    private Dictionary<string, Entity> _entities = [];
    private World _world = world;
    public Entity this[string name]
    {
        get
        {
            if (_entities.ContainsKey(name))
            {
                return _entities[name];
            }
            else
            {
                var entity = _world.Entity(name);
                _entities.Add(name, entity);
                return entity;
            }
        }
        set
        {
            if (_entities.ContainsKey(name))
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
        if (_entities.ContainsKey(name))
        {
            _entities.Remove(name);
        }
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