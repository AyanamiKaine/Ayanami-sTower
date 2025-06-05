
using System.Globalization;
using System.Text.Json;
using AyanamisTower.StellaDB.Model;
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
    private const int PoolSize = 1000; // Number of entities for read/update tests


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
        _world = new World("BenchmarkWorld", false, enabledOptimizations: true);

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
        return _world.Entity();
    }

    [Benchmark(Description = "Create a single new entity and ads a star component to it")]
    public Entity AddComponentToEntity()
    {
        // Use Guid to ensure unique name for DB, as World.Entity might expect unique names.
        return _world.Entity().Add<Star>();
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
