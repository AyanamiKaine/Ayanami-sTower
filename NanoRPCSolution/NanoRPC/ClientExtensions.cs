using System.Text.Json;
using NanoRpc.Core;
using NanoRpc.Protocol;

namespace NanoRpc.Extensions;

public static class NanoClientExtensions
{
    extension(NanoNode node)
    {
        /// <summary>
        /// Performs a synchronous RPC call and waits for the response.
        /// </summary>
        /// <typeparam name="TRequest">The request payload type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="target">The target actor name.</param>
        /// <param name="method">The method to invoke.</param>
        /// <param name="data">The request payload.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
        /// <returns>The deserialized response, or null if deserialization fails.</returns>
        /// <exception cref="ArgumentException">Thrown when target or method is null/empty.</exception>
        /// <exception cref="TimeoutException">Thrown when the call times out.</exception>
        /// <exception cref="NanoRpcException">Thrown when the remote returns an error.</exception>
        public async Task<TResponse?> CallAsync<TRequest, TResponse>(
            string target,
            string method,
            TRequest data,
            int timeoutMs = 5000)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(method);

            uint id = node.NextId();

            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var cts = new CancellationTokenSource(timeoutMs);
            using var reg = cts.Token.Register(() =>
            {
                if (node._pending.TryRemove(id, out _))
                {
                    tcs.TrySetException(new TimeoutException($"RPC call to {target}.{method} timed out after {timeoutMs}ms."));
                }
            });

            node._pending[id] = tcs;

            try
            {
                await node.SendFrameAsync(MsgType.Call, id, target, method, data);
                JsonElement resultRaw = await tcs.Task;
                return JsonSerializer.Deserialize<TResponse>(resultRaw);
            }
            catch (Exception) when (node._pending.TryRemove(id, out _))
            {
                // Clean up pending entry on any exception
                throw;
            }
        }

        /// <summary>
        /// Performs a synchronous RPC call without a request payload.
        /// </summary>
        public async Task<TResponse?> CallAsync<TResponse>(
            string target,
            string method,
            int timeoutMs = 5000)
        {
            return await node.CallAsync<object, TResponse>(target, method, new { }, timeoutMs);
        }

        /// <summary>
        /// Sends a fire-and-forget message (no response expected).
        /// </summary>
        public void Cast<TRequest>(string target, string method, TRequest data)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(method);

            _ = node.SendFrameAsync(MsgType.Cast, 0, target, method, data);
        }

        /// <summary>
        /// Sends a fire-and-forget message without a payload.
        /// </summary>
        public void Cast(string target, string method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);
            ArgumentException.ThrowIfNullOrWhiteSpace(method);

            _ = node.SendFrameAsync(MsgType.Cast, 0, target, method, new { });
        }

        /// <summary>
        /// Discovers available actions on a target actor.
        /// </summary>
        public async Task<string[]> DiscoverAsync(string target, int timeoutMs = 5000)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);

            var result = await node.CallAsync<Dictionary<string, string[]>>(target, "describe", timeoutMs);

            if (result?.TryGetValue("actions", out var actions) == true && actions != null)
            {
                return actions;
            }
            return [];
        }
    }
}