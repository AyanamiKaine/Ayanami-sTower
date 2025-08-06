using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class WorldTests
{
    private readonly World _world;

    public WorldTests()
    {
        // Arrange: A new world is created for each test to ensure isolation.
        _world = new World();
    }

    [Fact]
    public void CreateEntity_ShouldReturnValidEntity()
    {
        // Act
        var entity = _world.CreateEntity();

        // Assert
        Assert.True(_world.IsEntityValid(entity));
        Assert.True(entity.IsValid());
    }

    [Fact]
    public void DestroyEntity_ShouldInvalidateEntityHandle()
    {
        // Arrange
        var entity = _world.CreateEntity();
        Assert.True(entity.IsValid(), "Pre-condition: Entity should be valid after creation.");

        // Act
        entity.Destroy();

        // Assert
        Assert.False(_world.IsEntityValid(entity));
        Assert.False(entity.IsValid());
    }

    [Fact]
    public void CreateEntity_AfterDestroy_ShouldRecycleIdWithNewGeneration()
    {
        // Arrange
        var entity1 = _world.CreateEntity();
        var originalId = entity1.Id;
        var originalGen = entity1.Generation;
        entity1.Destroy();

        // Act
        var entity2 = _world.CreateEntity(); // This should recycle the ID from entity1.

        // Assert
        Assert.Equal(originalId, entity2.Id); // ID is recycled
        Assert.NotEqual(originalGen, entity2.Generation); // Generation is incremented
        Assert.False(_world.IsEntityValid(entity1)); // The original handle is still invalid
        Assert.True(_world.IsEntityValid(entity2)); // The new handle is valid
    }

    [Fact]
    public void IsEntityValid_ForUncreatedEntity_ShouldReturnFalse()
    {
        // Arrange
        var uncreatedEntity = new Entity(999, 1, _world);

        // Act & Assert
        Assert.False(_world.IsEntityValid(uncreatedEntity));
    }
}
