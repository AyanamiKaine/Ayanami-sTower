using System;
using AyanamisTower.Utilities.Aspects;

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents a dependency on another mod, including required version information.
/// </summary>
[PrettyPrint]
public class ModDependency
{
    /// <summary>
    /// Gets the unique name or identifier of the required mod.
    /// This should match the Name property of the dependency mod's IMod implementation.
    /// </summary>
    public string ModName { get; }

    /// <summary>
    /// Gets the required version string.
    /// This could be an exact version (e.g., "1.0.0"), a minimum version
    /// (e.g., ">=1.2.0"), or follow semantic versioning ranges (e.g., "~1.3").
    /// </summary>
    public string RequiredVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModDependency"/> class.
    /// </summary>
    /// <param name="modName">The unique name of the required mod.</param>
    /// <param name="requiredVersion">The required version string.</param>
    public ModDependency(string modName, string requiredVersion)
    {
        // Basic validation (can be enhanced)
        if (string.IsNullOrWhiteSpace(modName))
            throw new ArgumentException("Mod name cannot be empty.", nameof(modName));
        if (string.IsNullOrWhiteSpace(requiredVersion)) // Or validate version format
        {
            throw new ArgumentException(
                "Required version cannot be empty.",
                nameof(requiredVersion)
            );
        }

        ModName = modName;
        RequiredVersion = requiredVersion;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{ModName} (Version: {RequiredVersion})";
    }
}
