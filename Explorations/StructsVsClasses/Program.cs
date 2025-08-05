using System;
using System.Diagnostics; // Required for Stopwatch
using System.Linq;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class PerformanceTest
{
    // Define the number of elements, same as the C example.
    private const int NumElements = 20_000_000;

    // A value type (struct). When in an array, the data is stored contiguously.
    // [obj1_data][obj2_data][obj3_data]...
    public struct DataObjectStruct
    {
        public long Id;
        public int Value;
        // In C#, struct layout is optimized, so we don't need to worry about manual padding.
    }

    // A reference type (class). An array of classes is an array of references.
    // The actual objects are stored separately on the heap.
    // [ptr1][ptr2][ptr3] -> scattered [objA], [objB], [objC]...
    public class DataObjectClass
    {
        public long Id;
        public int Value;
    }

    // --- Scenario 1: Array of Structs (Optimal Cache Performance) ---
    public static double RunStructArrayScenario()
    {
        Console.WriteLine("--- Scenario 1: Array of Structs (Value Types) ---");
        var dataArray = new DataObjectStruct[NumElements];
        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i].Id = i;
            dataArray[i].Value = i % 100;
        }

        Console.WriteLine("Processing array with optimal data locality...");
        long sum = 0;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < NumElements; i++)
        {
            sum += dataArray[i].Value;
        }

        stopwatch.Stop();
        Console.WriteLine($"Sum: {sum}, Time: {stopwatch.Elapsed.TotalSeconds:F6} seconds\n");
        return stopwatch.Elapsed.TotalSeconds;
    }

    // --- Scenario 2: Array of Shuffled References (Worst Cache Performance) ---
    public static double RunScatteredClassArrayScenario()
    {
        Console.WriteLine("--- Scenario 2: Array of Shuffled Class References ---");
        var dataArray = new DataObjectClass[NumElements];
        Console.WriteLine("Individually allocating objects on the heap (creating fragmentation)...");
        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i] = new DataObjectClass { Id = i, Value = i % 100 };
        }

        Console.WriteLine("Shuffling references to ensure random memory access...");
        var random = new Random();
        var shuffledArray = dataArray.OrderBy(x => random.Next()).ToArray();

        Console.WriteLine("Processing references with worst-case data locality...");
        long sum = 0;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < NumElements; i++)
        {
            sum += shuffledArray[i].Value;
        }

        stopwatch.Stop();
        Console.WriteLine($"Sum: {sum}, Time: {stopwatch.Elapsed.TotalSeconds:F6} seconds\n");
        return stopwatch.Elapsed.TotalSeconds;
    }
    
    // --- Scenario 3: Simulating GC Compaction ---
    public static double RunCompactedClassArrayScenario()
    {
        Console.WriteLine("--- Scenario 3: Array of Class References after GC Compaction ---");
        var dataArray = new DataObjectClass[NumElements];
        Console.WriteLine("Individually allocating objects on the heap (initial fragmented state)...");
        for (int i = 0; i < NumElements; i++)
        {
            dataArray[i] = new DataObjectClass { Id = i, Value = i % 100 };
        }
        
        // No need to shuffle here, as the GC will rearrange the objects based on its own algorithm.
        // The key is that they are live objects that the GC will find and move.

        Console.WriteLine("Forcing a full Garbage Collection to compact the heap...");
        var gcWatch = Stopwatch.StartNew();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        gcWatch.Stop();
        Console.WriteLine($"Garbage Collection and Compaction took: {gcWatch.Elapsed.TotalSeconds:F6} seconds.");

        Console.WriteLine("Processing references now pointing to (likely) contiguous data...");
        long sum = 0;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < NumElements; i++)
        {
            sum += dataArray[i].Value;
        }

        stopwatch.Stop();
        Console.WriteLine($"Sum: {sum}, Time: {stopwatch.Elapsed.TotalSeconds:F6} seconds\n");
        return stopwatch.Elapsed.TotalSeconds;
    }


    public static void Main(string[] args)
    {
        // Run once to warm up the JIT compiler
        Console.WriteLine("JIT Warm-up run...\n");
        RunStructArrayScenario();
        Console.Clear();

        Console.WriteLine("--- Starting Performance Comparison ---\n");
        double timeStructs = RunStructArrayScenario();
        double timeClassesScattered = RunScatteredClassArrayScenario();
        double timeClassesCompacted = RunCompactedClassArrayScenario();
        
        Console.WriteLine("--- Performance Summary ---");
        Console.WriteLine($"1. Struct Array (Optimal):      {timeStructs:F6} seconds");
        Console.WriteLine($"2. Class Array Scattered:       {timeClassesScattered:F6} seconds ({(timeClassesScattered / timeStructs) - 1:P2} slower than optimal)");
        Console.WriteLine($"3. Class Array after GC:        {timeClassesCompacted:F6} seconds ({(timeClassesCompacted / timeStructs) - 1:P2} slower than optimal)");
        Console.WriteLine("\nNote: The 'after GC' time doesn't include the time for the GC itself to run.");
    }
}
