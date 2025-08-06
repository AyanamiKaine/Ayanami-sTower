using System;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// The contract for an external plugin. A plugin's job is to register its
/// systems and components with the main world.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Gets the public name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin (e.g., "1.0.0").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the author of the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets a brief description of what the plugin does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Called by the main application to let the plugin register its systems.
    /// </summary>
    /// <param name="world">The main world instance.</param>
    void Initialize(World world);
    /// <summary>
    /// Called by the plugin loader before unloading. The plugin must
    /// unregister all its systems and other resources from the world.
    /// </summary>
    /// <param name="world">The main world instance.</param>
    void Uninitialize(World world);
}
