namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Testing the query class
/// </summary>
public class QueryTests
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

    public struct Health(int value)
    {
        public int Value = value;
    }

    public struct Enemy(bool isActive = true)
    {
        public bool IsActive = isActive;
    }

    // Helper method to convert QueryEnumerable to array since ref structs can't use LINQ
    private static Entity[] ToArray(QueryEnumerable query)
    {
        var entities = new List<Entity>();
        foreach (var entity in query)
        {
            entities.Add(entity);
        }
        return [.. entities];
    }

    [Fact]
    public void QueryBuilder_With_SingleComponent_ShouldReturnCorrectEntities()
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
        var entities = ToArray(world.Query().With<Position>().Build());

        // Assert
        Assert.Equal(2, entities.Length);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void QueryBuilder_With_MultipleComponents_ShouldReturnOnlyEntitiesWithAllComponents()
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
        var entities = ToArray(world.Query()
            .With<Position>()
            .With<Velocity>()
            .Build());

        // Assert
        Assert.Equal(2, entities.Length);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity4, entities);
        Assert.DoesNotContain(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void QueryBuilder_Without_ShouldExcludeEntitiesWithSpecifiedComponent()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Position(1, 2));

        entity2.Add(new Position(3, 4));
        entity2.Add(new Enemy());

        entity3.Add(new Position(5, 6));

        // Act
        var entities = ToArray(world.Query()
            .With<Position>()
            .Without<Enemy>()
            .Build());

        // Assert
        Assert.Equal(2, entities.Length);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity3, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void QueryBuilder_Where_ShouldFilterBasedOnComponentData()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Health(100));
        entity2.Add(new Health(50));
        entity3.Add(new Health(10));

        // Act
        var entities = ToArray(world.Query()
            .Where<Health>(h => h.Value > 25)
            .Build());

        // Assert
        Assert.Equal(2, entities.Length);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.DoesNotContain(entity3, entities);
    }

    [Fact]
    public void QueryBuilder_Where_ShouldAutomaticallyAddWithClause()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Health(100));
        // entity2 has no Health component

        // Act - Using Where without explicit With<Health>()
        var entities = ToArray(world.Query()
            .Where<Health>(h => h.Value > 50)
            .Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void QueryBuilder_ComplexQuery_ShouldWorkCorrectly()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();
        var entity4 = world.CreateEntity();
        var entity5 = world.CreateEntity();

        // Moving enemy with low health
        entity1.Add(new Position(1, 2));
        entity1.Add(new Velocity(0.5f, 0.5f));
        entity1.Add(new Health(20));
        entity1.Add(new Enemy());

        // Stationary enemy with high health
        entity2.Add(new Position(3, 4));
        entity2.Add(new Health(100));
        entity2.Add(new Enemy());

        // Moving friendly with low health
        entity3.Add(new Position(5, 6));
        entity3.Add(new Velocity(1.0f, 1.0f));
        entity3.Add(new Health(15));

        // Moving enemy with high health
        entity4.Add(new Position(7, 8));
        entity4.Add(new Velocity(2.0f, 2.0f));
        entity4.Add(new Health(80));
        entity4.Add(new Enemy());

        // Stationary friendly with low health
        entity5.Add(new Position(9, 10));
        entity5.Add(new Health(25));

        // Act - Find moving enemies with low health
        var entities = ToArray(world.Query()
            .With<Position>()
            .With<Velocity>()
            .With<Enemy>()
            .Where<Health>(h => h.Value < 50)
            .Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
    }

    [Fact]
    public void QueryBuilder_NoWithClause_ShouldThrowException()
    {
        // Arrange
        var world = new World();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            world.Query().Without<Enemy>().Build());
    }

    [Fact]
    public void QueryBuilder_EmptyQuery_ShouldReturnEmptyResults()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        entity.Add(new Position(1, 2));

        // Act
        var entities = ToArray(world.Query()
            .With<Velocity>()  // No entity has Velocity
            .Build());

        // Assert
        Assert.Empty(entities);
    }

    [Fact]
    public void QueryBuilder_WithDeadEntity_ShouldNotReturnDeadEntity()
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
        var entities = ToArray(world.Query()
            .With<Position>()
            .Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void QueryBuilder_MultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        entity1.Add(new Position(10, 20));
        entity1.Add(new Health(75));

        entity2.Add(new Position(5, 15));
        entity2.Add(new Health(90));

        entity3.Add(new Position(15, 25));
        entity3.Add(new Health(60));

        // Act - Find entities with X > 7 AND Health > 70
        var entities = ToArray(world.Query()
            .Where<Position>(p => p.X > 7)
            .Where<Health>(h => h.Value > 70)
            .Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
    }

    [Fact]
    public void QueryBuilder_NonGenericTypes_ShouldWork()
    {
        // Arrange
        var world = new World();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        entity1.Add(new Position(1, 2));
        entity2.Add(new Position(3, 4));
        entity2.Add(new Velocity(1, 1));

        // Act
        var entities = ToArray(world.Query()
            .With(typeof(Position))
            .Without(typeof(Velocity))
            .Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
    }

    [Fact]
    public void QueryBuilder_FluentInterface_ShouldAllowMethodChaining()
    {
        // Arrange
        var world = new World();
        var entity = world.CreateEntity();
        entity.Add(new Position(1, 2));
        entity.Add(new Health(100));

        // Act - Test that fluent interface works
        var query = world.Query()
            .With<Position>()
            .With<Health>()
            .Where<Health>(h => h.Value > 50)
            .Without<Enemy>();

        var entities = ToArray(query.Build());

        // Assert
        Assert.Single(entities);
        Assert.Contains(entity, entities);
    }
}
