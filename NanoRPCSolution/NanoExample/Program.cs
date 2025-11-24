using NanoRpc.Core;
using NanoRpc.Client;
using NanoRpc.Extensions;
using NanoRpc.Middleware;
using NanoRpc.Protocol;

// --- NANORPC COMPLETE EXAMPLE ---
// Demonstrates: RPC, Middleware, Pub/Sub, and Streaming

const int Port = 8023;

Console.WriteLine("=== NanoRPC Complete Example ===\n");

// ========================================
// 1. START THE SERVER
// ========================================

await using var server = new NanoServer("nano-server", Port);

// Register RPC actors
server.RegisterActor("math", new MathActor());
server.RegisterActor("greeter", new GreeterActor());

// Register streaming handlers
server.Streams.RegisterStreamHandler<CountRequest, int>(
    "counter", "count",
    async (request, stream, ct) =>
    {
        int count = request?.Count ?? 5;
        Console.WriteLine($"  [Stream] Starting count to {count}");

        for (int i = 1; i <= count && !ct.IsCancellationRequested; i++)
        {
            await stream.SendAsync(i);
            await Task.Delay(200, ct);
        }

        Console.WriteLine("  [Stream] Count completed");
    });

server.Streams.RegisterStreamHandler<TickRequest, TickData>(
    "ticker", "subscribe",
    async (request, stream, ct) =>
    {
        string symbol = request?.Symbol ?? "BTC";
        Console.WriteLine($"  [Stream] Starting ticker for {symbol}");
        var random = new Random();

        while (!ct.IsCancellationRequested)
        {
            var price = 50000 + random.NextDouble() * 1000;
            await stream.SendAsync(new TickData(symbol, price, DateTime.UtcNow));
            await Task.Delay(500, ct);
        }

        Console.WriteLine($"  [Stream] Ticker stopped for {symbol}");
    });

server.Start();
await Task.Delay(100);

// ========================================
// 2. BASIC RPC EXAMPLE
// ========================================
Console.WriteLine("\n--- Part 1: Basic RPC ---\n");

await using var client1 = new NanoNode("client-1", "127.0.0.1", Port);
var sum = await client1.CallAsync<AddRequest, AddResponse>("math", "add", new(10, 20));
Console.WriteLine($"RPC Result: 10 + 20 = {sum?.Sum}\n");

// ========================================
// 3. PUB/SUB EXAMPLE
// ========================================
Console.WriteLine("--- Part 2: Pub/Sub ---\n");

await using var client2 = new NanoNode("client-2", "127.0.0.1", Port);
await using var client3 = new NanoNode("client-3", "127.0.0.1", Port);

// Subscribe both clients to "chat" topic
var messagesReceived = 0;

await using var sub1 = await client1.PubSub.SubscribeAsync<ChatMessage>("chat", (topic, msg) =>
{
    Console.WriteLine($"  [Client-1] Received on '{topic}': {msg?.User}: {msg?.Text}");
    Interlocked.Increment(ref messagesReceived);
});

await using var sub2 = await client2.PubSub.SubscribeAsync<ChatMessage>("chat", (topic, msg) =>
{
    Console.WriteLine($"  [Client-2] Received on '{topic}': {msg?.User}: {msg?.Text}");
    Interlocked.Increment(ref messagesReceived);
});

await Task.Delay(200); // Let subscriptions register

// Client 3 publishes a message (doesn't need to subscribe)
Console.WriteLine("Publishing messages to 'chat' topic...\n");
await client3.PubSub.PublishAsync("chat", new ChatMessage("Alice", "Hello everyone!"));
await Task.Delay(100);

await client3.PubSub.PublishAsync("chat", new ChatMessage("Bob", "Hey Alice!"));
await Task.Delay(100);

// Server can also broadcast
await server.PubSub.PublishAsync("chat", new ChatMessage("Server", "Welcome to the chat!"));
await Task.Delay(200);

Console.WriteLine($"\nTotal messages received across clients: {messagesReceived}\n");

// ========================================
// 4. STREAMING EXAMPLE
// ========================================
Console.WriteLine("--- Part 3: Streaming ---\n");

// Example 1: Simple count stream
Console.WriteLine("Starting count stream (1 to 5):");
await using var countStream = await client1.Streams.StartReadStreamAsync<CountRequest, int>(
    "counter", "count", new CountRequest(5));

await foreach (var number in countStream.ReadAllAsync())
{
    Console.WriteLine($"  Received: {number}");
}
Console.WriteLine("Count stream completed.\n");

// Example 2: Ticker stream (with cancellation)
Console.WriteLine("Starting ticker stream (will cancel after 3 ticks):");
await using var tickerStream = await client2.Streams.StartReadStreamAsync<TickRequest, TickData>(
    "ticker", "subscribe", new TickRequest("ETH"));

int tickCount = 0;
await foreach (var tick in tickerStream.ReadAllAsync())
{
    Console.WriteLine($"  {tick?.Symbol}: ${tick?.Price:F2} at {tick?.Timestamp:HH:mm:ss}");
    tickCount++;

    if (tickCount >= 3)
    {
        Console.WriteLine("  Cancelling stream...");
        await tickerStream.CancelAsync();
        break;
    }
}
Console.WriteLine("Ticker stream stopped.\n");

// ========================================
// 5. MIDDLEWARE + RPC
// ========================================
Console.WriteLine("--- Part 4: Middleware Pipeline ---\n");

await using var advancedClient = new NanoClientBuilder()
    .WithHost("127.0.0.1")
    .WithPort(Port)
    .WithName("advanced-client")
    .UseLogging("RPC")
    .UseMetrics(out var metrics)
    .Build();

await advancedClient.ConnectAsync();

await advancedClient.CallAsync<AddRequest, AddResponse>("math", "add", new(100, 200));
await advancedClient.CallAsync<GreetRequest, GreetResponse>("greeter", "hello", new("World"));

Console.WriteLine($"\n📊 Metrics: {metrics}\n");

// ========================================
// DONE
// ========================================
Console.WriteLine("=== All examples completed! ===");
Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();


// ========================================
// DTOs & INTERFACES
// ========================================

// RPC DTOs
public record AddRequest(int A, int B);
public record AddResponse(int Sum);
public record GreetRequest(string Name);
public record GreetResponse(string Message);

// Pub/Sub DTOs
public record ChatMessage(string User, string Text);

// Streaming DTOs
public record CountRequest(int Count);
public record TickRequest(string Symbol);
public record TickData(string Symbol, double Price, DateTime Timestamp);

// Actor Interfaces
public interface IMathActor : INanoActor
{
    [NanoAction("add")]
    AddResponse Add(AddRequest req);
}

public interface IGreeterActor : INanoActor
{
    [NanoAction("hello")]
    GreetResponse Hello(GreetRequest req);
}

// ========================================
// ACTOR IMPLEMENTATIONS
// ========================================

public class MathActor : IMathActor
{
    [NanoAction("add")]
    public AddResponse Add(AddRequest req)
    {
        Console.WriteLine($"  [MathActor] Computing {req.A} + {req.B}");
        return new AddResponse(req.A + req.B);
    }
}

public class GreeterActor : IGreeterActor
{
    [NanoAction("hello")]
    public GreetResponse Hello(GreetRequest req)
    {
        Console.WriteLine($"  [GreeterActor] Greeting {req.Name}");
        return new GreetResponse($"Hello, {req.Name}!");
    }
}