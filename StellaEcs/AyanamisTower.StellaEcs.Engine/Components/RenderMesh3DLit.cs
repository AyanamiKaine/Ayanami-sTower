using AyanamisTower.StellaEcs.Engine.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Components;

/// <summary>
/// ECS component that references a lit 3D mesh to be rendered by the engine.
/// </summary>
public struct RenderMesh3DLit
{
    /// <summary>
    /// The mesh to render. The mesh should use the Vertex3DLit layout.
    /// </summary>
    public Mesh Mesh;
}
