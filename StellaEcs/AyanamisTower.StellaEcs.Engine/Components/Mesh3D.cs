using AyanamisTower.StellaEcs.Engine.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Components;

/// <summary>
/// ECS component holding a reference to a 3D mesh resource.
/// Combine with transform components (e.g., Position3D/Rotation3D) and a render tag (e.g., RenderLit3D) to draw.
/// </summary>
public struct Mesh3D
{
    /// <summary>
    /// The 3D mesh resource.
    /// </summary>
    public Mesh Mesh;
}
