using System.Dynamic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace DynamicBenchmark;


//[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
//[SimpleJob(RuntimeMoniker.NativeAot90)]
//[Orderer(SummaryOrderPolicy.FastestToSlowest)]
//[RPlotExporter]
public class Person
{
    //[Params("Tim", "Katarina", "")]
    public string Name { get; set; } = "";

    //[Params(0, 9, 24)]
    public int Age { get; set; } = 0;

    //[Benchmark]
    public string SayHelloTyped()
    {
        return $"Hello my name is {Name} and I am {Age} years old!";
    }

    //[Benchmark]
    public dynamic SayHelloDynamic()
    {
        return $"Hello my name is {Name} and I am {Age} years old!"; ;
    }
}

[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RPlotExporter]
public class DynamicBenchmark
{

    private dynamic _person; // Store the ExpandoObject here

    [GlobalSetup]
    public void Setup()
    {
        // Create the ExpandoObject once before the benchmarks run
        _person = new ExpandoObject();
        _person.age = 20;
        _person.name = "Tom";
    }

    [Benchmark]
    public dynamic SayHelloDynamic()
    {
        dynamic age = 20;
        dynamic name = "Tom";

        return $"Hello my name is {name} and I am {age} years old!"; ;
    }


    [Benchmark]
    public dynamic SayHelloDynamicReassigned()
    {
        dynamic age = 20;
        dynamic name = "Tom";

        age = "Tom";
        name = 20;

        age = 20;
        name = "Tom";

        return $"Hello my name is {name} and I am {age} years old!"; ;
    }

    [Benchmark]
    public string SayHelloTyped()
    {
        int age = 20;
        string name = "Tom";

        return $"Hello my name is {name} and I am {age} years old!"; ;
    }

    [Benchmark]
    public dynamic SayHelloExpando()
    {
        return $"Hello my name is {_person.name} and I am {_person.age} years old!";
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<DynamicBenchmark>();
    }
}