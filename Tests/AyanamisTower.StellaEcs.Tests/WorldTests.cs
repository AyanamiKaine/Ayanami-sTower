#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using System.Collections.Generic;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;

// Test components
public struct Position { public float X, Y; }
public struct Velocity { public float Dx, Dy; }
public struct PlayerTag { } // A tag component
public struct IsHidden { } // Another tag component


public class WorldTests
{
    // --- Previous tests for entity lifecycle and component manipulation are omitted for brevity ---
    // --- They remain unchanged and are still valid ---

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

    // --- NEW FLUENT QUERY TESTS ---

    [Fact]
    public void Query_SingleComponent_ReturnsCorrectEntities()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();

        var e1 = world.CreateEntity(); world.AddComponent(e1, new Position());
        var e2 = world.CreateEntity(); world.AddComponent(e2, new Velocity());
        var e3 = world.CreateEntity(); world.AddComponent(e3, new Position());

        // Act
        var query = world.Query().With<Position>().Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(e1, results);
        Assert.Contains(e3, results);
    }

    [Fact]
    public void Query_TwoComponents_ReturnsCorrectEntities()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();

        var e1 = world.CreateEntity(); world.AddComponent(e1, new Position());
        var e2 = world.CreateEntity(); world.AddComponent(e2, new Velocity());
        var e3 = world.CreateEntity(); world.AddComponent(e3, new Position()); world.AddComponent(e3, new Velocity());

        // Act
        var query = world.Query().With<Position>().With<Velocity>().Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains(e3, results);
    }

    [Fact]
    public void Query_ThreeComponents_ReturnsCorrectEntities()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();
        world.RegisterComponent<PlayerTag>();

        var e1 = world.CreateEntity(); world.AddComponent(e1, new Position());
        var e2 = world.CreateEntity(); world.AddComponent(e2, new Position()); world.AddComponent(e2, new Velocity());
        var e3 = world.CreateEntity(); world.AddComponent(e3, new Position()); world.AddComponent(e3, new Velocity()); world.AddComponent(e3, new PlayerTag());

        // Act
        var query = world.Query().With<Position>().With<Velocity>().With<PlayerTag>().Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains(e3, results);
    }

    [Fact]
    public void Query_WithWithoutClause_ReturnsCorrectEntities()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<IsHidden>();

        var e1 = world.CreateEntity(); world.AddComponent(e1, new Position());
        var e2 = world.CreateEntity(); world.AddComponent(e2, new Position()); world.AddComponent(e2, new IsHidden());
        var e3 = world.CreateEntity(); world.AddComponent(e3, new Position());

        // Act
        var query = world.Query().With<Position>().Without<IsHidden>().Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(e1, results);
        Assert.Contains(e3, results);
    }

    [Fact]
    public void Query_ComplexWithAndWithout_ReturnsCorrectEntities()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();
        world.RegisterComponent<PlayerTag>();
        world.RegisterComponent<IsHidden>();

        // Should match: Has Pos, Vel, PlayerTag. Does NOT have IsHidden.
        var e1 = world.CreateEntity(); world.AddComponent(e1, new Position()); world.AddComponent(e1, new Velocity()); world.AddComponent(e1, new PlayerTag());
        // Should NOT match: Missing Velocity
        var e2 = world.CreateEntity(); world.AddComponent(e2, new Position()); world.AddComponent(e2, new PlayerTag());
        // Should NOT match: Has IsHidden
        var e3 = world.CreateEntity(); world.AddComponent(e3, new Position()); world.AddComponent(e3, new Velocity()); world.AddComponent(e3, new PlayerTag()); world.AddComponent(e3, new IsHidden());

        // Act
        var query = world.Query()
            .With<Position>()
            .With<PlayerTag>()
            .With<Velocity>()
            .Without<IsHidden>()
            .Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Single(results);
        Assert.Contains(e1, results);
    }

    [Fact]
    public void Query_DriverStorageOptimization_IsCorrect()
    {
        // Arrange
        var world = new World();
        world.RegisterComponent<Position>();  // Many instances
        world.RegisterComponent<PlayerTag>(); // Few instances

        for (int i = 0; i < 10; i++)
        {
            var e = world.CreateEntity();
            world.AddComponent(e, new Position());
        }

        var p1 = world.CreateEntity();
        world.AddComponent(p1, new Position());
        world.AddComponent(p1, new PlayerTag());

        var p2 = world.CreateEntity();
        world.AddComponent(p2, new Position());
        world.AddComponent(p2, new PlayerTag());

        // The query should be smart enough to iterate over the 2 PlayerTag components
        // instead of the 12 Position components.

        // Act
        var query = world.Query().With<Position>().With<PlayerTag>().Build();
        var results = new List<Entity>();
        foreach (var entity in query)
        {
            results.Add(entity);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(p1, results);
        Assert.Contains(p2, results);
    }

    [Fact]
    public void Query_WithoutWithClause_ThrowsException()
    {
        // Arrange
        var world = new World();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => world.Query().Build());
        Assert.Equal("A query must have at least one 'With' component specified.", ex.Message);
    }
}
