using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Scale for 3D transforms. Defaults to (1,1,1). Similar to Transform scale in common engines.
/// </summary>
public struct Size3D(float X = 1, float Y = 1, float Z = 1)
{
    /// <summary>
    /// The non-uniform scale vector. Use uniform values for best lighting with basic shaders.
    /// </summary>
    public Vector3 Value = new(X, Y, Z);

    /// <summary>
    /// Identity scale.
    /// </summary>
    public static Size3D One => new(1, 1, 1);
}
