// --------------------------------------------------------------------
// GameComponents.cs
// --------------------------------------------------------------------
// Note: I've created these component definitions based on your tests.
// In your project, these would likely be in their own file.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Tests.Examples
{
    public struct Position(float x, float y, float z)
    {
        public float X = x;
        public float Y = y;
        public float Z = z;
    }

    public struct Velocity(float x, float y, float z)
    {
        public float X = x;
        public float Y = y;
        public float Z = z;
    }

    public struct Health(int current, int max)
    {
        public int Current = current;
        public int Max = max;
    }

    public struct Damage(int amount)
    {
        public int Amount = amount;
    }

    // This is an empty "tag" component.
    public struct Enemy;

    public struct Collectible(int value, string type)
    {
        public int Value = value;
        public string Type = type;
    }
}


// --------------------------------------------------------------------
// GameLogicExtensions.cs
// --------------------------------------------------------------------
// This is the new file that defines the extension methods with the
// declarative [With] attributes that your tests are validating.
namespace AyanamisTower.StellaEcs.Tests.Examples
{
    using AyanamisTower.StellaEcs.Attributes;
    using AyanamisTower.StellaEcs.Extensions;
    using System;

    [EntityExtensions]
    public static class GameLogicExtensions
    {
        /// <summary>
        /// Updates an entity's position based on its velocity.
        /// This method will only execute if the entity has BOTH Position and Velocity.
        /// </summary>
        [With<Position>(Mutable = true)]
        [With<Velocity>]
        public static void UpdateMovement(this Entity entity, float deltaTime)
        {
            entity.ExecuteIfComponents(() =>
            {
                ref var pos = ref entity.GetMutable<Position>();
                ref readonly var vel = ref entity.Get<Velocity>();
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
                pos.Z += vel.Z * deltaTime;
            });
        }

        /// <summary>
        /// Applies damage to an entity and then removes the Damage component.
        /// This method requires both Health and Damage components to be present.
        /// </summary>
        [With<Health>(Mutable = true)]
        [With<Damage>]
        public static void ProcessDamage(this Entity entity)
        {
            entity.ExecuteIfComponents(() =>
            {
                ref var health = ref entity.GetMutable<Health>();
                ref readonly var damage = ref entity.Get<Damage>();
                health.Current -= damage.Amount;

                // After processing, it's common to remove the "event" component.
                entity.Remove<Damage>();
            });
        }

        /// <summary>
        /// Heals an entity by a certain amount.
        /// Requires only the Health component.
        /// </summary>
        [With<Health>(Mutable = true)]
        public static void Heal(this Entity entity, int amount)
        {
            entity.ExecuteIfComponents(() =>
            {
                ref var health = ref entity.GetMutable<Health>();
                health.Current = Math.Min(health.Max, health.Current + amount);
            });
        }

        /// <summary>
        /// Checks if an entity with health is dead. If so, it destroys the entity.
        /// Returns true if the entity was dead, false otherwise.
        /// </summary>
        [With<Health>]
        public static bool CheckDeath(this Entity entity)
        {
            return entity.ExecuteIfComponents(() =>
            {
                if (entity.Get<Health>().Current <= 0)
                {
                    entity.Destroy();
                    return true;
                }
                return false;
            }, defaultValue: false);
        }
    }
}


// --------------------------------------------------------------------
// DeclarativeExtensionTests.cs (Rewritten)
// --------------------------------------------------------------------
// This is your test file, rewritten to correctly test the declarative
// extension methods defined above.
namespace AyanamisTower.StellaEcs.Tests
{
    using System;
    using Xunit;
    using AyanamisTower.StellaEcs.Tests.Examples; // Use the namespace for our new components/extensions
    using AyanamisTower.StellaEcs.Extensions;

    public class DeclarativeExtensionTests
    {
        private World CreateWorld()
        {
            var world = new World();
            // Registering components is still necessary.
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
            // The UpdateMovement method is now correctly decorated with [With] attributes.
            entityWithBoth.UpdateMovement(1.0f);
            entityWithPosition.UpdateMovement(1.0f); // Should be ignored by ExecuteIfComponents
            entityEmpty.UpdateMovement(1.0f);      // Should be ignored by ExecuteIfComponents

            // Assert
            // Only entityWithBoth should have moved
            var movedPosition = entityWithBoth.Get<Position>();
            Assert.Equal(10, movedPosition.X);
            Assert.Equal(20, movedPosition.Y);
            Assert.Equal(30, movedPosition.Z);

            // entityWithPosition should be unchanged because it lacks a Velocity component
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
            entityWithHealth.ProcessDamage(); // Should be ignored (missing Damage component)

            // Assert
            Assert.Equal(75, entityWithBoth.Get<Health>().Current);
            Assert.False(entityWithBoth.Has<Damage>(), "Damage component should be removed after processing.");
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
            entityEmpty.Heal(30); // Should be ignored

            // Assert
            Assert.Equal(80, entityWithHealth.Get<Health>().Current);
            Assert.False(entityEmpty.Has<Health>());
        }

        [Fact]
        public void DeclarativeCheckDeath_HandlesDeathCorrectly()
        {
            // Arrange
            var world = CreateWorld();

            var dyingEntity = world.CreateEntity();
            dyingEntity.Add(new Health(0, 100)); // Already dead
            dyingEntity.Add(new Collectible(50, "Gold Coin"));

            var aliveEntity = world.CreateEntity();
            aliveEntity.Add(new Health(50, 100));

            // Act
            // The CheckDeath method is decorated with [With<Health>]
            bool died1 = dyingEntity.CheckDeath();
            bool died2 = aliveEntity.CheckDeath();

            // Assert
            Assert.True(died1, "CheckDeath should return true for the dead entity.");
            Assert.False(died2, "CheckDeath should return false for the alive entity.");
            Assert.False(dyingEntity.IsAlive(), "The dead entity should be destroyed.");
            Assert.True(aliveEntity.IsAlive(), "The alive entity should not be destroyed.");
        }

        // The tests for ComponentMacros were already correct as they test the
        // macro extensions directly, not a custom system. I'm including them
        // here for completeness.
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
    }
}
