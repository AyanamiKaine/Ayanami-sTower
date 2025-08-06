using System;

namespace AyanamisTower.StellaEcs;

/*
Idea used by https://github.com/MoonsideGames/MoonTools.ECS
CHECK IT OUT!
*/

/// <summary>
/// A generic message bus that collects and provides access to
/// messages of a specific type for a single frame/tick.
/// </summary>
/// <typeparam name="T">The type of the message, which should be a struct.</typeparam>
public class MessageBus<T> : IMessageBus where T : struct
{
    private readonly List<T> _messages = [];

    /// <summary>
    /// Publishes a new message to the bus. Other systems can read it this frame.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    public void Publish(T message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Gets a read-only list of all messages published this frame.
    /// </summary>
    /// <returns>A read-only list of messages of type <typeparamref name="T"/>.</returns>
    public IReadOnlyList<T> GetMessages()
    {
        return _messages;
    }

    /// <summary>
    /// Clears all messages. This is typically called by the World at the end of an update cycle.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }
}
