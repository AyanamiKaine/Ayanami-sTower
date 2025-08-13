using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Engine.Components;

/// <summary>
/// ECS component holding a reference to a 2D texture resource.
/// Combine with Mesh3D and RenderTexturedLit3D to draw textured meshes.
/// </summary>
public struct Texture2DRef
{
    /// <summary>
    /// The texture resource to sample in the fragment shader.
    /// </summary>
    public Texture Texture;
}
