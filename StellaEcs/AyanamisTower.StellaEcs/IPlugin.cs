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
    /// A short, unique prefix used for naming systems and services provided by this plugin.
    /// E.g., "Core" for the Core plugin. This will be used as the plugin's unique identifier.
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// Gets a collection of all system types this plugin provides.
    /// </summary>
    IEnumerable<Type> ProvidedSystems { get; }

    /// <summary>
    /// Gets a collection of all service types this plugin provides.
    /// </summary>
    IEnumerable<Type> ProvidedServices { get; }

    /// <summary>
    /// Gets a collection of all component types this plugin provides or primarily operates on.
    /// </summary>
    IEnumerable<Type> ProvidedComponents { get; }

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
