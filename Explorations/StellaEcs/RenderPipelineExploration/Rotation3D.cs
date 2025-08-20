using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.StellaInvicta;


/// <summary>
/// Represents a 3D rotation in space.
/// </summary>
public struct Rotation3D
{
    /// <summary>
    /// Gets or sets the value of the 3D rotation.
    /// </summary>
    public Quaternion Value { get; set; }
}
