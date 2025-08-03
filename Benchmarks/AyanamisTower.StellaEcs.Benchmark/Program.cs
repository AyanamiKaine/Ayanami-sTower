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
using Microsoft.EntityFrameworkCore;
using Perfolizer.Horology;
using Perfolizer.Metrology;

// --- Components for Benchmarking ---
// Note: Relationship features have been removed from StellaEcs
// This benchmark focuses on core ECS functionality: entities, components, and queries

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct Position { public float X, Y, Z; }
public struct Velocity { public float Dx, Dy, Dz; }
public struct Rotation { public float Qx, Qy, Qz, Qw; }
public struct IsPlayer; // Tag component
public struct IsFrozen; // Tag component

// Additional components for realistic scenarios
public struct Health { public float Value, MaxValue; }
public struct Damage { public float Amount; }
public struct Lifetime { public float TimeRemaining; }
public struct Physics { public float Mass, Friction; }
public struct Renderable { public int MeshId, MaterialId; }
public struct AI { public int StateId, TargetEntityId; }
public struct Inventory { public int[] ItemIds; }
public struct Transform { public float Scale; }
public struct Collider { public float Radius; }

public class MillisecondConfig : ManualConfig
{
    public MillisecondConfig()
    {
        // --- Add essential components ---

        // Keep default columns (like Mean, Error, StdDev, Median, Ratio, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);

        // Add your desired exporters
        AddExporter(MarkdownExporter.GitHub); // Keep GitHub Markdown format
        AddExporter(DefaultExporters.Csv); // Add CSV for easy data processing
        AddExporter(DefaultExporters.Html); // Add HTML report

        // Add your diagnosers
        AddDiagnoser(MemoryDiagnoser.Default); // Keep memory diagnoser

        // Optional: Add default Job definition if needed, otherwise defaults are used
        // AddJob(Job.Default);

        // --- Define and apply the Summary Style with Milliseconds ---
        var style = new SummaryStyle(
            cultureInfo: CultureInfo.InvariantCulture, // Use invariant culture for consistency
            printUnitsInHeader: true, // Show units in column headers (e.g., "Mean [ms]")
            sizeUnit: SizeUnit.KB, // Don't repeat units in every cell
            timeUnit: TimeUnit.Millisecond // *** Set the desired time unit here! ***
            , // Keep size units as KB (or choose B, MB)
            printUnitsInContent: false
        );

        // Apply the custom style
        WithSummaryStyle(style);
    }
}

// --- Benchmark Suite ---

[Config(typeof(MillisecondConfig))]
[MemoryDiagnoser]
public class StellaEcsBenchmarks
{
    private World _world = null!;
    private List<Entity> _entities = null!;

    [Params(1_000_000)]
    public int EntityCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _world = new World(EntityCount * 2); // Provide extra capacity

        // Register all types (now auto-registered via QueryBuilder, but explicit for performance)
        _world.RegisterComponent<Position>();
        _world.RegisterComponent<Velocity>();
        _world.RegisterComponent<Rotation>();
        _world.RegisterComponent<IsPlayer>();
        _world.RegisterComponent<IsFrozen>();
        _world.RegisterComponent<Health>();
        _world.RegisterComponent<Damage>();
        _world.RegisterComponent<Lifetime>();
        _world.RegisterComponent<Physics>();
        _world.RegisterComponent<Renderable>();
        _world.RegisterComponent<AI>();
        _world.RegisterComponent<Inventory>();
        _world.RegisterComponent<Transform>();
        _world.RegisterComponent<Collider>();

        _entities = new List<Entity>(EntityCount);
        var random = new Random(1234); // Use fixed seed for reproducibility

        // Create entities with diverse archetypes
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _entities.Add(entity);

            // Base archetype: All entities have position and transform
            entity.Add(new Position { X = i, Y = random.Next(-100, 100), Z = random.Next(-100, 100) });
            entity.Add(new Transform { Scale = 1.0f + random.NextSingle() });

            // Archetype 1: Moving entities (60% of entities)
            if (i % 10 < 6)
            {
                entity.Add(new Velocity { Dx = (random.NextSingle() * 2) - 1, Dy = (random.NextSingle() * 2) - 1 });
            }

            // Archetype 2: Physics entities (40% of entities)
            if (i % 10 < 4)
            {
                entity.Add(new Physics { Mass = 1.0f + (random.NextSingle() * 10), Friction = random.NextSingle() });
                entity.Add(new Collider { Radius = 0.5f + (random.NextSingle() * 2) });
            }

            // Archetype 3: Rendered entities (70% of entities)
            if (i % 10 < 7)
            {
                entity.Add(new Renderable { MeshId = random.Next(1, 100), MaterialId = random.Next(1, 50) });
            }

            // Archetype 4: Living entities with health (30% of entities)
            if (i % 10 < 3)
            {
                var maxHealth = 50.0f + (random.NextSingle() * 150);
                entity.Add(new Health { Value = maxHealth, MaxValue = maxHealth });
            }

            // Archetype 5: Rotating entities (25% of entities)
            if (i % 4 == 0)
            {
                entity.Add(new Rotation { Qx = random.NextSingle(), Qy = random.NextSingle(), Qz = random.NextSingle(), Qw = 1.0f });
            }

            // Archetype 6: AI entities (15% of entities)
            if (i % 20 < 3)
            {
                entity.Add(new AI { StateId = random.Next(1, 10), TargetEntityId = random.Next(0, EntityCount) });
            }

            // Archetype 7: Player entities (1% of entities)
            if (i % 100 == 0)
            {
                entity.Add(new IsPlayer());
                entity.Add(new Inventory { ItemIds = new int[random.Next(5, 20)] });
            }

            // Archetype 8: Temporary entities (5% of entities)
            if (i % 20 == 0)
            {
                entity.Add(new Lifetime { TimeRemaining = 10.0f + (random.NextSingle() * 90) });
            }
        }
    }

    // --- Iteration Benchmarks ---

    [Benchmark(Description = "Iteration: 1 Component (Position)")]
    public void Iterate_SingleComponent()
    {
        foreach (var entity in _world.Query().With<Position>().Build())
        {
            // This access is to prevent the loop from being optimized away.
            _ = entity.Id;
        }
    }

    [Benchmark(Description = "Iteration: 2 Components (Position, Velocity)")]
    public void Iterate_TwoComponents()
    {
        var query = _world.Query()
            .With<Position>()
            .With<Velocity>()
            .Build();

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetMutable<Position>();
            ref readonly var vel = ref entity.Get<Velocity>();
            pos.X += vel.Dx;
        }
    }

    [Benchmark(Description = "Iteration: 3 Components + Without (Pos, Vel, Rot, !IsFrozen)")]
    public void Iterate_ThreeComponents_WithExclusion()
    {
        // Add a component to a few entities to make the 'Without' meaningful
        _entities[0].Add(new IsFrozen());
        _entities[4].Add(new IsFrozen());

        var query = _world.Query()
            .With<Position>()
            .With<Velocity>()
            .With<Rotation>()
            .Without<IsFrozen>()
            .Build();

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetMutable<Position>();
            ref readonly var vel = ref entity.Get<Velocity>();
            pos.X += vel.Dx;
        }

        // Clean up for next run
        _entities[0].Remove<IsFrozen>();
        _entities[4].Remove<IsFrozen>();
    }

    [Benchmark(Description = "Iteration: With Where Clause (Position.X > 500)")]
    public void Iterate_WithWhereClause()
    {
        var query = _world.Query()
            .Where<Position>(p => p.X > EntityCount / 2)
            .Build();

        int count = 0;
        foreach (var entity in query)
        {
            count++;
        }
    }


    // --- Entity & Component Manipulation Benchmarks ---

    [Benchmark(Description = "Lifecycle: Create & Destroy 100 Entities")]
    public void CreateAndDestroyEntities()
    {
        for (int i = 0; i < 100; i++)
        {
            _world.CreateEntity().Destroy();
        }
    }

    [Benchmark(Description = "Components: Add & Remove Component")]
    public void AddAndRemoveComponent()
    {
        var entity = _entities[0];
        entity.Add(new IsFrozen());
        entity.Remove<IsFrozen>();
    }

    [Benchmark(Description = "Components: Add & Remove Multiple Components")]
    public void AddAndRemoveMultipleComponents()
    {
        var entity = _entities[1];
        entity.Add(new IsFrozen());
        entity.Add(new IsPlayer());
        entity.Remove<IsFrozen>();
        entity.Remove<IsPlayer>();
    }

    [Benchmark(Description = "Query: Complex Query Building")]
    public void ComplexQueryBuilding()
    {
        // Test the performance of building complex queries
        var query = _world.Query()
            .With<Position>()
            .With<Velocity>()
            .Without<IsFrozen>()
            .Where<Position>(p => p.X > 0)
            .Where<Velocity>(v => v.Dx != 0)
            .Build();

        // Execute the query to ensure it's fully built
        int count = 0;
        foreach (var entity in query)
        {
            count++;
        }
    }

    [Benchmark(Description = "Query: Auto-Registration Performance")]
    public void QueryAutoRegistration()
    {
        // Create a new world to test auto-registration
        var tempWorld = new World(100);

        // This should trigger auto-registration of all component types
        var query = tempWorld.Query()
            .With<Position>()
            .With<Velocity>()
            .With<Rotation>()
            .Without<IsPlayer>()
            .Where<Position>(p => p.X > 0)
            .Build();

        // Create a few entities to make the query meaningful
        for (int i = 0; i < 10; i++)
        {
            var entity = tempWorld.CreateEntity();
            entity.Add(new Position { X = i });
            entity.Add(new Velocity { Dx = 1 });
        }

        // Execute the query
        int count = 0;
        foreach (var entity in query)
        {
            count++;
        }
    }

    // --- Real-World Stress Tests ---

    [Benchmark(Description = "Game Loop: Physics System (Movement + Collision)")]
    public void GameLoop_PhysicsSystem()
    {
        // Simulate a physics system that updates position based on velocity and handles collisions
        var movingEntities = _world.Query()
            .With<Position>()
            .With<Velocity>()
            .With<Physics>()
            .Build();

        foreach (var entity in movingEntities)
        {
            ref var pos = ref entity.GetMutable<Position>();
            ref readonly var vel = ref entity.Get<Velocity>();
            ref readonly var physics = ref entity.Get<Physics>();

            // Apply velocity with physics
            pos.X += vel.Dx * (1.0f / physics.Mass);
            pos.Y += vel.Dy * (1.0f / physics.Mass);
            pos.Z += vel.Dz * (1.0f / physics.Mass);

            // Simple boundary check (collision)
            if (pos.X > 1000 || pos.X < -1000) pos.X = -pos.X;
            if (pos.Y > 1000 || pos.Y < -1000) pos.Y = -pos.Y;
            if (pos.Z > 1000 || pos.Z < -1000) pos.Z = -pos.Z;
        }
    }

    [Benchmark(Description = "Game Loop: Render Culling System")]
    public void GameLoop_RenderCulling()
    {
        // Simulate a rendering system that culls objects outside view frustum
        var renderableEntities = _world.Query()
            .With<Position>()
            .With<Renderable>()
            .With<Transform>()
            .Build();

        int visibleCount = 0;
        foreach (var entity in renderableEntities)
        {
            ref readonly var pos = ref entity.Get<Position>();
            ref readonly var transform = ref entity.Get<Transform>();

            // Frustum culling simulation (simple distance check)
            var distance = Math.Sqrt((pos.X * pos.X) + (pos.Y * pos.Y) + (pos.Z * pos.Z));
            if (distance < 100 * transform.Scale)
            {
                visibleCount++;
            }
        }
    }

    [Benchmark(Description = "Game Loop: AI Behavior System")]
    public void GameLoop_AISystem()
    {
        // Simulate an AI system that updates AI entities
        var aiEntities = _world.Query()
            .With<AI>()
            .With<Position>()
            .Where<Health>(h => h.Value > 0) // Only living AI entities
            .Build();

        foreach (var entity in aiEntities)
        {
            ref var ai = ref entity.GetMutable<AI>();
            ref readonly var pos = ref entity.Get<Position>();

            // Simple AI state machine simulation
            ai.StateId = ai.StateId switch
            {
                // Patrol
                1 => pos.X > 50 ? 2 : 1,
                // Chase
                2 => pos.X < -50 ? 1 : 2,
                _ => 1,
            };
        }
    }

    [Benchmark(Description = "Game Loop: Damage Processing System")]
    public void GameLoop_DamageSystem()
    {
        // First, add damage components to some entities
        for (int i = 0; i < Math.Min(100, _entities.Count); i++)
        {
            var entity = _entities[i];
            if (entity.Has<Health>())
            {
                entity.Add(new Damage { Amount = 10.0f + (i % 20) });
            }
        }

        // Process damage
        var damagedEntities = _world.Query()
            .With<Health>()
            .With<Damage>()
            .Build();

        foreach (var entity in damagedEntities)
        {
            ref var health = ref entity.GetMutable<Health>();
            ref readonly var damage = ref entity.Get<Damage>();

            health.Value = Math.Max(0, health.Value - damage.Amount);
            entity.Remove<Damage>(); // Remove damage after processing
        }
    }

    [Benchmark(Description = "Game Loop: Lifetime Management System")]
    public void GameLoop_LifetimeSystem()
    {
        // Simulate a system that manages entity lifetimes
        var temporaryEntities = _world.Query()
            .With<Lifetime>()
            .Build();

        var entitiesToDestroy = new List<Entity>();
        foreach (var entity in temporaryEntities)
        {
            ref var lifetime = ref entity.GetMutable<Lifetime>();
            lifetime.TimeRemaining -= 0.016f; // 60 FPS delta time

            if (lifetime.TimeRemaining <= 0)
            {
                entitiesToDestroy.Add(entity);
            }
        }

        // Destroy expired entities (simulation only - don't actually destroy for benchmark consistency)
        foreach (var entity in entitiesToDestroy)
        {
            // entity.Destroy(); // Commented out to maintain entity count for benchmarking
            _ = entity.Id; // Prevent optimization
        }
    }

    [Benchmark(Description = "Stress Test: Multi-Query System Frame")]
    public void StressTest_MultiQueryFrame()
    {
        // Simulate a complex game frame with multiple systems running

        // System 1: Movement
        var movingEntities = _world.Query().With<Position>().With<Velocity>().Build();
        foreach (var entity in movingEntities)
        {
            ref var pos = ref entity.GetMutable<Position>();
            ref readonly var vel = ref entity.Get<Velocity>();
            pos.X += vel.Dx * 0.016f;
        }

        // System 2: Rendering prep
        var renderEntities = _world.Query().With<Position>().With<Renderable>().Build();
        int renderCount = 0;
        foreach (var entity in renderEntities)
        {
            renderCount++;
        }

        // System 3: Player input
        var playerEntities = _world.Query().With<IsPlayer>().With<Position>().Build();
        foreach (var entity in playerEntities)
        {
            ref var pos = ref entity.GetMutable<Position>();
            pos.Y += 0.1f; // Simulate input
        }

        // System 4: Health check
        var livingEntities = _world.Query().Where<Health>(h => h.Value > 0).Build();
        int aliveCount = 0;
        foreach (var entity in livingEntities)
        {
            aliveCount++;
        }
    }

    [Benchmark(Description = "Stress Test: Dynamic Entity Creation/Destruction")]
    public void StressTest_DynamicEntityLifecycle()
    {
        var random = new Random(42);
        var tempEntities = new List<Entity>();

        // Create burst of entities
        for (int i = 0; i < 1000; i++)
        {
            var entity = _world.CreateEntity();
            entity.Add(new Position { X = random.NextSingle() * 100 });
            entity.Add(new Lifetime { TimeRemaining = random.NextSingle() * 5 });
            tempEntities.Add(entity);
        }

        // Query and process them
        var query = _world.Query().With<Position>().With<Lifetime>().Build();
        foreach (var entity in query)
        {
            ref readonly var pos = ref entity.Get<Position>();
            ref var lifetime = ref entity.GetMutable<Lifetime>();
            lifetime.TimeRemaining -= 0.1f;
        }

        // Clean up (destroy all temp entities to maintain benchmark consistency)
        foreach (var entity in tempEntities)
        {
            entity.Destroy();
        }
    }

    [Benchmark(Description = "Stress Test: Component Thrashing")]
    public void StressTest_ComponentThrashing()
    {
        // Simulate rapid component addition/removal (e.g., temporary buffs/debuffs)
        var entities = _entities.Take(1000).ToArray();

        // Add components rapidly
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (i % 2 == 0) entity.Add(new IsFrozen());
            if (i % 3 == 0) entity.Add(new Damage { Amount = 5 });
        }

        // Query with the new components
        var frozenEntities = _world.Query().With<IsFrozen>().Build();
        int frozenCount = 0;
        foreach (var entity in frozenEntities)
        {
            frozenCount++;
        }

        // Remove components
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (entity.Has<IsFrozen>()) entity.Remove<IsFrozen>();
            if (entity.Has<Damage>()) entity.Remove<Damage>();
        }
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
