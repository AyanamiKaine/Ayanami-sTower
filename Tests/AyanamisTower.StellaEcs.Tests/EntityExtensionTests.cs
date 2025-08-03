using System;
using Xunit;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Examples;
using AyanamisTower.StellaEcs.Extensions;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Tests;

public class EntityExtensionTests
{
    private World CreateWorld()
    {
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();
        world.RegisterComponent<Health>();
        world.RegisterComponent<Damage>();
        return world;
    }

    [Fact]
    public void BasicComponentAccess_Works()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(10, 20, 30));
        entity.Add(new Health(100, 100));

        // Act & Assert
        entity.With<Position>(pos =>
        {
            Assert.Equal(10, pos.X);
            Assert.Equal(20, pos.Y);
            Assert.Equal(30, pos.Z);
        });

        entity.With<Health>(health =>
        {
            Assert.Equal(100, health.Current);
            Assert.Equal(100, health.Max);
        });
    }

    [Fact]
    public void MultipleComponentAccess_Works()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(5, 10, 15));
        entity.Add(new Velocity(1, 2, 3));

        // Act & Assert
        entity.With<Position, Velocity>((pos, vel) =>
        {
            Assert.Equal(5, pos.X);
            Assert.Equal(1, vel.X);
        });
    }

    [Fact]
    public void MutableComponentAccess_ModifiesComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(80, 100));

        // Act
        entity.WithMutable<Health>((ref Health health) => health.Current = 90);

        // Assert
        Assert.Equal(90, entity.Get<Health>().Current);
    }

    [Fact]
    public void GameEntityExtensions_Move_UpdatesPosition()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(0, 0, 0));
        entity.Add(new Velocity(10, 20, 30));

        // Act
        entity.Move(1.0f); // Move for 1 second

        // Assert
        var position = entity.Get<Position>();
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
        Assert.Equal(30, position.Z);
    }

    [Fact]
    public void GameEntityExtensions_ApplyDamage_ReducesHealth()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(100, 100));
        entity.Add(new Damage(25));

        // Act
        entity.ApplyDamage();

        // Assert
        Assert.Equal(75, entity.Get<Health>().Current);
    }

    [Fact]
    public void GameEntityExtensions_IsAtPosition_ReturnsTrueForCorrectPosition()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(1, 2, 3));

        // Act & Assert
        Assert.True(entity.IsAtPosition(1, 2, 3));
        Assert.False(entity.IsAtPosition(1, 2, 4));
    }

    [Fact]
    public void GameEntityExtensions_DistanceTo_CalculatesCorrectDistance()
    {
        // Arrange
        var world = CreateWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        entity1.Add(new Position(0, 0, 0));
        entity2.Add(new Position(3, 4, 0)); // 3-4-5 triangle

        // Act
        var distance = entity1.DistanceTo(entity2);

        // Assert
        Assert.Equal(5.0f, distance, 2); // Allow small floating point error
    }

    [Fact]
    public void GameEntityExtensions_HealToFull_RestoresMaxHealth()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(50, 100));

        // Act
        entity.HealToFull();

        // Assert
        Assert.Equal(100, entity.Get<Health>().Current);
    }

    [Fact]
    public void TryWith_ReturnsFalseWhenComponentMissing()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        // Don't add Position component

        // Act
        bool executed = entity.TryWith<Position>(pos => Assert.Fail("This should not execute"));

        // Assert
        Assert.False(executed);
    }

    [Fact]
    public void TryWith_ReturnsTrueWhenComponentExists()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(1, 2, 3));
        bool actionExecuted = false;

        // Act
        bool executed = entity.TryWith<Position>(pos => 
        {
            actionExecuted = true;
            Assert.Equal(1, pos.X);
        });

        // Assert
        Assert.True(executed);
        Assert.True(actionExecuted);
    }

    [Fact]
    public void HasAll_ReturnsTrueWhenAllComponentsPresent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(0, 0, 0));
        entity.Add(new Velocity(1, 1, 1));

        // Act & Assert
        Assert.True(entity.HasAll<Position, Velocity>());
        Assert.False(entity.HasAll<Position, Health>());
    }

    [Fact]
    public void HasAny_ReturnsTrueWhenAnyComponentPresent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(0, 0, 0));

        // Act & Assert
        Assert.True(entity.HasAny<Position, Velocity>());
        Assert.False(entity.HasAny<Health, Damage>());
    }

    [Fact]
    public void WithFunctionOverload_ReturnsResult()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(75, 100));

        // Act
        float healthPercentage = entity.With<Health, float>(health => 
            (float)health.Current / health.Max);

        // Assert
        Assert.Equal(0.75f, healthPercentage);
    }

    [Fact]
    public void ComplexCombinedOperations_Work()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(0, 0, 0));
        entity.Add(new Velocity(5, 0, 0));
        entity.Add(new Health(100, 100));
        entity.Add(new Damage(30));

        // Act - Use the complex UpdateCombat method
        entity.UpdateCombat(2.0f);

        // Assert
        // Position should be updated by velocity
        var position = entity.Get<Position>();
        Assert.Equal(10, position.X);
        
        // Health should be reduced by damage
        var health = entity.Get<Health>();
        Assert.Equal(70, health.Current);
        
        // Damage should be removed after application
        Assert.False(entity.Has<Damage>());
    }

    [Fact]
    public void AdvancedExtensions_Transform_ModifiesComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(50, 100));

        // Act
        bool transformed = entity.Transform<Health>(health => 
            new Health(health.Current + 25, health.Max));

        // Assert
        Assert.True(transformed);
        Assert.Equal(75, entity.Get<Health>().Current);
    }

    [Fact]
    public void AdvancedExtensions_GetOrDefault_ReturnsDefaultWhenMissing()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        var health = entity.GetOrDefault<Health>(new Health(50, 50));

        // Assert
        Assert.Equal(50, health.Current);
        Assert.Equal(50, health.Max);
    }

    [Fact]
    public void AdvancedExtensions_EnsureComponent_AddsIfMissing()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        ref var health = ref entity.EnsureComponent<Health>(new Health(100, 100));
        health.Current = 80; // Modify through reference

        // Assert
        Assert.True(entity.Has<Health>());
        Assert.Equal(80, entity.Get<Health>().Current);
        Assert.Equal(100, entity.Get<Health>().Max);
    }

    [Fact]
    public void ConditionalBuilder_ExecutesCorrectBranches()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(50, 100));
        bool thenExecuted = false;
        bool elseExecuted = false;

        // Act - Test Then branch
        _ = entity.If<Health>()
            .Then<Health>(_ => thenExecuted = true);

        entity.If<Position>()
            .Then<Position>(_ => Assert.Fail("Should not execute"))
            .Else(() => elseExecuted = true);

        // Assert
        Assert.True(thenExecuted);
        Assert.True(elseExecuted);
    }
}
