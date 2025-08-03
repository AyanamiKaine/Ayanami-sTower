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

    [Params(1_000, 10_000, 100_000, 1_000_000)]
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

        _entities = new List<Entity>(EntityCount);
        var random = new Random(1234); // Use fixed seed for reproducibility

        // Create entities with diverse archetypes
        for (int i = 0; i < EntityCount; i++)
        {
            var entity = _world.CreateEntity();
            _entities.Add(entity);

            // Archetype 1: Movable object (50% of entities)
            if (i % 2 == 0)
            {
                entity.Add(new Position { X = i });
                entity.Add(new Velocity { Dx = 1 });
            }

            // Archetype 2: Rotatable movable object (25% of entities)
            if (i % 4 == 0)
            {
                entity.Add(new Rotation());
            }

            // Archetype 3: Player object (10% of entities)
            if (i % 10 == 0)
            {
                entity.Add(new IsPlayer());
            }
        }
    }

    // --- Iteration Benchmarks ---

    [Benchmark(Description = "Iteration: 1 Component (Position)")]
    public void Iterate_SingleComponent()
    {
        var query = _world.Query().With<Position>().Build();
        foreach (var entity in query)
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
