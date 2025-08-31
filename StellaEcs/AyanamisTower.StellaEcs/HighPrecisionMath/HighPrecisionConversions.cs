using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.HighPrecisionMath;

/// <summary>
/// Utilities to convert high-precision math types (double-based) into
/// float-based System.Numerics types used by physics/rendering systems.
/// All conversions are "safe" in the sense that they clamp infinities/overflows
/// and centralize the subtraction of a floating origin before narrowing.
/// </summary>
public static class HighPrecisionConversions
{
    /// <summary>Safely converts a double to float, clamping infinities and extremes.</summary>
    public static float ToFloatSafe(double v)
    {
        if (double.IsNaN(v) || double.IsInfinity(v))
            return 0f;
        if (v > float.MaxValue) return float.MaxValue;
        if (v < float.MinValue) return float.MinValue;
        // Avoid producing tiny denormal/subnormal floats from numerical noise.
        // Treat very small magnitudes as zero to keep renderer/shader constants clean.
        if (Math.Abs(v) < 1e-6) return 0f;
        return (float)v;
    }

    /// <summary>Convert a Vector3Double to System.Numerics.Vector3 (float) with safe casts.</summary>
    public static Vector3 ToVector3(Vector3Double v) => new Vector3(ToFloatSafe(v.X), ToFloatSafe(v.Y), ToFloatSafe(v.Z));

    /// <summary>Convert a QuaternionDouble to System.Numerics.Quaternion (float). Normalizes if possible.</summary>
    public static Quaternion ToQuaternion(QuaternionDouble q)
    {
        double lenSq = q.LengthSquared();
        if (lenSq <= double.Epsilon) return Quaternion.Identity;
        double invLen = 1.0 / Math.Sqrt(lenSq);
        return new Quaternion(ToFloatSafe(q.X * invLen), ToFloatSafe(q.Y * invLen), ToFloatSafe(q.Z * invLen), ToFloatSafe(q.W * invLen));
    }

    /// <summary>Convert a Matrix4x4Double to System.Numerics.Matrix4x4 via safe casts.</summary>
    public static Matrix4x4 ToMatrix(Matrix4x4Double m)
    {
        return new Matrix4x4(
            ToFloatSafe(m.M11), ToFloatSafe(m.M12), ToFloatSafe(m.M13), ToFloatSafe(m.M14),
            ToFloatSafe(m.M21), ToFloatSafe(m.M22), ToFloatSafe(m.M23), ToFloatSafe(m.M24),
            ToFloatSafe(m.M31), ToFloatSafe(m.M32), ToFloatSafe(m.M33), ToFloatSafe(m.M34),
            ToFloatSafe(m.M41), ToFloatSafe(m.M42), ToFloatSafe(m.M43), ToFloatSafe(m.M44)
        );
    }

    /// <summary>
    /// Convert an absolute double-precision position to a relative float Vector3
    /// by subtracting the current floating origin (in doubles) then narrowing.
    /// Always use this before handing positions to float-based systems (physics, render, etc.).
    /// </summary>
    public static Vector3 ToRelativeVector3(Vector3Double absolutePosition, Vector3Double origin)
    {
        var relX = absolutePosition.X - origin.X;
        var relY = absolutePosition.Y - origin.Y;
        var relZ = absolutePosition.Z - origin.Z;
        return new Vector3(ToFloatSafe(relX), ToFloatSafe(relY), ToFloatSafe(relZ));
    }

    /// <summary>
    /// Convert a relative float Vector3 back to an absolute Vector3Double by adding the double-precision origin.
    /// </summary>
    public static Vector3Double ToAbsoluteVector3(Vector3 relativePosition, Vector3Double origin)
    {
        return new Vector3Double(relativePosition.X + origin.X, relativePosition.Y + origin.Y, relativePosition.Z + origin.Z);
    }
}
