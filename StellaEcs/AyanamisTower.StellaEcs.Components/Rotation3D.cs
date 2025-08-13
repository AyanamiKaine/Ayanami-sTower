using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.Components;

/// <summary>
/// A basic wrapper around a quaternion type, exposing the quaternion.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Rotation3D : IEquatable<Rotation3D>
{
    /// <summary>
    /// The quaternion representing the rotation.
    /// </summary>
    public Quaternion Value;
    /// <summary>
    /// Gets the identity rotation (no rotation).
    /// </summary>
    public static Rotation3D Identity => new() { Value = Quaternion.Identity };
    /// <summary>
    /// Creates a rotation from an axis and angle.
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="radians"></param>
    /// <returns></returns>
    public static Rotation3D FromAxisAngle(Vector3 axis, float radians)
        => new() { Value = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), radians) };
    /// <summary>
    /// Creates a rotation from Euler angles (pitch, yaw, roll).
    /// </summary>
    /// <param name="pitch"></param>
    /// <param name="yaw"></param>
    /// <param name="roll"></param>
    /// <returns></returns>
    public static Rotation3D FromEulerRadians(float pitch, float yaw, float roll)
        => new() { Value = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll) };
    /// <summary>
    /// Normalizes the rotation.
    /// </summary>
    public void Normalize() => Value = Quaternion.Normalize(Value);
    /// <summary>
    /// Checks if this rotation is equal to another rotation.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public readonly bool Equals(Rotation3D other) => Value.Equals(other.Value);
    /// <summary>
    /// Checks if this rotation is equal to another rotation.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override readonly bool Equals(object? obj) => obj is Rotation3D r && Equals(r);
    /// <summary>
    /// Gets the hash code for this rotation.
    /// </summary>
    /// <returns></returns>
    public override readonly int GetHashCode() => Value.GetHashCode();
    /// <summary>
    /// Checks if two rotations are equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Rotation3D left, Rotation3D right)
    {
        return left.Equals(right);
    }
    /// <summary>
    /// Checks if two rotations are not equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Rotation3D left, Rotation3D right)
    {
        return !(left == right);
    }
}
