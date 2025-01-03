using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using MessagePack;

namespace JsonParseLatencyTest;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class JsonParsingBenchmark
{
    private Person? simplePerson;
    private ComplexObject? complexObject;
    private string? simpleJsonString;
    private string? complexJsonString;
    private byte[]? simpleMsgPackBytes;
    private byte[]? complexMsgPackBytes;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize the simple object
        simplePerson = new Person { Name = "John Doe", Age = 30, City = "New York" };

        // Create a more complex object with nested objects and arrays
        complexObject = new ComplexObject
        {
            Name = "John Doe",
            Age = 30,
            City = "New York",
            Address = new Address
            {
                Street = "123 Main St",
                Zip = "10001"
            },
            Orders =
            [
                new() { Id = 1, Product = "Laptop", Price = 1200 },
                new() { Id = 2, Product = "Mouse", Price = 25 }
            ],
            IsActive = true
        };

        // Serialize to JSON strings
        simpleJsonString = JsonSerializer.Serialize(simplePerson);
        complexJsonString = JsonSerializer.Serialize(complexObject);

        // Serialize to MessagePack
        simpleMsgPackBytes = MessagePackSerializer.Serialize(simplePerson);
        complexMsgPackBytes = MessagePackSerializer.Serialize(complexObject);
    }

    // --- Deserialize Benchmarks ---

    [Benchmark]
    public void SystemTextJson_Simple_Deserialize()
    {
        // Parse the simple JSON string using System.Text.Json
        var person = JsonSerializer.Deserialize<Person>(simpleJsonString!);
    }

    [Benchmark]
    public void NewtonsoftJson_Simple_Deserialize()
    {
        // Parse the simple JSON string using Newtonsoft.Json
        var person = Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(simpleJsonString!);
    }

    [Benchmark]
    public void SystemTextJson_Complex_Deserialize()
    {
        // Parse the complex JSON string using System.Text.Json
        var complexObjectResult = JsonSerializer.Deserialize<ComplexObject>(complexJsonString!);
    }

    [Benchmark]
    public void NewtonsoftJson_Complex_Deserialize()
    {
        // Parse the complex JSON string using Newtonsoft.Json
        var complexObjectResult = Newtonsoft.Json.JsonConvert.DeserializeObject<ComplexObject>(complexJsonString!);
    }

    [Benchmark]
    public void MessagePack_Simple_Deserialize_Int_Key()
    {
        // Deserialize the simple object from MessagePack bytes
        var person = MessagePackSerializer.Deserialize<Person>(simpleMsgPackBytes);
    }

    [Benchmark]
    public void MessagePack_Complex_Deserialize_Int_Key()
    {
        // Deserialize the complex object from MessagePack bytes
        var complexObjectResult = MessagePackSerializer.Deserialize<ComplexObject>(complexMsgPackBytes);
    }

    // --- Serialize Benchmarks ---

    [Benchmark]
    public void SystemTextJson_Simple_Serialize()
    {
        // Serialize the simple object using System.Text.Json
        var jsonString = JsonSerializer.Serialize(simplePerson);
    }

    [Benchmark]
    public void NewtonsoftJson_Simple_Serialize()
    {
        // Serialize the simple object using Newtonsoft.Json
        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(simplePerson);
    }

    [Benchmark]
    public void SystemTextJson_Complex_Serialize()
    {
        // Serialize the complex object using System.Text.Json
        var jsonString = JsonSerializer.Serialize(complexObject);
    }

    [Benchmark]
    public void NewtonsoftJson_Complex_Serialize()
    {
        // Serialize the complex object using Newtonsoft.Json
        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(complexObject);
    }

    [Benchmark]
    public void MessagePack_Simple_Serialize_Int_Key()
    {
        // Serialize the simple object to MessagePack
        var bytes = MessagePackSerializer.Serialize(simplePerson);
    }

    [Benchmark]
    public void MessagePack_Complex_Serialize_Int_Key()
    {
        // Serialize the complex object to MessagePack
        var bytes = MessagePackSerializer.Serialize(complexObject);
    }

    [MessagePackObject]
    public class Person
    {
        [Key(0)] // Important for MessagePack serialization order
        public required string Name { get; set; }
        [Key(1)]
        public required int Age { get; set; }
        [Key(2)]
        public required string City { get; set; }
    }

    [MessagePackObject]
    public class ComplexObject
    {
        [Key(0)]
        public required string Name { get; set; }
        [Key(1)]
        public required int Age { get; set; }
        [Key(2)]
        public required string City { get; set; }
        [Key(3)]
        public required Address Address { get; set; }
        [Key(4)]
        public required List<Order> Orders { get; set; }
        [Key(5)]
        public required bool IsActive { get; set; }
    }

    [MessagePackObject]
    public class Address
    {
        [Key(0)]
        public required string Street { get; set; }
        [Key(1)]
        public required string Zip { get; set; }
    }

    [MessagePackObject]
    public class Order
    {
        [Key(0)]
        public required int Id { get; set; }
        [Key(1)]
        public required string Product { get; set; }
        [Key(2)]
        public required decimal Price { get; set; }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<JsonParsingBenchmark>();
    }
}