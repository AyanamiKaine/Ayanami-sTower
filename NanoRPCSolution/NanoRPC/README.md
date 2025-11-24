# NanoRPC

A lightweight, high-performance RPC (Remote Procedure Call) framework for .NET 10 with support for Pub/Sub messaging and streaming.

## Features

- **🚀 High Performance** - Binary protocol with minimal overhead, using `System.IO.Pipelines` for efficient I/O
- **📡 RPC Calls** - Synchronous request/response pattern with automatic serialization
- **🔥 Fire-and-Forget** - Cast messages that don't wait for a response
- **📢 Pub/Sub** - Topic-based publish/subscribe messaging
- **🌊 Streaming** - Server-to-client streaming with backpressure support
- **🔌 Middleware Pipeline** - Extensible middleware for logging, retry, metrics, and circuit breaker patterns
- **🔄 Auto-Reconnect** - Automatic reconnection with configurable retry policies
- **🎯 Actor Model** - Register actors with attributed methods for clean API design
- **🛡️ Type Safety** - Strongly-typed proxies and full generic support

## Installation

Add a reference to the NanoRPC project or package:

```xml
<ProjectReference Include="..\NanoRPC\NanoRPC.csproj" />
```

## Quick Start

### Define an Actor

```csharp
using NanoRpc.Protocol;

public class CalculatorActor : INanoActor
{
    [NanoAction]
    public int Add(AddRequest request) => request.A + request.B;

    [NanoAction("multiply")]  // Custom action name
    public int Multiply(AddRequest request) => request.A * request.B;

    [NanoAction]
    public async Task<int> AddAsync(AddRequest request)
    {
        await Task.Delay(10);  // Simulate async work
        return request.A + request.B;
    }
}

public record AddRequest(int A, int B);
```

### Create a Server

```csharp
using NanoRpc.Core;

// Create and configure the server
var server = new NanoServer("MyServer", port: 8023);

// Register actors
server.RegisterActor("calculator", new CalculatorActor());

// Start listening
server.Start();

Console.WriteLine("Server running. Press Enter to stop...");
Console.ReadLine();

await server.DisposeAsync();
```

### Create a Client

```csharp
using NanoRpc.Core;
using NanoRpc.Extensions;

// Connect to the server
await using var client = new NanoNode("MyClient", "127.0.0.1", 8023);

// Make RPC calls
var result = await client.CallAsync<AddRequest, int>(
    "calculator",
    "Add",
    new AddRequest(5, 3));

Console.WriteLine($"5 + 3 = {result}");  // Output: 5 + 3 = 8

// Fire-and-forget
client.Cast("calculator", "LogSomething", new { Message = "Hello!" });
```

## Advanced Usage

### High-Level Client with Middleware

```csharp
using NanoRpc.Core;

var client = new NanoClientBuilder()
    .WithHost("127.0.0.1")
    .WithPort(8023)
    .WithName("my-client")
    .WithTimeout(5000)
    .WithAutoReconnect(enabled: true, delayMs: 1000, maxAttempts: 10)
    .UseLogging("RPC", logRequests: true, logResponses: true)
    .UseRetry(maxRetries: 3, baseDelayMs: 100)
    .UseCircuitBreaker(failureThreshold: 5, resetTimeoutSeconds: 30)
    .Build();

await client.ConnectAsync();

var result = await client.CallAsync<AddRequest, int>("calculator", "Add", new AddRequest(1, 2));
```

### Pub/Sub Messaging

```csharp
// Subscriber
await using var subscriber = new NanoNode("Subscriber", "127.0.0.1", 8023);

var subscription = await subscriber.PubSub.SubscribeAsync<ChatMessage>("chat-room", (topic, message) =>
{
    Console.WriteLine($"[{topic}] {message.User}: {message.Text}");
});

// Publisher
await using var publisher = new NanoNode("Publisher", "127.0.0.1", 8023);

await publisher.PubSub.PublishAsync("chat-room", new ChatMessage("Alice", "Hello everyone!"));

// Unsubscribe when done
await subscription.DisposeAsync();
```

**Server-side broadcasting:**

```csharp
// Broadcast from server to all subscribers
await server.PubSub.PublishAsync("announcements", new { Message = "Server maintenance in 5 minutes" });
```

### Streaming

**Server-side stream handler:**

```csharp
server.Streams.RegisterStreamHandler<CountRequest, int>("counter", "count",
    async (request, stream, cancellationToken) =>
    {
        for (int i = 1; i <= request.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            await stream.SendAsync(i);
            await Task.Delay(100, cancellationToken);
        }
    });
```

**Client-side stream consumption:**

```csharp
await using var stream = await client.Streams.StartReadStreamAsync<CountRequest, int>(
    "counter", "count", new CountRequest { Count = 10 });

await foreach (var number in stream.ReadAllAsync())
{
    Console.WriteLine($"Received: {number}");
}
```

### Custom Middleware

```csharp
using NanoRpc.Middleware;

public class CustomMiddleware : INanoMiddleware
{
    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        // Before the call
        Console.WriteLine($"Calling {context.Target}.{context.Method}");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"Completed in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}

// Usage
client.Use(new CustomMiddleware());
```

### Built-in Middleware

| Middleware                 | Description                                              |
| -------------------------- | -------------------------------------------------------- |
| `LoggingMiddleware`        | Logs RPC calls with timing information                   |
| `RetryMiddleware`          | Retries failed calls with exponential backoff            |
| `MetricsMiddleware`        | Collects call counts, latency, and error metrics         |
| `CircuitBreakerMiddleware` | Prevents cascading failures with circuit breaker pattern |
| `TimeoutMiddleware`        | Enforces call timeouts                                   |

## Erlang/OTP-Style Patterns

NanoRPC supports building distributed systems using patterns inspired by Erlang/OTP and Elixir. See the `NanoExample.Erlang` project for a complete example.

### Supervision Trees

Implement hierarchical fault tolerance with restart strategies:

```csharp
// Supervisor with restart strategies: one_for_one, one_for_all, rest_for_one
public class MySupervisor : INanoActor
{
    private readonly ConcurrentDictionary<string, ChildSpec> _children = new();

    [NanoAction("start_child")]
    public SupervisorResponse StartChild(StartChildRequest req)
    {
        _children[req.Name] = new ChildSpec(req.Name, req.Strategy, req.MaxRestarts);
        return new SupervisorResponse(true, $"Child '{req.Name}' started");
    }

    [NanoAction("restart_child")]
    public SupervisorResponse RestartChild(string childName)
    {
        // Implement restart logic with backoff
        return new SupervisorResponse(true, "Restarted");
    }
}
```

### GenServer Pattern

Implement stateful actors with synchronous calls and asynchronous casts:

```csharp
public class KeyValueStore : INanoActor
{
    private readonly ConcurrentDictionary<string, string> _state = new();

    // Synchronous call - client waits for reply (like GenServer.call)
    [NanoAction("handle_call")]
    public KVResponse HandleCall(KVRequest req) => req.Op switch
    {
        "get" => new KVResponse(_state.TryGetValue(req.Key, out var val), val),
        "put" => new KVResponse(_state.TryAdd(req.Key, req.Value), null),
        _ => new KVResponse(false, "Unknown operation")
    };

    // Asynchronous cast - fire and forget (like GenServer.cast)
    [NanoAction("handle_cast")]
    public void HandleCast(KVRequest req)
    {
        if (req.Op == "put") _state[req.Key] = req.Value;
    }

    // Get internal state (like :sys.get_state)
    [NanoAction("get_state")]
    public int GetState() => _state.Count;
}
```

### Process Registry

Named process discovery with location transparency:

```csharp
public class ProcessRegistry : INanoActor
{
    private readonly ConcurrentDictionary<string, RegistryEntry> _registry = new();

    [NanoAction("register")]
    public bool Register(RegistryRequest req) =>
        _registry.TryAdd(req.Name, new RegistryEntry(req.Name, req.Pid, req.Node));

    [NanoAction("whereis")]
    public RegistryEntry? Whereis(string name) =>
        _registry.TryGetValue(name, out var entry) ? entry : null;

    // Via-tuple pattern for routing
    [NanoAction("via_call")]
    public ViaResponse ViaCall(ViaRequest req) =>
        _registry.TryGetValue(req.Name, out var entry)
            ? new ViaResponse(true, $"Routed to {entry.Pid}")
            : new ViaResponse(false, "Not found");
}
```

### Let It Crash Philosophy

Design workers that can fail and be restarted by supervisors:

```csharp
public class CrashyWorker : INanoActor
{
    [NanoAction("do_work")]
    public WorkerResponse DoWork(WorkerRequest req)
    {
        // Let exceptions propagate - supervisor handles restart
        if (req.Value == 0)
            throw new DivideByZeroException("Cannot divide by zero!");

        return new WorkerResponse($"Processed {100 / req.Value}");
    }
}

// Client code with fault tolerance
try
{
    await client.CallAsync<WorkerRequest, WorkerResponse>("worker", "do_work", request);
}
catch (Exception)
{
    // Supervisor restarts worker, retry the call
    await Task.Delay(100);
    await client.CallAsync<WorkerRequest, WorkerResponse>("worker", "do_work", safeRequest);
}
```

### Distributed PubSub (Phoenix.PubSub style)

Broadcast events across nodes:

```csharp
// Subscribe to events
await using var subscription = await node.PubSub.SubscribeAsync<UserEvent>("user:events",
    (topic, evt) => Console.WriteLine($"User {evt.UserId}: {evt.Action}"));

// Broadcast to all subscribers
await publisher.PubSub.PublishAsync("user:events",
    new UserEvent { UserId = 1001, Action = "logged_in" });
```

### Streaming Pipelines (GenStage/Flow style)

Implement backpressure-aware data pipelines:

```csharp
// Server: Register a producer
server.Streams.RegisterStreamHandler<ProducerRequest, DataItem>("pipeline", "produce",
    async (request, stream, ct) =>
    {
        for (int i = 0; i < request.Count && !ct.IsCancellationRequested; i++)
        {
            await stream.SendAsync(new DataItem(i, $"Item-{i}"));
            await Task.Delay(100, ct); // Simulate backpressure
        }
    });

// Client: Consume with backpressure
await using var stream = await client.Streams.StartReadStreamAsync<ProducerRequest, DataItem>(
    "pipeline", "produce", new ProducerRequest(100));

await foreach (var item in stream.ReadAllAsync())
{
    await ProcessAsync(item); // Consumer controls pace
}
```

## Protocol

NanoRPC uses a custom binary protocol with a 17-byte header:

| Field     | Size    | Description                                   |
| --------- | ------- | --------------------------------------------- |
| Type      | 1 byte  | Message type (Call, Cast, Reply, Error, etc.) |
| Id        | 4 bytes | Message ID for request/response correlation   |
| TargetLen | 4 bytes | Length of target actor name                   |
| MethodLen | 4 bytes | Length of method name                         |
| BodyLen   | 4 bytes | Length of JSON payload                        |

**Message Types:**

- `Call` (0x01) - RPC call expecting a response
- `Cast` (0x02) - Fire-and-forget message
- `Reply` (0x03) - Successful response
- `Error` (0x04) - Error response
- `Handshake` (0x05) - Connection handshake
- `Subscribe` (0x10) - Subscribe to topic
- `Unsubscribe` (0x11) - Unsubscribe from topic
- `Publish` (0x12) - Publish to topic
- `StreamStart` (0x20) - Start a stream
- `StreamData` (0x21) - Stream data chunk
- `StreamEnd` (0x22) - End of stream
- `StreamCancel` (0x23) - Cancel stream

## Configuration

### Server Options

```csharp
var server = new NanoServer("MyServer", port: 8023);

// Events
server.ClientConnected += clientId => Console.WriteLine($"Client connected: {clientId}");
server.ClientDisconnected += clientId => Console.WriteLine($"Client disconnected: {clientId}");
```

### Client Options

```csharp
var options = new NanoClientOptions
{
    Host = "127.0.0.1",
    Port = 8023,
    Name = "my-client",
    DefaultTimeoutMs = 5000,
    AutoReconnect = true,
    ReconnectDelayMs = 1000,
    MaxReconnectAttempts = 10,
    HealthCheckIntervalMs = 30000
};

var client = new NanoClient(options);

// Events
client.Connected += () => Console.WriteLine("Connected!");
client.Disconnected += () => Console.WriteLine("Disconnected!");
client.Reconnecting += attempt => Console.WriteLine($"Reconnecting (attempt {attempt})...");
client.ConnectionFailed += ex => Console.WriteLine($"Connection failed: {ex.Message}");
```

## Limits

Default protocol limits (configurable in `NanoLimits`):

| Limit             | Value     |
| ----------------- | --------- |
| Max Target Length | 256 bytes |
| Max Method Length | 256 bytes |
| Max Body Length   | 16 MB     |

## Error Handling

```csharp
try
{
    var result = await client.CallAsync<Request, Response>("actor", "method", request);
}
catch (TimeoutException ex)
{
    // Call timed out
}
catch (NanoRpcException ex)
{
    // RPC-specific error (target/method info available)
    Console.WriteLine($"RPC Error on {ex.Target}.{ex.Method}: {ex.Message}");
}
catch (Exception ex)
{
    // Remote method threw an exception
    Console.WriteLine($"Remote error: {ex.Message}");
}
```

## Project Structure

```
NanoRPC/
├── NanoProtocol.cs      # Protocol definitions, header parsing, wire formatting
├── NanoNode.cs          # Low-level TCP client with receive loop
├── NanoServer.cs        # TCP server with client connection management
├── NanoClient.cs        # High-level client with middleware support
├── NanoProxy.cs         # Typed proxy generation
├── NanoMiddleware.cs    # Middleware interfaces and implementations
├── NanoPubSub.cs        # Pub/Sub client and broker
├── NanoStream.cs        # Streaming support
└── ClientExtensions.cs  # Extension methods for NanoNode
```

## Requirements

- .NET 10.0 or later
- C# 14 (uses extension blocks feature)

## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
