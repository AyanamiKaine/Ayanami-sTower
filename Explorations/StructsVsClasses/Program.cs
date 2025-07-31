#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Diagnostics;

namespace MemoryBenchmark;

// A simple class (reference type)
public class PointClass
{
    public long X { get; set; }
    public long Y { get; set; }
    public long Z { get; set; }
}

// An identical struct (value type)
public struct PointStruct
{
    public long X { get; set; }
    public long Y { get; set; }
    public long Z { get; set; }
}

static class Program
{
    const int NumberOfItems = 20_000_000; // 20 million items

    static void Main(string[] args)
    {
        Console.WriteLine("Preparing data...");

        // --- Class Setup ---
        // This array will hold POINTERS to PointClass objects
        var classArray = new PointClass[NumberOfItems];
        // We create the objects. They will be scattered on the heap.
        for (int i = 0; i < NumberOfItems; i++)
        {
            classArray[i] = new PointClass { X = i, Y = i + 1, Z = i + 2 };
        }

        // --- Struct Setup ---
        // This array holds the ACTUAL data for 20 million structs, contiguously.
        var structArray = new PointStruct[NumberOfItems];
        for (int i = 0; i < NumberOfItems; i++)
        {
            structArray[i] = new PointStruct { X = i, Y = i + 1, Z = i + 2 };
        }

        Console.WriteLine($"Data ready. Processing {NumberOfItems:N0} items.\n");

        // Force a full garbage collection before each test to ensure a clean slate
        // and minimize interference between the tests.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // --- Benchmark Classes ---
        var stopwatch = Stopwatch.StartNew();
        long classSum = 0;
        for (int i = 0; i < NumberOfItems; i++)
        {
            // For each item, we follow a pointer to get the data
            classSum += classArray[i].X;
        }
        stopwatch.Stop();
        Console.WriteLine($"Time to iterate CLASS array: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Sum (for verification): {classSum}");


        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // --- Benchmark Structs ---
        stopwatch.Restart();
        long structSum = 0;
        for (int i = 0; i < NumberOfItems; i++)
        {
            // For each item, the data is right here. No pointer chasing.
            structSum += structArray[i].X;
        }
        stopwatch.Stop();
        Console.WriteLine($"\nTime to iterate STRUCT array: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Sum (for verification): {structSum}");
    }
}
