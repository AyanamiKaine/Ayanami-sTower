using System;

namespace StellaInvicta.Components;

/// <summary>
/// Per-entity selection rules. Allows overriding the maximum camera distance at which the entity can be selected.
/// </summary>
public struct SelectionRules
{
    /// <summary>
    /// If true, use <see cref="MaxSelectionDistance"/> instead of the global selection max distance.
    /// </summary>
    public bool OverrideMaxDistance;

    /// <summary>
    /// Maximum camera distance (world units) at which this entity can be selected. 0 = unlimited.
    /// </summary>
    public float MaxSelectionDistance;

    /// <summary>
    /// Initializes defaults. No override by default.
    /// </summary>
    public SelectionRules()
    {
        OverrideMaxDistance = false;
        MaxSelectionDistance = 0f;
    }
}
