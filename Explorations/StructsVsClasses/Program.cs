using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class ImprovedPerformanceTest
{
    // Define the number of elements
    private const int NumElements = 20_000_000;
    private const int NumRuns = 5; // Multiple runs for statistical analysis
    private const int WarmupRuns = 2;

    // A value type (struct). When in an array, the data is stored contiguously.
    public struct DataObjectStruct
    {
        public long Id;
        public int Value;
        public double SecondaryValue; // More realistic data size
        public bool IsActive;
    }

    // A reference type (class). An array of classes is an array of references.
    public class DataObjectClass
    {
        public long Id;
        public int Value;
        public double SecondaryValue; // More realistic data size
        public bool IsActive;
    }

    // Results structure for statistical analysis
    public struct BenchmarkResult
    {
        public double Mean;
        public double Median;
        public double StdDev;
        public double Min;
        public double Max;
        public long MemoryBefore;
        public long MemoryAfter;
    }

    // Proper Fisher-Yates shuffle algorithm
    private static void FisherYatesShuffle<T>(T[] array, Random random)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // Statistical analysis helper
    private static BenchmarkResult AnalyzeResults(List<double> times, long memBefore = 0, long memAfter = 0)
    {
        times.Sort();
        double mean = times.Average();
        double median = times.Count % 2 == 0
            ? (times[(times.Count / 2) - 1] + times[times.Count / 2]) / 2
            : times[times.Count / 2];

        double variance = times.Average(t => Math.Pow(t - mean, 2));
        double stdDev = Math.Sqrt(variance);

        return new BenchmarkResult
        {
            Mean = mean,
            Median = median,
            StdDev = stdDev,
            Min = times.Min(),
            Max = times.Max(),
            MemoryBefore = memBefore,
            MemoryAfter = memAfter
        };
    }

    // More realistic workload simulation
    private static long ProcessData<T>(T[] dataArray, Func<T, int> getValue, Func<T, double> getSecondary, Func<T, bool> getActive)
    {
        long sum = 0;
        double secondarySum = 0;
        int activeCount = 0;

        for (int i = 0; i < dataArray.Length; i++)
        {
            var item = dataArray[i];
            sum += getValue(item);
            secondarySum += getSecondary(item);
            if (getActive(item)) activeCount++;

            // Add some conditional logic to make it more realistic
            if (i % 1000 == 0 && secondarySum > 50000)
            {
                sum += activeCount;
            }
        }

        return sum + (long)secondarySum + activeCount;
    }

    // --- Scenario 1: Array of Structs (Optimal Cache Performance) ---
    public static BenchmarkResult RunStructArrayScenario()
    {
        Console.WriteLine("--- Scenario 1: Array of Structs (Value Types) ---");

        // Setup
        var dataArray = new DataObjectStruct[NumElements];
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i] = new DataObjectStruct
            {
                Id = i,
                Value = i % 100,
                SecondaryValue = random.NextDouble() * 1000,
                IsActive = i % 3 == 0
            };
        }

        Console.WriteLine($"Processing array with optimal data locality ({NumRuns} runs)...");

        // Warmup runs
        for (int w = 0; w < WarmupRuns; w++)
        {
            ProcessData(dataArray, s => s.Value, s => s.SecondaryValue, s => s.IsActive);
        }

        // Actual benchmark runs
        var times = new List<double>();
        long memBefore = GC.GetTotalMemory(true);

        for (int run = 0; run < NumRuns; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stopwatch = Stopwatch.StartNew();
            long result = ProcessData(dataArray, s => s.Value, s => s.SecondaryValue, s => s.IsActive);
            stopwatch.Stop();

            times.Add(stopwatch.Elapsed.TotalSeconds);
            if (run == 0) Console.WriteLine($"Sample result: {result}");
        }

        long memAfter = GC.GetTotalMemory(false);
        var results = AnalyzeResults(times, memBefore, memAfter);

        Console.WriteLine($"Mean: {results.Mean:F6}s, Median: {results.Median:F6}s, StdDev: {results.StdDev:F6}s");
        Console.WriteLine($"Range: {results.Min:F6}s - {results.Max:F6}s");
        Console.WriteLine($"Memory usage: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB\n");

        return results;
    }

    // --- Scenario 2: Array of Shuffled References (Worst Cache Performance) ---
    public static BenchmarkResult RunScatteredClassArrayScenario()
    {
        Console.WriteLine("--- Scenario 2: Array of Shuffled Class References ---");

        var dataArray = new DataObjectClass[NumElements];
        var random = new Random(42);

        Console.WriteLine("Allocating objects individually to create fragmentation...");
        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i] = new DataObjectClass
            {
                Id = i,
                Value = i % 100,
                SecondaryValue = random.NextDouble() * 1000,
                IsActive = i % 3 == 0
            };
        }

        Console.WriteLine("Shuffling references using Fisher-Yates algorithm...");
        FisherYatesShuffle(dataArray, random);

        Console.WriteLine($"Processing scattered references ({NumRuns} runs)...");

        // Warmup runs
        for (int w = 0; w < WarmupRuns; w++)
        {
            ProcessData(dataArray, c => c.Value, c => c.SecondaryValue, c => c.IsActive);
        }

        // Actual benchmark runs
        var times = new List<double>();
        long memBefore = GC.GetTotalMemory(true);

        for (int run = 0; run < NumRuns; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stopwatch = Stopwatch.StartNew();
            long result = ProcessData(dataArray, c => c.Value, c => c.SecondaryValue, c => c.IsActive);
            stopwatch.Stop();

            times.Add(stopwatch.Elapsed.TotalSeconds);
            if (run == 0) Console.WriteLine($"Sample result: {result}");
        }

        long memAfter = GC.GetTotalMemory(false);
        var results = AnalyzeResults(times, memBefore, memAfter);

        Console.WriteLine($"Mean: {results.Mean:F6}s, Median: {results.Median:F6}s, StdDev: {results.StdDev:F6}s");
        Console.WriteLine($"Range: {results.Min:F6}s - {results.Max:F6}s");
        Console.WriteLine($"Memory usage: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB\n");

        return results;
    }

    // --- Scenario 3: Sequential Class Array (Better Cache Performance) ---
    public static BenchmarkResult RunSequentialClassArrayScenario()
    {
        Console.WriteLine("--- Scenario 3: Sequential Class Array (No Shuffling) ---");

        var dataArray = new DataObjectClass[NumElements];
        var random = new Random(42);

        Console.WriteLine("Allocating objects sequentially...");
        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i] = new DataObjectClass
            {
                Id = i,
                Value = i % 100,
                SecondaryValue = random.NextDouble() * 1000,
                IsActive = i % 3 == 0
            };
        }

        Console.WriteLine("Triggering GC compaction to improve locality...");
        var gcWatch = Stopwatch.StartNew();
        // Force multiple GC generations to ensure compaction
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        gcWatch.Stop();
        Console.WriteLine($"GC compaction took: {gcWatch.Elapsed.TotalSeconds:F6} seconds");

        Console.WriteLine($"Processing sequential references after GC ({NumRuns} runs)...");

        // Warmup runs
        for (int w = 0; w < WarmupRuns; w++)
        {
            ProcessData(dataArray, c => c.Value, c => c.SecondaryValue, c => c.IsActive);
        }

        // Actual benchmark runs
        var times = new List<double>();
        long memBefore = GC.GetTotalMemory(true);

        for (int run = 0; run < NumRuns; run++)
        {
            // Light GC to avoid interference, but don't force major compaction
            if (run > 0) GC.Collect(0, GCCollectionMode.Optimized);

            var stopwatch = Stopwatch.StartNew();
            long result = ProcessData(dataArray, c => c.Value, c => c.SecondaryValue, c => c.IsActive);
            stopwatch.Stop();

            times.Add(stopwatch.Elapsed.TotalSeconds);
            if (run == 0) Console.WriteLine($"Sample result: {result}");
        }

        long memAfter = GC.GetTotalMemory(false);
        var results = AnalyzeResults(times, memBefore, memAfter);

        Console.WriteLine($"Mean: {results.Mean:F6}s, Median: {results.Median:F6}s, StdDev: {results.StdDev:F6}s");
        Console.WriteLine($"Range: {results.Min:F6}s - {results.Max:F6}s");
        Console.WriteLine($"Memory usage: {(memAfter - memAfter) / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"Note: GC compaction overhead of {gcWatch.Elapsed.TotalSeconds:F6}s not included in processing times\n");

        return results;
    }

    public static void Main(string[] args)
    {
        // Configure GC for more predictable behavior
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

        Console.WriteLine("=== Improved Cache Locality Benchmark ===");
        Console.WriteLine($"Elements: {NumElements:N0}, Runs per scenario: {NumRuns}, Warmup runs: {WarmupRuns}");
        Console.WriteLine($".NET Version: {Environment.Version}");
        Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
        Console.WriteLine($"GC Server Mode: {GCSettings.IsServerGC}");
        Console.WriteLine();

        var structResults = RunStructArrayScenario();
        var scatteredResults = RunScatteredClassArrayScenario();
        var sequentialResults = RunSequentialClassArrayScenario();

        Console.WriteLine("=== Performance Summary ===");
        Console.WriteLine($"1. Struct Array (Optimal):           {structResults.Mean:F6}s ± {structResults.StdDev:F6}s");
        Console.WriteLine($"2. Scattered Class References:       {scatteredResults.Mean:F6}s ± {scatteredResults.StdDev:F6}s ({scatteredResults.Mean / structResults.Mean:F2}x slower)");
        Console.WriteLine($"3. Sequential Class References:      {sequentialResults.Mean:F6}s ± {sequentialResults.StdDev:F6}s ({sequentialResults.Mean / structResults.Mean:F2}x slower)");

        Console.WriteLine("\n=== Statistical Confidence ===");
        Console.WriteLine($"Struct Array CV:      {structResults.StdDev / structResults.Mean * 100:F2}%");
        Console.WriteLine($"Scattered Array CV:   {scatteredResults.StdDev / scatteredResults.Mean * 100:F2}%");
        Console.WriteLine($"Sequential Array CV:  {sequentialResults.StdDev / sequentialResults.Mean * 100:F2}%");
        Console.WriteLine("\n(CV = Coefficient of Variation, lower is more consistent)");

        // Reset GC settings
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
    }
}