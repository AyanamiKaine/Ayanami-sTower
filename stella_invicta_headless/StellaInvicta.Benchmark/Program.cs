using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;

using StellaInvicta.Components;
using Flecs.NET.Core;


namespace StellaInvicta.Benchmark;


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

public class Character(string name, double health, int age)
{
    public string Name { get; set; } = name;
    public double Health { get; set; } = health;
    public int Age { get; set; } = age;

}

[SimpleJob(runtimeMoniker: RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StellaInvictaBenchmarks
{
    GoodsList inventory = [];
    GoodsList inputRequirements = [];
    Entity entity;
    Character character;
    World world;
    // Runs ONCE
    [IterationSetup]
    public void Setup()
    {
        world = World.Create();

        entity = world.Entity()
            .Set<Name>(new("DEFAULTNAME"))
            .Set<Health>(new(20))
            .Set<Age>(new(20));

        character = new Character("DEFAULTNAME", 20, 20);
        inventory = [];
        inventory += new Coal(20);
        inventory += new Coal(20);
        inventory += new ModGood("unobtainium", 5); // Custom modded good

        // Create input requirements
        inputRequirements = [];
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

    [Benchmark]
    public void ConstructingAnEntity()
    {
        world.Entity()
            .Set<Name>(new("DEFAULTNAME"))
            .Set<Health>(new(20))
            .Set<Age>(new(20));
    }

    [Benchmark]
    public void ConstructingAnObject()
    {
        var entity = new Character("DEFAULTNAME", 20, 20);
    }

    [Benchmark]
    public void MutatingAnEntity()
    {
        entity.GetMut<Name>().Value = "NewName";
    }

    [Benchmark]
    public void MutatingAnObject()
    {
        character.Name = "NewName";
    }


    /* On benchmarking flecs.net systems
    Its quite hard to get the actual performance charateristics of flecs because
    its not good micro benchable. For know be aware not to micro benchmark anything
    related to Flecs. Good benchmarking targets are algorithms that are part of systems
    and components.
    [Benchmark]
    public void BenchmarkingSystems()
    {
    }
    */
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
