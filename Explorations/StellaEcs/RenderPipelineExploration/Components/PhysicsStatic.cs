using System;
using BepuPhysics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;

/// <summary>
/// Stores a Bepu static handle for entities that use static colliders.
/// </summary>
public struct PhysicsStatic
{
    /// <summary>
    /// Handle to the static object in the Bepu simulation.
    /// </summary>
    public StaticHandle Handle;
}