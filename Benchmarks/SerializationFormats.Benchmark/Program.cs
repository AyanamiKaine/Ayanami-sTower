using System;
using System.Collections.Generic;
using System.Formats.Cbor;
using System.IO;
using System.Linq;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MemoryPack;
using MessagePack;
using ProtoBuf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace SerializationFormats.Benchmark
{
    /// <summary>
    /// A sample data model for serialization.
    /// It must be a partial class for MemoryPack and decorated with attributes for each serializer.
    /// </summary>
    [ProtoContract]
    [MessagePackObject]
    [MemoryPackable]
    public partial class LoginPacket
    {
        [ProtoMember(1)]
        [Key(0)]
        public string Username { get; set; } = "";

        [ProtoMember(2)]
        [Key(1)]
        public Guid SessionId { get; set; }

        [ProtoMember(3)]
        [Key(2)]
        public long Timestamp { get; set; }

        [ProtoMember(4)]
        [Key(3)]
        public bool IsValid { get; set; }

        [ProtoMember(5)]
        [Key(4)]
        public List<string> Permissions { get; set; } = [];
    }

    [ProtoContract]
    [MessagePackObject]
    [MemoryPackable]
    public partial struct Vector3
    {
        [ProtoMember(1)][Key(0)] public float X;
        [ProtoMember(2)][Key(1)] public float Y;
        [ProtoMember(3)][Key(2)] public float Z;
    }

    /// <summary>
    /// Custom column to display the size of the serialized payload.
    /// This gives us a clear view of the space efficiency of each format.
    /// </summary>
    public class PayloadSizeColumn : IColumn
    {
        public string Id => nameof(PayloadSizeColumn);
        public string ColumnName => "Payload Size";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => -10;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Size;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            // The BenchmarkCategoryAttribute stores categories in a string array property called 'Categories'.
            var isSerializeBenchmark = benchmarkCase.Descriptor.WorkloadMethod
                .GetCustomAttributes(typeof(BenchmarkCategoryAttribute), true)
                .Cast<BenchmarkCategoryAttribute>()
                .Any(attr => attr.Categories.Contains("Serialize"));

            if (!isSerializeBenchmark)
            {
                return "N/A";
            }

            var n = (int)benchmarkCase.Parameters[nameof(SerializationBenchmarks.NumberOfPermissions)];
            var testPacket = new LoginPacket
            {
                Username = "TestUser",
                SessionId = Guid.Empty, // Use a fixed Guid for consistent sizing
                Timestamp = 0,
                IsValid = true,
                Permissions = new List<string>(n)
            };
            for (int i = 0; i < n; i++)
            {
                testPacket.Permissions.Add($"permission.scope.action.{i}");
            }

            long size = 0;
            var methodName = benchmarkCase.Descriptor.WorkloadMethod.Name;

            switch (methodName)
            {
                case nameof(SerializationBenchmarks.JsonSerialize):
                    size = JsonSerializer.SerializeToUtf8Bytes(testPacket).Length;
                    break;
                case nameof(SerializationBenchmarks.MessagePackSerialize):
                    size = MessagePackSerializer.Serialize(testPacket).Length;
                    break;
                case nameof(SerializationBenchmarks.MemoryPackSerialize):
                    size = MemoryPackSerializer.Serialize(testPacket)?.Length ?? 0;
                    break;
                case nameof(SerializationBenchmarks.ProtobufSerialize):
                    using (var stream = new MemoryStream())
                    {
                        Serializer.Serialize(stream, testPacket);
                        size = stream.Length;
                    }
                    break;
                case nameof(SerializationBenchmarks.CborSerializeSystem):
                    var cborWriter = new CborWriter();
                    cborWriter.WriteStartMap(5);
                    cborWriter.WriteTextString("Username"); cborWriter.WriteTextString(testPacket.Username);
                    cborWriter.WriteTextString("SessionId"); cborWriter.WriteByteString(testPacket.SessionId.ToByteArray());
                    cborWriter.WriteTextString("Timestamp"); cborWriter.WriteInt64(testPacket.Timestamp);
                    cborWriter.WriteTextString("IsValid"); cborWriter.WriteBoolean(testPacket.IsValid);
                    cborWriter.WriteTextString("Permissions");
                    cborWriter.WriteStartArray(testPacket.Permissions.Count);
                    foreach (var p in testPacket.Permissions) cborWriter.WriteTextString(p);
                    cborWriter.WriteEndArray();
                    cborWriter.WriteEndMap();
                    size = cborWriter.Encode().Length;
                    break;
            }

            return BytesToString(size);
        }

        // Helper to format bytes into a readable string (B, KB, MB)
        private static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            if (byteCount == 0) return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
        public string Legend => "The size of the serialized payload";
    }


    /// <summary>
    /// This class contains the benchmark tests for different serialization formats.
    /// We test JSON (the baseline), MessagePack, MemoryPack, Protobuf and CBOR.
    /// Benchmarks are grouped by category to have separate baselines for Serialize and Deserialize.
    /// </summary>
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfig))]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class SerializationBenchmarks
    {
        /// <summary>
        /// We use the [Params] attribute to test how each serializer scales
        /// with different amounts of data. Here, we're varying the number of
        /// items in the 'Permissions' list.
        /// </summary>
        [Params(10, 100, 1000)]
        public int NumberOfPermissions;
        private Vector3[] _vectors = null!;
        private LoginPacket? _testPacket;
        private byte[]? _jsonPayload;
        private byte[]? _messagePackPayload;
        private byte[]? _cborPayload;
        private byte[]? _memoryPackPayload;
        private byte[]? _protobufPayload;

        private byte[]? _jsonVectorPayload;
        private byte[]? _MessagePackVectorPayload;
        private byte[]? _MemoryVectorPayload;
        private byte[]? _ProtoBufVectorPayload;

        /// <summary>
        /// The GlobalSetup method is run once per set of parameters.
        /// We prepare our test object and pre-serialize it for the deserialization benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _testPacket = new LoginPacket
            {
                Username = "TestUser",
                SessionId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsValid = true,
                Permissions = new List<string>(NumberOfPermissions)
            };

            for (int i = 0; i < NumberOfPermissions; i++)
            {
                _testPacket.Permissions.Add($"permission.scope.action.{i}");
            }

            _vectors = new Vector3[10000];
            var rand = new Random(42);
            for (int i = 0; i < _vectors.Length; i++)
            {
                _vectors[i] = new Vector3 { X = rand.NextSingle(), Y = rand.NextSingle(), Z = rand.NextSingle() };
            }

            _jsonVectorPayload = JsonSerializer.SerializeToUtf8Bytes(_vectors);
            _MessagePackVectorPayload = MessagePackSerializer.Serialize(_vectors);
            _MemoryVectorPayload = MemoryPackSerializer.Serialize(_vectors);
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, _vectors);
            _ProtoBufVectorPayload = ms.ToArray();


            // Pre-serialize the data for deserialization benchmarks
            _jsonPayload = JsonSerializer.SerializeToUtf8Bytes(_testPacket);
            _messagePackPayload = MessagePackSerializer.Serialize(_testPacket);
            _cborPayload = CborSerializeSystem(); // Use our manual serializer
            _memoryPackPayload = MemoryPackSerializer.Serialize(_testPacket);

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, _testPacket);
            _protobufPayload = stream.ToArray();
        }

        /*
        [Benchmark(Baseline = true),
        BenchmarkCategory("Vector3ListJson_Serialize")]
        public byte[] Vector3ListJsonSerialize() => JsonSerializer.SerializeToUtf8Bytes(_vectors);
        [Benchmark(Baseline = true),
        BenchmarkCategory("Vector3ListJson_Deserialize")]
        public Vector3[] Vector3ListJsonDeserialize() => JsonSerializer.Deserialize<Vector3[]>(_jsonVectorPayload)!;

        [Benchmark, BenchmarkCategory("Vector3ListMessagePack_Serialize")]
        public byte[] Vector3ListMessagePackSerialize() => MessagePackSerializer.Serialize(_vectors);
        [Benchmark, BenchmarkCategory("Vector3ListMessagePack_Deserialize")]
        public Vector3[] Vector3ListMessagePackDeserialize() => MessagePackSerializer.Deserialize<Vector3[]>(_MessagePackVectorPayload);

        [Benchmark, BenchmarkCategory("Vector3ListMemoryPack_Serialize")]
        public byte[]? Vector3ListMemoryPackSerialize() => MemoryPackSerializer.Serialize(_vectors);
        [Benchmark, BenchmarkCategory("Vector3ListMemoryPack_Deserialize")]
        public Vector3[]? Vector3ListMemoryPackDeserialize() => MemoryPackSerializer.Deserialize<Vector3[]>(_MemoryVectorPayload);

        [Benchmark, BenchmarkCategory("Vector3ListProtobuf_Serialize")]
        public byte[] Vector3ListProtobufSerialize() { using var ms = new MemoryStream(); Serializer.Serialize(ms, _vectors); return ms.ToArray(); }
        [Benchmark, BenchmarkCategory("Vector3ListProtobuf_Deserialize")]
        public Vector3[] Vector3ListProtobufDeserialize() { using var ms = new MemoryStream(_ProtoBufVectorPayload!); return Serializer.Deserialize<Vector3[]>(ms); }
        */
        // ------------- JSON Benchmarks (Baseline) -------------

        [Benchmark(Baseline = true, Description = "System.Text.Json_Serialize")]
        [BenchmarkCategory("Serialize")]
        public byte[] JsonSerialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes(_testPacket!);
        }

        [Benchmark(Baseline = true, Description = "System.Text.Json_Deserialize")]
        [BenchmarkCategory("Deserialize")]
        public LoginPacket JsonDeserialize()
        {
            return JsonSerializer.Deserialize<LoginPacket>(_jsonPayload!)!;
        }

        // ------------- MessagePack-CSharp Benchmarks -------------

        [Benchmark(Description = "MessagePack_Serialize")]
        [BenchmarkCategory("Serialize")]
        public byte[] MessagePackSerialize()
        {
            return MessagePackSerializer.Serialize(_testPacket);
        }

        [Benchmark(Description = "MessagePack_Deserialize")]
        [BenchmarkCategory("Deserialize")]
        public LoginPacket MessagePackDeserialize()
        {
            return MessagePackSerializer.Deserialize<LoginPacket>(_messagePackPayload!);
        }

        // ------------- MemoryPack Benchmarks -------------

        [Benchmark(Description = "MemoryPack_Serialize")]
        [BenchmarkCategory("Serialize")]
        public byte[]? MemoryPackSerialize()
        {
            return MemoryPackSerializer.Serialize(_testPacket);
        }

        [Benchmark(Description = "MemoryPack_Deserialize")]
        [BenchmarkCategory("Deserialize")]
        public LoginPacket? MemoryPackDeserialize()
        {
            return MemoryPackSerializer.Deserialize<LoginPacket>(_memoryPackPayload!);
        }

        // ------------- Protobuf-net Benchmarks -------------

        [Benchmark(Description = "Protobuf-net_Serialize")]
        [BenchmarkCategory("Serialize")]
        public byte[] ProtobufSerialize()
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, _testPacket);
            return stream.ToArray();
        }

        [Benchmark(Description = "Protobuf-net_Deserialize")]
        [BenchmarkCategory("Deserialize")]
        public LoginPacket ProtobufDeserialize()
        {
            using var stream = new MemoryStream(_protobufPayload!);
            return Serializer.Deserialize<LoginPacket>(stream);
        }

        // ------------- System.Formats.Cbor Benchmarks -------------

        [Benchmark(Description = "Cbor_Serialize")]
        [BenchmarkCategory("Serialize")]
        public byte[] CborSerializeSystem()
        {
            var writer = new CborWriter();
            writer.WriteStartMap(5);

            writer.WriteTextString("Username");
            writer.WriteTextString(_testPacket!.Username);

            writer.WriteTextString("SessionId");
            writer.WriteByteString(_testPacket.SessionId.ToByteArray());

            writer.WriteTextString("Timestamp");
            writer.WriteInt64(_testPacket.Timestamp);

            writer.WriteTextString("IsValid");
            writer.WriteBoolean(_testPacket.IsValid);

            writer.WriteTextString("Permissions");
            writer.WriteStartArray(_testPacket.Permissions.Count);
            foreach (var permission in _testPacket.Permissions)
            {
                writer.WriteTextString(permission);
            }
            writer.WriteEndArray();

            writer.WriteEndMap();
            return writer.Encode();
        }

        [Benchmark(Description = "Cbor_Deserialize")]
        [BenchmarkCategory("Deserialize")]
        public LoginPacket CborDeserializeSystem()
        {
            var reader = new CborReader(_cborPayload!);
            var packet = new LoginPacket();

            reader.ReadStartMap();
            while (reader.PeekState() != CborReaderState.EndMap)
            {
                var propertyName = reader.ReadTextString();
                switch (propertyName)
                {
                    case "Username":
                        packet.Username = reader.ReadTextString();
                        break;
                    case "SessionId":
                        packet.SessionId = new Guid(reader.ReadByteString());
                        break;
                    case "Timestamp":
                        packet.Timestamp = reader.ReadInt64();
                        break;
                    case "IsValid":
                        packet.IsValid = reader.ReadBoolean();
                        break;
                    case "Permissions":
                        reader.ReadStartArray();
                        var perms = new List<string>();
                        while (reader.PeekState() != CborReaderState.EndArray)
                        {
                            perms.Add(reader.ReadTextString());
                        }
                        packet.Permissions = perms;
                        reader.ReadEndArray();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }
            reader.ReadEndMap();
            return packet;
        }
    }

    /// <summary>
    /// Custom configuration for BenchmarkDotNet.
    /// </summary>
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, RPlotExporter.Default);
            AddJob(Job.Default.WithWarmupCount(1).WithIterationCount(10));
            Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);

            // Add our custom column to the output
            AddColumn(new PayloadSizeColumn());
        }
    }

    // ------------- User Provided Program Class -------------
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Running serialization format benchmarks...");
            var summary = BenchmarkRunner.Run<SerializationBenchmarks>();
            Console.WriteLine("\nBenchmark run complete.");
            Console.WriteLine(summary);
        }
    }
}
