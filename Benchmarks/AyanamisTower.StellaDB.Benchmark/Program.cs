
using System.Globalization;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Perfolizer.Metrology;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaDB.Benchmark;

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
public class StellaDBBenchmark
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private World _world;
    private readonly List<Entity> _entityPool = [];
    private readonly Random _random = new();
    private const int PoolSize = 100000; // Number of entities for read/update tests

    // Entities for specific benchmark iterations, prepared in IterationSetup
    private Entity _nameTestEntityForGetCold;
    private Entity _nameTestEntityForGetWarm;
    private string _fetchedNameForWarmCache; // To ensure JIT doesn't optimize away the priming call
    private Entity _nameTestEntityForSet;
    private Entity _entityForParentIdGet;
    private Entity _childEntityForParentIdSet;
    private Entity _parentEntityForParentIdSet;

    [GlobalSetup]
    public void GlobalSetup()
    {

        string dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkWorld.db");
        if (File.Exists(dbFilePath))
        {
            try
            {
                File.Delete(dbFilePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not delete existing DB file '{dbFilePath}' before setup: {ex.Message}");
            }
        }
        // --- Initialize World and pre-populate entities ---
        _world = new World("BenchmarkWorld", false, enabledOptimizations: false);

        _entityPool.Clear();
        for (int i = 0; i < PoolSize; i++)
        {
            var entity = _world.Entity($"PreCreatedEntity_{Guid.NewGuid()}");
            _entityPool.Add(entity);
        }
    }

    // --- Benchmarks for Entity Creation ---
    [Benchmark(Description = "Create a single new entity with a name")]
    public Entity CreateSingleEntity()
    {
        // Use Guid to ensure unique name for DB, as World.Entity might expect unique names.
        return _world.Entity($"NewBenchEntity_{Guid.NewGuid()}");
    }

    // --- Benchmarks for Entity.Name property ---

    // IterationSetup for GetEntityName_Cold and SetEntityName
    [IterationSetup(Targets = new[] { nameof(GetEntityName_Cold), nameof(SetEntityName) })]
    public void SetupNameTestEntityForColdAndSet()
    {
        _nameTestEntityForGetCold = _world.Entity($"ColdNameEntity_{Guid.NewGuid()}");
        _nameTestEntityForSet = _world.Entity($"SetNameEntity_{Guid.NewGuid()}");
    }

    [Benchmark(Description = "Get entity name (uncached/cold read)")]
    public string GetEntityName_Cold()
    {
        // _nameTestEntityForGetCold is fresh from IterationSetup, so its _cachedName is null.
        return _nameTestEntityForGetCold.Name;
    }

    [IterationSetup(Target = nameof(GetEntityName_Warm))]
    public void SetupNameTestEntityForWarm()
    {
        _nameTestEntityForGetWarm = _world.Entity($"WarmNameEntity_{Guid.NewGuid()}");
        // Prime the cache by accessing the name once.
        _fetchedNameForWarmCache = _nameTestEntityForGetWarm.Name;
    }

    [Benchmark(Description = "Get entity name (cached/warm read)")]
    public string GetEntityName_Warm()
    {
        // _nameTestEntityForGetWarm's name should be cached from its IterationSetup.
        return _nameTestEntityForGetWarm.Name;
    }

    [Benchmark(Description = "Set entity name")]
    public void SetEntityName()
    {
        // _nameTestEntityForSet is fresh from IterationSetup.
        _nameTestEntityForSet.Name = "UpdatedBenchmarkName";
    }

    // --- Benchmarks for Entity.ParentId property ---

    // IterationSetup for all ParentId related benchmarks
    [IterationSetup(Targets = new[] { nameof(GetEntityParentId), nameof(SetEntityParentId_ToNull), nameof(SetEntityParentId_ToValue) })]
    public void SetupParentIdTestEntities()
    {
        _entityForParentIdGet = _entityPool[_random.Next(PoolSize)];

        int childIndex = _random.Next(PoolSize);
        _childEntityForParentIdSet = _entityPool[childIndex];

        int parentIndex;
        if (PoolSize > 1)
        {
            do { parentIndex = _random.Next(PoolSize); } while (parentIndex == childIndex);
            _parentEntityForParentIdSet = _entityPool[parentIndex];
        }

        // For SetEntityParentId_ToNull: ensure current ParentId is NOT null
        _childEntityForParentIdSet.ParentId ??= _parentEntityForParentIdSet.Id;

        // For SetEntityParentId_ToValue: ensure current ParentId IS null or different
        // This setup is tricky because the same _childEntityForParentIdSet is used.
        // The benchmark itself will handle the "before" state.
        // For instance, one iteration might set it to null, the next to a value.
        // We can ensure it starts null for ToValue, and non-null for ToNull if benchmarks run in specific order
        // or make them more idempotent. Let's reset for ToValue.
        // This specific IterationSetup runs before EACH of the target benchmarks.
        // So, for SetEntityParentId_ToValue, we want ParentId to be null or different.
        // For SetEntityParentId_ToNull, we want ParentId to be non-null.

        // This logic will be specific to the benchmark method being set up if we check current method.
        // For simplicity, the benchmarks themselves will be responsible for ensuring a change happens.
        // e.g. SetEntityParentId_ToValue will first set to null if it's already the target parent.
    }


    [Benchmark(Description = "Get entity ParentId")]
    public long? GetEntityParentId()
    {
        return _entityForParentIdGet.ParentId;
    }

    [Benchmark(Description = "Set entity ParentId to a new value")]
    public void SetEntityParentId_ToValue()
    {
        // Ensure the operation is meaningful: if it's already set to this parent, change it first.
        if (_childEntityForParentIdSet.ParentId == _parentEntityForParentIdSet.Id)
        {
            _childEntityForParentIdSet.ParentId = null; // Make the next update a change
        }
        _childEntityForParentIdSet.ParentId = _parentEntityForParentIdSet.Id;
    }

    [Benchmark(Description = "Set entity ParentId to null")]
    public void SetEntityParentId_ToNull()
    {
        // Ensure the operation is meaningful: if it's already null, set it to something first.
        _childEntityForParentIdSet.ParentId ??= _parentEntityForParentIdSet.Id; // Make the next update (to null) a change
        _childEntityForParentIdSet.ParentId = null;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // Attempt to close the connection if World exposes it or has a Close/Dispose method.
        // (_world as IDisposable)?.Dispose(); // If World implemented IDisposable

        // Sqlite connections can lock the file, making it hard to delete immediately.
        // Forcing garbage collection MIGHT help release file locks from the _world's connection
        // if it's not properly disposed. This is a bit of a hack.
        // A better solution is for World to implement IDisposable.
        GC.Collect();
        GC.WaitForPendingFinalizers();

        string dbFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkWorld.db");
        if (File.Exists(dbFilePath))
        {
            try
            {
                File.Delete(dbFilePath);
                Console.WriteLine($"Cleaned up database file: {dbFilePath}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not clean up database file '{dbFilePath}': {ex.Message}. It might still be in use.");
            }
        }
    }
}

// ------------- User Provided Program Class -------------
public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting StellaDB Benchmark...");
        // --- Ensure Entity and World classes are accessible here ---
        // If they are in a different assembly, you'll need to reference it.
        // Assuming they are in the same project/assembly for this example.

        // Run the benchmarks defined in StellaDBBenchmark
        var summary = BenchmarkRunner.Run<StellaDBBenchmark>();

        // You can do something with the summary if needed,
        // though BenchmarkDotNet typically outputs results to console and files.
        Console.WriteLine("\nBenchmark Run Complete.");
        // summary contains detailed results.
    }
}
