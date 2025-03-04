using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Flecs.NET.Core;
using StellaInvicta;

namespace StellaInvicta.Benchmark;
public record struct Name(string Value) : IComparable<Name>
{
    public readonly int CompareTo(Name other) => Value.CompareTo(strB: other.Value);
}
public record struct Map(string Name) : IComparable<Map>
{
    public readonly int CompareTo(Map other) => Name.CompareTo(strB: other.Name);
}

public record struct Health(int Value);
public record struct Position(int X, int Y);

// Base interface for goods
public interface IGood
{
    int Quantity { get; }
    string GoodId { get; } // String identifier instead of Type
}

// Example concrete implementations
public class Coal(int quantity) : Good("coal", quantity)
{
}

public class Iron(int quantity) : Good("iron", quantity)
{
}

// Custom mod goods can be created easily
public class ModGood(string goodId, int quantity) : Good(goodId, quantity)
{
}

// Abstract base class for all goods
public abstract class Good(string goodId, int quantity) : IGood
{
    public int Quantity { get; } = Math.Max(0, quantity);
    public string GoodId { get; } = goodId;
}

public class GoodsList
{
    // Using a dictionary for O(1) lookups instead of lists and grouping
    private readonly Dictionary<string, int> _goods = new Dictionary<string, int>();

    public void Add(IGood good)
    {
        if (_goods.ContainsKey(good.GoodId))
            _goods[good.GoodId] += good.Quantity;
        else
            _goods[good.GoodId] = good.Quantity;
    }

    public void AddRange(IEnumerable<IGood> goods)
    {
        foreach (var good in goods)
            Add(good);
    }

    public int GetQuantity(string goodId)
    {
        return _goods.TryGetValue(goodId, out int quantity) ? quantity : 0;
    }

    // Optimized operator for fast comparison
    public static bool operator >=(GoodsList inventory, GoodsList input)
    {
        // Only check required input goods (subset check)
        foreach (var kvp in input._goods)
        {
            if (inventory.GetQuantity(kvp.Key) < kvp.Value)
                return false;
        }
        return true;
    }

    public static bool operator <=(GoodsList input, GoodsList inventory)
    {
        return inventory >= input;
    }
}

[SimpleJob(runtimeMoniker: RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StellaInvictaBenchmarks
{
    World world;
    GoodsList inventory;
    GoodsList inputRequirements;

    [IterationSetup]
    public void Setup()
    {
        world = World.Create();
        world.Import<StellaInvictaECSModule>();

        inventory = new GoodsList();
        inventory.Add(new Coal(20));
        inventory.Add(new Iron(15));
        inventory.Add(new ModGood("unobtainium", 5)); // Custom modded good

        // Create input requirements
        inputRequirements = new GoodsList();
        inputRequirements.Add(new Coal(5));
        inputRequirements.Add(new ModGood("unobtainium", 3));
    }


    /// <summary>
    /// This should simulate the building logic that runs
    /// when a building wants to produce something, it always
    /// first checks if he has enough inventory for the desired 
    ///  output
    /// </summary>
    [Benchmark]
    public void CheckIfInventoryHasEnoughGoods()
    {
        for (int i = 0; i < 100000; i++)
        {
            if (inventory >= inputRequirements)
            {
                // Do some work
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // Otherwise it complains that flecs is not build in release,
            // I dont think its true.
            var config = ManualConfig.Create(config: DefaultConfig.Instance)
                .WithOptions(options: ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<StellaInvictaBenchmarks>(config: config);
        }
    }
}
