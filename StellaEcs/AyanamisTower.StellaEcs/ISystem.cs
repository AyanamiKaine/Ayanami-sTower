using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// Represents a system that processes entities with a specific set of components.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Name of the system, used for identification and debugging.
    /// </summary>
    public string Name { get; set; } 
    /// <summary>
    /// Gets or sets a value indicating whether the system is enabled.
    /// /// If false, the system will be skipped during updates.
    /// </summary>
    public bool Enabled { get; set; }

  /// <summary>
    /// A list of system Names that this system depends on.
    /// The framework will ensure this system runs *after* all of its dependencies.
    /// </summary>
    public List<string> Dependencies { get; }

    /// <summary>
    /// This method is called once per frame to update the state of the world.
    /// </summary>
    /// <param name="world">A reference to the main world.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    void Update(World world, float deltaTime);
}
