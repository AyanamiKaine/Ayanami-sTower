using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Flecs.NET.Core;
using StellaInvicta;
using StellaInvicta.Components;

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


// Example implementations
public class Coal(int quantity) : Good("coal", quantity)
{
    public override IGood WithQuantity(int newQuantity)
    {
        return new Coal(newQuantity);
    }
}

public class Iron(int quantity) : Good("iron", quantity)
{
    public override IGood WithQuantity(int newQuantity)
    {
        return new Iron(newQuantity);
    }
}

// Custom mod goods can be created easily
public class ModGood(string goodId, int quantity) : Good(goodId, quantity)
{
    public override IGood WithQuantity(int newQuantity)
    {
        return new ModGood(GoodId, newQuantity);
    }
}

[SimpleJob(runtimeMoniker: RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StellaInvictaBenchmarks
{
    World world;
    GoodsList inventory = [];
    GoodsList inputRequirements = [];

    [IterationSetup]
    public void Setup()
    {
        world = World.Create();
        world.Import<StellaInvictaECSModule>();

        inventory += new Coal(20);
        inventory += new Coal(20);
        inventory += new ModGood("unobtainium", 5); // Custom modded good

        // Create input requirements
        inputRequirements += new Coal(5);
        inputRequirements += new ModGood("unobtainium", 3);
    }


    /// <summary>
    /// This should simulate the building logic that runs
    /// when a building wants to produce something, it always
    /// first checks if he has enough inventory for the desired 
    /// output
    /// <remarks>
    /// Here we bechnmark an object based goods type. Instead of 
    /// defining goods as entities.
    /// </remarks>
    /// </summary>
    [Benchmark]
    public void CheckIfInventoryHasEnoughGoodsObjectBased100000Times()
    {
        for (int i = 0; i < 100000; i++)
        {
            if (inventory >= inputRequirements)
            {
                // Do some work
            }
        }
    }

    [Benchmark]
    public void AddingInventoriesTogether100000Times()
    {
        for (int i = 0; i < 100000; i++)
        {
            var newInventory = inventory + inputRequirements;
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
