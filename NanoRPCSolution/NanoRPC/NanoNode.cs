using NanoRpc.Protocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NanoRpc.Core;

public class NanoNode : IAsyncDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _receiveLoopTask;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly PipeWriter _pipeWriter;
    private bool _disposed;

    private readonly ConcurrentDictionary<string, Func<object, JsonElement, Task<object>>> _dispatchMap = new();
    private readonly ConcurrentDictionary<string, object> _actors = new();

    internal readonly ConcurrentDictionary<uint, TaskCompletionSource<JsonElement>> _pending = new();

    private int _idCounter = 0;
    internal uint NextId() => (uint)Interlocked.Increment(ref _idCounter);

    public string NodeName { get; }

    /// <summary>
    /// Fired when the connection is closed unexpectedly (not during disposal).
    /// </summary>
    public event Action? Disconnected;

    // Internal pub/sub and stream client instances
    private NanoPubSubClient? _pubSubClient;
    private NanoStreamClient? _streamClient;

    /// <summary>
    /// Gets the Pub/Sub client for this node. Lazily initialized.
    /// </summary>
    internal NanoPubSubClient? PubSubClient => _pubSubClient;

    /// <summary>
    /// Gets the Stream client for this node. Lazily initialized.
    /// </summary>
    internal NanoStreamClient? StreamClient => _streamClient;

    /// <summary>
    /// Initializes or gets the Pub/Sub client.
    /// </summary>
    internal NanoPubSubClient GetOrCreatePubSubClient() => _pubSubClient ??= new NanoPubSubClient(this);

    /// <summary>
    /// Initializes or gets the Stream client.
    /// </summary>
    internal NanoStreamClient GetOrCreateStreamClient() => _streamClient ??= new NanoStreamClient(this);

    public NanoNode(string name, string host, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        NodeName = name;
        _client = new TcpClient();
        _client.Connect(host, port);
        _stream = _client.GetStream();
        _pipeWriter = PipeWriter.Create(_stream, new StreamPipeWriterOptions(leaveOpen: true));

        // Start background listener with proper error handling
        _receiveLoopTask = Task.Run(async () =>
        {
            try
            {
                await ReceiveLoopAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{NodeName}] Receive loop crashed: {ex}");
            }
        });

        // Immediately send handshake
        // (Implementation details omitted for brevity, uses WireFormatter)
    }

    public void RegisterActor<T>(string name, T actor) where T : INanoActor
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(actor);

        _actors[name] = actor;

        // Reflection ONLY at startup (Performance optimization)
        foreach (var method in typeof(T).GetMethods())
        {
            if (method.GetCustomAttributes(typeof(NanoActionAttribute), true).FirstOrDefault() is not NanoActionAttribute attr)
                continue;

            string actionName = attr.Name ?? method.Name;
            string routeKey = $"{name}.{actionName}";
            var parameters = method.GetParameters();

            // Compile a fast delegate
            _dispatchMap[routeKey] = async (target, json) =>
            {
                object? args = null;

                // Handle methods with parameters
                if (parameters.Length > 0)
                {
                    var paramType = parameters[0].ParameterType;
                    args = JsonSerializer.Deserialize(json, paramType);
                }

                // Invoke with or without arguments
                var result = parameters.Length > 0
                    ? method.Invoke(target, [args])
                    : method.Invoke(target, null);

                // Handle async results
                if (result is Task t)
                {
                    await t.ConfigureAwait(false);
                    var taskProp = t.GetType().GetProperty("Result");
                    return taskProp?.GetValue(t) ?? new { };
                }
                return result ?? new { };
            };
        }
    }

    // Low-level send with thread safety
    internal async Task SendFrameAsync<T>(MsgType type, uint id, string target, string method, T data)
    {
        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            WireFormatter.WriteFrame(_pipeWriter, type, id, target, method, data);
            await _pipeWriter.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ProcessMessageAsync(MsgType type, uint id, string target, string method, JsonElement data)
    {
        try
        {
            switch (type)
            {
                case MsgType.Call:
                case MsgType.Cast:
                    await HandleInvocationAsync(type, id, target, method, data);
                    break;

                case MsgType.Reply:
                    // - Resolve pending promise
                    if (_pending.TryRemove(id, out var tcsSuccess))
                        tcsSuccess.TrySetResult(data);
                    break;

                case MsgType.Error:
                    // - Reject pending promise
                    if (_pending.TryRemove(id, out var tcsError))
                    {
                        string errorMsg = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("error", out var e)
                        ? e.ToString()
                        : "Unknown RPC Error";
                        tcsError.TrySetException(new NanoRpcException(errorMsg, target, method));
                    }
                    break;

                case MsgType.Handshake:
                    // - Log handshake (Discovery logic would go here)
                    Console.WriteLine($"[{NodeName}] Handshake received from {target}.");
                    break;

                // Pub/Sub: Handle incoming published messages
                case MsgType.Publish:
                    if (_pubSubClient != null)
                    {
                        _pubSubClient.HandlePublish(target, data);
                    }
                    else
                    {
                        Console.WriteLine($"[{NodeName}] WARNING: Received publish but no PubSubClient registered");
                    }
                    break;

                // Streaming: Handle incoming stream data
                case MsgType.StreamData:
                    _streamClient?.HandleStreamDataRaw(id, data);
                    break;

                case MsgType.StreamEnd:
                    _streamClient?.HandleStreamEnd(id);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{NodeName}] Error processing message {id}: {ex.Message}");
        }
    }

    private async Task HandleInvocationAsync(MsgType type, uint id, string target, string method, JsonElement data)
    {
        // 1. Construct Route Key (matches RegisterActor logic)
        string routeKey = $"{target}.{method}";

        // 2. Lookup Handler and Actor Instance
        if (_dispatchMap.TryGetValue(routeKey, out var handler) && _actors.TryGetValue(target, out var actor))
        {
            try
            {
                // 3. Invoke Actor
                var result = await handler(actor, data);

                // 4. Send Reply (Only for CALL)
                if (type == MsgType.Call)
                {
                    await SendFrameAsync(MsgType.Reply, id, target, method, result);
                }
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException to get the actual error
                var actualException = ex.InnerException ?? ex;

                // 5. Send Error on Exception (Only for CALL)
                if (type == MsgType.Call)
                {
                    await SendFrameAsync(MsgType.Error, id, target, method, new
                    {
                        error = actualException.Message,
                        type = actualException.GetType().Name
                    });
                }
            }
        }
        else
        {
            // Actor or Method not found
            if (type == MsgType.Call)
            {
                await SendFrameAsync(MsgType.Error, id, target, method, new
                {
                    error = $"Method '{routeKey}' not found."
                });
            }
        }
    }

    private static async Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct = default)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
            if (read == 0)
                return false; // Stream closed
            offset += read;
        }
        return true;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct = default)
    {
        // Allocate a reusable buffer for headers to reduce GC pressure
        byte[] headerBuffer = new byte[NanoHeader.Size];
        bool disconnectedFired = false;

        try
        {
            while (!ct.IsCancellationRequested && !_disposed)
            {
                // 1. Read Header (Strictly 17 bytes)
                // We use a helper to ensure we don't proceed with partial data
                if (!await ReadExactlyAsync(_stream, headerBuffer, NanoHeader.Size, ct).ConfigureAwait(false))
                    break; // Socket closed gracefully

                // 2. Decode Header properties
                var header = new NanoHeader(headerBuffer);

                // Validate header to prevent malicious payloads
                if (!header.IsValid())
                {
                    Console.WriteLine($"[{NodeName}] Invalid header received, closing connection.");
                    break;
                }

                // 3. Allocate/Rent buffer for the body
                int totalBodyLen = header.TotalBodyLength;
                byte[]? rentedBuffer = null;
                byte[] bodyBuffer;

                // Use ArrayPool for larger allocations to reduce GC pressure
                if (totalBodyLen > 1024)
                {
                    rentedBuffer = ArrayPool<byte>.Shared.Rent(totalBodyLen);
                    bodyBuffer = rentedBuffer;
                }
                else
                {
                    bodyBuffer = new byte[totalBodyLen];
                }

                try
                {
                    // 4. Read Body (Strictly totalBodyLen bytes)
                    if (totalBodyLen > 0)
                    {
                        if (!await ReadExactlyAsync(_stream, bodyBuffer, totalBodyLen, ct).ConfigureAwait(false))
                            throw new EndOfStreamException("Connection closed while reading body frame.");
                    }

                    // 5. Slice and Parse Body
                    string target = Encoding.UTF8.GetString(bodyBuffer, 0, header.TargetLen);
                    string method = Encoding.UTF8.GetString(bodyBuffer, header.TargetLen, header.MethodLen);

                    // Extract JSON Payload (if exists)
                    JsonElement data = default;
                    if (header.BodyLen > 0)
                    {
                        var jsonSpan = bodyBuffer.AsSpan(header.TargetLen + header.MethodLen, header.BodyLen);
                        var reader = new Utf8JsonReader(jsonSpan);
                        if (JsonElement.TryParseValue(ref reader, out JsonElement? parsed))
                        {
                            data = parsed.Value;
                        }
                    }

                    // 6. Process (Fire-and-forget to keep reading the stream)
                    _ = ProcessMessageAsync(header.Type, header.Id, target, method, data);
                }
                finally
                {
                    // Return rented buffer to pool
                    if (rentedBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
        {
            // Connection closed - only log if unexpected
            if (!_disposed)
            {
                Console.WriteLine($"[{NodeName}] Connection closed.");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{NodeName}] Critical Receive Error: {ex}");
        }

        // Fire disconnected exactly once if loop exits and we're not disposing
        if (!_disposed && !disconnectedFired)
        {
            disconnectedFired = true;
            Disconnected?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Signal cancellation to receive loop
        await _cts.CancelAsync();

        // Wait for receive loop to complete (with timeout)
        try
        {
            await _receiveLoopTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            // Receive loop didn't complete in time - this is usually fine during shutdown
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Cancel all pending requests
        foreach (var pending in _pending)
        {
            pending.Value.TrySetCanceled();
        }
        _pending.Clear();

        // Cleanup resources
        await _pipeWriter.CompleteAsync();
        _sendLock.Dispose();
        _cts.Dispose();
        _stream.Dispose();
        _client.Dispose();
    }
}