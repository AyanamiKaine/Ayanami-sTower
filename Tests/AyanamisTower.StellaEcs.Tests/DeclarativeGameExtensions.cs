using System;
using AyanamisTower.StellaEcs.Attributes;
using AyanamisTower.StellaEcs.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.Examples;

/// <summary>
/// Advanced examples demonstrating the declarative component system
/// with fluent method chaining and automatic component validation.
/// </summary>
[EntityExtensions]
public static class DeclarativeGameExtensions
{
    /// <summary>
    /// Movement system using declarative component requirements.
    /// No manual HasAll checks needed!
    /// </summary>
    [EntityExtension]
    [With<Position>(mutable: true)]
    [With<Velocity>]
    public static void UpdateMovement(this Entity entity, float deltaTime)
    {
        // This method will only execute if the entity has both Position and Velocity
        entity.WithComponentsMixed<Position, Velocity>((ref Position pos, in Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
            pos.Z += vel.Z * deltaTime;
        });
    }

    /// <summary>
    /// Combat system using declarative requirements.
    /// </summary>
    [EntityExtension]
    [With<Health>(mutable: true)]
    [With<Damage>]
    public static void ProcessDamage(this Entity entity)
    {
        entity.WithComponentsMixed<Health, Damage>((ref Health health, in Damage damage) =>
        {
            int newHealth = Math.Max(0, health.Current - damage.Amount);
            Console.WriteLine($"Entity took {damage.Amount} damage! Health: {health.Current} -> {newHealth}");
            health.Current = newHealth;
        });
    }

    /// <summary>
    /// Healing system - only works on entities with Health.
    /// </summary>
    [EntityExtension]
    [With<Health>(mutable: true)]
    public static void Heal(this Entity entity, int amount)
    {
        entity.WithComponentMutable<Health>((ref Health health) =>
        {
            int oldHealth = health.Current;
            health.Current = Math.Min(health.Max, health.Current + amount);
            Console.WriteLine($"Entity healed for {health.Current - oldHealth} HP");
        });
    }

    /// <summary>
    /// Death check system - automatically handles entity death.
    /// </summary>
    [EntityExtension]
    [With<Health>]
    public static bool CheckDeath(this Entity entity)
    {
        return entity.ExecuteIfComponents(() =>
        {
            var health = entity.Get<Health>();
            if (health.Current <= 0)
            {
                Console.WriteLine($"Entity {entity} has died!");
                entity.OnDeath(); // Chain to death handler
                return true;
            }
            return false;
        }, false);
    }

    /// <summary>
    /// Death handling system.
    /// </summary>
    [EntityExtension]
    public static void OnDeath(this Entity entity)
    {
        // Drop loot if the entity has it
        entity.WithComponent<Collectible>(loot =>
        {
            Console.WriteLine($"Dropped {loot.Type} worth {loot.Value} points!");
        });

        // Apply death effects
        entity.WithComponent<Position>(pos =>
        {
            Console.WriteLine($"Death occurred at ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
        });

        // Could spawn death particles, play sounds, etc.
        entity.Destroy();
    }

    /// <summary>
    /// AI system for enemies - uses multiple declarative components.
    /// </summary>
    [EntityExtension]
    [With<Position>]
    [With<Enemy>]
    [With<Velocity>(mutable: true)]
    public static void UpdateAI(this Entity entity, Entity target, float deltaTime)
    {
        if (!target.Has<Position>()) return;

        entity.WithComponents<Position, Enemy>((pos, enemy) =>
        {
            var targetPos = target.Get<Position>();
            float distance = CalculateDistance(pos, targetPos);

            if (distance <= enemy.AggroRange)
            {
                // Move towards target
                entity.WithComponentMutable<Velocity>((ref Velocity vel) =>
                {
                    float direction = Math.Sign(targetPos.X - pos.X);
                    vel.X = direction * 50f; // Chase speed
                    Console.WriteLine($"Enemy chasing target! Distance: {distance:F1}");
                });

                // Attack if close enough
                if (distance < 5f && target.Has<Health>())
                {
                    entity.AttackTarget(target);
                }
            }
        });
    }

    /// <summary>
    /// Attack system - automatically applies damage to target.
    /// </summary>
    [EntityExtension]
    [With<Enemy>]
    public static void AttackTarget(this Entity attacker, Entity target)
    {
        attacker.WithComponent<Enemy>(enemy =>
        {
            if (target.Has<Health>())
            {
                // Add temporary damage component to target
                target.Set(new Damage(enemy.AttackPower));
                target.ProcessDamage(); // Process the damage immediately
                target.Remove<Damage>(); // Remove the temporary damage
            }
        });
    }

    /// <summary>
    /// Complete game entity update - chains multiple systems declaratively.
    /// </summary>
    [EntityExtension]
    public static void UpdateGameEntity(this Entity entity, float deltaTime, Entity? player = null)
    {
        // All these methods use declarative component requirements
        // They will only execute if the entity has the required components!

        entity.UpdateMovement(deltaTime);    // Requires: Position + Velocity
        entity.ProcessDamage();              // Requires: Health + Damage  

        // AI update for enemies
        if (entity.Has<Enemy>() && player != null)
        {
            entity.UpdateAI(player.Value, deltaTime); // Requires: Position + Enemy + Velocity
        }

        // Death check
        entity.CheckDeath(); // Requires: Health
    }

    /// <summary>
    /// Fluent builder pattern for creating entities with component validation.
    /// </summary>
    public static EntityBuilder CreateGameEntity(this World world)
    {
        return new EntityBuilder(world.CreateEntity());
    }

    private static float CalculateDistance(Position pos1, Position pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

/// <summary>
/// Fluent builder for creating game entities with automatic component validation.
/// </summary>
public struct EntityBuilder
{
    private readonly Entity _entity;

    public EntityBuilder(Entity entity)
    {
        _entity = entity;
    }

    public EntityBuilder WithPosition(float x, float y, float z = 0)
    {
        _entity.Add(new Position(x, y, z));
        return this;
    }

    public EntityBuilder WithVelocity(float x, float y, float z = 0)
    {
        _entity.Add(new Velocity(x, y, z));
        return this;
    }

    public EntityBuilder WithHealth(int current, int max)
    {
        _entity.Add(new Health(current, max));
        return this;
    }

    public EntityBuilder AsEnemy(int attackPower, float aggroRange)
    {
        _entity.Add(new Enemy(attackPower, aggroRange));
        return this;
    }

    public EntityBuilder WithLoot(int value, string type)
    {
        _entity.Add(new Collectible(value, type));
        return this;
    }

    /// <summary>
    /// Validates that the entity has required components for specific behaviors.
    /// </summary>
    public EntityBuilder ValidateMovement()
    {
        if (!_entity.HasAll<Position, Velocity>())
            throw new InvalidOperationException("Entity requires Position and Velocity for movement");
        return this;
    }

    public EntityBuilder ValidateCombat()
    {
        if (!_entity.Has<Health>())
            throw new InvalidOperationException("Entity requires Health for combat");
        return this;
    }

    public Entity Build() => _entity;

    public static implicit operator Entity(EntityBuilder builder) => builder._entity;
}
