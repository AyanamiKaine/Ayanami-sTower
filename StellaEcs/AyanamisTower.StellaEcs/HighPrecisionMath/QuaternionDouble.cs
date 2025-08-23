using System;
using System.Diagnostics.CodeAnalysis;

namespace AyanamisTower.StellaEcs.HighPrecisionMath;

/// <summary>Represents a vector that is used to encode three-dimensional physical rotations using double-precision floating-point numbers.</summary>
public struct QuaternionDouble : IEquatable<QuaternionDouble>
{
    /// <summary>
    /// The X component of the quaternion.
    /// </summary>
    public double X;
    /// <summary>
    /// The Y component of the quaternion.
    /// </summary>
    public double Y;
    /// <summary>
    /// The Z component of the quaternion.
    /// </summary>
    public double Z;
    /// <summary>
    /// The W component of the quaternion.
    /// </summary>
    public double W;

    /// <inheritdoc/>
    public static QuaternionDouble Identity => new(0, 0, 0, 1);
    /// <inheritdoc/>
    public readonly bool IsIdentity => Equals(Identity);

    /// <inheritdoc/>
    public QuaternionDouble(double x, double y, double z, double w)
    {
        X = x; Y = y; Z = z; W = w;
    }

    /// <inheritdoc/>
    public QuaternionDouble(Vector3Double vectorPart, double scalarPart)
    {
        X = vectorPart.X; Y = vectorPart.Y; Z = vectorPart.Z; W = scalarPart;
    }

    /// <inheritdoc/>
    public static QuaternionDouble operator +(QuaternionDouble value1, QuaternionDouble value2) => new QuaternionDouble(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z, value1.W + value2.W);

    /// <inheritdoc/>
    public static QuaternionDouble operator /(QuaternionDouble value1, QuaternionDouble value2) => value1 * Inverse(value2);

    /// <inheritdoc/>
    public static bool operator ==(QuaternionDouble value1, QuaternionDouble value2) => value1.Equals(value2);

    /// <inheritdoc/>
    public static bool operator !=(QuaternionDouble value1, QuaternionDouble value2) => !value1.Equals(value2);

    /// <inheritdoc/>
    public static QuaternionDouble operator *(QuaternionDouble value1, QuaternionDouble value2)
    {
        return new QuaternionDouble(
            (value1.W * value2.X) + (value1.X * value2.W) + (value1.Y * value2.Z) - (value1.Z * value2.Y),
            (value1.W * value2.Y) - (value1.X * value2.Z) + (value1.Y * value2.W) + (value1.Z * value2.X),
            (value1.W * value2.Z) + (value1.X * value2.Y) - (value1.Y * value2.X) + (value1.Z * value2.W),
            (value1.W * value2.W) - (value1.X * value2.X) - (value1.Y * value2.Y) - (value1.Z * value2.Z)
        );
    }

    /// <inheritdoc/>
    public static QuaternionDouble operator *(QuaternionDouble value1, double value2) => new QuaternionDouble(value1.X * value2, value1.Y * value2, value1.Z * value2, value1.W * value2);

    /// <inheritdoc/>
    public static QuaternionDouble operator -(QuaternionDouble value1, QuaternionDouble value2) => new QuaternionDouble(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z, value1.W - value2.W);

    /// <inheritdoc/>
    public static QuaternionDouble operator -(QuaternionDouble value) => new QuaternionDouble(-value.X, -value.Y, -value.Z, -value.W);

    /// <inheritdoc/>
    public static QuaternionDouble Conjugate(QuaternionDouble value) => new QuaternionDouble(-value.X, -value.Y, -value.Z, value.W);

    /// <inheritdoc/>
    public static QuaternionDouble Inverse(QuaternionDouble value)
    {
        double ls = value.LengthSquared();
        if (ls <= 1.192092896e-7)
        {
            return Identity;
        }
        return Conjugate(value) * (1.0 / ls);
    }

    /// <inheritdoc/>
    public static double Dot(QuaternionDouble quaternion1, QuaternionDouble quaternion2) => (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);

    /// <inheritdoc/>
    public readonly double Length() => Math.Sqrt(LengthSquared());

    /// <inheritdoc/>
    public readonly double LengthSquared() => Dot(this, this);

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is QuaternionDouble other) && Equals(other);

    /// <inheritdoc/>
    public readonly bool Equals(QuaternionDouble other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <inheritdoc/>
    public override readonly string ToString() => $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";
}

