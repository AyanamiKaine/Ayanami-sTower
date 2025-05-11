using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AyanamisTower.NihilEx; // Your Memory class namespace
using AyanamisTower.SFPM; // Your SFPM namespace
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using NLog;
using Perfolizer.Horology;
using Perfolizer.Metrology;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SfpMemoryBenchmark;

public class MillisecondConfig : ManualConfig
{
    public MillisecondConfig()
    {
        // --- Add essential components ---

        // Keep default columns (like Mean, Error, StdDev, Median, Ratio, Allocated)
        AddColumnProvider(DefaultColumnProviders.Instance);

        // Add your desired exporters
        AddExporter(MarkdownExporter.GitHub); // Keep GitHub Markdown format
        AddExporter(DefaultExporters.Csv); // Add CSV for easy data processing
        AddExporter(DefaultExporters.Html); // Add HTML report

        // Add your diagnosers
        AddDiagnoser(MemoryDiagnoser.Default); // Keep memory diagnoser

        // Optional: Add default Job definition if needed, otherwise defaults are used
        // AddJob(Job.Default);

        // --- Define and apply the Summary Style with Milliseconds ---
        var style = new SummaryStyle(
            cultureInfo: CultureInfo.InvariantCulture, // Use invariant culture for consistency
            printUnitsInHeader: true, // Show units in column headers (e.g., "Mean [ms]")
            sizeUnit: SizeUnit.KB, // Don't repeat units in every cell
            timeUnit: TimeUnit.Millisecond // *** Set the desired time unit here! ***
            , // Keep size units as KB (or choose B, MB)
            printUnitsInContent: false
        );

        // Apply the custom style
        WithSummaryStyle(style);
    }
}

[MemoryDiagnoser] // Add this to get memory allocation stats
[Config(typeof(MillisecondConfig))]
[MarkdownExporterAttribute.GitHub] // Nice formatting for results
public class MemorySfpBenchmark
{
    // --- Fields for Memory Benchmark ---
    // Now using lists to hold multiple instances
    private List<Memory> _memoryInstances = null!;
    private List<Rule> _rules = null!; // Rules can be shared

    // --- Fields for Dictionary Baseline ---
    private List<Dictionary<string, object>> _dictionaryFactInstances = null!;
    private List<Rule> _rulesForDict = null!; // Separate rules list for baseline setup

    // --- Shared Fields ---
    private List<string> _factKeysPool = null!; // Pool of possible keys
    private Random _random = null!;
    private volatile int _dummyPayloadCounter = 0;
    private int _writesPerTick; // Calculated based on params

    // --- Parameters to vary the benchmark ---

    [Params(10000)] // Number of Memory instances to simulate
    public int MemoryInstanceCount { get; set; }

    [Params(500)] // Number of rules to match against
    public int RuleCount { get; set; }

    [Params(100)] // Number of facts PER Memory instance
    public int FactsPerInstance { get; set; }

    [Params(50)] // Number of facts stored in Memory/Dictionary
    public int FactCount { get; set; }

    [Params(10)] // Average number of criteria per rule
    public int CriteriaPerRule { get; set; }

    // Ratio of writes/updates to perform relative to FactCount per "tick"
    [Params(0.01)]
    public double WriteRatioPerTick { get; set; }

    [GlobalSetup(
        Targets = new[]
        {
            nameof(GameTickSimulation_MultiMemory_SequentialMatch),
            nameof(GameTickSimulation_MultiMemory_ParallelMatch),
        }
    )]
    public void GlobalSetupMemory()
    {
        Console.WriteLine(
            $"\nGlobalSetup START for Memory: Instances={MemoryInstanceCount}, Rules={RuleCount}, FactsPerInstance={FactsPerInstance}, Criteria={CriteriaPerRule}, WriteRatio={WriteRatioPerTick}"
        );
        SetupCommon(isBaseline: false);
        Console.WriteLine("GlobalSetup END for Memory");
    }

    // --- Setup for the Dictionary baseline benchmark (Sequential and Parallel) ---
    // Updated targets to include the new parallel benchmark
    [GlobalSetup(
        Targets = new[]
        {
            nameof(GameTickSimulation_MultiDictionary_SequentialBaseline),
            nameof(GameTickSimulation_MultiDictionary_ParallelBaseline),
        }
    )]
    public void GlobalSetupDictionary()
    {
        Console.WriteLine(
            $"\nGlobalSetup START for Dictionary: Instances={MemoryInstanceCount}, Rules={RuleCount}, FactsPerInstance={FactsPerInstance}, Criteria={CriteriaPerRule}, WriteRatio={WriteRatioPerTick}"
        );
        SetupCommon(isBaseline: true);
        Console.WriteLine("GlobalSetup END for Dictionary");
    }

    /// <summary>
    /// Common setup logic used by both GlobalSetups.
    /// </summary>
    private void SetupCommon(bool isBaseline)
    {
        LogManager.Configuration?.LoggingRules.Clear();
        LogManager.ReconfigExistingLoggers();

        _random = new Random(42);
        // Calculate writes per tick based on *one* instance, applied randomly later
        _writesPerTick = Math.Max(1, (int)(FactsPerInstance * WriteRatioPerTick));

        var factTypeMap = new Dictionary<string, Type>();
        int intFacts = FactsPerInstance / 2;
        int stringFacts = FactsPerInstance - intFacts;

        // Generate key pool first
        _factKeysPool = new List<string>(FactsPerInstance);
        for (int i = 0; i < intFacts; i++)
            _factKeysPool.Add($"IntFact{i}");
        for (int i = 0; i < stringFacts; i++)
            _factKeysPool.Add($"StringFact{i}");

        // --- Setup Fact Instances (Memory or Dictionary) ---
        if (!isBaseline)
            _memoryInstances = new List<Memory>(MemoryInstanceCount);
        else
            _dictionaryFactInstances = new List<Dictionary<string, object>>(MemoryInstanceCount);

        for (int inst = 0; inst < MemoryInstanceCount; inst++)
        {
            if (!isBaseline)
            {
                var memory = new Memory();
                for (int i = 0; i < intFacts; i++)
                    memory.SetValue($"IntFact{i}", _random.Next(1000));
                for (int i = 0; i < stringFacts; i++)
                    memory.SetValue($"StringFact{i}", $"Value_{_random.Next(1000)}");
                _memoryInstances.Add(memory);
            }
            else
            {
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < intFacts; i++)
                    dict[$"IntFact{i}"] = _random.Next(1000);
                for (int i = 0; i < stringFacts; i++)
                    dict[$"StringFact{i}"] = $"Value_{_random.Next(1000)}";
                _dictionaryFactInstances.Add(dict);
            }
        }

        // --- Setup Rules (Only needs to be done once, can be shared if payload is safe) ---
        var rulesList = new List<Rule>();
        var availableIntKeys = _factKeysPool.Where(k => k.StartsWith("IntFact")).ToList();
        var availableStringKeys = _factKeysPool.Where(k => k.StartsWith("StringFact")).ToList();

        if (availableIntKeys.Count == 0)
            availableIntKeys.Add("DummyIntKey");
        if (availableStringKeys.Count == 0)
            availableStringKeys.Add("DummyStringKey");

        for (int i = 0; i < RuleCount; i++)
        {
            var criterias = new List<ICriteria>();
            int numCriteria = _random.Next(Math.Max(1, CriteriaPerRule - 1), CriteriaPerRule + 2);

            for (int j = 0; j < numCriteria; j++)
            {
                string key;
                ICriteria criteria;
                Operator op = (Operator)_random.Next(6);
                if (_random.Next(2) == 0)
                {
                    key = availableIntKeys[_random.Next(availableIntKeys.Count)];
                    criteria = new Criteria<int>(key, _random.Next(1000), op);
                }
                else
                {
                    key = availableStringKeys[_random.Next(availableStringKeys.Count)];
                    op = (op == Operator.Equal || op == Operator.NotEqual) ? op : Operator.Equal;
                    if (_random.Next(5) == 0)
                        criteria = new Criteria<string>(key, s => s.EndsWith("5"));
                    else
                        criteria = new Criteria<string>(key, $"Value_{_random.Next(1000)}", op);
                }
                criterias.Add(criteria);
            }
            rulesList.Add(
                new Rule(
                    criterias,
                    () =>
                    {
                        _dummyPayloadCounter++;
                    }
                )
            ); // Shared dummy payload is safe
        }
        rulesList.OptimizeRules();

        // Assign to the correct field based on which setup we are running
        if (!isBaseline)
            _rules = rulesList;
        else
            _rulesForDict = rulesList;

        Console.WriteLine($"    Generated {_factKeysPool.Count} unique keys.");
        Console.WriteLine(
            $"    Created {MemoryInstanceCount} fact instances (Type: {(isBaseline ? "Dictionary" : "Memory")})."
        );
        Console.WriteLine($"    Generated {rulesList.Count} rules.");
        Console.WriteLine(
            $"    Calculated {_writesPerTick} writes per tick (to be applied randomly)."
        );
    }

    [Benchmark(Description = "Memory_MultiInstance_Tick")]
    public void GameTickSimulation_MultiMemory_SequentialMatch()
    {
        // --- 1. Match ---
        // Match the *same* ruleset against *each* memory instance
        foreach (var memoryInstance in _memoryInstances)
        {
            _rules.Match(memoryInstance);
        }

        // --- 2. Simulate Writes/Updates ---
        // Randomly pick instances and keys to update
        for (int i = 0; i < _writesPerTick * MemoryInstanceCount; i++) // Scale writes with instance count
        {
            if (_factKeysPool.Count == 0 || _memoryInstances.Count == 0)
                continue;

            // Select random instance
            var targetMemory = _memoryInstances[_random.Next(_memoryInstances.Count)];

            // Select random key
            int keyIndex = _random.Next(_factKeysPool.Count);
            string keyToUpdate = _factKeysPool[keyIndex];

            // Update based on key prefix convention
            if (keyToUpdate.StartsWith("IntFact"))
                targetMemory.SetValue(keyToUpdate, _random.Next(1000));
            else if (keyToUpdate.StartsWith("StringFact"))
                targetMemory.SetValue(keyToUpdate, $"Value_{_random.Next(1000)}");
        }
    }

    [Benchmark(Baseline = true, Description = "Dictionary_MultiInstance_Tick")]
    public void GameTickSimulation_MultiDictionary_SequentialBaseline()
    {
        // --- 1. Match ---
        // Match the ruleset against each dictionary instance
        foreach (var dictInstance in _dictionaryFactInstances)
        {
            // Create Query object for each dictionary - this includes adapter overhead
            var query = Query.FromDictionary(dictInstance);
            query.Match(_rulesForDict);
        }

        // --- 2. Simulate Writes/Updates ---
        // Randomly pick instances and keys to update
        for (int i = 0; i < _writesPerTick * MemoryInstanceCount; i++) // Scale writes
        {
            if (_factKeysPool.Count == 0 || _dictionaryFactInstances.Count == 0)
                continue;

            // Select random instance
            var targetDict = _dictionaryFactInstances[_random.Next(_dictionaryFactInstances.Count)];

            // Select random key
            int keyIndex = _random.Next(_factKeysPool.Count);
            string keyToUpdate = _factKeysPool[keyIndex];

            // Update based on key prefix convention
            if (keyToUpdate.StartsWith("IntFact"))
                targetDict[keyToUpdate] = _random.Next(1000);
            else if (keyToUpdate.StartsWith("StringFact"))
                targetDict[keyToUpdate] = $"Value_{_random.Next(1000)}";
        }
    }

    [Benchmark(Description = "Memory_ParallelMatch_Tick")]
    public void GameTickSimulation_MultiMemory_ParallelMatch()
    {
        // Rules list is read-only during match, Memory instance handles its own thread-safety internally
        // --- 1. Match in Parallel ---
        Parallel.ForEach(_memoryInstances, _rules.Match);

        // --- 2. Simulate Writes/Updates (Sequentially after parallel match) ---
        Simulate_Sequential_Updates(_memoryInstances);
    }

    [Benchmark(Description = "Dictionary_ParallelMatch_Tick")]
    public void GameTickSimulation_MultiDictionary_ParallelBaseline()
    {
        // --- 1. Match in Parallel ---
        Parallel.ForEach(
            _dictionaryFactInstances,
            dictInstance =>
            {
                // Create Query object (and internal adapter) per iteration within the parallel loop
                var query = Query.FromDictionary(dictInstance);
                // Rules list is read-only, adapter reads from dictionary (should be safe if dict not modified here)
                query.Match(_rulesForDict);
            }
        );

        // --- 2. Simulate Writes/Updates (Sequentially after parallel match) ---
        Simulate_Sequential_Updates(_dictionaryFactInstances);
    }

    private void Simulate_Sequential_Updates(List<Memory> instances)
    {
        int totalWrites = _writesPerTick * instances.Count;
        for (int i = 0; i < totalWrites; i++)
        {
            if (_factKeysPool.Count == 0 || instances.Count == 0)
                continue;
            var targetMemory = instances[_random.Next(instances.Count)];
            int keyIndex = _random.Next(_factKeysPool.Count);
            string keyToUpdate = _factKeysPool[keyIndex];
            if (keyToUpdate.StartsWith("IntFact"))
                targetMemory.SetValue(keyToUpdate, _random.Next(1000));
            else if (keyToUpdate.StartsWith("StringFact"))
                targetMemory.SetValue(keyToUpdate, $"Value_{_random.Next(1000)}");
        }
    }

    private void Simulate_Sequential_Updates(List<Dictionary<string, object>> instances)
    {
        int totalWrites = _writesPerTick * instances.Count;
        for (int i = 0; i < totalWrites; i++)
        {
            if (_factKeysPool.Count == 0 || instances.Count == 0)
                continue;
            var targetDict = instances[_random.Next(instances.Count)];
            int keyIndex = _random.Next(_factKeysPool.Count);
            string keyToUpdate = _factKeysPool[keyIndex];
            if (keyToUpdate.StartsWith("IntFact"))
                targetDict[keyToUpdate] = _random.Next(1000);
            else if (keyToUpdate.StartsWith("StringFact"))
                targetDict[keyToUpdate] = $"Value_{_random.Next(1000)}";
        }
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting SFPM + Memory Benchmark...");
        // Run the benchmark
        var summary = BenchmarkRunner.Run<MemorySfpBenchmark>();

        // Optional: Print summary details to console
        Console.WriteLine("\nBenchmark Complete.");
        // You can access results programmatically from the 'summary' object if needed
    }
}
