#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
// Make sure to reference the project containing your ECS classes
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;

// A simple component for testing purposes
public struct Position
{
    public float X, Y;
}

public class WorldTests
{
    [Fact]
    public void CreateEntity_ReturnsValidAndAliveEntity()
    {
        // Arrange
        var world = new World(100);

        // Act
        Entity entity = world.CreateEntity();

        // Assert
        Assert.True(world.IsAlive(entity), "A newly created entity should be alive.");
        Assert.Equal(0, entity.Id); // The first entity should have ID 0.
        Assert.Equal(1, entity.Generation); // A new entity should have generation 1.
    }

    [Fact]
    public void DestroyEntity_MakesEntityHandleNotAlive()
    {
        // Arrange
        var world = new World(100);
        world.RegisterComponent<Position>();
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new Position());

        // Act
        world.DestroyEntity(entity);

        // Assert
        Assert.False(world.IsAlive(entity), "A destroyed entity handle should not be alive.");
        Assert.False(world.HasComponent<Position>(entity), "A destroyed entity should not have components.");
    }

    [Fact]
    public void EntityIdAndGeneration_RecyclesIdAndIncrementsGeneration()
    {
        // Arrange
        var world = new World(100);

        // Act
        Entity entity1 = world.CreateEntity(); // Should be Id=0, Gen=1
        world.DestroyEntity(entity1);
        Entity entity2 = world.CreateEntity(); // Should be Id=0, Gen=2

        // Assert
        Assert.Equal(entity1.Id, entity2.Id); // The ID should be recycled.
        Assert.Equal(entity1.Generation + 1, entity2.Generation); // The generation should be incremented.
        Assert.True(world.IsAlive(entity2), "The new handle should be alive.");
        Assert.False(world.IsAlive(entity1), "The old handle should be stale.");
    }

    [Fact]
    public void UsingStaleHandle_OperationsFailSafely()
    {
        // Arrange
        var world = new World(100);
        world.RegisterComponent<Position>();

        Entity staleHandle = world.CreateEntity();
        world.AddComponent(staleHandle, new Position { X = 10, Y = 10 });

        // Destroy the entity, making the handle stale
        world.DestroyEntity(staleHandle);

        // Create a new entity that reuses the ID
        Entity liveHandle = world.CreateEntity();
        world.AddComponent(liveHandle, new Position { X = 100, Y = 100 });

        // Act & Assert

        // 1. Read operations on stale handle should fail or return false
        Assert.False(world.IsAlive(staleHandle), "Stale handle should not be alive.");
        Assert.False(world.HasComponent<Position>(staleHandle), "HasComponent should return false for a stale handle.");

        // 2. Get operations on stale handle should throw exceptions
        Assert.Throws<System.InvalidOperationException>(() => world.GetComponent<Position>(staleHandle));
        Assert.Throws<System.InvalidOperationException>(() => world.GetComponentMutable<Position>(staleHandle));

        // 3. Write operations on stale handle should do nothing
        world.SetComponent(staleHandle, new Position { X = 999, Y = 999 });

        // Verify that the operation did not affect the live entity that reused the ID
        ref readonly var livePosition = ref world.GetComponent<Position>(liveHandle);
        Assert.Equal(100, livePosition.X); // SetComponent on a stale handle should not affect the live entity.
    }

    [Fact]
    public void ComponentMethods_CorrectlyManipulateDataForLiveEntity()
    {
        // Arrange
        var world = new World(100);
        world.RegisterComponent<Position>();
        Entity entity = world.CreateEntity();

        // Act & Assert for AddComponent
        world.AddComponent(entity, new Position { X = 10, Y = 20 });
        Assert.True(world.HasComponent<Position>(entity));

        // Act & Assert for GetComponent
        ref readonly var posRead = ref world.GetComponent<Position>(entity);
        Assert.Equal(10, posRead.X);

        // Act & Assert for GetComponentMutable
        ref var posMutable = ref world.GetComponentMutable<Position>(entity);
        posMutable.X = 50;
        Assert.Equal(50, posRead.X); // Modifying a mutable reference should be reflected.

        // Act & Assert for RemoveComponent
        world.RemoveComponent<Position>(entity);
        Assert.False(world.HasComponent<Position>(entity));
    }
}
