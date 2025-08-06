using System;

namespace AyanamisTower.StellaEcs;


/*
Idea used by https://github.com/MoonsideGames/MoonTools.ECS
CHECK IT OUT!
*/

/// <summary>
/// Non-generic interface for a message bus, allowing different
/// message types to be managed in a single collection by the World.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Clears all messages from the bus. This should be called by the World
    /// at the end of each update cycle.
    /// </summary>
    void Clear();
}