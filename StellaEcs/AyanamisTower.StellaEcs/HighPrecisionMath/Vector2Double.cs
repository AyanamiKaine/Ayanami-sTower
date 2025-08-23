
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.HighPrecisionMath;

/// <summary>Represents a vector with two double-precision floating-point values.</summary>
public struct Vector2Double : IEquatable<Vector2Double>, IFormattable
{
    /// <summary>The X component of the vector.</summary>
    public double X;

    /// <summary>The Y component of the vector.</summary>
    public double Y;

    internal const int Count = 2;

    /// <summary>Creates a new <see cref="Vector2Double" /> with both components set to the same value.</summary>
    public Vector2Double(double value)
    {
        X = value;
        Y = value;
    }

    /// <summary>Creates a new <see cref="Vector2Double" /> with specified components.</summary>
    public Vector2Double(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Gets a vector whose elements are all one.</summary>
    public static Vector2Double One => new Vector2Double(1.0);

    /// <summary>Gets the vector (1,0).</summary>
    public static Vector2Double UnitX => new Vector2Double(1.0, 0.0);

    /// <summary>Gets the vector (0,1).</summary>
    public static Vector2Double UnitY => new Vector2Double(0.0, 1.0);

    /// <summary>Gets a vector whose elements are all zero.</summary>
    public static Vector2Double Zero => new Vector2Double(0.0);

    /// <summary>Indexer for X/Y components.</summary>
    public double this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Adds two vectors elementwise.</summary>
    public static Vector2Double operator +(Vector2Double a, Vector2Double b) => new Vector2Double(a.X + b.X, a.Y + b.Y);

    /// <summary>Subtracts the second vector from the first elementwise.</summary>
    public static Vector2Double operator -(Vector2Double a, Vector2Double b) => new Vector2Double(a.X - b.X, a.Y - b.Y);

    /// <summary>Negates the vector.</summary>
    public static Vector2Double operator -(Vector2Double v) => new Vector2Double(-v.X, -v.Y);

    /// <summary>Multiplies the vector by a scalar.</summary>
    public static Vector2Double operator *(Vector2Double a, double s) => new Vector2Double(a.X * s, a.Y * s);

    /// <summary>Multiplies the vector by a scalar.</summary>
    public static Vector2Double operator *(double s, Vector2Double a) => a * s;

    /// <summary>Divides the vector by a scalar.</summary>
    public static Vector2Double operator /(Vector2Double a, double s) => new Vector2Double(a.X / s, a.Y / s);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Vector2Double left, Vector2Double right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Vector2Double left, Vector2Double right) => !left.Equals(right);

    /// <summary>Computes the dot product of two vectors.</summary>
    public static double Dot(Vector2Double a, Vector2Double b) => (a.X * b.X) + (a.Y * b.Y);

    /// <summary>Returns the squared Euclidean length of the vector.</summary>
    public readonly double LengthSquared() => Dot(this, this);

    /// <summary>Returns the Euclidean length of the vector.</summary>
    public readonly double Length() => Math.Sqrt(LengthSquared());

    /// <summary>Determines whether the specified object is equal to the current vector.</summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector2Double other) && Equals(other);

    /// <summary>Indicates whether this instance and another Vector2Double are equal.</summary>
    public readonly bool Equals(Vector2Double other) => X.Equals(other.X) && Y.Equals(other.Y);

    /// <summary>Gets the hash code for the vector.</summary>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

    /// <summary>Returns the string representation of the current instance using the specified format.</summary>
    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>Returns the string representation of the current instance using the specified format and provider.</summary>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator}>";
    }
}
