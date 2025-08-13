using AyanamisTower.StellaEcs.Engine.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Components;

/// <summary>
/// Deprecated: use Mesh3D + RenderLit3D tag instead.
/// </summary>
[System.Obsolete("Use Mesh3D + RenderLit3D instead.")]
public struct RenderMesh3DLit
{
    /// <summary>
    /// The mesh to render. The mesh should use the Vertex3DLit layout.
    /// </summary>
    public Mesh Mesh;
}
