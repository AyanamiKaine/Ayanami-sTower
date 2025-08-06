using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a system that processes entities with a specific set of components.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Gets or sets a value indicating whether the system is enabled.
    /// /// If false, the system will be skipped during updates.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// This method is called once per frame to update the state of the world.
    /// </summary>
    /// <param name="world">A reference to the main world.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    void Update(World world, float deltaTime);
}
