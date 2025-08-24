using BepuPhysics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;

/// <summary>
/// Stores a Bepu body handle for entities that have a physics body.
/// </summary>
public struct PhysicsBody
{
    /// <summary>
    /// Handle to the kinematic/dynamic body in the Bepu simulation.
    /// </summary>
    public BodyHandle Handle;
}
