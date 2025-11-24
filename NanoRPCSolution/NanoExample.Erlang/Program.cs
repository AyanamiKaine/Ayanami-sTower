// ============================================================================
// NANORPC ERLANG/OTP-STYLE ARCHITECTURE EXAMPLE
// ============================================================================
// This example demonstrates how to implement Erlang/Elixir-style patterns
// using NanoRPC, including:
//
// 1. Supervision Trees - Hierarchical actor supervision with restart strategies
// 2. GenServer Pattern - Stateful actors with handle_call/handle_cast
// 3. Process Registry - Named process discovery and location transparency
// 4. Let It Crash - Fault tolerance through isolation and supervision
// 5. Process Monitoring - Detecting and reacting to actor failures
// 6. Application Pattern - Structured startup and shutdown
// 7. Distributed Nodes - Multi-node communication and clustering
// ============================================================================

using NanoRpc.Core;
using NanoRpc.Client;
using NanoRpc.Extensions;
using NanoRpc.Middleware;
using NanoRpc.Protocol;
using System.Collections.Concurrent;

const int ClusterPort = 9100; // Different port to avoid conflicts

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     NanoRPC - Erlang/OTP Style Architecture Example              ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝\n");

// ============================================================================
// PART 1: START THE CLUSTER NODE (Similar to an Erlang Node)
// ============================================================================

Console.WriteLine("▶ Starting Cluster Node...\n");

await using var clusterNode = new NanoServer("erlang-cluster@localhost", ClusterPort);

// Register the supervisor infrastructure
var rootSupervisor = new RootSupervisor("root");
var registry = new ProcessRegistry();
var applicationController = new ApplicationController(registry);

clusterNode.RegisterActor("supervisor", rootSupervisor);
clusterNode.RegisterActor("registry", registry);
clusterNode.RegisterActor("application", applicationController);

clusterNode.Start();
await Task.Delay(200);

// ============================================================================
// PART 2: BUILD THE SUPERVISION TREE
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 1: Building Supervision Tree (like Erlang/OTP Supervisors)\n");

// Connect a management client
await using var admin = new NanoNode("admin-console", "127.0.0.1", ClusterPort);

// Start children under the root supervisor
// Strategy: one_for_one - if a child dies, only restart that child
Console.WriteLine("Starting supervision tree with 'one_for_one' strategy...\n");

// Start a UserSessionSupervisor (manages user sessions)
await admin.CallAsync<StartChildRequest, SupervisorResponse>(
    "supervisor", "start_child",
    new StartChildRequest("user_sessions", "one_for_one", 5, 60));

// Start a WorkerPoolSupervisor (manages worker pool)
await admin.CallAsync<StartChildRequest, SupervisorResponse>(
    "supervisor", "start_child",
    new StartChildRequest("worker_pool", "one_for_all", 3, 30));

// Start a CacheSupervisor (manages cache actors)
await admin.CallAsync<StartChildRequest, SupervisorResponse>(
    "supervisor", "start_child",
    new StartChildRequest("cache", "rest_for_one", 5, 60));

// Query supervision tree
var treeInfo = await admin.CallAsync<object, SupervisionTreeInfo>("supervisor", "which_children", new { });
Console.WriteLine("📊 Supervision Tree:");
PrintTree(treeInfo, 0);

// ============================================================================
// PART 3: GENSERVER PATTERN - STATEFUL ACTORS
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 2: GenServer Pattern (Stateful Actors)\n");

// Register a GenServer-style KeyValue store
var kvStore = new KeyValueGenServer("kv_store");
clusterNode.RegisterActor("kv", kvStore);

await using var client1 = new NanoNode("client-1", "127.0.0.1", ClusterPort);

// GenServer call (synchronous, waits for reply)
Console.WriteLine("Making GenServer calls (synchronous with reply):");

await client1.CallAsync<KVRequest, KVResponse>("kv", "handle_call",
    new KVRequest("put", "user:1001", "{\"name\": \"Alice\", \"role\": \"admin\"}"));
Console.WriteLine("  → PUT user:1001");

await client1.CallAsync<KVRequest, KVResponse>("kv", "handle_call",
    new KVRequest("put", "user:1002", "{\"name\": \"Bob\", \"role\": \"user\"}"));
Console.WriteLine("  → PUT user:1002");

var getResult = await client1.CallAsync<KVRequest, KVResponse>("kv", "handle_call",
    new KVRequest("get", "user:1001", null));
Console.WriteLine($"  ← GET user:1001 = {getResult?.Value}");

// GenServer cast (asynchronous, no reply - fire and forget)
Console.WriteLine("\nMaking GenServer casts (asynchronous, fire-and-forget):");
client1.Cast("kv", "handle_cast", new KVRequest("put", "metrics:requests", "42"));
Console.WriteLine("  → CAST metrics:requests (no reply expected)");

await Task.Delay(100);

var stateInfo = await client1.CallAsync<object, GenServerState>("kv", "get_state", new { });
Console.WriteLine($"\n📊 GenServer State: {stateInfo?.EntryCount} entries, {stateInfo?.CallCount} calls, {stateInfo?.CastCount} casts");

// ============================================================================
// PART 4: PROCESS REGISTRY (Named Process Discovery)
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 3: Process Registry (Location Transparency)\n");

// Register named processes (like Erlang's :global or Registry)
await admin.CallAsync<RegistryRequest, RegistryResponse>(
    "registry", "register",
    new RegistryRequest("PaymentProcessor", "payment@node1", "node1"));

await admin.CallAsync<RegistryRequest, RegistryResponse>(
    "registry", "register",
    new RegistryRequest("NotificationService", "notify@node2", "node2"));

await admin.CallAsync<RegistryRequest, RegistryResponse>(
    "registry", "register",
    new RegistryRequest("UserCache", "cache@node1", "node1"));

// Lookup processes by name
var payment = await admin.CallAsync<RegistryLookup, RegistryEntry>(
    "registry", "whereis",
    new RegistryLookup("PaymentProcessor"));

Console.WriteLine($"  whereis(PaymentProcessor) → {payment?.Pid} on {payment?.Node}");

// List all registered names
var allNames = await admin.CallAsync<object, RegistryList>("registry", "registered", new { });
Console.WriteLine($"\n📋 Registered Processes ({allNames?.Names?.Length ?? 0}):");
foreach (var name in allNames?.Names ?? [])
{
    Console.WriteLine($"    • {name}");
}

// Via-tuple pattern - route by name
Console.WriteLine("\n  Using {:via, Registry, name} pattern for process routing...");
await admin.CallAsync<ViaRequest, ViaResponse>(
    "registry", "via_call",
    new ViaRequest("UserCache", "get", "session:abc123"));

// ============================================================================
// PART 5: LET IT CRASH - FAULT TOLERANCE
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 4: Let It Crash (Fault Tolerance)\n");

// Register a worker that will crash
var crashyWorker = new CrashyWorker("crashy-1");
clusterNode.RegisterActor("worker", crashyWorker);

// Subscribe to supervisor events (like process monitors)
await using var monitor = new NanoNode("monitor", "127.0.0.1", ClusterPort);
var crashEvents = new List<string>();

await using var monitorSub = await monitor.PubSub.SubscribeAsync<SupervisorEvent>("supervisor:events", (topic, evt) =>
{
    if (evt != null)
    {
        var eventStr = $"[{evt.Timestamp:HH:mm:ss}] {evt.EventType}: {evt.ChildId} - {evt.Reason}";
        crashEvents.Add(eventStr);
        Console.WriteLine($"  🔔 {eventStr}");
    }
});

Console.WriteLine("Simulating actor failures with supervisor restarts...\n");

// First call succeeds
try
{
    var result = await client1.CallAsync<WorkerRequest, WorkerResponse>(
        "worker", "do_work",
        new WorkerRequest("safe_operation", 100));
    Console.WriteLine($"  ✓ Safe operation completed: {result?.Result}");
}
catch (Exception ex)
{
    Console.WriteLine($"  ✗ Unexpected error: {ex.Message}");
}

// This call will trigger a crash (simulating division by zero)
Console.WriteLine("\n  Triggering intentional crash (division by zero)...");
try
{
    await client1.CallAsync<WorkerRequest, WorkerResponse>(
        "worker", "do_work",
        new WorkerRequest("divide", 0)); // Will crash!
}
catch (Exception ex)
{
    Console.WriteLine($"  ✗ Worker crashed: {ex.Message}");
    Console.WriteLine("  ↻ Supervisor will restart the worker...");
}

// Supervisor restarts the worker - next call should work
crashyWorker.Reset(); // Simulate supervisor restart
await Task.Delay(100);

try
{
    var result = await client1.CallAsync<WorkerRequest, WorkerResponse>(
        "worker", "do_work",
        new WorkerRequest("safe_operation", 200));
    Console.WriteLine($"\n  ✓ Worker recovered! Result: {result?.Result}");
}
catch (Exception ex)
{
    Console.WriteLine($"  ✗ Worker still dead: {ex.Message}");
}

// Show crash statistics
var crashStats = crashyWorker.GetStats();
Console.WriteLine($"\n📊 Worker Stats: {crashStats.TotalCalls} calls, {crashStats.Crashes} crashes, {crashStats.Restarts} restarts");

// ============================================================================
// PART 6: PROCESS MONITORING AND LINKING
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 5: Process Monitoring (Links & Monitors)\n");

// Start monitoring specific processes (like erlang:monitor/2)
await admin.CallAsync<MonitorRequest, MonitorResponse>(
    "supervisor", "monitor",
    new MonitorRequest("crashy-1", "admin-console"));

Console.WriteLine("Setting up process monitors...");
Console.WriteLine("  → monitor(crashy-1, admin-console)");

// Demonstrate linked processes (crash propagation)
Console.WriteLine("\nLinked processes share fate (like Erlang process links):");
Console.WriteLine("  Process A ←──link──→ Process B");
Console.WriteLine("  If A crashes, B receives EXIT signal");

// Subscribe to link events
await using var linkSub = await monitor.PubSub.SubscribeAsync<LinkEvent>("process:links", (topic, evt) =>
{
    if (evt != null)
    {
        Console.WriteLine($"  🔗 Link event: {evt.From} → {evt.To}: {evt.Reason}");
    }
});

await Task.Delay(200);

// ============================================================================
// PART 7: DISTRIBUTED COMMUNICATION (Multi-Node)
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 6: Distributed Communication\n");

// Spawn remote actors on different "nodes"
Console.WriteLine("Simulating distributed node communication...\n");

// Register actors that simulate being on different nodes
var node1Actors = new NodeSimulator("node1@192.168.1.10");
var node2Actors = new NodeSimulator("node2@192.168.1.11");

clusterNode.RegisterActor("node1", node1Actors);
clusterNode.RegisterActor("node2", node2Actors);

// RPC to remote node (like :rpc.call in Erlang)
Console.WriteLine("Making remote procedure calls (rpc:call):");

var remoteResult = await client1.CallAsync<RemoteCallRequest, RemoteCallResponse>(
    "node1", "rpc_call",
    new RemoteCallRequest("String", "upcase", ["hello distributed world"]));

Console.WriteLine($"  rpc:call(node1, String, :upcase, [\"hello\"]) → \"{remoteResult?.Result}\"");

// Spawn on remote node (like Node.spawn in Elixir)
var spawnResult = await client1.CallAsync<RemoteSpawnRequest, RemoteSpawnResponse>(
    "node2", "spawn",
    new RemoteSpawnRequest("worker", "fibonacci", new { n = 10 }));

Console.WriteLine($"  Node.spawn(node2, fn -> fib(10) end) → pid={spawnResult?.Pid}");

// List connected nodes (like Node.list in Elixir)
var connectedNodes = await admin.CallAsync<object, NodeList>("registry", "nodes", new { });
Console.WriteLine($"\n🌐 Connected Nodes: [{string.Join(", ", connectedNodes?.Nodes ?? [])}]");

// ============================================================================
// PART 8: APPLICATION PATTERN (OTP Application)
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 7: OTP Application Pattern\n");

// Start an OTP-style application
Console.WriteLine("Starting application: :my_app");
Console.WriteLine("  Application.start(:my_app)\n");

var appStart = await admin.CallAsync<AppRequest, AppResponse>(
    "application", "start",
    new AppRequest("my_app", "permanent"));

Console.WriteLine($"  Application ':my_app' started with strategy: permanent");
Console.WriteLine("  Supervision tree initialized:");
Console.WriteLine("    └── MyApp.Supervisor");
Console.WriteLine("        ├── MyApp.Cache (worker)");
Console.WriteLine("        ├── MyApp.TaskQueue (worker)");
Console.WriteLine("        └── MyApp.WorkerSupervisor (supervisor)");
Console.WriteLine("            ├── MyApp.Worker.1");
Console.WriteLine("            ├── MyApp.Worker.2");
Console.WriteLine("            └── MyApp.Worker.3");

// Get application info
var appInfo = await admin.CallAsync<AppInfoRequest, AppInfo>(
    "application", "info",
    new AppInfoRequest("my_app"));

Console.WriteLine($"\n📋 Application Info:");
Console.WriteLine($"    Started: {appInfo?.StartedAt:HH:mm:ss}");
Console.WriteLine($"    Status: {appInfo?.Status}");
Console.WriteLine($"    Children: {appInfo?.ChildCount}");

// ============================================================================
// PART 9: HOT CODE RELOADING CONCEPT
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 8: Hot Code Reloading Concept\n");

// Demonstrate replacing an actor at runtime (like hot code loading)
Console.WriteLine("Demonstrating actor replacement (hot code loading)...\n");

var v1Actor = new VersionedActor(1);
clusterNode.RegisterActor("versioned", v1Actor);

var v1Result = await client1.CallAsync<VersionRequest, VersionResponse>(
    "versioned", "get_version", new VersionRequest());
Console.WriteLine($"  Current version: v{v1Result?.Version}");

// "Hot reload" - replace with new version
Console.WriteLine("  Performing hot code reload...");
var v2Actor = new VersionedActor(2);
clusterNode.RegisterActor("versioned", v2Actor);

var v2Result = await client1.CallAsync<VersionRequest, VersionResponse>(
    "versioned", "get_version", new VersionRequest());
Console.WriteLine($"  New version: v{v2Result?.Version}");
Console.WriteLine("  ✓ No downtime during upgrade!");

// ============================================================================
// PART 10: PUBSUB FOR DISTRIBUTED EVENTS (like Phoenix.PubSub)
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 9: Distributed PubSub (Phoenix.PubSub style)\n");

await using var subscriber1 = new NanoNode("subscriber-1", "127.0.0.1", ClusterPort);
await using var subscriber2 = new NanoNode("subscriber-2", "127.0.0.1", ClusterPort);
await using var publisher = new NanoNode("publisher", "127.0.0.1", ClusterPort);

var receivedEvents = new ConcurrentBag<string>();

// Subscribe to a topic (like Phoenix.PubSub.subscribe)
await using var sub1 = await subscriber1.PubSub.SubscribeAsync<DistributedEvent>("user:events", (topic, evt) =>
{
    if (evt != null)
    {
        receivedEvents.Add($"[Sub1] {evt.Type}: {evt.Payload}");
        Console.WriteLine($"  📨 [Subscriber-1] Received: {evt.Type}");
    }
});

await using var sub2 = await subscriber2.PubSub.SubscribeAsync<DistributedEvent>("user:events", (topic, evt) =>
{
    if (evt != null)
    {
        receivedEvents.Add($"[Sub2] {evt.Type}: {evt.Payload}");
        Console.WriteLine($"  📨 [Subscriber-2] Received: {evt.Type}");
    }
});

await Task.Delay(200);

// Broadcast to all subscribers (like Phoenix.PubSub.broadcast)
Console.WriteLine("Broadcasting events to all subscribers...\n");

await publisher.PubSub.PublishAsync("user:events",
    new DistributedEvent("user:created", "{\"id\": 1001, \"name\": \"Charlie\"}"));

await publisher.PubSub.PublishAsync("user:events",
    new DistributedEvent("user:updated", "{\"id\": 1001, \"email\": \"charlie@example.com\"}"));

await Task.Delay(300);

Console.WriteLine($"\n📊 Total events received: {receivedEvents.Count}");

// ============================================================================
// PART 11: STREAMING (like GenStage/Flow)
// ============================================================================

Console.WriteLine("\n" + new string('─', 70));
Console.WriteLine("▶ PART 10: Streaming Pipeline (GenStage/Flow style)\n");

// Register a producer-consumer pipeline
clusterNode.Streams.RegisterStreamHandler<ProducerRequest, ProducerItem>(
    "pipeline", "produce",
    async (request, stream, ct) =>
    {
        var count = request?.Count ?? 10;
        Console.WriteLine($"  🏭 Producer started (demand: {count})");

        for (int i = 1; i <= count && !ct.IsCancellationRequested; i++)
        {
            // Simulate backpressure-aware production
            await stream.SendAsync(new ProducerItem(i, $"Item-{i}", DateTime.UtcNow));
            await Task.Delay(100, ct);
        }

        Console.WriteLine("  🏭 Producer completed");
    });

// Consume the stream with backpressure
Console.WriteLine("Starting producer-consumer pipeline...\n");

await using var pipelineStream = await client1.Streams.StartReadStreamAsync<ProducerRequest, ProducerItem>(
    "pipeline", "produce", new ProducerRequest(5));

var processed = 0;
await foreach (var item in pipelineStream.ReadAllAsync())
{
    if (item != null)
    {
        Console.WriteLine($"  🔄 Consumer processed: {item.Id} - {item.Data}");
        processed++;
    }
}

Console.WriteLine($"\n✓ Pipeline completed. Processed {processed} items.");

// ============================================================================
// DONE
// ============================================================================

Console.WriteLine("\n" + new string('═', 70));
Console.WriteLine("✅ All Erlang/OTP patterns demonstrated successfully!");
Console.WriteLine(new string('═', 70));

Console.WriteLine("\n📚 Summary of Erlang/OTP patterns demonstrated:");
Console.WriteLine("   1. Supervision Trees (one_for_one, one_for_all, rest_for_one)");
Console.WriteLine("   2. GenServer Pattern (handle_call, handle_cast, get_state)");
Console.WriteLine("   3. Process Registry (register, whereis, via tuples)");
Console.WriteLine("   4. Let It Crash (fault tolerance through supervision)");
Console.WriteLine("   5. Process Monitoring (monitor, link, EXIT signals)");
Console.WriteLine("   6. Distributed Communication (rpc:call, Node.spawn)");
Console.WriteLine("   7. OTP Applications (start, stop, supervision trees)");
Console.WriteLine("   8. Hot Code Reloading (zero-downtime upgrades)");
Console.WriteLine("   9. Distributed PubSub (Phoenix.PubSub patterns)");
Console.WriteLine("  10. Streaming Pipelines (GenStage/Flow backpressure)");

Console.WriteLine("\nPress Enter to exit...");
Console.ReadLine();


// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

static void PrintTree(SupervisionTreeInfo? info, int indent)
{
    if (info == null) return;

    var prefix = new string(' ', indent * 2);
    var marker = indent == 0 ? "└─" : "├─";

    Console.WriteLine($"{prefix}{marker} {info.Name} ({info.Strategy}) [{info.Status}]");

    if (info.Children != null)
    {
        foreach (var child in info.Children)
        {
            PrintTree(child, indent + 1);
        }
    }
}


// ============================================================================
// DTOs (Data Transfer Objects)
// ============================================================================

#region Supervisor DTOs

public record StartChildRequest(string Name, string Strategy, int MaxRestarts, int MaxSeconds);
public record SupervisorResponse(bool Success, string? Message);
public record SupervisionTreeInfo(string Name, string Strategy, string Status, SupervisionTreeInfo[]? Children);
public record MonitorRequest(string Target, string Monitor);
public record MonitorResponse(string Ref);
public record SupervisorEvent(string EventType, string ChildId, string Reason, DateTime Timestamp);
public record LinkEvent(string From, string To, string Reason);

#endregion

#region GenServer DTOs

public record KVRequest(string Op, string Key, string? Value);
public record KVResponse(bool Success, string? Value);
public record GenServerState(int EntryCount, int CallCount, int CastCount);

#endregion

#region Registry DTOs

public record RegistryRequest(string Name, string Pid, string Node);
public record RegistryResponse(bool Success);
public record RegistryLookup(string Name);
public record RegistryEntry(string Name, string Pid, string Node);
public record RegistryList(string[] Names);
public record ViaRequest(string Name, string Action, string Payload);
public record ViaResponse(bool Success, string? Result);
public record NodeList(string[] Nodes);

#endregion

#region Worker DTOs

public record WorkerRequest(string Operation, int Value);
public record WorkerResponse(string Result, long DurationMs);
public record WorkerStats(int TotalCalls, int Crashes, int Restarts);

#endregion

#region Remote Call DTOs

public record RemoteCallRequest(string Module, string Function, string[] Args);
public record RemoteCallResponse(string? Result, string? Error);
public record RemoteSpawnRequest(string Module, string Function, object Args);
public record RemoteSpawnResponse(string Pid);

#endregion

#region Application DTOs

public record AppRequest(string Name, string Strategy);
public record AppResponse(bool Started);
public record AppInfoRequest(string Name);
public record AppInfo(string Name, string Status, int ChildCount, DateTime StartedAt);

#endregion

#region Streaming DTOs

public record ProducerRequest(int Count);
public record ProducerItem(int Id, string Data, DateTime Timestamp);

#endregion

#region Distributed Event DTOs

public record DistributedEvent(string Type, string Payload);
public record VersionRequest();
public record VersionResponse(int Version);

#endregion


// ============================================================================
// ACTOR IMPLEMENTATIONS
// ============================================================================

#region RootSupervisor - Implements Erlang-style Supervision

/// <summary>
/// Implements Erlang-style supervision with restart strategies.
/// Supports: one_for_one, one_for_all, rest_for_one
/// </summary>
public class RootSupervisor : INanoActor
{
    private readonly string _name;
    private readonly ConcurrentDictionary<string, ChildSpec> _children = new();
    private readonly ConcurrentDictionary<string, int> _restartCounts = new();

    public RootSupervisor(string name) => _name = name;

    [NanoAction("start_child")]
    public SupervisorResponse StartChild(StartChildRequest req)
    {
        Console.WriteLine($"  [Supervisor] Starting child: {req.Name} with strategy: {req.Strategy}");

        _children[req.Name] = new ChildSpec(
            req.Name,
            req.Strategy,
            req.MaxRestarts,
            req.MaxSeconds,
            DateTime.UtcNow,
            "running"
        );

        _restartCounts[req.Name] = 0;

        return new SupervisorResponse(true, $"Child '{req.Name}' started");
    }

    [NanoAction("terminate_child")]
    public SupervisorResponse TerminateChild(string childName)
    {
        if (_children.TryRemove(childName, out _))
        {
            Console.WriteLine($"  [Supervisor] Terminated child: {childName}");
            return new SupervisorResponse(true, $"Child '{childName}' terminated");
        }

        return new SupervisorResponse(false, $"Child '{childName}' not found");
    }

    [NanoAction("restart_child")]
    public SupervisorResponse RestartChild(string childName)
    {
        if (_children.TryGetValue(childName, out var spec))
        {
            var count = _restartCounts.AddOrUpdate(childName, 1, (_, c) => c + 1);

            if (count > spec.MaxRestarts)
            {
                Console.WriteLine($"  [Supervisor] Max restarts exceeded for {childName}, giving up");
                return new SupervisorResponse(false, "Max restarts exceeded");
            }

            Console.WriteLine($"  [Supervisor] Restarting child: {childName} (attempt {count}/{spec.MaxRestarts})");
            return new SupervisorResponse(true, $"Restarted (attempt {count})");
        }

        return new SupervisorResponse(false, $"Child '{childName}' not found");
    }

    [NanoAction("which_children")]
    public SupervisionTreeInfo WhichChildren()
    {
        var children = _children.Values
            .Select(c => new SupervisionTreeInfo(c.Name, c.Strategy, c.Status, null))
            .ToArray();

        return new SupervisionTreeInfo(_name, "one_for_one", "running", children);
    }

    [NanoAction("monitor")]
    public MonitorResponse Monitor(MonitorRequest req)
    {
        var monitorRef = $"#Ref<{Guid.NewGuid():N}>"[..20];
        Console.WriteLine($"  [Supervisor] Monitor set: {req.Monitor} watching {req.Target}");
        return new MonitorResponse(monitorRef);
    }

    [NanoAction("count_children")]
    public int CountChildren() => _children.Count;

    private record ChildSpec(string Name, string Strategy, int MaxRestarts, int MaxSeconds, DateTime StartedAt, string Status);
}

#endregion

#region KeyValueGenServer - GenServer Pattern Implementation

/// <summary>
/// Implements the GenServer pattern from Erlang/OTP.
/// Provides handle_call (sync) and handle_cast (async) callbacks.
/// </summary>
public class KeyValueGenServer : INanoActor
{
    private readonly string _name;
    private readonly ConcurrentDictionary<string, string> _state = new();
    private int _callCount;
    private int _castCount;

    public KeyValueGenServer(string name) => _name = name;

    /// <summary>
    /// Synchronous call - client waits for reply (like GenServer.call)
    /// </summary>
    [NanoAction("handle_call")]
    public KVResponse HandleCall(KVRequest req)
    {
        Interlocked.Increment(ref _callCount);

        return req.Op.ToLower() switch
        {
            "get" => new KVResponse(
                _state.TryGetValue(req.Key, out var val),
                val),

            "put" => _state.TryAdd(req.Key, req.Value ?? "")
                ? new KVResponse(true, null)
                : new KVResponse(_state.TryUpdate(req.Key, req.Value ?? "", _state[req.Key]), null),

            "delete" => new KVResponse(_state.TryRemove(req.Key, out _), null),

            "keys" => new KVResponse(true, string.Join(",", _state.Keys)),

            _ => new KVResponse(false, "Unknown operation")
        };
    }

    /// <summary>
    /// Asynchronous cast - no reply expected (like GenServer.cast)
    /// </summary>
    [NanoAction("handle_cast")]
    public void HandleCast(KVRequest req)
    {
        Interlocked.Increment(ref _castCount);

        switch (req.Op.ToLower())
        {
            case "put":
                _state[req.Key] = req.Value ?? "";
                break;
            case "delete":
                _state.TryRemove(req.Key, out _);
                break;
        }
    }

    /// <summary>
    /// Get internal state (like :sys.get_state in Erlang)
    /// </summary>
    [NanoAction("get_state")]
    public GenServerState GetState() =>
        new(_state.Count, _callCount, _castCount);

    /// <summary>
    /// Terminate callback (like GenServer.terminate)
    /// </summary>
    [NanoAction("terminate")]
    public void Terminate(string reason)
    {
        Console.WriteLine($"  [{_name}] Terminating: {reason}");
        _state.Clear();
    }
}

#endregion

#region ProcessRegistry - Named Process Discovery

/// <summary>
/// Implements a process registry for named process discovery.
/// Similar to Erlang's :global or Elixir's Registry module.
/// </summary>
public class ProcessRegistry : INanoActor
{
    private readonly ConcurrentDictionary<string, RegistryEntry> _registry = new();
    private readonly HashSet<string> _nodes = ["node1@192.168.1.10", "node2@192.168.1.11", "node3@192.168.1.12"];

    [NanoAction("register")]
    public RegistryResponse Register(RegistryRequest req)
    {
        var entry = new RegistryEntry(req.Name, req.Pid, req.Node);

        if (_registry.TryAdd(req.Name, entry))
        {
            Console.WriteLine($"  [Registry] Registered: {req.Name} → {req.Pid}@{req.Node}");
            return new RegistryResponse(true);
        }

        return new RegistryResponse(false);
    }

    [NanoAction("unregister")]
    public RegistryResponse Unregister(string name)
    {
        return new RegistryResponse(_registry.TryRemove(name, out _));
    }

    [NanoAction("whereis")]
    public RegistryEntry? Whereis(RegistryLookup req)
    {
        _registry.TryGetValue(req.Name, out var entry);
        return entry;
    }

    [NanoAction("registered")]
    public RegistryList Registered()
    {
        return new RegistryList([.. _registry.Keys]);
    }

    [NanoAction("via_call")]
    public ViaResponse ViaCall(ViaRequest req)
    {
        // Implements {:via, Registry, name} pattern
        if (_registry.TryGetValue(req.Name, out var entry))
        {
            Console.WriteLine($"  [Registry] via_call to {req.Name} ({entry.Pid}): {req.Action}");
            return new ViaResponse(true, $"Routed to {entry.Pid}");
        }

        return new ViaResponse(false, "Process not found");
    }

    [NanoAction("nodes")]
    public NodeList Nodes()
    {
        return new NodeList([.. _nodes]);
    }
}

#endregion

#region CrashyWorker - Demonstrates Let It Crash

/// <summary>
/// A worker that can crash to demonstrate the "let it crash" philosophy.
/// </summary>
public class CrashyWorker : INanoActor
{
    private readonly string _name;
    private int _totalCalls;
    private int _crashes;
    private int _restarts;
    private bool _healthy = true;

    public CrashyWorker(string name) => _name = name;

    [NanoAction("do_work")]
    public WorkerResponse DoWork(WorkerRequest req)
    {
        Interlocked.Increment(ref _totalCalls);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            return req.Operation.ToLower() switch
            {
                "divide" when req.Value == 0 => throw new DivideByZeroException("Cannot divide by zero!"),
                "divide" => new WorkerResponse($"{100 / req.Value}", sw.ElapsedMilliseconds),
                "crash" => throw new InvalidOperationException("Intentional crash!"),
                "safe_operation" => new WorkerResponse($"Processed {req.Value}", sw.ElapsedMilliseconds),
                _ => new WorkerResponse($"Unknown: {req.Operation}", sw.ElapsedMilliseconds)
            };
        }
        catch
        {
            Interlocked.Increment(ref _crashes);
            _healthy = false;
            throw;
        }
    }

    [NanoAction("health")]
    public bool Health() => _healthy;

    public void Reset()
    {
        _healthy = true;
        Interlocked.Increment(ref _restarts);
    }

    public WorkerStats GetStats() => new(_totalCalls, _crashes, _restarts);
}

#endregion

#region NodeSimulator - Distributed Node Simulation

/// <summary>
/// Simulates a remote Erlang node for distributed communication examples.
/// </summary>
public class NodeSimulator : INanoActor
{
    private readonly string _nodeName;
    private int _spawnCounter;

    public NodeSimulator(string nodeName) => _nodeName = nodeName;

    [NanoAction("rpc_call")]
    public RemoteCallResponse RpcCall(RemoteCallRequest req)
    {
        // Simulate remote procedure call
        var result = (req.Module, req.Function) switch
        {
            ("String", "upcase") => req.Args.FirstOrDefault()?.ToUpperInvariant(),
            ("String", "downcase") => req.Args.FirstOrDefault()?.ToLowerInvariant(),
            ("Math", "add") => req.Args.Length >= 2
                ? (int.Parse(req.Args[0]) + int.Parse(req.Args[1])).ToString()
                : null,
            _ => null
        };

        return result != null
            ? new RemoteCallResponse(result, null)
            : new RemoteCallResponse(null, "Function not found");
    }

    [NanoAction("spawn")]
    public RemoteSpawnResponse Spawn(RemoteSpawnRequest req)
    {
        var pid = $"<0.{Interlocked.Increment(ref _spawnCounter)}.0>";
        Console.WriteLine($"  [{_nodeName}] Spawned {req.Module}:{req.Function} → {pid}");
        return new RemoteSpawnResponse(pid);
    }

    [NanoAction("ping")]
    public string Ping() => "pong";

    [NanoAction("node")]
    public string Node() => _nodeName;
}

#endregion

#region ApplicationController - OTP Application Pattern

/// <summary>
/// Manages OTP-style applications with lifecycle management.
/// </summary>
public class ApplicationController : INanoActor
{
    private readonly ProcessRegistry _registry;
    private readonly ConcurrentDictionary<string, AppState> _applications = new();

    public ApplicationController(ProcessRegistry registry) => _registry = registry;

    [NanoAction("start")]
    public AppResponse Start(AppRequest req)
    {
        var state = new AppState(req.Name, "running", 6, DateTime.UtcNow, req.Strategy);
        _applications[req.Name] = state;

        // Simulate starting the application's supervision tree
        Console.WriteLine($"  [Application] Starting ':{req.Name}' with strategy: {req.Strategy}");

        return new AppResponse(true);
    }

    [NanoAction("stop")]
    public AppResponse Stop(string name)
    {
        if (_applications.TryRemove(name, out var state))
        {
            Console.WriteLine($"  [Application] Stopping ':{name}'");
            return new AppResponse(true);
        }

        return new AppResponse(false);
    }

    [NanoAction("info")]
    public AppInfo? Info(AppInfoRequest req)
    {
        if (_applications.TryGetValue(req.Name, out var state))
        {
            return new AppInfo(state.Name, state.Status, state.ChildCount, state.StartedAt);
        }

        return null;
    }

    [NanoAction("which_applications")]
    public string[] WhichApplications() => [.. _applications.Keys];

    private record AppState(string Name, string Status, int ChildCount, DateTime StartedAt, string Strategy);
}

#endregion

#region VersionedActor - Hot Code Reloading Demonstration

/// <summary>
/// Demonstrates hot code reloading by tracking version numbers.
/// </summary>
public class VersionedActor : INanoActor
{
    private readonly int _version;

    public VersionedActor(int version) => _version = version;

    [NanoAction("get_version")]
    public VersionResponse GetVersion() => new(_version);

    [NanoAction("process")]
    public string Process(string input) =>
        $"[v{_version}] Processed: {input}";
}

#endregion
