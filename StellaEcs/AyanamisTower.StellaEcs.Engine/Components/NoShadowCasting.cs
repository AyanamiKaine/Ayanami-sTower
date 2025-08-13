namespace AyanamisTower.StellaEcs.Engine.Components;

/// <summary>
/// Component that marks an entity as not casting shadows.
/// Objects with this component will be rendered but won't contribute to shadow maps.
/// </summary>
public readonly struct NoShadowCasting
{
    // Empty component - presence indicates the entity should not cast shadows
}
