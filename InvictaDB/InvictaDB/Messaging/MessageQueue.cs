using System.Collections.Immutable;

namespace InvictaDB.Messaging;

/// <summary>
/// Represents a message that can be sent between systems.
/// </summary>
/// <param name="MessageType">The type/category of the message.</param>
/// <param name="Sender">The name of the system that sent the message.</param>
/// <param name="Payload">The message payload data.</param>
/// <param name="Timestamp">When the message was created.</param>
public record GameMessage(
    string MessageType,
    string Sender,
    object? Payload,
    long Timestamp)
{
    /// <summary>
    /// Creates a new message with the current timestamp.
    /// </summary>
    public static GameMessage Create(string messageType, string sender, object? payload = null) =>
        new(messageType, sender, payload, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Creates a new typed message with the current timestamp.
    /// </summary>
    public static GameMessage Create<T>(string sender, T payload) =>
        new(typeof(T).Name, sender, payload, DateTimeOffset.UtcNow.Ticks);

    /// <summary>
    /// Gets the payload as the specified type.
    /// </summary>
    public T? GetPayload<T>() => Payload is T typed ? typed : default;

    /// <summary>
    /// Checks if this message matches the specified type.
    /// </summary>
    public bool IsType(string messageType) => MessageType == messageType;

    /// <summary>
    /// Checks if this message matches the specified type.
    /// </summary>
    public bool IsType<T>() => MessageType == typeof(T).Name;
}

/// <summary>
/// Immutable message queue for inter-system communication.
/// </summary>
public class MessageQueue
{
    /// <summary>
    /// Empty message queue.
    /// </summary>
    public static readonly MessageQueue Empty = new(ImmutableQueue<GameMessage>.Empty);

    private readonly ImmutableQueue<GameMessage> _messages;

    private MessageQueue(ImmutableQueue<GameMessage> messages)
    {
        _messages = messages;
    }

    /// <summary>
    /// Gets all messages in the queue.
    /// </summary>
    public IEnumerable<GameMessage> Messages => _messages;

    /// <summary>
    /// Gets the number of messages in the queue.
    /// </summary>
    public int Count => _messages.Count();

    /// <summary>
    /// Checks if the queue is empty.
    /// </summary>
    public bool IsEmpty => _messages.IsEmpty;

    /// <summary>
    /// Enqueues a message, returning a new queue.
    /// </summary>
    public MessageQueue Enqueue(GameMessage message) =>
        new(_messages.Enqueue(message));

    /// <summary>
    /// Enqueues multiple messages, returning a new queue.
    /// </summary>
    public MessageQueue EnqueueRange(IEnumerable<GameMessage> messages)
    {
        var queue = _messages;
        foreach (var message in messages)
        {
            queue = queue.Enqueue(message);
        }
        return new MessageQueue(queue);
    }

    /// <summary>
    /// Dequeues a message, returning the message and a new queue.
    /// </summary>
    public (GameMessage? Message, MessageQueue Queue) Dequeue()
    {
        if (_messages.IsEmpty)
        {
            return (null, this);
        }
        return (_messages.Peek(), new MessageQueue(_messages.Dequeue()));
    }

    /// <summary>
    /// Peeks at the next message without removing it.
    /// </summary>
    public GameMessage? Peek() =>
        _messages.IsEmpty ? null : _messages.Peek();

    /// <summary>
    /// Clears all messages, returning an empty queue.
    /// </summary>
    public MessageQueue Clear() => Empty;

    /// <summary>
    /// Gets all messages of a specific type.
    /// </summary>
    public IEnumerable<GameMessage> GetMessages(string messageType) =>
        _messages.Where(m => m.MessageType == messageType);

    /// <summary>
    /// Gets all messages of a specific type.
    /// </summary>
    public IEnumerable<GameMessage> GetMessages<T>() =>
        _messages.Where(m => m.IsType<T>());

    /// <summary>
    /// Gets all messages from a specific sender.
    /// </summary>
    public IEnumerable<GameMessage> GetMessagesFrom(string sender) =>
        _messages.Where(m => m.Sender == sender);

    /// <summary>
    /// Removes all messages of a specific type, returning a new queue.
    /// </summary>
    public MessageQueue RemoveMessages(string messageType)
    {
        var remaining = _messages.Where(m => m.MessageType != messageType);
        var queue = ImmutableQueue<GameMessage>.Empty;
        foreach (var msg in remaining)
        {
            queue = queue.Enqueue(msg);
        }
        return new MessageQueue(queue);
    }

    /// <summary>
    /// Removes all messages of a specific type, returning a new queue.
    /// </summary>
    public MessageQueue RemoveMessages<T>() => RemoveMessages(typeof(T).Name);

    /// <summary>
    /// Consumes all messages of a specific type, returning them and a new queue without those messages.
    /// </summary>
    public (IReadOnlyList<GameMessage> Consumed, MessageQueue Queue) ConsumeMessages(string messageType)
    {
        var consumed = _messages.Where(m => m.MessageType == messageType).ToList();
        var remaining = _messages.Where(m => m.MessageType != messageType);
        var queue = ImmutableQueue<GameMessage>.Empty;
        foreach (var msg in remaining)
        {
            queue = queue.Enqueue(msg);
        }
        return (consumed, new MessageQueue(queue));
    }

    /// <summary>
    /// Consumes all messages of a specific type, returning them and a new queue without those messages.
    /// </summary>
    public (IReadOnlyList<GameMessage> Consumed, MessageQueue Queue) ConsumeMessages<T>() =>
        ConsumeMessages(typeof(T).Name);
}
