using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using NanoRpc.Protocol;

namespace NanoRpc.Core;

/// <summary>
/// Represents a readable stream from the server.
/// </summary>
public class NanoReadStream<T> : IAsyncDisposable
{
    private readonly NanoStreamClient _client;
    private readonly uint _streamId;
    private readonly Channel<T?> _channel;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private bool _completed;

    public uint StreamId => _streamId;
    public bool IsCompleted => _completed;

    internal NanoReadStream(NanoStreamClient client, uint streamId, int bufferSize = 100)
    {
        _client = client;
        _streamId = streamId;
        _channel = Channel.CreateBounded<T?>(new BoundedChannelOptions(bufferSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });
    }

    /// <summary>
    /// Reads all items from the stream as an async enumerable.
    /// </summary>
    public async IAsyncEnumerable<T?> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        await foreach (var item in _channel.Reader.ReadAllAsync(linkedCts.Token))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Tries to read the next item from the stream.
    /// </summary>
    public async ValueTask<(bool Success, T? Item)> TryReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _channel.Reader.ReadAsync(cancellationToken);
            return (true, item);
        }
        catch (ChannelClosedException)
        {
            return (false, default);
        }
    }

    /// <summary>
    /// Cancels the stream, requesting the server to stop sending.
    /// </summary>
    public async Task CancelAsync()
    {
        if (_completed || _disposed) return;
        await _client.CancelStreamAsync(_streamId);
        Complete();
    }

    internal void Write(T? item)
    {
        if (!_completed && !_disposed)
        {
            _channel.Writer.TryWrite(item);
        }
    }

    internal void Complete()
    {
        if (_completed) return;
        _completed = true;
        _channel.Writer.TryComplete();
    }

    internal void Fault(Exception ex)
    {
        _completed = true;
        _channel.Writer.TryComplete(ex);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_completed)
        {
            await CancelAsync();
        }

        await _cts.CancelAsync();
        _cts.Dispose();
        _client.RemoveStream(_streamId);
    }
}

/// <summary>
/// Represents a writable stream to send data to the server.
/// </summary>
public class NanoWriteStream<T> : IAsyncDisposable
{
    private readonly NanoNode _node;
    private readonly uint _streamId;
    private readonly string _target;
    private readonly string _method;
    private bool _disposed;
    private bool _completed;

    public uint StreamId => _streamId;
    public bool IsCompleted => _completed;

    internal NanoWriteStream(NanoNode node, uint streamId, string target, string method)
    {
        _node = node;
        _streamId = streamId;
        _target = target;
        _method = method;
    }

    /// <summary>
    /// Writes an item to the stream.
    /// </summary>
    public async Task WriteAsync(T data)
    {
        if (_completed || _disposed)
            throw new InvalidOperationException("Stream is closed.");

        await _node.SendFrameAsync(MsgType.StreamData, _streamId, _target, _method, data);
    }

    /// <summary>
    /// Writes multiple items to the stream.
    /// </summary>
    public async Task WriteAllAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            await WriteAsync(item);
        }
    }

    /// <summary>
    /// Completes the stream, signaling no more data will be sent.
    /// </summary>
    public async Task CompleteAsync()
    {
        if (_completed || _disposed) return;
        _completed = true;
        await _node.SendFrameAsync(MsgType.StreamEnd, _streamId, _target, _method, new { });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_completed)
        {
            await CompleteAsync();
        }
    }
}

/// <summary>
/// Client-side streaming functionality for NanoNode.
/// </summary>
public class NanoStreamClient
{
    private readonly NanoNode _node;
    private readonly ConcurrentDictionary<uint, object> _streams = new();

    public NanoStreamClient(NanoNode node)
    {
        _node = node;
    }

    /// <summary>
    /// Starts a read stream from the server (server pushes data to client).
    /// </summary>
    public async Task<NanoReadStream<T>> StartReadStreamAsync<TRequest, T>(
        string target,
        string method,
        TRequest request)
    {
        var streamId = _node.NextId();
        var stream = new NanoReadStream<T>(this, streamId);

        _streams[streamId] = stream;

        await _node.SendFrameAsync(MsgType.StreamStart, streamId, target, method, request);

        return stream;
    }

    /// <summary>
    /// Starts a read stream without request data.
    /// </summary>
    public Task<NanoReadStream<T>> StartReadStreamAsync<T>(string target, string method)
    {
        return StartReadStreamAsync<object, T>(target, method, new { });
    }

    /// <summary>
    /// Starts a write stream to the server (client pushes data to server).
    /// </summary>
    public async Task<NanoWriteStream<T>> StartWriteStreamAsync<T>(string target, string method)
    {
        var streamId = _node.NextId();
        var stream = new NanoWriteStream<T>(_node, streamId, target, method);

        await _node.SendFrameAsync(MsgType.StreamStart, streamId, target, method, new { direction = "upload" });

        return stream;
    }

    internal async Task CancelStreamAsync(uint streamId)
    {
        await _node.SendFrameAsync(MsgType.StreamCancel, streamId, "", "", new { });
    }

    internal void RemoveStream(uint streamId)
    {
        _streams.TryRemove(streamId, out _);
    }

    /// <summary>
    /// Called by NanoNode when stream data is received.
    /// </summary>
    internal void HandleStreamData<T>(uint streamId, JsonElement data)
    {
        if (_streams.TryGetValue(streamId, out var streamObj) && streamObj is NanoReadStream<T> stream)
        {
            var item = JsonSerializer.Deserialize<T>(data);
            stream.Write(item);
        }
    }

    /// <summary>
    /// Called by NanoNode when stream data is received (raw).
    /// </summary>
    internal void HandleStreamDataRaw(uint streamId, JsonElement data)
    {
        if (_streams.TryGetValue(streamId, out var streamObj))
        {
            // Use reflection to call Write with the correct type
            var streamType = streamObj.GetType();
            if (streamType.IsGenericType && streamType.GetGenericTypeDefinition() == typeof(NanoReadStream<>))
            {
                var itemType = streamType.GetGenericArguments()[0];
                var item = JsonSerializer.Deserialize(data, itemType);
                var writeMethod = streamType.GetMethod("Write", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                writeMethod?.Invoke(streamObj, [item]);
            }
        }
    }

    /// <summary>
    /// Called by NanoNode when stream ends.
    /// </summary>
    internal void HandleStreamEnd(uint streamId)
    {
        if (_streams.TryGetValue(streamId, out var streamObj))
        {
            var completeMethod = streamObj.GetType().GetMethod("Complete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            completeMethod?.Invoke(streamObj, null);
        }
    }

    /// <summary>
    /// Called by NanoNode when stream errors.
    /// </summary>
    internal void HandleStreamError(uint streamId, string error)
    {
        if (_streams.TryGetValue(streamId, out var streamObj))
        {
            var faultMethod = streamObj.GetType().GetMethod("Fault", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            faultMethod?.Invoke(streamObj, [new NanoRpcException(error)]);
        }
    }
}

/// <summary>
/// Server-side stream context for handling streaming requests.
/// </summary>
public class NanoServerStream<T>
{
    private readonly NanoServer.ClientConnection _connection;
    private readonly uint _streamId;
    private readonly string _target;
    private readonly string _method;
    private bool _completed;

    public uint StreamId => _streamId;
    public string Target => _target;
    public string Method => _method;

    internal NanoServerStream(NanoServer.ClientConnection connection, uint streamId, string target, string method)
    {
        _connection = connection;
        _streamId = streamId;
        _target = target;
        _method = method;
    }

    /// <summary>
    /// Sends a single item to the client.
    /// </summary>
    public async Task SendAsync(T data)
    {
        if (_completed) throw new InvalidOperationException("Stream is closed.");
        await _connection.SendFrameAsync(MsgType.StreamData, _streamId, _target, _method, data);
    }

    /// <summary>
    /// Sends an async enumerable to the client.
    /// </summary>
    public async Task SendAllAsync(IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            await SendAsync(item);
        }
    }

    /// <summary>
    /// Completes the stream successfully.
    /// </summary>
    public async Task CompleteAsync()
    {
        if (_completed) return;
        _completed = true;
        await _connection.SendFrameAsync(MsgType.StreamEnd, _streamId, _target, _method, new { });
    }

    /// <summary>
    /// Sends an error and closes the stream.
    /// </summary>
    public async Task ErrorAsync(string error)
    {
        if (_completed) return;
        _completed = true;
        await _connection.SendFrameAsync(MsgType.Error, _streamId, _target, _method, new { error });
    }
}

/// <summary>
/// Attribute to mark a method as a streaming endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NanoStreamAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

/// <summary>
/// Server-side stream handler manager.
/// </summary>
public class NanoStreamBroker
{
    private readonly NanoServer _server;
    private readonly ConcurrentDictionary<string, Func<NanoServer.ClientConnection, uint, string, string, JsonElement, Task>> _handlers = new();
    private readonly ConcurrentDictionary<(string ClientId, uint StreamId), CancellationTokenSource> _activeStreams = new();

    public NanoStreamBroker(NanoServer server)
    {
        _server = server;
    }

    /// <summary>
    /// Registers a streaming handler.
    /// </summary>
    public void RegisterStreamHandler<TRequest, TItem>(
        string target,
        string method,
        Func<TRequest?, NanoServerStream<TItem>, CancellationToken, Task> handler)
    {
        string routeKey = $"{target}.{method}";

        _handlers[routeKey] = async (connection, streamId, t, m, data) =>
        {
            var cts = new CancellationTokenSource();
            var key = (connection.ClientId, streamId);
            _activeStreams[key] = cts;

            var request = data.ValueKind != JsonValueKind.Undefined
                ? JsonSerializer.Deserialize<TRequest>(data)
                : default;

            var stream = new NanoServerStream<TItem>(connection, streamId, t, m);

            try
            {
                await handler(request, stream, cts.Token);
                await stream.CompleteAsync();
            }
            catch (OperationCanceledException)
            {
                // Stream was cancelled by client
            }
            catch (Exception ex)
            {
                await stream.ErrorAsync(ex.Message);
            }
            finally
            {
                _activeStreams.TryRemove(key, out _);
            }
        };

        Console.WriteLine($"[Stream] Registered handler: {routeKey}");
    }

    /// <summary>
    /// Handles an incoming stream start request.
    /// </summary>
    internal async Task HandleStreamStartAsync(NanoServer.ClientConnection connection, uint streamId, string target, string method, JsonElement data)
    {
        string routeKey = $"{target}.{method}";

        if (_handlers.TryGetValue(routeKey, out var handler))
        {
            // Run handler in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler(connection, streamId, target, method, data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stream] Handler error for {routeKey}: {ex.Message}");
                }
            });
        }
        else
        {
            await connection.SendFrameAsync(MsgType.Error, streamId, target, method, new
            {
                error = $"Stream handler '{routeKey}' not found."
            });
        }
    }

    /// <summary>
    /// Handles a stream cancel request from client.
    /// </summary>
    internal void HandleStreamCancel(string clientId, uint streamId)
    {
        var key = (clientId, streamId);
        if (_activeStreams.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <summary>
    /// Cancels all streams for a disconnected client.
    /// </summary>
    internal void CancelAllStreams(string clientId)
    {
        var keysToRemove = _activeStreams.Keys.Where(k => k.ClientId == clientId).ToList();
        foreach (var key in keysToRemove)
        {
            if (_activeStreams.TryRemove(key, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}

/// <summary>
/// Extension methods for Streaming support.
/// </summary>
public static class NanoStreamExtensions
{
    extension(NanoNode node)
    {
        /// <summary>
        /// Gets the streaming client for this node.
        /// </summary>
        public NanoStreamClient Streams => node.GetOrCreateStreamClient();
    }
}