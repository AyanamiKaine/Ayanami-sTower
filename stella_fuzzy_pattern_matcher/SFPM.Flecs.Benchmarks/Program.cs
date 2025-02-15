using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Flecs.NET.Core;

namespace SFPM.Flecs.Benchmarks;
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

[SimpleJob(runtimeMoniker: RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[Orderer(summaryOrderPolicy: SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SFPMFlecsBenchmarks
{
    World world;
    Dictionary<string, object> queryData;
    Entity player;

    [GlobalSetup]
    public void Setup()
    {
        world = World.Create();

        world.Set(data: new Map(Name: "circus"));

        world.Set
            (data: new List<Rule>(collection:
            [
                    new Rule(criterias:
                    [
                    new Criteria<string>(factName: "who", predicate: who => { return who == "Nick"; }),
                    new Criteria<string>(factName: "concept", predicate: concept => { return concept == "onHit"; }),
                ], payload: () =>
                {
                }),
                new Rule(criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => { return who == "Nick"; }),
                    new Criteria<string>(factName: "concept", predicate: concept => { return concept == "onHit"; }),
                    new Criteria<int>(factName: "nearAllies", predicate: nearAllies => { return nearAllies > 1; }),
                ], payload: () =>
                {
                }),
                new Rule(criterias:
                [
                    new Criteria<Name>(factName: "who", predicate: who => { return who.Value == "Nick"; }),
                    new Criteria<string>(factName: "concept", predicate: concept => { return concept == "onHit"; }),
                    new Criteria<Map>(factName: "curMap", predicate: curMap => { return curMap.Name == "circus"; }),
                ], payload: () =>
                {

                }),
                new Rule(criterias:
                [
                    new Criteria<string>(factName: "who", predicate: who => { return who == "Nick"; }),
                    new Criteria<string>(factName: "concept", predicate: concept => { return concept == "onHit"; }),
                    new Criteria<string>(factName: "hitBy", predicate: hitBy => { return hitBy == "zombieClown"; }),
                ], payload: () =>
                {
                })
        ]));

        player = world.Entity()
            .Set<Name>(data: new(Value: "Nick"))
            .Set<Health>(data: new(Value: 100))
            .Set<Position>(data: new(X: 10, Y: 20));

        world.OptimizeWorldRules();

        queryData = new Dictionary<string, object>
            {
                { "concept",    "onHit" },
                { "who",        player.Get<Name>()}, // Here we query the data from an entity and its component
                { "curMap",     world.Get<Map>()}    // If the component data changes it gets automaticall reflected here
            };
    }


    [Benchmark]
    public void MatchOnWorldOnce()
    {
        world.MatchOnWorld(queryData: queryData);
    }

    [Benchmark]
    public void MatchOnWorld10000Times()
    {
        for (int i = 0; i < 9999; i++)
        {
            world.MatchOnWorld(queryData: queryData);
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

            BenchmarkRunner.Run<SFPMFlecsBenchmarks>(config: config);
        }
    }
}

internal record struct EnemyCounter(int Count) : IComparable<EnemyCounter>
{
    public readonly int CompareTo(EnemyCounter other) => Count.CompareTo(value: other.Count);
}
internal record struct Stamina(double Value) : IComparable<Stamina>
{
    public readonly int CompareTo(Stamina other) => Value.CompareTo(value: other.Value);
}