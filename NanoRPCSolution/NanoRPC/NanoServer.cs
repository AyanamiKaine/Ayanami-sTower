using NanoRpc.Protocol;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NanoRpc.Core;

/// <summary>
/// A TCP server that accepts NanoRPC connections and dispatches messages to registered actors.
/// </summary>
public class NanoServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, ClientConnection> _clients = new();
    private readonly ConcurrentDictionary<string, Func<object, JsonElement, Task<object>>> _dispatchMap = new();
    private readonly ConcurrentDictionary<string, object> _actors = new();
    private Task? _acceptLoopTask;
    private bool _disposed;

    private NanoPubSubBroker? _pubSubBroker;
    private NanoStreamBroker? _streamBroker;

    public string ServerName { get; }
    public int Port { get; }
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the Pub/Sub broker for this server.
    /// </summary>
    public NanoPubSubBroker PubSub => _pubSubBroker ??= new NanoPubSubBroker(this);

    /// <summary>
    /// Gets the Stream broker for this server.
    /// </summary>
    public NanoStreamBroker Streams => _streamBroker ??= new NanoStreamBroker(this);

    public event Action<string>? ClientConnected;
    public event Action<string>? ClientDisconnected;

    public NanoServer(string name, int port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ServerName = name;
        Port = port;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>
    /// Starts the server and begins accepting connections.
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        _listener.Start();
        IsRunning = true;

        _acceptLoopTask = Task.Run(AcceptLoopAsync);
        Console.WriteLine($"[{ServerName}] Server started on port {Port}");
    }

    /// <summary>
    /// Registers an actor that can handle RPC calls.
    /// </summary>
    public void RegisterActor<T>(string name, T actor) where T : INanoActor
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(actor);

        _actors[name] = actor;

        foreach (var method in typeof(T).GetMethods())
        {
            if (method.GetCustomAttributes(typeof(NanoActionAttribute), true).FirstOrDefault() is not NanoActionAttribute attr)
                continue;

            string actionName = attr.Name ?? method.Name;
            string routeKey = $"{name}.{actionName}";
            var parameters = method.GetParameters();

            _dispatchMap[routeKey] = async (target, json) =>
            {
                object? args = null;

                if (parameters.Length > 0)
                {
                    var paramType = parameters[0].ParameterType;
                    args = JsonSerializer.Deserialize(json, paramType);
                }

                var result = parameters.Length > 0
                    ? method.Invoke(target, [args])
                    : method.Invoke(target, null);

                if (result is Task t)
                {
                    await t.ConfigureAwait(false);
                    var taskProp = t.GetType().GetProperty("Result");
                    return taskProp?.GetValue(t) ?? new { };
                }
                return result ?? new { };
            };

            Console.WriteLine($"[{ServerName}] Registered action: {routeKey}");
        }
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                var clientId = Guid.NewGuid().ToString("N")[..8];

                var connection = new ClientConnection(clientId, tcpClient, this);
                _clients[clientId] = connection;

                Console.WriteLine($"[{ServerName}] Client connected: {clientId}");
                ClientConnected?.Invoke(clientId);

                _ = connection.StartAsync(_cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ServerName}] Accept loop error: {ex.Message}");
        }
    }

    internal void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out _))
        {
            // Clean up pub/sub subscriptions
            _pubSubBroker?.UnsubscribeAll(clientId);

            // Cancel active streams
            _streamBroker?.CancelAllStreams(clientId);

            Console.WriteLine($"[{ServerName}] Client disconnected: {clientId}");
            ClientDisconnected?.Invoke(clientId);
        }
    }

    internal async Task HandleInvocationAsync(ClientConnection client, MsgType type, uint id, string target, string method, JsonElement data)
    {
        string routeKey = $"{target}.{method}";

        if (_dispatchMap.TryGetValue(routeKey, out var handler) && _actors.TryGetValue(target, out var actor))
        {
            try
            {
                var result = await handler(actor, data);

                if (type == MsgType.Call)
                {
                    await client.SendFrameAsync(MsgType.Reply, id, target, method, result);
                }
            }
            catch (Exception ex)
            {
                var actualException = ex.InnerException ?? ex;

                if (type == MsgType.Call)
                {
                    await client.SendFrameAsync(MsgType.Error, id, target, method, new
                    {
                        error = actualException.Message,
                        type = actualException.GetType().Name
                    });
                }
            }
        }
        else
        {
            if (type == MsgType.Call)
            {
                await client.SendFrameAsync(MsgType.Error, id, target, method, new
                {
                    error = $"Method '{routeKey}' not found."
                });
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync();
        _listener.Stop();

        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();

        if (_acceptLoopTask != null)
        {
            try
            {
                await _acceptLoopTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        _cts.Dispose();
        Console.WriteLine($"[{ServerName}] Server stopped.");
    }

    /// <summary>
    /// Represents a connected client session.
    /// </summary>
    public class ClientConnection : IAsyncDisposable
    {
        private readonly string _clientId;
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly NanoServer _server;
        private readonly PipeWriter _pipeWriter;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private bool _disposed;

        public string ClientId => _clientId;

        internal ClientConnection(string clientId, TcpClient tcpClient, NanoServer server)
        {
            _clientId = clientId;
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            _server = server;
            _pipeWriter = PipeWriter.Create(_stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public async Task StartAsync(CancellationToken ct)
        {
            try
            {
                await ReceiveLoopAsync(ct);
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is OperationCanceledException)
            {
                // Connection closed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client:{_clientId}] Error: {ex.Message}");
            }
            finally
            {
                _server.RemoveClient(_clientId);
                await DisposeAsync();
            }
        }

        public async Task SendFrameAsync<T>(MsgType type, uint id, string target, string method, T data)
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

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] headerBuffer = new byte[NanoHeader.Size];

            while (!ct.IsCancellationRequested)
            {
                if (!await ReadExactlyAsync(_stream, headerBuffer, NanoHeader.Size, ct).ConfigureAwait(false))
                    break;

                var header = new NanoHeader(headerBuffer);

                if (!header.IsValid())
                {
                    Console.WriteLine($"[Client:{_clientId}] Invalid header received.");
                    break;
                }

                int totalBodyLen = header.TotalBodyLength;
                byte[]? rentedBuffer = null;
                byte[] bodyBuffer;

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
                    if (totalBodyLen > 0)
                    {
                        if (!await ReadExactlyAsync(_stream, bodyBuffer, totalBodyLen, ct).ConfigureAwait(false))
                            break;
                    }

                    string target = Encoding.UTF8.GetString(bodyBuffer, 0, header.TargetLen);
                    string method = Encoding.UTF8.GetString(bodyBuffer, header.TargetLen, header.MethodLen);

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

                    // Handle message
                    switch (header.Type)
                    {
                        case MsgType.Call:
                        case MsgType.Cast:
                            _ = _server.HandleInvocationAsync(this, header.Type, header.Id, target, method, data);
                            break;
                        case MsgType.Handshake:
                            Console.WriteLine($"[Client:{_clientId}] Handshake from: {target}");
                            break;

                        // Pub/Sub messages
                        case MsgType.Subscribe:
                            _server.PubSub.Subscribe(_clientId, target, this);
                            break;
                        case MsgType.Unsubscribe:
                            _server.PubSub.Unsubscribe(_clientId, target);
                            break;
                        case MsgType.Publish:
                            _ = _server.PubSub.HandleClientPublishAsync(_clientId, target, data);
                            break;

                        // Streaming messages
                        case MsgType.StreamStart:
                            _ = _server.Streams.HandleStreamStartAsync(this, header.Id, target, method, data);
                            break;
                        case MsgType.StreamCancel:
                            _server.Streams.HandleStreamCancel(_clientId, header.Id);
                            break;
                    }
                }
                finally
                {
                    if (rentedBuffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }
                }
            }
        }

        private static async Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct = default)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await _pipeWriter.CompleteAsync();
            _sendLock.Dispose();
            _stream.Dispose();
            _tcpClient.Dispose();
        }
    }
}
