using AyanamisTower.StellaEcs;
using System.Linq;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class SystemAndQueryTests
{
    private readonly World _world;

    public SystemAndQueryTests()
    {
        _world = new World();
    }

    [Fact]
    public void RegisterSystem_AndUpdate_ShouldExecuteSystemLogic()
    {
        // Arrange
        var mockSystem = new MockSystem();
        _world.RegisterSystem(mockSystem);

        // Act
        _world.Update(0.16f);
        _world.Update(0.16f);

        // Assert
        Assert.Equal(2, mockSystem.UpdateCount);
    }

    [Fact]
    public void RegisterAndInvokeFunction_ShouldExecuteFunctionLogic()
    {
        // Arrange
        var mockFunction = new MockFunction();
        _world.RegisterFunction("Test", mockFunction);
        var entity = _world.CreateEntity();

        // Act
        _world.InvokeFunction(entity, "Test", 123, "hello");

        // Assert
        Assert.True(mockFunction.WasExecuted);
        Assert.NotNull(mockFunction.ReceivedParameters);
        Assert.Equal(2, mockFunction.ReceivedParameters.Length);
        Assert.Equal(123, mockFunction.ReceivedParameters[0]);
        Assert.Equal("hello", mockFunction.ReceivedParameters[1]);
    }

    [Fact]
    public void InvokeFunction_OnEntityWithComponent_ShouldModifyComponent()
    {
        // Arrange
        var mockFunction = new MockFunction();
        _world.RegisterFunction("Damage", mockFunction);
        var entity = _world.CreateEntity();
        entity.Set(new HealthComponent { Health = 100 });

        // Act
        _world.InvokeFunction(entity, "Damage");

        // Assert
        var health = entity.GetCopy<HealthComponent>();
        Assert.Equal(90, health.Health);
    }

    [Fact]
    public void Query_ShouldReturnCorrectEntities()
    {
        // Arrange
        var entity1 = _world.CreateEntity(); // Position + Velocity
        entity1.Set(new PositionComponent());
        entity1.Set(new VelocityComponent());

        var entity2 = _world.CreateEntity(); // Position only
        entity2.Set(new PositionComponent());

        var entity3 = _world.CreateEntity(); // Position + Velocity
        entity3.Set(new PositionComponent());
        entity3.Set(new VelocityComponent());

        var entity4 = _world.CreateEntity(); // Velocity only
        entity4.Set(new VelocityComponent());

        // Act
        var posQuery = _world.Query(typeof(PositionComponent)).ToList();
        var velQuery = _world.Query(typeof(VelocityComponent)).ToList();
        var posAndVelQuery = _world.Query(typeof(PositionComponent), typeof(VelocityComponent)).ToList();
        var noMatchQuery = _world.Query(typeof(PositionComponent), typeof(HealthComponent)).ToList();

        // Assert
        Assert.Equal(3, posQuery.Count);
        Assert.Contains(entity1, posQuery);
        Assert.Contains(entity2, posQuery);
        Assert.Contains(entity3, posQuery);

        Assert.Equal(3, velQuery.Count);
        Assert.Contains(entity1, velQuery);
        Assert.Contains(entity3, velQuery);

        Assert.Equal(2, posAndVelQuery.Count);
        Assert.Contains(entity1, posAndVelQuery);
        Assert.Contains(entity3, posAndVelQuery);

        Assert.Empty(noMatchQuery);
    }

    [Fact]
    public void DisablesSystemsShouldNotUpdate()
    {
        // Arrange
        var mockSystem = new MockSystem { Enabled = false };
        _world.RegisterSystem(mockSystem);

        // Act
        _world.Update(0.16f);

        // Assert
        Assert.Equal(0, mockSystem.UpdateCount);
    }

    [Fact]
    public void EnableSystem_ShouldUpdate()
    {
        // Arrange
        var mockSystem = new MockSystem { Enabled = false };
        _world.RegisterSystem(mockSystem);

        // Act
        _world.EnableSystem<MockSystem>();
        _world.Update(0.16f);

        // Assert
        Assert.Equal(1, mockSystem.UpdateCount);
    }

    [Fact]
    public void RemoveSystem()
    {
        // Arrange
        var mockSystem = new MockSystem();
        _world.RegisterSystem(mockSystem);

        // Act
        _world.RemoveSystem<MockSystem>();
        _world.Update(0.16f);

        // Assert
        Assert.Equal(0, mockSystem.UpdateCount);
    }


    [Fact]
    public void MessageSystem_ShouldDeliverAndClearMessagesPerFrame()
    {
        // Arrange
        var publishingSystem = new MessagePublishingSystem();
        var readingSystem = new MessageReadingSystem();

        _world.RegisterSystem(publishingSystem);
        _world.RegisterSystem(readingSystem);

        // --- Act & Assert: Frame 1 (Publish a message) ---
        // Tell the system to publish a message with data 42 on the next update.
        publishingSystem.ShouldPublish = true;
        publishingSystem.DataToPublish = 42;

        _world.Update(0.16f);

        // The reading system should have received exactly one message with the correct data.
        Assert.Single(readingSystem.ReceivedMessages);
        Assert.Equal(42, readingSystem.ReceivedMessages[0].Data);

        // --- Act & Assert: Frame 2 (Don't publish, verify clearance) ---
        // Tell the system NOT to publish a message on the next update.
        publishingSystem.ShouldPublish = false;

        _world.Update(0.16f);

        // The reading system should now have an empty list, proving that
        // the message from Frame 1 was successfully cleared.
        Assert.Empty(readingSystem.ReceivedMessages);
    }

    [Fact]
    public void DependencySort_ShouldOrderSystemsCorrectly()
    {
        // Arrange: C depends on B, B depends on A.
        var executionOrder = new List<string>();
        var world = new World();

        var systemC = new DependencyTrackingSystem("C", executionOrder, dependencies: ["B"]);
        var systemA = new DependencyTrackingSystem("A", executionOrder);
        var systemB = new DependencyTrackingSystem("B", executionOrder, dependencies: ["A"]);

        // Register out of order
        world.RegisterSystem(systemC);
        world.RegisterSystem(systemA);
        world.RegisterSystem(systemB);

        // Act: Update() will trigger the automatic sort.
        world.Update(0.1f);

        // Assert: The execution order should be A, then B, then C.
        var expectedOrder = new List<string> { "A", "B", "C" };
        Assert.Equal(expectedOrder, executionOrder);
    }

    [Fact]
    public void DependencySort_ShouldThrowOnCircularDependency()
    {
        // Arrange: A depends on B, and B depends on A.
        var executionOrder = new List<string>();
        var world = new World();

        var systemA = new DependencyTrackingSystem("A", executionOrder, dependencies: ["B"]);
        var systemB = new DependencyTrackingSystem("B", executionOrder, dependencies: ["A"]);

        world.RegisterSystem(systemA);
        world.RegisterSystem(systemB);

        // Act & Assert: Update() should detect the cycle and throw.
        var exception = Assert.Throws<InvalidOperationException>(() => world.Update(0.1f));
        Assert.Contains("Circular dependency detected", exception.Message);
    }

    [Fact]
    public void DependencySort_ShouldThrowOnUnresolvedDependency()
    {
        // Arrange: A depends on B, but B is never registered.
        var executionOrder = new List<string>();
        var world = new World();

        var systemA = new DependencyTrackingSystem("A", executionOrder, dependencies: ["B"]);

        world.RegisterSystem(systemA);

        // Act & Assert: Update() should detect the missing dependency and throw.
        var exception = Assert.Throws<InvalidOperationException>(() => world.Update(0.1f));
        Assert.Contains("unresolved dependency on 'B'", exception.Message);
    }
}
