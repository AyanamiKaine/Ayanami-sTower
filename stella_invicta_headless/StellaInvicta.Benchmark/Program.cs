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
using StellaInvicta.Tags.Identifiers;

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
}

public class Iron(int quantity) : Good("iron", quantity)
{
}

// Custom mod goods can be created easily
public class ModGood(string goodId, int quantity) : Good(goodId, quantity)
{
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
    System<GoodsList, GoodsList, GoodsList> buildingSimulation;

    // Runs ONCE
    [GlobalSetup]
    public void Setup()
    {
        world = World.Create();
        world.Import<StellaInvictaECSModule>();
        world.SetTaskThreads(32);
        world.SetThreads(32);

        inventory += new Coal(20);
        inventory += new Coal(20);
        inventory += new ModGood("unobtainium", 5); // Custom modded good

        // Create input requirements
        inputRequirements += new Coal(5);
        inputRequirements += new ModGood("unobtainium", 3);

        Random rand = new();

        // Here we are creating 100.000 buildings that will be simulated
        for (int i = 0; i < 100000; i++)
        {
            world.Entity()
            .Add<Building>()
            .Set<Inventory, GoodsList>([
                new Coal(rand.Next(0,2000))
            ])
            .Set<Input, GoodsList>([
                new Coal(rand.Next(0,20))
            ])
            .Set<Output, GoodsList>([
                new Iron(rand.Next(0,7))
            ]);
        }

        buildingSimulation = world.System<GoodsList, GoodsList, GoodsList>()
            // When defined as multithreaded its execution will be split based on the set threads
            // so lets say we have 100.000 entities this system matches and 32 threads.
            // then one thread will iterate over (100.000 / 32) entities. 
            .MultiThreaded()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList) =>
            {
                /*
                Here we check if the inventory has enough 
                input goods to produce the output
                */
                if (inventory >= inputGoodsList)
                {
                    // We subtract the input goods from the inventory.
                    inventory -= inputGoodsList;
                    // New produced goods will be added to the inventory.
                    inventory += outputGoodsList;
                }
            });
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

    [Benchmark]
    public void BuildingSimulation()
    {
        buildingSimulation.Run();
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

//TODO: Implement performance analyser that report when a benchmark falls below a
// user defined baseline: See: "https://github.com/NewDayTechnology/benchmarkdotnet.analyser"
// and "https://dev.to/newday-technology/measuring-performance-using-benchmarkdotnet-part-3-breaking-builds-36il"
