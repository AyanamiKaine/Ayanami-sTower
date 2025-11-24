using System.Collections.Concurrent;
using System.Text.Json;
using NanoRpc.Protocol;

namespace NanoRpc.Core;

/// <summary>
/// Represents a subscription to a topic.
/// </summary>
public class NanoSubscription : IAsyncDisposable
{
    private readonly NanoPubSubClient _client;
    private readonly string _topic;
    private readonly Action<string, JsonElement> _handler;
    private bool _disposed;

    public string Topic => _topic;
    public bool IsActive => !_disposed;

    internal NanoSubscription(NanoPubSubClient client, string topic, Action<string, JsonElement> handler)
    {
        _client = client;
        _topic = topic;
        _handler = handler;
    }

    internal void Invoke(string topic, JsonElement data) => _handler(topic, data);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _client.UnsubscribeAsync(_topic, this);
    }
}

/// <summary>
/// Client-side Pub/Sub functionality for NanoNode.
/// </summary>
public class NanoPubSubClient
{
    private readonly NanoNode _node;
    private readonly ConcurrentDictionary<string, ConcurrentBag<NanoSubscription>> _subscriptions = new();

    public NanoPubSubClient(NanoNode node)
    {
        _node = node;
    }

    /// <summary>
    /// Subscribes to a topic with a typed handler.
    /// </summary>
    public async Task<NanoSubscription> SubscribeAsync<T>(string topic, Action<string, T?> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new NanoSubscription(this, topic, (t, json) =>
        {
            var data = JsonSerializer.Deserialize<T>(json);
            handler(t, data);
        });

        var subs = _subscriptions.GetOrAdd(topic, _ => []);
        subs.Add(subscription);

        // Send subscribe message to server
        await _node.SendFrameAsync(MsgType.Subscribe, 0, topic, "", new { });

        return subscription;
    }

    /// <summary>
    /// Subscribes to a topic with raw JsonElement handler.
    /// </summary>
    public async Task<NanoSubscription> SubscribeAsync(string topic, Action<string, JsonElement> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new NanoSubscription(this, topic, handler);

        var subs = _subscriptions.GetOrAdd(topic, _ => []);
        subs.Add(subscription);

        await _node.SendFrameAsync(MsgType.Subscribe, 0, topic, "", new { });

        return subscription;
    }

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    public async Task PublishAsync<T>(string topic, T data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        await _node.SendFrameAsync(MsgType.Publish, 0, topic, "", data);
    }

    internal async Task UnsubscribeAsync(string topic, NanoSubscription subscription)
    {
        if (_subscriptions.TryGetValue(topic, out var subs))
        {
            // Remove subscription from bag (ConcurrentBag doesn't support removal, so we rebuild)
            var remaining = subs.Where(s => s != subscription).ToList();
            _subscriptions[topic] = new ConcurrentBag<NanoSubscription>(remaining);

            // If no more subscriptions for this topic, unsubscribe from server
            if (remaining.Count == 0)
            {
                _subscriptions.TryRemove(topic, out _);
                await _node.SendFrameAsync(MsgType.Unsubscribe, 0, topic, "", new { });
            }
        }
    }

    /// <summary>
    /// Called by NanoNode when a Publish message is received.
    /// </summary>
    internal void HandlePublish(string topic, JsonElement data)
    {
        if (_subscriptions.TryGetValue(topic, out var subs))
        {
            foreach (var sub in subs.Where(s => s.IsActive))
            {
                try
                {
                    sub.Invoke(topic, data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PubSub] Error in subscription handler for '{topic}': {ex.Message}");
                }
            }
        }
    }
}

/// <summary>
/// Server-side Pub/Sub broker for managing topics and subscribers.
/// </summary>
public class NanoPubSubBroker
{
    private readonly NanoServer _server;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, NanoServer.ClientConnection>> _topics = new();

    public event Action<string, string>? ClientSubscribed;   // (topic, clientId)
    public event Action<string, string>? ClientUnsubscribed; // (topic, clientId)
    public event Action<string, int>? MessagePublished;      // (topic, subscriberCount)

    public NanoPubSubBroker(NanoServer server)
    {
        _server = server;
    }

    /// <summary>
    /// Subscribes a client to a topic.
    /// </summary>
    internal void Subscribe(string clientId, string topic, NanoServer.ClientConnection connection)
    {
        var subscribers = _topics.GetOrAdd(topic, _ => new ConcurrentDictionary<string, NanoServer.ClientConnection>());

        if (subscribers.TryAdd(clientId, connection))
        {
            Console.WriteLine($"[PubSub] Client '{clientId}' subscribed to '{topic}'");
            ClientSubscribed?.Invoke(topic, clientId);
        }
    }

    /// <summary>
    /// Unsubscribes a client from a topic.
    /// </summary>
    internal void Unsubscribe(string clientId, string topic)
    {
        if (_topics.TryGetValue(topic, out var subscribers))
        {
            if (subscribers.TryRemove(clientId, out _))
            {
                Console.WriteLine($"[PubSub] Client '{clientId}' unsubscribed from '{topic}'");
                ClientUnsubscribed?.Invoke(topic, clientId);

                // Clean up empty topics
                if (subscribers.IsEmpty)
                {
                    _topics.TryRemove(topic, out _);
                }
            }
        }
    }

    /// <summary>
    /// Unsubscribes a client from all topics (called on disconnect).
    /// </summary>
    internal void UnsubscribeAll(string clientId)
    {
        foreach (var (topic, subscribers) in _topics)
        {
            if (subscribers.TryRemove(clientId, out _))
            {
                Console.WriteLine($"[PubSub] Client '{clientId}' unsubscribed from '{topic}' (disconnected)");
                ClientUnsubscribed?.Invoke(topic, clientId);
            }
        }
    }

    /// <summary>
    /// Publishes a message to all subscribers of a topic.
    /// </summary>
    public async Task PublishAsync<T>(string topic, T data)
    {
        if (!_topics.TryGetValue(topic, out var subscribers))
        {
            return; // No subscribers
        }

        var tasks = new List<Task>();
        int count = 0;

        foreach (var (clientId, connection) in subscribers)
        {
            try
            {
                tasks.Add(connection.SendFrameAsync(MsgType.Publish, 0, topic, "", data));
                count++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PubSub] Failed to publish to client '{clientId}': {ex.Message}");
                // Remove failed client
                subscribers.TryRemove(clientId, out _);
            }
        }

        await Task.WhenAll(tasks);
        MessagePublished?.Invoke(topic, count);
    }

    /// <summary>
    /// Handles an incoming publish from a client and broadcasts to other subscribers.
    /// </summary>
    internal async Task HandleClientPublishAsync<T>(string sourceClientId, string topic, T data)
    {
        if (!_topics.TryGetValue(topic, out var subscribers))
        {
            return;
        }

        var tasks = new List<Task>();

        foreach (var (clientId, connection) in subscribers)
        {
            // Optionally skip the source client (set to false to echo back)
            if (clientId == sourceClientId) continue;

            try
            {
                tasks.Add(connection.SendFrameAsync(MsgType.Publish, 0, topic, "", data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PubSub] Failed to publish to client '{clientId}': {ex.Message}");
                subscribers.TryRemove(clientId, out _);
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets all active topics.
    /// </summary>
    public IEnumerable<string> GetTopics() => _topics.Keys;

    /// <summary>
    /// Gets subscriber count for a topic.
    /// </summary>
    public int GetSubscriberCount(string topic) =>
        _topics.TryGetValue(topic, out var subs) ? subs.Count : 0;
}

/// <summary>
/// Extension methods for Pub/Sub support.
/// </summary>
public static class NanoPubSubExtensions
{
    extension(NanoNode node)
    {
        /// <summary>
        /// Gets the Pub/Sub client for this node.
        /// </summary>
        public NanoPubSubClient PubSub => node.GetOrCreatePubSubClient();
    }
}