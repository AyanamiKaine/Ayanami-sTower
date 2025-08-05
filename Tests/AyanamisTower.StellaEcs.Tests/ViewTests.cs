#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace AyanamisTower.StellaEcs.Tests;

/// <summary>
/// Tests for the high-performance View system.
/// </summary>
public class ViewTests
{
    // Test component structs
    public struct Position
    {
        public float X, Y;
        public Position(float x, float y) => (X, Y) = (x, y);
    }

    public struct Velocity
    {
        public float VX, VY;
        public Velocity(float vx, float vy) => (VX, VY) = (vx, vy);
    }

    public struct Health
    {
        public int Value;
        public Health(int value) => Value = value;
    }

    [Fact]
    public void SingleComponentView_ShouldReturnCorrectEntities()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity2.Add(new Position(3, 4));
        // entity3 has no Position component

        // Act
        var view = world.View<Position>();
        var entities = new System.Collections.Generic.List<Entity>();
        foreach (var entity in view)
        {
            entities.Add(entity);
        }

        // Assert
        Assert.Equal(2, view.Count);
        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void SingleComponentView_DirectAccess_ShouldWork()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Position(10, 20));
        entity2.Add(new Position(30, 40));

        // Act
        var view = world.View<Position>();

        // Assert
        Assert.Equal(2, view.Count);

        // Test direct access by index
        var (e1, pos1) = view.Get(0);
        var (e2, pos2) = view.Get(1);

        Assert.True(e1 == entity1 || e1 == entity2);
        Assert.True(e2 == entity1 || e2 == entity2);
        Assert.NotEqual(e1, e2);
    }

    [Fact]
    public void SingleComponentView_SpanAccess_ShouldWork()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Position(10, 20));
        entity2.Add(new Position(30, 40));

        // Act
        var view = world.View<Position>();
        var entityIds = view.EntityIds;
        var components = view.Components;

        // Assert
        Assert.Equal(2, entityIds.Length);
        Assert.Equal(2, components.Length);

        // Verify the data makes sense
        for (int i = 0; i < entityIds.Length; i++)
        {
            var entity = world.GetEntityFromId(entityIds[i]);
            Assert.True(entity == entity1 || entity == entity2);
        }
    }

    [Fact]
    public void SingleComponentView_MutableAccess_ShouldWork()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        entity.Add(new Position(10, 20));

        // Act
        var view = world.View<Position>();
        ref var component = ref view.GetComponentMutable(0);
        component.X = 100;

        // Assert
        Assert.Equal(100, entity.Get<Position>().X);
        Assert.Equal(20, entity.Get<Position>().Y);
    }

    [Fact]
    public void TwoComponentView_ShouldReturnOnlyEntitiesWithBothComponents()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();
        var entity4 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity1.Add(new Velocity(0.5f, 0.5f));

        entity2.Add(new Position(3, 4));
        // entity2 has no Velocity

        entity3.Add(new Velocity(1.0f, 1.0f));
        // entity3 has no Position

        entity4.Add(new Position(5, 6));
        entity4.Add(new Velocity(2.0f, 2.0f));

        // Act
        var view = world.View<Position, Velocity>();
        var entities = new System.Collections.Generic.List<Entity>();
        foreach (var entity in view)
        {
            entities.Add(entity);
        }

        // Assert
        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity4, entities);
        Assert.DoesNotContain(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void ThreeComponentView_ShouldReturnOnlyEntitiesWithAllComponents()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity1.Add(new Velocity(0.5f, 0.5f));
        entity1.Add(new Health(100));

        entity2.Add(new Position(3, 4));
        entity2.Add(new Velocity(1.0f, 1.0f));
        // entity2 has no Health

        entity3.Add(new Position(5, 6));
        entity3.Add(new Health(50));
        // entity3 has no Velocity

        // Act
        var view = world.View<Position, Velocity, Health>();
        var entities = new System.Collections.Generic.List<Entity>();
        foreach (var entity in view)
        {
            entities.Add(entity);
        }

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void View_Contains_ShouldReturnCorrectResults()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        // entity2 has no Position

        // Act
        var view = world.View<Position>();

        // Assert
        Assert.True(view.Contains(entity1));
        Assert.False(view.Contains(entity2));
    }

    [Fact]
    public void View_WithDeadEntity_ShouldNotIncludeDeadEntity()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity2.Add(new Position(3, 4));

        // Kill entity2
        entity2.Destroy();

        // Act
        var view = world.View<Position>();
        var entities = new System.Collections.Generic.List<Entity>();
        foreach (var entity in view)
        {
            entities.Add(entity);
        }

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void View_EstimatedCount_ShouldBeReasonable()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity1.Add(new Velocity(1, 1));

        entity2.Add(new Position(3, 4));
        // No Velocity for entity2

        entity3.Add(new Velocity(2, 2));
        // No Position for entity3

        // Act
        var view = world.View<Position, Velocity>();

        // Assert
        // EstimatedCount should be based on the smaller storage
        // Both Position and Velocity have entities, but only entity1 has both
        Assert.True(view.EstimatedCount > 0);
        Assert.Equal(1, view.Count); // Actual count should be 1
    }
}
