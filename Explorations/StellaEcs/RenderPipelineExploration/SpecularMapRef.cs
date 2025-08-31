using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Component that references a GPU texture to use as a specular map when rendering an entity.
/// </summary>
public struct SpecularMapRef
{
    /// <summary>
    /// The specular texture to use when rendering the entity.
    /// </summary>
    public Texture Texture;
}
