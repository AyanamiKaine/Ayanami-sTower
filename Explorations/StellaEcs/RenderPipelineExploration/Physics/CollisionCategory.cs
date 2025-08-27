namespace StellaInvicta.Physics;

/// <summary>
/// Named collision categories. Use bitwise OR to combine categories.
/// </summary>
[Flags]
public enum CollisionCategory : uint
{
    /// <summary>
    /// No collision.
    /// </summary>
    None = 0,
    /// <summary>
    /// Default category for general objects.
    /// </summary>
    Default = 1u << 0,
    /// <summary>
    /// Category for the sun.
    /// </summary>
    Sun = 1u << 1,
    /// <summary>
    /// Category for asteroids.
    /// </summary>
    Asteroid = 1u << 2,
    /// <summary>
    /// Category for all entities
    /// </summary>
    All = 0xffffffffu
}

