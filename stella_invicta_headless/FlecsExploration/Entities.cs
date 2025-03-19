using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// </summary>

public class Entities
{
    /// <summary>
    /// An entity is a unique thing in the world, and is represented by a 64 bit id. Entities can be created and deleted. If an entity is deleted it is no longer considered "alive". A world can contain up to 4 billion(!) alive entities. Entity identifiers contain a few bits that make it possible to check whether an entity is alive or not.
    /// </summary>
    [Fact]
    public void CreatingEntities()
    {
        World world = World.Create();
        var entity = world.Entity();

        Assert.True(entity.IsAlive()); // The entity is alive, IsAlive should return true

        entity.Destruct(); // Destroying entity

        Assert.False(entity.IsAlive()); // Now IsAlive should return false
    }


    private record struct TAG;

    [Fact]
    public void ClearEntities()
    {
        World world = World.Create();
        var entity = world.Entity().Add<TAG>();
        var child = world.Entity();

        child.ChildOf(entity);

        Assert.True(entity.Has<TAG>()); // Now IsAlive should return false
        Assert.True(child.IsChildOf(entity));

        entity.Clear();
        child.Clear(); // This will remove the relationship (childOf | entity)

        Assert.False(child.IsChildOf(entity));
        Assert.False(entity.Has<TAG>()); // Now IsAlive should return false
    }


    /// <summary>
    /// Entities can be created by name, when we define a string name
    /// for an entity we may lookup the entity by name. Also
    /// if we create another entity with the same name, we instead
    /// return the already defined entity.
    /// </summary>
    [Fact]
    public void CreatingEntitiesByName()
    {
        World world = World.Create();
        var player = world.Entity("Player");

        var shouldBePlayer = world.Lookup("Player");
        var shouldAlsoBePlayer = world.Entity("Player");

        Assert.Equal(shouldBePlayer, player);
        Assert.Equal(shouldAlsoBePlayer, player);
    }
}