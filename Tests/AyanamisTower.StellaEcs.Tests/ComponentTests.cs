using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ComponentTests
{
    private readonly World _world;
    private readonly Entity _entity;

    public ComponentTests()
    {
        _world = new World();
        _entity = _world.CreateEntity();
    }

    [Fact]
    public void SetAndGetComponent_ShouldStoreAndRetrieveCorrectValue()
    {
        // Arrange
        var position = new PositionComponent { X = 10, Y = 20 };

        // Act
        _entity.Set(position);
        var retrieved = _entity.Get<PositionComponent>();

        // Assert
        Assert.True(_entity.Has<PositionComponent>());
        Assert.Equal(10, retrieved.X);
        Assert.Equal(20, retrieved.Y);
    }

    [Fact]
    public void GetMutComponent_ShouldAllowInPlaceModification()
    {
        // Arrange
        _entity.Set(new PositionComponent { X = 10, Y = 20 });

        // Act
        ref var mutablePosition = ref _entity.GetMut<PositionComponent>();
        mutablePosition.X = 50;

        // Assert
        var finalPosition = _entity.Get<PositionComponent>();
        Assert.Equal(50, finalPosition.X);
    }

    [Fact]
    public void RemoveComponent_ShouldMakeComponentUnavailable()
    {
        // Arrange
        _entity.Set(new PositionComponent { X = 1, Y = 1 });
        Assert.True(_entity.Has<PositionComponent>(), "Pre-condition: Entity must have the component.");

        // Act
        _entity.Remove<PositionComponent>();

        // Assert
        Assert.False(_entity.Has<PositionComponent>());
    }

    [Fact]
    public void GetComponent_OnEntityWithoutComponent_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _entity.Get<PositionComponent>());
    }

    [Fact]
    public void InteractingWithInvalidEntity_ShouldThrowArgumentException()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.Destroy();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => entity.Set(new PositionComponent()));
        Assert.Throws<ArgumentException>(() => entity.Get<PositionComponent>());
        Assert.Throws<ArgumentException>(() => entity.Has<PositionComponent>());
        Assert.Throws<ArgumentException>(() => entity.Remove<PositionComponent>());
    }
}
