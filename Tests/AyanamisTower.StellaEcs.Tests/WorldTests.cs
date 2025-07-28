#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;

// A simple component for testing purposes
public struct Position
{
    public float X, Y;
}

// Another simple component for testing
public struct Velocity
{
    public float Dx, Dy;
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
    public void CreateEntity_WhenAtCapacity_ThrowsException()
    {
        // Arrange
        var world = new World(2);
        world.CreateEntity(); // Entity 0
        world.CreateEntity(); // Entity 1

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => world.CreateEntity());
        Assert.Equal("Cannot create new entity: World has reached maximum capacity.", ex.Message);
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
    public void DestroyEntity_RemovesAllComponents()
    {
        // Arrange
        var world = new World(100);
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();
        Entity entity = world.CreateEntity();
        world.AddComponent(entity, new Position());
        world.AddComponent(entity, new Velocity());

        // Act
        world.DestroyEntity(entity);
        var storagePos = world.GetStorage<Position>();
        var storageVel = world.GetStorage<Velocity>();

        // Assert
        Assert.False(storagePos.Has(entity.Id));
        Assert.False(storageVel.Has(entity.Id));
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

        // 1. Read operations on stale handle should return false
        Assert.False(world.IsAlive(staleHandle), "Stale handle should not be alive.");
        Assert.False(world.HasComponent<Position>(staleHandle), "HasComponent should return false for a stale handle.");

        // 2. Get operations on stale handle should throw exceptions
        Assert.Throws<System.InvalidOperationException>(() => world.GetComponent<Position>(staleHandle));
        Assert.Throws<System.InvalidOperationException>(() => world.GetComponentMutable<Position>(staleHandle));

        // 3. Write operations on stale handle should do nothing
        world.AddComponent(staleHandle, new Position { X = 888, Y = 888 }); // Should not add
        world.SetComponent(staleHandle, new Position { X = 999, Y = 999 }); // Should not set
        world.RemoveComponent<Position>(staleHandle); // Should not remove anything from the live handle

        // Verify that the operations did not affect the live entity that reused the ID
        ref readonly var livePosition = ref world.GetComponent<Position>(liveHandle);
        Assert.Equal(100, livePosition.X); // SetComponent on a stale handle should not affect the live entity.
        Assert.True(world.HasComponent<Position>(liveHandle)); // RemoveComponent on stale handle should not affect live one.
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

    [Fact]
    public void GetStorage_ForUnregisteredComponent_ThrowsException()
    {
        // Arrange
        var world = new World(100);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => world.GetStorage<Position>());
        Assert.Equal("Component type 'Position' has not been registered. Call RegisterComponent<Position>() first.", ex.Message);
    }

    [Fact]
    public void RegisterComponent_CalledTwice_DoesNothing()
    {
        // Arrange
        var world = new World(100);

        // Act
        world.RegisterComponent<Position>();
        var storage1 = world.GetStorage<Position>();
        world.RegisterComponent<Position>(); // Call it again
        var storage2 = world.GetStorage<Position>();

        // Assert
        Assert.NotNull(storage1);
        Assert.Same(storage1, storage2); // Should be the exact same instance
    }
}
