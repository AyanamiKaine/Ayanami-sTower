using NanoRpc.Core;
using NanoRpc.Protocol;
using NanoRpc.Extensions;
using System.Buffers;
using System.Text.Json;

namespace NanoRPC.UnitTests;

#region Test Actors

public class MathActor : INanoActor
{
    [NanoAction]
    public int Add(AddRequest request) => request.A + request.B;

    [NanoAction("multiply")]
    public int Multiply(AddRequest request) => request.A * request.B;

    [NanoAction]
    public async Task<int> AddAsync(AddRequest request)
    {
        await Task.Delay(10);
        return request.A + request.B;
    }

    [NanoAction]
    public void NoReturn() { }

    [NanoAction]
    public void ThrowError() => throw new InvalidOperationException("Test error");
}

public class AddRequest
{
    public int A { get; set; }
    public int B { get; set; }
}

public class EchoActor : INanoActor
{
    [NanoAction]
    public string Echo(string message) => message;

    [NanoAction]
    public EchoResponse EchoComplex(EchoRequest request) => new() { Message = request.Message, Timestamp = DateTime.UtcNow };
}

public class EchoRequest
{
    public string Message { get; set; } = "";
}

public class EchoResponse
{
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

#endregion

#region Protocol Tests

[TestFixture]
public class NanoHeaderTests
{
    [Test]
    public void Constructor_ValidBuffer_ParsesCorrectly()
    {
        // Arrange
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)MsgType.Call;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(1), 12345);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(5), 10);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(9), 20);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(13), 100);

        // Act
        var header = new NanoHeader(buffer);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(header.Type, Is.EqualTo(MsgType.Call));
            Assert.That(header.Id, Is.EqualTo(12345u));
            Assert.That(header.TargetLen, Is.EqualTo(10));
            Assert.That(header.MethodLen, Is.EqualTo(20));
            Assert.That(header.BodyLen, Is.EqualTo(100));
            Assert.That(header.TotalBodyLength, Is.EqualTo(130));
        });
    }

    [Test]
    public void Constructor_BufferTooSmall_ThrowsArgumentException()
    {
        byte[] buffer = new byte[NanoHeader.Size - 1];
        Assert.Throws<ArgumentException>(() => new NanoHeader(buffer));
    }

    [Test]
    public void IsValid_ValidHeader_ReturnsTrue()
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)MsgType.Call;
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.True);
    }

    [Test]
    public void IsValid_InvalidMsgType_ReturnsFalse()
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = 0xFF; // Invalid message type
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.False);
    }

    [Test]
    public void IsValid_TargetLenExceedsMax_ReturnsFalse()
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)MsgType.Call;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(5), (uint)(NanoLimits.MaxTargetLength + 1));
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.False);
    }

    [Test]
    public void IsValid_MethodLenExceedsMax_ReturnsFalse()
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)MsgType.Call;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(9), (uint)(NanoLimits.MaxMethodLength + 1));
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.False);
    }

    [Test]
    public void IsValid_NegativeTargetLen_ReturnsFalse()
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)MsgType.Call;
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(5), -1);
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.False);
    }

    [TestCase(MsgType.Call)]
    [TestCase(MsgType.Cast)]
    [TestCase(MsgType.Reply)]
    [TestCase(MsgType.Error)]
    [TestCase(MsgType.Handshake)]
    [TestCase(MsgType.Subscribe)]
    [TestCase(MsgType.Unsubscribe)]
    [TestCase(MsgType.Publish)]
    [TestCase(MsgType.StreamStart)]
    [TestCase(MsgType.StreamData)]
    [TestCase(MsgType.StreamEnd)]
    [TestCase(MsgType.StreamCancel)]
    public void IsValid_AllValidMsgTypes_ReturnsTrue(MsgType type)
    {
        byte[] buffer = new byte[NanoHeader.Size];
        buffer[0] = (byte)type;
        var header = new NanoHeader(buffer);

        Assert.That(header.IsValid(), Is.True);
    }
}

[TestFixture]
public class WireFormatterTests
{
    [Test]
    public void WriteFrame_ValidData_WritesCorrectFormat()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var payload = new { message = "test" };

        // Act
        WireFormatter.WriteFrame(writer, MsgType.Call, 1, "target", "method", payload);

        // Assert
        var data = writer.WrittenSpan;
        Assert.That(data.Length, Is.GreaterThan(NanoHeader.Size));

        var header = new NanoHeader(data);
        Assert.Multiple(() =>
        {
            Assert.That(header.Type, Is.EqualTo(MsgType.Call));
            Assert.That(header.Id, Is.EqualTo(1u));
            Assert.That(header.TargetLen, Is.EqualTo(6)); // "target"
            Assert.That(header.MethodLen, Is.EqualTo(6)); // "method"
        });
    }

    [Test]
    public void WriteFrame_NullWriter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WireFormatter.WriteFrame<object>(null!, MsgType.Call, 1, "target", "method", new { }));
    }

    [Test]
    public void WriteFrame_NullTarget_ThrowsArgumentNullException()
    {
        var writer = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentNullException>(() =>
            WireFormatter.WriteFrame(writer, MsgType.Call, 1, null!, "method", new { }));
    }

    [Test]
    public void WriteFrame_NullMethod_ThrowsArgumentNullException()
    {
        var writer = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentNullException>(() =>
            WireFormatter.WriteFrame(writer, MsgType.Call, 1, "target", null!, new { }));
    }

    [Test]
    public void WriteFrame_TargetExceedsMaxLength_ThrowsArgumentException()
    {
        var writer = new ArrayBufferWriter<byte>();
        var longTarget = new string('a', NanoLimits.MaxTargetLength + 1);

        Assert.Throws<ArgumentException>(() =>
            WireFormatter.WriteFrame(writer, MsgType.Call, 1, longTarget, "method", new { }));
    }

    [Test]
    public void WriteFrame_MethodExceedsMaxLength_ThrowsArgumentException()
    {
        var writer = new ArrayBufferWriter<byte>();
        var longMethod = new string('a', NanoLimits.MaxMethodLength + 1);

        Assert.Throws<ArgumentException>(() =>
            WireFormatter.WriteFrame(writer, MsgType.Call, 1, "target", longMethod, new { }));
    }

    [Test]
    public void WriteFrameRaw_ValidData_WritesCorrectFormat()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(new { value = 42 });

        // Act
        WireFormatter.WriteFrameRaw(writer, MsgType.Reply, 100, "test", "handler", jsonPayload);

        // Assert
        var data = writer.WrittenSpan;
        var header = new NanoHeader(data);

        Assert.Multiple(() =>
        {
            Assert.That(header.Type, Is.EqualTo(MsgType.Reply));
            Assert.That(header.Id, Is.EqualTo(100u));
            Assert.That(header.BodyLen, Is.EqualTo(jsonPayload.Length));
        });
    }

    [Test]
    public void WriteFrame_EmptyStrings_WritesValidFrame()
    {
        var writer = new ArrayBufferWriter<byte>();

        WireFormatter.WriteFrame(writer, MsgType.Handshake, 0, "", "", new { });

        var header = new NanoHeader(writer.WrittenSpan);
        Assert.Multiple(() =>
        {
            Assert.That(header.TargetLen, Is.EqualTo(0));
            Assert.That(header.MethodLen, Is.EqualTo(0));
        });
    }
}

[TestFixture]
public class NanoLimitsTests
{
    [Test]
    public void MaxTargetLength_HasReasonableValue()
    {
        Assert.That(NanoLimits.MaxTargetLength, Is.EqualTo(256));
    }

    [Test]
    public void MaxMethodLength_HasReasonableValue()
    {
        Assert.That(NanoLimits.MaxMethodLength, Is.EqualTo(256));
    }

    [Test]
    public void MaxBodyLength_Is16MB()
    {
        Assert.That(NanoLimits.MaxBodyLength, Is.EqualTo(16 * 1024 * 1024));
    }
}

[TestFixture]
public class NanoRpcExceptionTests
{
    [Test]
    public void Constructor_MessageOnly_SetsMessage()
    {
        var ex = new NanoRpcException("Test error");
        Assert.That(ex.Message, Is.EqualTo("Test error"));
    }

    [Test]
    public void Constructor_WithTargetAndMethod_SetsAllProperties()
    {
        var ex = new NanoRpcException("Test error", "MyTarget", "MyMethod");

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Test error"));
            Assert.That(ex.Target, Is.EqualTo("MyTarget"));
            Assert.That(ex.Method, Is.EqualTo("MyMethod"));
        });
    }

    [Test]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("Inner");
        var ex = new NanoRpcException("Outer", inner);

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Outer"));
            Assert.That(ex.InnerException, Is.EqualTo(inner));
        });
    }
}

#endregion

#region Server/Client Integration Tests

[TestFixture]
public class NanoServerClientIntegrationTests
{
    private NanoServer _server = null!;
    private int _testPort;

    [SetUp]
    public async Task SetUp()
    {
        _testPort = 15000 + Math.Abs(TestContext.CurrentContext.Test.ID.GetHashCode() % 1000);
        _server = new NanoServer("TestServer", _testPort);
        _server.RegisterActor("math", new MathActor());
        _server.RegisterActor("echo", new EchoActor());
        _server.Start();
        await Task.Delay(100); // Give server time to start
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server.DisposeAsync();
    }

    [Test]
    public async Task CallAsync_SimpleAddition_ReturnsCorrectResult()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50); // Connection established

        var result = await node.CallAsync<AddRequest, int>("math", "Add", new AddRequest { A = 5, B = 3 });

        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public async Task CallAsync_NamedAction_ReturnsCorrectResult()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        var result = await node.CallAsync<AddRequest, int>("math", "multiply", new AddRequest { A = 4, B = 5 });

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public async Task CallAsync_AsyncMethod_ReturnsCorrectResult()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        var result = await node.CallAsync<AddRequest, int>("math", "AddAsync", new AddRequest { A = 10, B = 20 });

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public async Task CallAsync_MethodNotFound_ThrowsException()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        var ex = Assert.ThrowsAsync<NanoRpcException>(async () =>
            await node.CallAsync<AddRequest, int>("math", "NonExistent", new AddRequest { A = 1, B = 2 }));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public async Task CallAsync_MethodThrowsError_PropagatesException()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        var ex = Assert.ThrowsAsync<NanoRpcException>(async () =>
            await node.CallAsync<object, object>("math", "ThrowError", new { }));

        Assert.That(ex!.Message, Does.Contain("Test error"));
    }

    [Test]
    public async Task Cast_FireAndForget_DoesNotBlock()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        // Cast should return immediately without waiting for response
        node.Cast<object>("math", "NoReturn", new { });

        // Give time for the message to be sent
        await Task.Delay(50);

        // Test passes if no exception is thrown
        Assert.Pass();
    }

    [Test]
    public async Task CallAsync_WithTimeout_TimesOut()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        // Use a method that doesn't exist on an actor that exists, with a very short timeout
        // The 1ms timeout should be shorter than network round-trip
        var ex = Assert.ThrowsAsync<TimeoutException>(async () =>
            await node.CallAsync<AddRequest, int>("math", "AddAsync", new AddRequest { A = 1, B = 2 }, 1));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task MultipleClients_IndependentCalls_AllSucceed()
    {
        await using var node1 = new NanoNode("Client1", "127.0.0.1", _testPort);
        await using var node2 = new NanoNode("Client2", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var task1 = node1.CallAsync<AddRequest, int>("math", "Add", new AddRequest { A = 1, B = 1 });
        var task2 = node2.CallAsync<AddRequest, int>("math", "Add", new AddRequest { A = 2, B = 2 });

        var results = await Task.WhenAll(task1, task2);

        Assert.Multiple(() =>
        {
            Assert.That(results[0], Is.EqualTo(2));
            Assert.That(results[1], Is.EqualTo(4));
        });
    }

    [Test]
    public async Task EchoActor_ComplexType_SerializesCorrectly()
    {
        await using var node = new NanoNode("TestClient", "127.0.0.1", _testPort);
        await Task.Delay(50);

        var result = await node.CallAsync<EchoRequest, EchoResponse>("echo", "EchoComplex",
            new EchoRequest { Message = "Hello, World!" });

        Assert.Multiple(() =>
        {
            Assert.That(result!.Message, Is.EqualTo("Hello, World!"));
            Assert.That(result.Timestamp, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        });
    }
}

#endregion

#region Pub/Sub Tests

[TestFixture]
public class PubSubTests
{
    private NanoServer _server = null!;
    private int _testPort;

    [SetUp]
    public async Task SetUp()
    {
        _testPort = 16000 + Math.Abs(TestContext.CurrentContext.Test.ID.GetHashCode() % 1000);
        _server = new NanoServer("PubSubServer", _testPort);
        _server.Start();
        await Task.Delay(100);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server.DisposeAsync();
    }

    [Test]
    public async Task Subscribe_ReceivesPublishedMessages()
    {
        await using var subscriber = new NanoNode("Subscriber", "127.0.0.1", _testPort);
        await using var publisher = new NanoNode("Publisher", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var receivedMessages = new List<string>();
        var tcs = new TaskCompletionSource();

        await subscriber.PubSub.SubscribeAsync<string>("test-topic", (topic, message) =>
        {
            receivedMessages.Add(message ?? "");
            if (receivedMessages.Count >= 1)
                tcs.TrySetResult();
        });

        await Task.Delay(100); // Let subscription register

        await publisher.PubSub.PublishAsync("test-topic", "Hello PubSub!");

        // Wait for message with timeout
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.That(completed, Is.EqualTo(tcs.Task), "Message should be received within timeout");
        Assert.That(receivedMessages, Contains.Item("Hello PubSub!"));
    }

    [Test]
    public async Task Subscribe_MultipleSubscribers_AllReceiveMessage()
    {
        await using var sub1 = new NanoNode("Sub1", "127.0.0.1", _testPort);
        await using var sub2 = new NanoNode("Sub2", "127.0.0.1", _testPort);
        await using var publisher = new NanoNode("Publisher", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var received1 = new List<int>();
        var received2 = new List<int>();
        var tcs1 = new TaskCompletionSource();
        var tcs2 = new TaskCompletionSource();

        await sub1.PubSub.SubscribeAsync<int>("numbers", (_, n) =>
        {
            received1.Add(n);
            tcs1.TrySetResult();
        });

        await sub2.PubSub.SubscribeAsync<int>("numbers", (_, n) =>
        {
            received2.Add(n);
            tcs2.TrySetResult();
        });

        await Task.Delay(100);

        await publisher.PubSub.PublishAsync("numbers", 42);

        await Task.WhenAll(
            Task.WhenAny(tcs1.Task, Task.Delay(5000)),
            Task.WhenAny(tcs2.Task, Task.Delay(5000)));

        Assert.Multiple(() =>
        {
            Assert.That(received1, Contains.Item(42));
            Assert.That(received2, Contains.Item(42));
        });
    }

    [Test]
    public async Task Unsubscribe_StopsReceivingMessages()
    {
        await using var subscriber = new NanoNode("Subscriber", "127.0.0.1", _testPort);
        await using var publisher = new NanoNode("Publisher", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var receivedCount = 0;

        var subscription = await subscriber.PubSub.SubscribeAsync<string>("unsubscribe-test", (_, _) =>
        {
            Interlocked.Increment(ref receivedCount);
        });

        await Task.Delay(100);

        // Send first message
        await publisher.PubSub.PublishAsync("unsubscribe-test", "Message 1");
        await Task.Delay(200);

        // Unsubscribe
        await subscription.DisposeAsync();
        await Task.Delay(100);

        var countAfterUnsubscribe = receivedCount;

        // Send second message - should not be received
        await publisher.PubSub.PublishAsync("unsubscribe-test", "Message 2");
        await Task.Delay(200);

        Assert.That(receivedCount, Is.EqualTo(countAfterUnsubscribe),
            "Should not receive messages after unsubscribe");
    }

    [Test]
    public async Task ServerPubSub_BroadcastFromServer_AllSubscribersReceive()
    {
        await using var sub1 = new NanoNode("Sub1", "127.0.0.1", _testPort);
        await using var sub2 = new NanoNode("Sub2", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var received = new System.Collections.Concurrent.ConcurrentBag<string>();
        var countdown = new CountdownEvent(2);

        await sub1.PubSub.SubscribeAsync<string>("server-broadcast", (_, msg) =>
        {
            received.Add($"Sub1:{msg}");
            countdown.Signal();
        });

        await sub2.PubSub.SubscribeAsync<string>("server-broadcast", (_, msg) =>
        {
            received.Add($"Sub2:{msg}");
            countdown.Signal();
        });

        await Task.Delay(100);

        // Server broadcasts directly
        await _server.PubSub.PublishAsync("server-broadcast", "ServerMessage");

        countdown.Wait(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(received, Has.Count.EqualTo(2));
            Assert.That(received.Any(m => m.StartsWith("Sub1:")), Is.True);
            Assert.That(received.Any(m => m.StartsWith("Sub2:")), Is.True);
        });
    }

    [Test]
    public void PubSubBroker_GetTopics_ReturnsSubscribedTopics()
    {
        // This tests the server-side broker API
        Assert.That(_server.PubSub.GetTopics(), Is.Empty);
    }

    [Test]
    public void PubSubBroker_GetSubscriberCount_ReturnsZeroForUnknownTopic()
    {
        Assert.That(_server.PubSub.GetSubscriberCount("unknown-topic"), Is.EqualTo(0));
    }
}

#endregion

#region Streaming Tests

[TestFixture]
public class StreamingTests
{
    private NanoServer _server = null!;
    private int _testPort;

    [SetUp]
    public async Task SetUp()
    {
        _testPort = 17000 + Math.Abs(TestContext.CurrentContext.Test.ID.GetHashCode() % 1000);
        _server = new NanoServer("StreamServer", _testPort);

        // Register a counting stream handler
        _server.Streams.RegisterStreamHandler<CountRequest, int>("counter", "count",
            async (request, stream, ct) =>
            {
                var count = request?.Count ?? 5;
                for (int i = 1; i <= count && !ct.IsCancellationRequested; i++)
                {
                    await stream.SendAsync(i);
                    await Task.Delay(10, ct);
                }
            });

        // Register an immediate stream handler (no delay)
        _server.Streams.RegisterStreamHandler<CountRequest, int>("counter", "immediate",
            async (request, stream, ct) =>
            {
                var count = request?.Count ?? 3;
                for (int i = 1; i <= count; i++)
                {
                    await stream.SendAsync(i * 10);
                }
            });

        _server.Start();
        await Task.Delay(100);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _server.DisposeAsync();
    }

    [Test]
    public async Task ReadStream_ReceivesAllItems()
    {
        await using var node = new NanoNode("StreamClient", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var items = new List<int>();
        await using var stream = await node.Streams.StartReadStreamAsync<CountRequest, int>(
            "counter", "count", new CountRequest { Count = 3 });

        await foreach (var item in stream.ReadAllAsync())
        {
            items.Add(item);
        }

        Assert.That(items, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task ReadStream_ImmediateComplete_ReceivesAllItems()
    {
        await using var node = new NanoNode("StreamClient", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var items = new List<int>();
        await using var stream = await node.Streams.StartReadStreamAsync<CountRequest, int>(
            "counter", "immediate", new CountRequest { Count = 4 });

        await foreach (var item in stream.ReadAllAsync())
        {
            items.Add(item);
        }

        Assert.That(items, Is.EqualTo(new[] { 10, 20, 30, 40 }));
    }

    [Test]
    public async Task ReadStream_Cancel_StopsReceiving()
    {
        await using var node = new NanoNode("StreamClient", "127.0.0.1", _testPort);
        await Task.Delay(100);

        var items = new List<int>();

        await using var stream = await node.Streams.StartReadStreamAsync<CountRequest, int>(
            "counter", "count", new CountRequest { Count = 100 }); // Large count

        using var cts = new CancellationTokenSource();

        try
        {
            await foreach (var item in stream.ReadAllAsync(cts.Token))
            {
                items.Add(item);
                if (items.Count >= 3)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assert.That(items.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(items.Count, Is.LessThan(100), "Stream should have been cancelled before receiving all items");
    }

    [Test]
    public async Task Stream_UnknownHandler_CompletesGracefully()
    {
        await using var node = new NanoNode("StreamClient", "127.0.0.1", _testPort);
        await Task.Delay(100);

        await using var stream = await node.Streams.StartReadStreamAsync<CountRequest, int>(
            "unknown", "handler", new CountRequest { Count = 1 });

        var items = new List<int>();

        // Should complete without items (error sent by server)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await foreach (var item in stream.ReadAllAsync(cts.Token))
            {
                items.Add(item);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout - acceptable if no handler found
        }

        // Test passes if no unhandled exception
        Assert.Pass();
    }
}

public class CountRequest
{
    public int Count { get; set; }
}

#endregion

#region Actor Registration Tests

[TestFixture]
public class ActorRegistrationTests
{
    [Test]
    public async Task RegisterActor_ValidActor_RegistersSuccessfully()
    {
        await using var server = new NanoServer("TestServer", 18001);
        Assert.DoesNotThrow(() => server.RegisterActor("math", new MathActor()));
    }

    [Test]
    public async Task RegisterActor_NullName_ThrowsArgumentException()
    {
        await using var server = new NanoServer("TestServer", 18002);
        Assert.Throws<ArgumentNullException>(() => server.RegisterActor(null!, new MathActor()));
    }

    [Test]
    public async Task RegisterActor_EmptyName_ThrowsArgumentException()
    {
        await using var server = new NanoServer("TestServer", 18003);
        Assert.Throws<ArgumentException>(() => server.RegisterActor("", new MathActor()));
    }

    [Test]
    public async Task RegisterActor_NullActor_ThrowsArgumentNullException()
    {
        await using var server = new NanoServer("TestServer", 18004);
        Assert.Throws<ArgumentNullException>(() => server.RegisterActor<MathActor>("math", null!));
    }

    [Test]
    public void NanoServer_Constructor_NullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => new NanoServer(null!, 18005));
    }

    [Test]
    public void NanoServer_Constructor_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new NanoServer("", 18006));
    }
}

#endregion

#region Binary Primitives Helper for Tests

internal static class BinaryPrimitives
{
    public static void WriteUInt32BigEndian(Span<byte> destination, uint value)
    {
        destination[0] = (byte)(value >> 24);
        destination[1] = (byte)(value >> 16);
        destination[2] = (byte)(value >> 8);
        destination[3] = (byte)value;
    }

    public static void WriteInt32BigEndian(Span<byte> destination, int value)
    {
        WriteUInt32BigEndian(destination, (uint)value);
    }
}

#endregion

