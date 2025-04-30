using System;
using Flecs.NET.Core;

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents the core interface for a game mod, providing both
/// metadata and the Flecs module loading entry point.
/// </summary>
public interface IMod : IFlecsModule // Inherit from IFlecsModule to ensure Load(World) exists
{
    /// <summary>
    /// Gets the human-readable name of the mod.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the mod's author or development team.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the version string of the mod (e.g., "1.0.0", "0.2-alpha").
    /// Consider using System.Version if strict semantic versioning is needed.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a brief description of what the mod does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets a list of dependencies required by this mod, including specific versions.
    /// The ModLoader should ensure these are loaded first and versions are compatible.
    /// Return an empty list or null if there are no dependencies.
    /// </summary>
    IReadOnlyList<ModDependency> Dependencies { get; }

    /// <summary>
    /// Gets the desired loading priority for this mod. Lower numbers
    /// are typically loaded earlier. Mods with the same priority
    /// may load in an undefined order relative to each other.
    /// Defaults to 0 if not specified otherwise.
    /// </summary>
    int LoadPriority { get; } // Default could be 0

    /// <summary>
    /// Gets a list of tags or categories for this mod (e.g., "Gameplay", "UI").
    /// Useful for filtering or organization.
    /// Return an empty list or null if no tags apply.
    /// </summary>
    IReadOnlyList<string> Tags { get; }
}
