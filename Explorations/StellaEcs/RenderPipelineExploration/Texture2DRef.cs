using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Component that references a GPU texture to use when rendering an entity.
/// </summary>
public struct Texture2DRef
{
    /// <summary>
    /// The texture to use when rendering the entity.
    /// </summary>
    public Texture Texture;
}
