using System.Text.Json;
using NanoRpc.Middleware;
using NanoRpc.Protocol;

namespace NanoRpc.Core;

/// <summary>
/// Configuration options for NanoClient.
/// </summary>
public class NanoClientOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8023;
    public string Name { get; set; } = "nano-client";
    public int DefaultTimeoutMs { get; set; } = 5000;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelayMs { get; set; } = 1000;
    public int MaxReconnectAttempts { get; set; } = 10;
    public int HealthCheckIntervalMs { get; set; } = 30000;
}

/// <summary>
/// High-level RPC client with middleware support, auto-reconnection, and typed proxies.
/// </summary>
public class NanoClient : IAsyncDisposable
{
    private readonly NanoClientOptions _options;
    private readonly List<INanoMiddleware> _middlewares = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private NanoNode? _node;
    private bool _disposed;
    private int _reconnectAttempts;
    private Task? _healthCheckTask;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<Exception>? ConnectionFailed;
    public event Action<int>? Reconnecting;

    public bool IsConnected => _node != null;
    public string Name => _options.Name;

    public NanoClient(NanoClientOptions? options = null)
    {
        _options = options ?? new NanoClientOptions();
    }

    public NanoClient(string host, int port) : this(new NanoClientOptions { Host = host, Port = port })
    {
    }

    /// <summary>
    /// Adds a middleware to the pipeline.
    /// </summary>
    public NanoClient Use(INanoMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_node != null) return;

            _node = new NanoNode(_options.Name, _options.Host, _options.Port);
            _node.Disconnected += OnNodeDisconnected;
            _reconnectAttempts = 0;

            Connected?.Invoke();
            Console.WriteLine($"[{_options.Name}] Connected to {_options.Host}:{_options.Port}");

            // Start health check if enabled
            if (_options.HealthCheckIntervalMs > 0)
            {
                _healthCheckTask = Task.Run(HealthCheckLoopAsync);
            }
        }
        catch (Exception ex)
        {
            ConnectionFailed?.Invoke(ex);
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Performs an RPC call through the middleware pipeline.
    /// </summary>
    public async Task<TResponse?> CallAsync<TRequest, TResponse>(
        string target,
        string method,
        TRequest data,
        int? timeoutMs = null)
    {
        await EnsureConnectedAsync();

        var context = new NanoContext
        {
            Target = target,
            Method = method,
            MessageType = MsgType.Call,
            Id = _node!.NextId(),
            Request = data
        };

        var pipeline = BuildPipeline(async ctx =>
        {
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeout = timeoutMs ?? _options.DefaultTimeoutMs;

            using var cts = new CancellationTokenSource(timeout);
            using var reg = cts.Token.Register(() =>
            {
                if (_node._pending.TryRemove(ctx.Id, out _))
                {
                    tcs.TrySetException(new TimeoutException(
                        $"RPC call to {ctx.Target}.{ctx.Method} timed out after {timeout}ms."));
                }
            });

            _node._pending[ctx.Id] = tcs;

            try
            {
                await _node.SendFrameAsync(MsgType.Call, ctx.Id, ctx.Target, ctx.Method, ctx.Request).ConfigureAwait(false);
                var result = await tcs.Task.ConfigureAwait(false);
                ctx.Response = JsonSerializer.Deserialize<TResponse>(result);
            }
            finally
            {
                _node._pending.TryRemove(ctx.Id, out _);
            }
        });

        await pipeline(context).ConfigureAwait(false);
        return (TResponse?)context.Response;
    }

    /// <summary>
    /// Performs an RPC call without request data.
    /// </summary>
    public Task<TResponse?> CallAsync<TResponse>(string target, string method, int? timeoutMs = null)
    {
        return CallAsync<object, TResponse>(target, method, new { }, timeoutMs);
    }

    /// <summary>
    /// Sends a fire-and-forget message.
    /// </summary>
    public async Task CastAsync<TRequest>(string target, string method, TRequest data)
    {
        await EnsureConnectedAsync().ConfigureAwait(false);
        await _node!.SendFrameAsync(MsgType.Cast, 0, target, method, data).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a fire-and-forget message without data.
    /// </summary>
    public Task CastAsync(string target, string method)
    {
        return CastAsync(target, method, new { });
    }

    private NanoMiddlewareDelegate BuildPipeline(NanoMiddlewareDelegate final)
    {
        var pipeline = final;

        // Build pipeline in reverse order
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var next = pipeline;
            pipeline = ctx => middleware.InvokeAsync(ctx, next);
        }

        return pipeline;
    }

    private async Task EnsureConnectedAsync()
    {
        if (_node != null) return;

        if (_options.AutoReconnect)
        {
            await ReconnectAsync().ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("Not connected to server. Call ConnectAsync() first.");
        }
    }

    private async Task ReconnectAsync()
    {
        await _connectionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_node != null) return;

            while (_reconnectAttempts < _options.MaxReconnectAttempts && !_cts.Token.IsCancellationRequested)
            {
                _reconnectAttempts++;
                Reconnecting?.Invoke(_reconnectAttempts);
                Console.WriteLine($"[{_options.Name}] Reconnecting... (attempt {_reconnectAttempts}/{_options.MaxReconnectAttempts})");

                try
                {
                    _node = new NanoNode(_options.Name, _options.Host, _options.Port);
                    _node.Disconnected += OnNodeDisconnected;
                    _reconnectAttempts = 0;
                    Connected?.Invoke();
                    Console.WriteLine($"[{_options.Name}] Reconnected successfully!");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_options.Name}] Reconnect failed: {ex.Message}");
                    await Task.Delay(_options.ReconnectDelayMs, _cts.Token).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException($"Failed to reconnect after {_options.MaxReconnectAttempts} attempts.");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task HealthCheckLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(_options.HealthCheckIntervalMs, _cts.Token).ConfigureAwait(false);

                // Simple health check - try to send a ping
                // In a real implementation, you'd add a proper ping/pong mechanism
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    private void OnNodeDisconnected()
    {
        var node = _node;
        _node = null;

        if (node != null)
        {
            node.Disconnected -= OnNodeDisconnected;
        }

        if (!_disposed)
        {
            Console.WriteLine($"[{_options.Name}] Disconnected from server.");
            Disconnected?.Invoke();

            // Trigger auto-reconnect if enabled
            if (_options.AutoReconnect)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ReconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        ConnectionFailed?.Invoke(ex);
                    }
                });
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_healthCheckTask != null)
        {
            try { await _healthCheckTask.ConfigureAwait(false); } catch { }
        }

        if (_node != null)
        {
            _node.Disconnected -= OnNodeDisconnected;
            await _node.DisposeAsync().ConfigureAwait(false);
        }

        _connectionLock.Dispose();
        _cts.Dispose();
    }
}

/// <summary>
/// Builder for fluent NanoClient configuration.
/// </summary>
public class NanoClientBuilder
{
    private readonly NanoClientOptions _options = new();
    private readonly List<INanoMiddleware> _middlewares = new();

    public NanoClientBuilder WithHost(string host)
    {
        _options.Host = host;
        return this;
    }

    public NanoClientBuilder WithPort(int port)
    {
        _options.Port = port;
        return this;
    }

    public NanoClientBuilder WithName(string name)
    {
        _options.Name = name;
        return this;
    }

    public NanoClientBuilder WithTimeout(int timeoutMs)
    {
        _options.DefaultTimeoutMs = timeoutMs;
        return this;
    }

    public NanoClientBuilder WithAutoReconnect(bool enabled = true, int delayMs = 1000, int maxAttempts = 10)
    {
        _options.AutoReconnect = enabled;
        _options.ReconnectDelayMs = delayMs;
        _options.MaxReconnectAttempts = maxAttempts;
        return this;
    }

    public NanoClientBuilder WithHealthCheck(int intervalMs)
    {
        _options.HealthCheckIntervalMs = intervalMs;
        return this;
    }

    public NanoClientBuilder Use(INanoMiddleware middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public NanoClientBuilder UseLogging(string prefix = "RPC", bool logRequests = false, bool logResponses = false)
    {
        return Use(new LoggingMiddleware(prefix, logRequests, logResponses));
    }

    public NanoClientBuilder UseRetry(int maxRetries = 3, int baseDelayMs = 100)
    {
        return Use(new RetryMiddleware(maxRetries, baseDelayMs));
    }

    public NanoClientBuilder UseMetrics(out MetricsMiddleware metrics)
    {
        metrics = new MetricsMiddleware();
        return Use(metrics);
    }

    public NanoClientBuilder UseCircuitBreaker(int failureThreshold = 5, int resetTimeoutSeconds = 30)
    {
        return Use(new CircuitBreakerMiddleware(failureThreshold, resetTimeoutSeconds));
    }

    public NanoClient Build()
    {
        var client = new NanoClient(_options);
        foreach (var middleware in _middlewares)
        {
            client.Use(middleware);
        }
        return client;
    }
}
