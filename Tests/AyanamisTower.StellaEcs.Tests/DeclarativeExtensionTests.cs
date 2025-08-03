using System;
using Xunit;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Examples;
using AyanamisTower.StellaEcs.Extensions;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Tests;

public class DeclarativeExtensionTests
{
    private World CreateWorld()
    {
        var world = new World();
        world.RegisterComponent<Position>();
        world.RegisterComponent<Velocity>();
        world.RegisterComponent<Health>();
        world.RegisterComponent<Damage>();
        world.RegisterComponent<Enemy>();
        world.RegisterComponent<Collectible>();
        return world;
    }

    [Fact]
    public void DeclarativeMove_OnlyExecutesWithBothComponents()
    {
        // Arrange
        var world = CreateWorld();
        var entityWithBoth = world.CreateEntity();
        var entityWithPosition = world.CreateEntity();
        var entityEmpty = world.CreateEntity();

        entityWithBoth.Add(new Position(0, 0, 0));
        entityWithBoth.Add(new Velocity(10, 20, 30));
        
        entityWithPosition.Add(new Position(5, 5, 5));

        // Act
        entityWithBoth.UpdateMovement(1.0f);
        entityWithPosition.UpdateMovement(1.0f); // Should not execute
        entityEmpty.UpdateMovement(1.0f); // Should not execute

        // Assert
        // Only entityWithBoth should have moved
        var movedPosition = entityWithBoth.Get<Position>();
        Assert.Equal(10, movedPosition.X);
        Assert.Equal(20, movedPosition.Y);
        Assert.Equal(30, movedPosition.Z);

        // entityWithPosition should be unchanged
        var unchangedPosition = entityWithPosition.Get<Position>();
        Assert.Equal(5, unchangedPosition.X);
        Assert.Equal(5, unchangedPosition.Y);
        Assert.Equal(5, unchangedPosition.Z);
    }

    [Fact]
    public void DeclarativeDamage_OnlyExecutesWithBothComponents()
    {
        // Arrange
        var world = CreateWorld();
        var entityWithBoth = world.CreateEntity();
        var entityWithHealth = world.CreateEntity();

        entityWithBoth.Add(new Health(100, 100));
        entityWithBoth.Add(new Damage(25));
        
        entityWithHealth.Add(new Health(80, 100));

        // Act
        entityWithBoth.ProcessDamage();
        entityWithHealth.ProcessDamage(); // Should not execute

        // Assert
        Assert.Equal(75, entityWithBoth.Get<Health>().Current);
        Assert.Equal(80, entityWithHealth.Get<Health>().Current); // Unchanged
    }

    [Fact]
    public void DeclarativeHeal_OnlyExecutesWithHealthComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entityWithHealth = world.CreateEntity();
        var entityEmpty = world.CreateEntity();

        entityWithHealth.Add(new Health(50, 100));

        // Act
        entityWithHealth.Heal(30);
        entityEmpty.Heal(30); // Should not execute

        // Assert
        Assert.Equal(80, entityWithHealth.Get<Health>().Current);
        Assert.False(entityEmpty.Has<Health>());
    }

    [Fact]
    public void ComponentMacros_WithComponent_ExecutesOnlyWhenPresent()
    {
        // Arrange
        var world = CreateWorld();
        var entityWithHealth = world.CreateEntity();
        var entityEmpty = world.CreateEntity();
        
        entityWithHealth.Add(new Health(100, 100));
        
        bool actionExecuted = false;

        // Act
        entityWithHealth.WithComponent<Health>(health => { actionExecuted = true; });
        
        bool actionExecuted2 = false;
        entityEmpty.WithComponent<Health>(health => { actionExecuted2 = true; });

        // Assert
        Assert.True(actionExecuted);
        Assert.False(actionExecuted2);
    }

    [Fact]
    public void ComponentMacros_WithComponentMutable_ModifiesComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Health(50, 100));

        // Act
        entity.WithComponentMutable<Health>((ref Health health) =>
        {
            health.Current = 75;
        });

        // Assert
        Assert.Equal(75, entity.Get<Health>().Current);
    }

    [Fact]
    public void ComponentMacros_WithComponents_RequiresBothComponents()
    {
        // Arrange
        var world = CreateWorld();
        var entityWithBoth = world.CreateEntity();
        var entityWithOne = world.CreateEntity();
        
        entityWithBoth.Add(new Position(1, 2, 3));
        entityWithBoth.Add(new Velocity(4, 5, 6));
        
        entityWithOne.Add(new Position(7, 8, 9));
        
        bool actionExecuted1 = false;
        bool actionExecuted2 = false;

        // Act
        entityWithBoth.WithComponents<Position, Velocity>((pos, vel) => { actionExecuted1 = true; });
        entityWithOne.WithComponents<Position, Velocity>((pos, vel) => { actionExecuted2 = true; });

        // Assert
        Assert.True(actionExecuted1);
        Assert.False(actionExecuted2);
    }

    [Fact]
    public void ComponentMacros_WithComponentsMixed_HandlesMutableAndReadonly()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new Position(0, 0, 0));
        entity.Add(new Velocity(10, 20, 30));

        // Act
        entity.WithComponentsMixed<Position, Velocity>((ref Position pos, in Velocity vel) =>
        {
            pos.X += vel.X;
            pos.Y += vel.Y;
            pos.Z += vel.Z;
        });

        // Assert
        var position = entity.Get<Position>();
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
        Assert.Equal(30, position.Z);
    }

    [Fact]
    public void EntityBuilder_CreatesEntityWithComponents()
    {
        // Arrange
        var world = CreateWorld();

        // Act
        var entity = world.CreateGameEntity()
            .WithPosition(10, 20, 30)
            .WithVelocity(1, 2, 3)
            .WithHealth(100, 100)
            .AsEnemy(25, 50f)
            .WithLoot(100, "Treasure")
            .Build();

        // Assert
        Assert.True(entity.Has<Position>());
        Assert.True(entity.Has<Velocity>());
        Assert.True(entity.Has<Health>());
        Assert.True(entity.Has<Enemy>());
        Assert.True(entity.Has<Collectible>());

        var position = entity.Get<Position>();
        Assert.Equal(10, position.X);
        Assert.Equal(20, position.Y);
        Assert.Equal(30, position.Z);
    }

    [Fact]
    public void EntityBuilder_Validation_ThrowsWhenComponentsMissing()
    {
        // Arrange
        var world = CreateWorld();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            world.CreateGameEntity()
                .WithPosition(0, 0, 0)
                // Missing Velocity
                .ValidateMovement()
                .Build());

        Assert.Throws<InvalidOperationException>(() =>
            world.CreateGameEntity()
                .WithPosition(0, 0, 0)
                // Missing Health
                .ValidateCombat()
                .Build());
    }

    [Fact]
    public void DeclarativeAI_UpdatesEnemyBehavior()
    {
        // Arrange
        var world = CreateWorld();
        
        var player = world.CreateEntity();
        player.Add(new Position(0, 0, 0));
        player.Add(new Health(100, 100));

        var enemy = world.CreateGameEntity()
            .WithPosition(30, 0, 0) // Within aggro range
            .WithVelocity(0, 0, 0)
            .WithHealth(50, 50)
            .AsEnemy(attackPower: 20, aggroRange: 40f)
            .Build();

        // Act
        enemy.UpdateAI(player, 1f/60f);

        // Assert
        var velocity = enemy.Get<Velocity>();
        Assert.True(Math.Abs(velocity.X) > 0, "Enemy should start moving towards player");
    }

    [Fact]
    public void DeclarativeAttack_DamagesTarget()
    {
        // Arrange
        var world = CreateWorld();
        
        var target = world.CreateEntity();
        target.Add(new Health(100, 100));

        var attacker = world.CreateEntity();
        attacker.Add(new Enemy(25, 50f));

        // Act
        attacker.AttackTarget(target);

        // Assert
        Assert.Equal(75, target.Get<Health>().Current);
    }

    [Fact]
    public void DeclarativeCheckDeath_HandlesDeathCorrectly()
    {
        // Arrange
        var world = CreateWorld();
        
        var dyingEntity = world.CreateGameEntity()
            .WithPosition(10, 20, 30)
            .WithHealth(0, 100) // Already dead
            .WithLoot(50, "Gold Coin")
            .Build();

        var aliveEntity = world.CreateEntity();
        aliveEntity.Add(new Health(50, 100));

        // Act
        bool died1 = dyingEntity.CheckDeath();
        bool died2 = aliveEntity.CheckDeath();

        // Assert
        Assert.True(died1);
        Assert.False(died2);
        Assert.False(dyingEntity.IsAlive()); // Should be destroyed
        Assert.True(aliveEntity.IsAlive());
    }

    [Fact]
    public void CompleteGameEntityUpdate_ChainsSystemsCorrectly()
    {
        // Arrange
        var world = CreateWorld();
        
        var player = world.CreateGameEntity()
            .WithPosition(0, 0, 0)
            .WithVelocity(5, 0, 0)
            .WithHealth(100, 100)
            .Build();

        var enemy = world.CreateGameEntity()
            .WithPosition(20, 0, 0)
            .WithVelocity(-2, 0, 0)
            .WithHealth(30, 30)
            .AsEnemy(15, 25f)
            .Build();

        // Add damage to enemy
        enemy.Add(new Damage(20));

        // Act
        player.UpdateGameEntity(1.0f);
        enemy.UpdateGameEntity(1.0f, player);

        // Assert
        // Player should have moved
        var playerPos = player.Get<Position>();
        Assert.Equal(5, playerPos.X);

        // Enemy should have moved and taken damage
        var enemyPos = enemy.Get<Position>();
        Assert.Equal(18, enemyPos.X); // Moved by velocity

        var enemyHealth = enemy.Get<Health>();
        Assert.Equal(10, enemyHealth.Current); // Took 20 damage
    }

    [Fact]
    public void DeclarativeSystem_IgnoresEntitiesWithoutRequiredComponents()
    {
        // Arrange
        var world = CreateWorld();
        
        // Create entities with partial components
        var positionOnly = world.CreateEntity();
        positionOnly.Add(new Position(1, 1, 1));

        var velocityOnly = world.CreateEntity();
        velocityOnly.Add(new Velocity(2, 2, 2));

        var healthOnly = world.CreateEntity();
        healthOnly.Add(new Health(50, 100));

        var complete = world.CreateEntity();
        complete.Add(new Position(0, 0, 0));
        complete.Add(new Velocity(10, 0, 0));
        complete.Add(new Health(100, 100));

        // Act
        positionOnly.UpdateMovement(1.0f);  // Should not move (no velocity)
        velocityOnly.UpdateMovement(1.0f);  // Should not move (no position)
        healthOnly.UpdateMovement(1.0f);    // Should not move (no position or velocity)
        complete.UpdateMovement(1.0f);      // Should move

        positionOnly.Heal(25);              // Should not heal (no health)
        healthOnly.Heal(25);                // Should heal
        complete.Heal(25);                  // Should heal (already at max, but should work)

        // Assert
        var pos1 = positionOnly.Get<Position>();
        Assert.Equal(1, pos1.X); // Unchanged

        // velocityOnly has no position to check

        var health1 = healthOnly.Get<Health>();
        Assert.Equal(75, health1.Current); // Healed

        var pos2 = complete.Get<Position>();
        Assert.Equal(10, pos2.X); // Moved

        var health2 = complete.Get<Health>();
        Assert.Equal(100, health2.Current); // Already at max, so unchanged
    }
}
