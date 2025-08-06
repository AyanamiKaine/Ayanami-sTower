using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AyanamisTower.StellaEcs;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Perfolizer.Metrology;

// --- Components for Benchmarking ---
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct Position { public float X, Y, Z; }
public struct Velocity { public float Dx, Dy, Dz; }
public struct Rotation { public float Qx, Qy, Qz, Qw; }
public struct IsPlayer; // Tag component
public struct IsFrozen; // Tag component
public struct Health { public float Value; }
public struct Damage { public float Amount; }
public struct Lifetime { public float TimeRemaining; }
public struct Target { public Entity TargetEntity; }

public struct IsEnemy;
public struct IsParticle;

/// <summary>
/// A simple system that updates the position of entities based on their velocity.
/// </summary>
public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var query = world.Query(typeof(Position), typeof(Velocity));
        foreach (var entity in query)
        {
            ref var pos = ref entity.GetMut<Position>();
            ref readonly var vel = ref entity.GetMut<Velocity>();
            pos.X += vel.Dx * deltaTime;
            pos.Y += vel.Dy * deltaTime;
            pos.Z += vel.Dz * deltaTime;
        }
    }
}

/// <summary>
/// A system that simulates lifetime, destroying entities when their time runs out.
/// This involves component removal and entity destruction, making it a good test case.
/// </summary>
public class LifetimeSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // We must .ToList() here because destroying an entity modifies the collection we are iterating over.
        var query = world.Query(typeof(Lifetime)).ToList();
        foreach (var entity in query)
        {
            // Ensure the entity is still valid, as a previous operation in the same frame
            // could have already destroyed it.
            if (!entity.IsValid()) continue;

            ref var lifetime = ref entity.GetMut<Lifetime>();
            lifetime.TimeRemaining -= deltaTime;

            if (lifetime.TimeRemaining <= 0)
            {
                world.DestroyEntity(entity);
            }
        }
    }
}

/// <summary>
/// A system that applies damage to entities with health.
/// This simulates events and state changes.
/// </summary>
public class DamageSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // We must .ToList() here because we are removing the Damage component.
        var query = world.Query(typeof(Health), typeof(Damage)).ToList();
        foreach (var entity in query)
        {
            if (!entity.IsValid()) continue;

            ref var health = ref entity.GetMut<Health>();
            var damage = entity.Get<Damage>().Amount;
            health.Value -= damage;

            // The damage is applied, so we remove the component.
            entity.Remove<Damage>();
        }
    }
}

public class EnemyAISystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Find the player first. In a real game, this might be cached.
        var playerEntity = world.Query(typeof(IsPlayer)).FirstOrDefault();
        if (playerEntity == default || !playerEntity.IsValid()) return;

        // Find all enemies that don't currently have a target.
        var enemiesWithoutTarget = world.Query(typeof(IsEnemy))
                                        .Where(e => !e.Has<Target>())
                                        .ToList();

        foreach (var enemy in enemiesWithoutTarget)
        {
            enemy.Set(new Target { TargetEntity = playerEntity });
        }
    }
}

public class ParticleSpawningSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // This system reacts to entities that just took damage this frame.
        // For the benchmark, we'll assume the DamageSystem ran first.
        // To find them, we'd typically use a message or a "DamageEvent" component.
        // Here, we'll just spawn a few particles for demonstration.
        var damagedEntities = world.Query(typeof(Health)).Where(e => e.Get<Health>().Value < 100).Take(5);

        foreach (var damagedEntity in damagedEntities)
        {
            var particle = world.CreateEntity();
            particle.Set(new IsParticle());
            particle.Set(damagedEntity.Get<Position>()); // Spawn at the location of the damaged entity
            particle.Set(new Lifetime { TimeRemaining = 0.5f });
        }
    }
}

/// <summary>
/// Applies a status effect by adding a tag component based on entity state.
/// Tests component addition based on a condition.
/// </summary>
public class StatusEffectSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var lowHealthEntities = world.Query(typeof(Health))
                                     .Where(e => e.Get<Health>().Value < 30 && !e.Has<IsFrozen>())
                                     .ToList();

        foreach (var entity in lowHealthEntities)
        {
            entity.Set(new IsFrozen());
        }
    }
}

public class MillisecondConfig : ManualConfig
{
    public MillisecondConfig()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddExporter(MarkdownExporter.GitHub, DefaultExporters.Csv, DefaultExporters.Html);
        AddDiagnoser(MemoryDiagnoser.Default);
        WithSummaryStyle(new SummaryStyle(CultureInfo.InvariantCulture, printUnitsInHeader: true, sizeUnit: SizeUnit.KB, timeUnit: TimeUnit.Millisecond, printUnitsInContent: false));
    }
}

// --- Benchmark Suite ---

[Config(typeof(MillisecondConfig))]
[MemoryDiagnoser]
public class StellaEcsBenchmarks
{
    private World _world = null!;
    private List<Entity> _entities = null!;
    private readonly Random _random = new();
    private readonly MovementSystem _movementSystem = new();
    private readonly LifetimeSystem _lifetimeSystem = new();
    private readonly DamageSystem _damageSystem = new();
    private readonly EnemyAISystem _enemyAISystem = new();

    private readonly ParticleSpawningSystem _particleSpawningSystem = new();
    private readonly StatusEffectSystem _statusEffectSystem = new();
    [Params(1_000, 10_000)]
    public int EntityCount;

    /// <summary>
    /// GlobalSetup is run once per benchmark execution.
    /// It creates the world and populates it with entities and a baseline of components.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        _world = new World((uint)EntityCount * 2); // Give some headroom
        _entities = new List<Entity>(EntityCount);

        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _entities.Add(entity);

            // Add a baseline set of components to simulate a realistic scenario
            entity.Set(new Position { X = (float)_random.NextDouble(), Y = (float)_random.NextDouble(), Z = (float)_random.NextDouble() });

            // Add Velocity to 50% of entities
            if (i % 2 == 0)
            {
                entity.Set(new Velocity { Dx = (float)_random.NextDouble(), Dy = (float)_random.NextDouble(), Dz = (float)_random.NextDouble() });
            }

            // Add Rotation to 25% of entities
            if (i % 4 == 0)
            {
                entity.Set(new Rotation { Qx = (float)_random.NextDouble(), Qy = (float)_random.NextDouble(), Qz = (float)_random.NextDouble(), Qw = (float)_random.NextDouble() });
            }
        }
    }

    // --- BENCHMARKS ---

    [Benchmark(Description = "Create & Destroy Entities")]
    public void CreateAndDestroyEntities()
    {
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _world.DestroyEntity(entity);
        }
    }

    [Benchmark(Description = "Add One Component")]
    public void AddOneComponent()
    {
        // Add a 'Health' component to all entities that don't have it.
        foreach (var entity in _entities)
        {
            entity.Set(new Health { Value = 100 });
        }
        // Cleanup: Remove the component so the next iteration is fair.
        foreach (var entity in _entities)
        {
            entity.Remove<Health>();
        }
    }

    [Benchmark(Description = "Remove One Component")]
    public void RemoveOneComponent()
    {
        // Setup: Ensure all entities have the component to be removed.
        foreach (var entity in _entities)
        {
            entity.Set(new Damage { Amount = 10 });
        }
        // Act: Remove the 'Damage' component from all entities.
        foreach (var entity in _entities)
        {
            entity.Remove<Damage>();
        }
    }

    [Benchmark(Description = "Query (1 Component)")]
    public void QuerySingleComponent()
    {
        foreach (var entity in _world.Query(typeof(Position)))
        {
            // Access the component to prevent the compiler from optimizing the loop away.
            ref var pos = ref entity.GetMut<Position>();
            pos.X += 1.0f;
        }
    }

    [Benchmark(Description = "Query (2 Components)")]
    public void QueryTwoComponents()
    {
        foreach (var entity in _world.Query(typeof(Position), typeof(Velocity)))
        {
            ref var pos = ref entity.GetMut<Position>();
            ref readonly var vel = ref entity.GetMut<Velocity>(); // Use GetMut even for readonly access for a fair comparison
            pos.X += vel.Dx;
        }
    }

    [Benchmark(Description = "Query (3 Components)")]
    public void QueryThreeComponents()
    {
        foreach (var entity in _world.Query(typeof(Position), typeof(Velocity), typeof(Rotation)))
        {
            ref var pos = ref entity.GetMut<Position>();
            ref readonly var vel = ref entity.GetMut<Velocity>();
            ref readonly var rot = ref entity.GetMut<Rotation>();
            pos.X += vel.Dx * rot.Qw;
        }
    }

    [Benchmark(Description = "Get Component (Direct Access)")]
    public void GetComponent()
    {
        // This measures the raw speed of the Get<T> operation when iterating
        // over a pre-fetched list of entities, simulating a common pattern.
        foreach (var entity in _entities)
        {
            // We know all entities have a Position component from the setup.
            var pos = entity.Get<Position>();
        }
    }

    [Benchmark(Description = "Run World (3 Systems)")]
    public void RunWorld_ThreeSystems()
    {
        _world.RegisterSystem(_movementSystem);
        _world.RegisterSystem(_damageSystem);
        _world.RegisterSystem(_lifetimeSystem);
        _world.Update(0.016f);
    }

    [Benchmark(Description = "Run World (6 Systems, Complex Workload)")]
    public void RunWorld_SixSystems_Complex()
    {
        // The order of system execution can matter.
        _world.RegisterSystem(_enemyAISystem);         // AI decides what to do.
        _world.RegisterSystem(_movementSystem);        // Entities move.
        _world.RegisterSystem(_damageSystem);          // Damage is applied.
        _world.RegisterSystem(_statusEffectSystem);    // Status effects are applied based on new health.
        _world.RegisterSystem(_particleSpawningSystem);// Effects are spawned based on damage.
        _world.RegisterSystem(_lifetimeSystem);        // Lifetimes (including particles) are updated.

        _world.Update(0.016f);
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting StellaEcs Benchmarks...");
        var summary = BenchmarkRunner.Run<StellaEcsBenchmarks>();
        Console.WriteLine("Benchmarks finished.");
    }
}
