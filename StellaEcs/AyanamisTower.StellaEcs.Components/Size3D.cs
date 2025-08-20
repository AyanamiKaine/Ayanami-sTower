using System.Numerics;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// Scale for 3D transforms. Defaults to (1,1,1). Similar to Transform scale in common engines.
/// </summary>
public struct Size3D
{
    /// <summary>
    /// Creates a new instance of the <see cref="Size3D"/> struct.
    /// </summary>
    /// <param name="size"></param>
    public Size3D(Vector3 size)
    {
        Value = size;
    }
    /// <summary>
    /// Creates a new instance of the <see cref="Size3D"/> struct.
    /// </summary>
    /// <param name="size"></param>
    public Size3D(float size)
    {
        Value = new Vector3(size, size, size);
    }
    /// <summary>
    /// Creates a new instance of the <see cref="Size3D"/> struct.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Size3D(float x, float y, float z)
    {
        Value = new Vector3(x, y, z);
    }

    /// <summary>
    /// The non-uniform scale vector. Use uniform values for best lighting with basic shaders.
    /// </summary>
    public Vector3 Value = Vector3.One;

    /// <summary>
    /// Identity scale.
    /// </summary>
    public static Size3D One => new(1, 1, 1);
}
