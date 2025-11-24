using System.Reflection;
using System.Text.Json;
using NanoRpc.Core;
using NanoRpc.Protocol;

namespace NanoRpc.Client;

/// <summary>
/// Creates strongly-typed client proxies for NanoRPC actors.
/// Eliminates magic strings and provides compile-time safety.
/// </summary>
/// <example>
/// // Define your actor interface (shared between client and server)
/// public interface IMathActor : INanoActor
/// {
///     [NanoAction("add")]
///     AddResponse Add(AddRequest req);
/// }
/// 
/// // Create a typed proxy
/// var math = node.CreateProxy&lt;IMathActor&gt;("math");
/// var result = await math.AddAsync(new AddRequest(10, 20));
/// </example>
public class NanoProxy<TActor> where TActor : class, INanoActor
{
    private readonly NanoNode _node;
    private readonly string _target;
    private readonly Dictionary<string, MethodMetadata> _methods = new();
    private readonly int _defaultTimeoutMs;

    public NanoProxy(NanoNode node, string target, int defaultTimeoutMs = 5000)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        _node = node;
        _target = target;
        _defaultTimeoutMs = defaultTimeoutMs;

        // Build method metadata from interface
        foreach (var method in typeof(TActor).GetMethods())
        {
            var attr = method.GetCustomAttribute<NanoActionAttribute>();
            if (attr == null) continue;

            string actionName = attr.Name ?? method.Name;
            var parameters = method.GetParameters();

            _methods[method.Name] = new MethodMetadata
            {
                ActionName = actionName,
                RequestType = parameters.Length > 0 ? parameters[0].ParameterType : null,
                ResponseType = GetUnwrappedReturnType(method.ReturnType),
                IsAsync = typeof(Task).IsAssignableFrom(method.ReturnType)
            };
        }
    }

    /// <summary>
    /// Calls a method on the remote actor with the specified request.
    /// </summary>
    public async Task<TResponse?> CallAsync<TRequest, TResponse>(
        string methodName,
        TRequest request,
        int? timeoutMs = null)
    {
        if (!_methods.TryGetValue(methodName, out var metadata))
            throw new InvalidOperationException($"Method '{methodName}' not found on {typeof(TActor).Name}");

        var id = _node.NextId();
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        var timeout = timeoutMs ?? _defaultTimeoutMs;
        using var cts = new CancellationTokenSource(timeout);
        using var reg = cts.Token.Register(() =>
        {
            if (_node._pending.TryRemove(id, out _))
            {
                tcs.TrySetException(new TimeoutException(
                    $"RPC call to {_target}.{metadata.ActionName} timed out after {timeout}ms."));
            }
        });

        _node._pending[id] = tcs;

        try
        {
            await _node.SendFrameAsync(MsgType.Call, id, _target, metadata.ActionName, request);
            var result = await tcs.Task;
            return JsonSerializer.Deserialize<TResponse>(result);
        }
        finally
        {
            _node._pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Calls a method on the remote actor without a request payload.
    /// </summary>
    public Task<TResponse?> CallAsync<TResponse>(string methodName, int? timeoutMs = null)
    {
        return CallAsync<object, TResponse>(methodName, new { }, timeoutMs);
    }

    /// <summary>
    /// Sends a fire-and-forget message to the remote actor.
    /// </summary>
    public void Cast<TRequest>(string methodName, TRequest request)
    {
        if (!_methods.TryGetValue(methodName, out var metadata))
            throw new InvalidOperationException($"Method '{methodName}' not found on {typeof(TActor).Name}");

        _ = _node.SendFrameAsync(MsgType.Cast, 0, _target, metadata.ActionName, request);
    }

    /// <summary>
    /// Sends a fire-and-forget message without a payload.
    /// </summary>
    public void Cast(string methodName)
    {
        Cast(methodName, new { });
    }

    private static Type? GetUnwrappedReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task))
            return null;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            return returnType.GetGenericArguments()[0];

        return returnType;
    }

    private class MethodMetadata
    {
        public required string ActionName { get; init; }
        public Type? RequestType { get; init; }
        public Type? ResponseType { get; init; }
        public bool IsAsync { get; init; }
    }
}

/// <summary>
/// Extension methods for creating typed proxies.
/// </summary>
public static class NanoProxyExtensions
{
    extension(NanoNode node)
    {
        /// <summary>
        /// Creates a strongly-typed proxy for the specified actor interface.
        /// </summary>
        /// <typeparam name="TActor">The actor interface type.</typeparam>
        /// <param name="target">The registered name of the remote actor.</param>
        /// <param name="defaultTimeoutMs">Default timeout for RPC calls.</param>
        public NanoProxy<TActor> CreateProxy<TActor>(string target, int defaultTimeoutMs = 5000)
            where TActor : class, INanoActor
        {
            return new NanoProxy<TActor>(node, target, defaultTimeoutMs);
        }
    }
}
