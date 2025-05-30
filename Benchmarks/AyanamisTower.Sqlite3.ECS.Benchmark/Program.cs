using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.Sqlite3.ECS.Benchmark;

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

[MemoryDiagnoser]
[Config(typeof(MillisecondConfig))]
public class Sqlite3ECSBenchmark
{
    [Params(1000, 10000)] // Number of entities to operate on
    public int N;

    private EntityContext? _dbContext;
    private List<int> _entityIds = [];
    private List<Entity> _testEntities = []; // For benchmarks that operate on existing entities
    private readonly Random _random = new(42); // Seed for reproducibility
    private string _benchmarkDbPath = Path.Combine(Path.GetTempPath(), $"EntityContext_Benchmark_{Guid.NewGuid()}.db");

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Define a unique DB path for this benchmark run to avoid conflicts
        _benchmarkDbPath = Path.Combine(Path.GetTempPath(), $"EntityContext_Benchmark_{Guid.NewGuid()}.db");

        // Ensure no old DB file exists from a previous failed run
        if (File.Exists(_benchmarkDbPath))
        {
            File.Delete(_benchmarkDbPath);
        }

        // Create the database and schema once globally.
        using var context = new EntityContext(_benchmarkDbPath);
        context.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // Clean up the database file after all benchmarks are done
        if (File.Exists(_benchmarkDbPath))
        {
            try
            {
                // Ensure context is disposed before deleting file
                _dbContext?.Dispose(); // Dispose if it wasn't in IterationCleanup
                File.Delete(_benchmarkDbPath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error deleting benchmark database: {ex.Message}");
            }
        }
    }

    // Common setup for most benchmarks: creates a fresh DB and context.
    private void BaseIterationSetup()
    {
        _dbContext = new EntityContext(_benchmarkDbPath);
        _dbContext.Database.EnsureDeleted(); // Clear any data from previous iteration/benchmark method
        _dbContext.Database.EnsureCreated(); // Recreate schema

        // Apply PRAGMAs for performance to the newly created database connection for this iteration.
        // These settings enhance performance for the current iteration's database operations.
        using (var connection = _dbContext.Database.GetDbConnection())
        {
            connection.Open(); // Must open the connection to execute PRAGMAs
            using var command = connection.CreateCommand();
            // WAL (Write-Ahead Logging) mode: Generally improves concurrency and write performance.
            command.CommandText = "PRAGMA journal_mode=WAL;";
            command.ExecuteNonQuery();

            // Synchronous mode: NORMAL is a good balance between safety and speed.
            // OFF is faster but risks database corruption on power loss/crash. Use with caution.
            // FULL is safest but slower.
            command.CommandText = "PRAGMA synchronous=NORMAL;";
            command.ExecuteNonQuery();

            command.CommandText = "PRAGMA cache_size = -64000;";
            command.ExecuteNonQuery();

            // Temporary store: Use memory for temporary tables and indices.
            command.CommandText = "PRAGMA temp_store=MEMORY;";
            command.ExecuteNonQuery();
        }

        _entityIds = new List<int>(N);
        _testEntities = new List<Entity>(N);
    }


    [IterationSetup(Targets = new[] {
            nameof(CreateEntities_NoComponents),
            nameof(CreateEntities_WithPosition),
            nameof(CreateEntities_WithPositionAndVelocity),
        })]
    public void IterationSetup_ForCreationBenchmarks()
    {
        BaseIterationSetup();
    }

    [IterationSetup(Targets = new[] {
            nameof(GetPosition_DirectAccess),
            nameof(GetPosition_GenericGetReflection),
            nameof(GetPosition_GetComponentFromContext),
            nameof(UpdatePosition),
            nameof(QueryEntities_WithPosition),
            nameof(QueryEntities_WithPositionAndVelocity),
            nameof(AddVelocity_ToExistingWithout),
            nameof(RemoveVelocity_FromExistingWith)
        })]
    public void IterationSetup_WithPrepopulatedEntities()
    {
        BaseIterationSetup();
        for (int i = 0; i < N; i++)
        {
            var entity = new Entity
            {
                Name = $"Entity_{i}",
                Position2DComponent = new Position2D { X = _random.NextSingle() * 100, Y = _random.NextSingle() * 100 },
                // Add VelocityComponent to roughly half of the entities
                Velocity2DComponent = (i % 2 == 0) ? new Velocity2D { X = _random.NextSingle() * 10, Y = _random.NextSingle() * 10 } : null
            };
            _dbContext?.Entities.Add(entity);
        }
        _dbContext?.SaveChanges();

        // Load entities for the benchmarks to use. Include components to simulate realistic scenarios.
        _testEntities = [.. _dbContext!.Entities
                                  .Include(e => e.Position2DComponent)
                                  .Include(e => e.Velocity2DComponent)
                                  .AsNoTracking()];

        _entityIds = _testEntities.ConvertAll(e => e.EntityId);
    }

    [IterationSetup(Target = nameof(GetChildren_OfRoots))]
    public void IterationSetup_WithParentChildStructure()
    {
        BaseIterationSetup();
        int childrenPerParent = Math.Max(1, N / 10); // Aim for roughly 10 roots, or 1 if N is small
        if (childrenPerParent == 0 && N > 0) childrenPerParent = N; // If N < 10, one root with N children
        if (N == 0) childrenPerParent = 0;

        int numRoots = (childrenPerParent > 0) ? (N / childrenPerParent) : 0;
        if (numRoots == 0 && N > 0) numRoots = 1; // At least one root if N > 0

        List<Entity> roots = [];

        for (int i = 0; i < numRoots; i++)
        {
            var rootParent = new Entity { Name = $"RootParent_{i}" };
            _dbContext?.Entities.Add(rootParent);
            // Saving parent here to get its ID for children.
            // Batching adds complexity with ParentId assignment if not careful.
            _dbContext?.SaveChanges(); // Ensures rootParent.EntityId is populated

            for (int j = 0; j < childrenPerParent; j++)
            {
                // Ensure we don't create more than N total entities (approx)
                if (roots.Count + (i * childrenPerParent) + j >= N && N > 0) break;

                var child = new Entity { Name = $"Child_{i}_{j}", ParentId = rootParent.EntityId };
                _dbContext?.Entities.Add(child);
            }
            roots.Add(rootParent);
        }
        _dbContext?.SaveChanges(); // Save all children and remaining changes

        // Re-fetch roots with children included for the benchmark.
        // Use AsNoTracking if the benchmark itself doesn't modify these specific parent instances.
        var rootIds = roots.ConvertAll(r => r.EntityId);
        _testEntities = [.. _dbContext!.Entities
                                  .Include(e => e.Children)
                                  .Where(e => rootIds.Contains(e.EntityId))
                                  .AsNoTracking()];
    }


    [IterationCleanup]
    public void IterationCleanup()
    {
        _dbContext?.Dispose();
    }

    // --- Benchmark Methods ---

    [Benchmark(Description = "Create N entities (no components)")]
    public void CreateEntities_NoComponents()
    {
        for (int i = 0; i < N; i++)
        {
            var entity = new Entity { Name = $"Entity_{i}" };
            _dbContext?.Entities.Add(entity);
        }
    }

    [Benchmark(Description = "Create N entities (with Position2D)")]
    public void CreateEntities_WithPosition()
    {
        for (int i = 0; i < N; i++)
        {
            var entity = new Entity
            {
                Name = $"Entity_{i}",
                Position2DComponent = new Position2D { X = i, Y = i }
            };
            _dbContext?.Entities.Add(entity);
        }
    }

    [Benchmark(Description = "Create N entities (with Position2D & Velocity2D)")]
    public void CreateEntities_WithPositionAndVelocity()
    {
        for (int i = 0; i < N; i++)
        {
            var entity = new Entity
            {
                Name = $"Entity_{i}",
                Position2DComponent = new Position2D { X = i, Y = i },
                Velocity2DComponent = new Velocity2D { X = i * 0.1f, Y = i * 0.1f }
            };
            _dbContext?.Entities.Add(entity);
        }
    }

    [Benchmark(Description = "Get Position2D (direct property) for N entities")]
    public List<Position2D> GetPosition_DirectAccess()
    {
        var positions = new List<Position2D>(N);
        foreach (var entity in _testEntities) // _testEntities loaded with components in IterationSetup
        {
            if (entity.Position2DComponent != null)
            {
                positions.Add(entity.Position2DComponent);
            }
        }
        return positions; // Ensure the operation isn't optimized away
    }

    [Benchmark(Description = "Get Position2D (entity.GetReflection<T>) for N entities")]
    public List<Position2D> GetPosition_GenericGetReflection()
    {
        var positions = new List<Position2D>(N);
        foreach (var entity in _testEntities)
        {
            var pos = entity.GetReflection<Position2D>();
            if (pos != null)
            {
                positions.Add(pos);
            }
        }
        return positions;
    }

    [Benchmark(Description = "Get Position2D (context.Set.Find) for N entities")]
    public List<Position2D> GetPosition_GetComponentFromContext()
    {
        var positions = new List<Position2D>(N);
        foreach (var entityId in _entityIds) // _entityIds populated in IterationSetup
        {
            var pos = _dbContext?.Set<Position2D>().Find(entityId);
            if (pos != null)
            {
                positions.Add(pos);
            }
        }
        return positions;
    }

    [Benchmark(Description = "Query N entities with Position2D")]
    public List<Entity> QueryEntities_WithPosition()
    {
        return [.. _dbContext!.Entities
                         .Where(e => e.Position2DComponent != null)
                         .AsNoTracking()];
    }

    [Benchmark(Description = "Query N entities with Position2D & Velocity2D")]
    public List<Entity> QueryEntities_WithPositionAndVelocity()
    {
        return [.. _dbContext!.Entities
                         .Where(e => e.Position2DComponent != null && e.Velocity2DComponent != null)
                         .AsNoTracking()];
    }

    [Benchmark(Description = "Update Position2D for N entities")]
    public void UpdatePosition()
    {
        // Re-fetch entities with tracking if they were loaded with AsNoTracking in setup
        // Or, ensure setup loads them with tracking if they are to be updated.
        // For this benchmark, let's assume _testEntities are tracked or we fetch them.
        // If IterationSetup_WithPrepopulatedEntities used AsNoTracking, we need to re-attach or re-query.
        // Let's adjust setup or re-query here. For simplicity, let's re-query with tracking.
        var entitiesToUpdate = _dbContext!.Entities
                                         .Include(e => e.Position2DComponent)
                                         .Where(e => _entityIds.Contains(e.EntityId) && e.Position2DComponent != null)
                                         .ToList();

        foreach (var entity in entitiesToUpdate)
        {
            if (entity.Position2DComponent is not null)
            {
                entity.Position2DComponent.X++;
                entity.Position2DComponent.Y++;
            }
        }
    }


    [Benchmark(Description = "Add Velocity2D to N/2 entities")]
    public void AddVelocity_ToExistingWithout()
    {
        // IterationSetup_WithPrepopulatedEntities creates N entities, N/2 with Velocity.
        // This benchmark will add Velocity to those that don't have it.
        var entitiesToUpdate = _dbContext!.Entities
                                        .Where(e => _entityIds.Contains(e.EntityId) && e.Velocity2DComponent == null)
                                        .ToList();
        foreach (var entity in entitiesToUpdate)
        {
            entity.Velocity2DComponent = new Velocity2D { X = _random.NextSingle(), Y = _random.NextSingle() };
        }
    }

    [Benchmark(Description = "Remove Velocity2D from N/2 entities")]
    public void RemoveVelocity_FromExistingWith()
    {
        // IterationSetup_WithPrepopulatedEntities creates N entities, N/2 with Velocity.
        // This benchmark will remove Velocity from those that have it.
        // Cascade delete on Entity->Velocity2D relationship should handle component removal.
        var entitiesToUpdate = _dbContext!.Entities
                                         .Include(e => e.Velocity2DComponent) // Important to load the component to remove it
                                         .Where(e => _entityIds.Contains(e.EntityId) && e.Velocity2DComponent != null)
                                         .ToList();
        foreach (var entity in entitiesToUpdate)
        {
            // Removing the component from the context will delete it due to cascade rules or by FK nullification.
            if (entity.Velocity2DComponent != null) // Check as it's nullable
            {
                _dbContext.Velocity2DComponents.Remove(entity.Velocity2DComponent);
                // Or, if you want to keep the Entity object simpler:
                // entity.Velocity2DComponent = null; // This might not delete the component row depending on EF Core config.
                // With one-to-one and shared PK, removing the dependent is cleaner.
            }
        }
    }

    [Benchmark(Description = "Get all children for N root entities")]
    public List<Entity> GetChildren_OfRoots()
    {
        var allChildren = new List<Entity>();
        // _testEntities are loaded as roots with their Children collection populated in IterationSetup_WithParentChildStructure
        foreach (var parent in _testEntities)
        {
            allChildren.AddRange(parent.Children);
        }
        return allChildren; // Ensure usage
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting SFPM + Memory Benchmark...");
        var summary = BenchmarkRunner.Run<Sqlite3ECSBenchmark>();
        Console.WriteLine("\nBenchmark Complete.");
    }
}
