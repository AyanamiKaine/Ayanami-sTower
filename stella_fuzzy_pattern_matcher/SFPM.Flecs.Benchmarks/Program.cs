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
    public readonly int CompareTo(Name other) => Value.CompareTo(other.Value);
}
public record struct Map(string Name) : IComparable<Map>
{
    public readonly int CompareTo(Map other) => Name.CompareTo(other.Name);
}

[SimpleJob(RuntimeMoniker.Net90)]               // JIT
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SFPMFlecsBenchmarks
{

    World world;
    Entity rulesList;

    [GlobalSetup]
    public void Setup()
    {
        world = World.Create();
        world.Set
            (new List<Rule>([
                        new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], () =>
                {
                }),
                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], () =>
                {
                }),
                new Rule([
                    new Criteria<Name>("who", who => { return who.Value == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<Map>("curMap", curMap => { return curMap.Name == "circus"; }),
                ], () =>
                {

                }),
                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], () =>
                {
                })
        ]));


        world.OptimizeWorldRules();
    }


    [Benchmark]
    public void OneRuleTwoCriteriaOperatorBasedMatch()
    {
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SFPMFlecsBenchmarks>();
        }
    }
}

internal record struct EnemyCounter(int Count) : IComparable<EnemyCounter>
{
    public readonly int CompareTo(EnemyCounter other) => Count.CompareTo(other.Count);
}
internal record struct Stamina(double Value) : IComparable<Stamina>
{
    public readonly int CompareTo(Stamina other) => Value.CompareTo(other.Value);
}