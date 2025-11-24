using System.Diagnostics;
using System.Text.Json;
using NanoRpc.Core;
using NanoRpc.Protocol;

namespace NanoRpc.Middleware;

/// <summary>
/// Context object passed through the middleware pipeline.
/// </summary>
public class NanoContext
{
    public required string Target { get; init; }
    public required string Method { get; init; }
    public required MsgType MessageType { get; init; }
    public required uint Id { get; init; }
    public object? Request { get; set; }
    public object? Response { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, object> Items { get; } = new();
    public Stopwatch Stopwatch { get; } = new();
}

/// <summary>
/// Delegate for the next middleware in the pipeline.
/// </summary>
public delegate Task NanoMiddlewareDelegate(NanoContext context);

/// <summary>
/// Interface for implementing middleware components.
/// </summary>
public interface INanoMiddleware
{
    Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next);
}

/// <summary>
/// Logs all RPC calls with timing information.
/// </summary>
public class LoggingMiddleware : INanoMiddleware
{
    private readonly string _prefix;
    private readonly bool _logRequests;
    private readonly bool _logResponses;

    public LoggingMiddleware(string prefix = "RPC", bool logRequests = false, bool logResponses = false)
    {
        _prefix = prefix;
        _logRequests = logRequests;
        _logResponses = logResponses;
    }

    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        context.Stopwatch.Start();

        var requestInfo = _logRequests && context.Request != null
            ? $" Request: {JsonSerializer.Serialize(context.Request)}"
            : "";

        Console.WriteLine($"[{_prefix}] --> {context.Target}.{context.Method}{requestInfo}");

        try
        {
            await next(context).ConfigureAwait(false);

            context.Stopwatch.Stop();

            var responseInfo = _logResponses && context.Response != null
                ? $" Response: {JsonSerializer.Serialize(context.Response)}"
                : "";

            Console.WriteLine($"[{_prefix}] <-- {context.Target}.{context.Method} ({context.Stopwatch.ElapsedMilliseconds}ms){responseInfo}");
        }
        catch (Exception ex)
        {
            context.Stopwatch.Stop();
            Console.WriteLine($"[{_prefix}] <-- {context.Target}.{context.Method} FAILED ({context.Stopwatch.ElapsedMilliseconds}ms): {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Automatically retries failed RPC calls with exponential backoff.
/// </summary>
public class RetryMiddleware : INanoMiddleware
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;
    private readonly Func<Exception, bool>? _shouldRetry;

    public RetryMiddleware(int maxRetries = 3, int baseDelayMs = 100, Func<Exception, bool>? shouldRetry = null)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
        _shouldRetry = shouldRetry ?? DefaultShouldRetry;
    }

    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _maxRetries)
        {
            try
            {
                await next(context).ConfigureAwait(false);
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt >= _maxRetries || !_shouldRetry!(ex))
                    throw;

                attempt++;
                var delay = _baseDelayMs * (1 << (attempt - 1)); // Exponential backoff
                Console.WriteLine($"[Retry] Attempt {attempt}/{_maxRetries} for {context.Target}.{context.Method}, waiting {delay}ms...");
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        throw lastException!;
    }

    private static bool DefaultShouldRetry(Exception ex)
    {
        return ex is TimeoutException || ex is IOException;
    }
}

/// <summary>
/// Adds timeout handling to RPC calls.
/// </summary>
public class TimeoutMiddleware : INanoMiddleware
{
    private readonly int _timeoutMs;

    public TimeoutMiddleware(int timeoutMs = 5000)
    {
        _timeoutMs = timeoutMs;
    }

    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        using var cts = new CancellationTokenSource(_timeoutMs);

        try
        {
            var task = next(context);
            var completedTask = await Task.WhenAny(task, Task.Delay(_timeoutMs, cts.Token)).ConfigureAwait(false);

            if (completedTask != task)
            {
                throw new TimeoutException($"RPC call to {context.Target}.{context.Method} timed out after {_timeoutMs}ms.");
            }

            await task.ConfigureAwait(false); // Propagate any exception
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Timeout cancelled - this is fine
        }
    }
}

/// <summary>
/// Collects metrics about RPC calls.
/// </summary>
public class MetricsMiddleware : INanoMiddleware
{
    private long _totalCalls;
    private long _successfulCalls;
    private long _failedCalls;
    private long _totalLatencyMs;

    public long TotalCalls => Interlocked.Read(ref _totalCalls);
    public long SuccessfulCalls => Interlocked.Read(ref _successfulCalls);
    public long FailedCalls => Interlocked.Read(ref _failedCalls);
    public double AverageLatencyMs => TotalCalls > 0 ? (double)Interlocked.Read(ref _totalLatencyMs) / TotalCalls : 0;

    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        Interlocked.Increment(ref _totalCalls);
        var sw = Stopwatch.StartNew();

        try
        {
            await next(context).ConfigureAwait(false);
            sw.Stop();

            Interlocked.Increment(ref _successfulCalls);
            Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            Interlocked.Increment(ref _failedCalls);
            Interlocked.Add(ref _totalLatencyMs, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public override string ToString() =>
        $"Calls: {TotalCalls} (Success: {SuccessfulCalls}, Failed: {FailedCalls}), Avg Latency: {AverageLatencyMs:F2}ms";
}

/// <summary>
/// Simple circuit breaker to prevent cascading failures.
/// </summary>
public class CircuitBreakerMiddleware : INanoMiddleware
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private int _failureCount;
    private DateTime _lastFailure = DateTime.MinValue;
    private bool _isOpen;
    private readonly object _lock = new();

    public bool IsOpen => _isOpen;

    public CircuitBreakerMiddleware(int failureThreshold = 5, int resetTimeoutSeconds = 30)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = TimeSpan.FromSeconds(resetTimeoutSeconds);
    }

    public async Task InvokeAsync(NanoContext context, NanoMiddlewareDelegate next)
    {
        lock (_lock)
        {
            if (_isOpen)
            {
                if (DateTime.UtcNow - _lastFailure > _resetTimeout)
                {
                    _isOpen = false; // Try half-open state
                    _failureCount = 0;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Circuit breaker is open for {context.Target}.{context.Method}. Try again later.");
                }
            }
        }

        try
        {
            await next(context).ConfigureAwait(false);

            lock (_lock)
            {
                _failureCount = 0; // Reset on success
            }
        }
        catch
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailure = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _isOpen = true;
                    Console.WriteLine($"[CircuitBreaker] OPEN after {_failureCount} failures");
                }
            }
            throw;
        }
    }
}
