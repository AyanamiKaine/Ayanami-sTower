using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AyanamisTower.StellaEcs.HighPrecisionMath;

/// <summary>Represents a vector with three double-precision floating-point values.</summary>
/// <remarks>
/// This structure is a double-precision version of Vector3.
/// It does not include hardware acceleration-specific members.
/// </remarks>
public struct Vector3Double : IEquatable<Vector3Double>, IFormattable
{
    /// <summary>The X component of the vector.</summary>
    public double X;

    /// <summary>The Y component of the vector.</summary>
    public double Y;

    /// <summary>The Z component of the vector.</summary>
    public double Z;

    internal const int Count = 3;

    /// <summary>Creates a new <see cref="Vector3Double" /> object whose three elements have the same value.</summary>
    /// <param name="value">The value to assign to all three elements.</param>
    public Vector3Double(double value)
    {
        X = value;
        Y = value;
        Z = value;
    }

    /// <summary>Creates a new <see cref="Vector3Double" /> object from the specified Vector2Double object and the specified value.</summary>
    /// <param name="value">The vector with two elements.</param>
    /// <param name="z">The additional value to assign to the <see cref="Z" /> field.</param>
    // Assuming a Vector2Double struct exists.
    public Vector3Double(Vector2Double value, double z)
    {
        X = value.X;
        Y = value.Y;
        Z = z;
    }

    /// <summary>Creates a vector whose elements have the specified values.</summary>
    /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
    /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
    /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
    public Vector3Double(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Double}" />. The span must contain at least 3 elements.</summary>
    /// <param name="values">The span of elements to assign to the vector.</param>
    public Vector3Double(ReadOnlySpan<double> values)
    {
        if (values.Length < Count)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "The span must contain at least 3 elements.");
        }
        // Assign elements individually to avoid unsafe memory aliasing and layout assumptions.
        X = values[0];
        Y = values[1];
        Z = values[2];
    }

    /// <summary>Gets a vector whose elements are all one.</summary>
    public static Vector3Double One => new Vector3Double(1.0);

    /// <summary>Gets the vector (1,0,0).</summary>
    public static Vector3Double UnitX => new Vector3Double(1.0, 0.0, 0.0);

    /// <summary>Gets the vector (0,1,0).</summary>
    public static Vector3Double UnitY => new Vector3Double(0.0, 1.0, 0.0);

    /// <summary>Gets the vector (0,0,1).</summary>
    public static Vector3Double UnitZ => new Vector3Double(0.0, 0.0, 1.0);

    /// <summary>Gets a vector whose elements are all zero.</summary>
    public static Vector3Double Zero => new Vector3Double(0.0);

    /// <summary>
    /// Implicitly convert a double-precision vector to a System.Numerics.Vector3 (float-based).
    /// This narrows precision; use explicitly when precision isn't required by the consumer.
    /// </summary>
    public static implicit operator Vector3(Vector3Double v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);

    /// <summary>
    /// Implicitly convert a System.Numerics.Vector3 (float-based) to a double-precision vector.
    /// </summary>
    public static implicit operator Vector3Double(Vector3 v) => new Vector3Double(v.X, v.Y, v.Z);

    /// <summary>Gets or sets the element at the specified index.</summary>
    /// <param name="index">The index of the element to get or set.</param>
    /// <returns>The the element at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
    public double this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Adds two vectors together.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator +(Vector3Double left, Vector3Double right) => new Vector3Double(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>Divides the first vector by the second.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator /(Vector3Double left, Vector3Double right) => new Vector3Double(left.X / right.X, left.Y / right.Y, left.Z / right.Z);

    /// <summary>Divides the specified vector by a specified scalar value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator /(Vector3Double value1, double value2) => new Vector3Double(value1.X / value2, value1.Y / value2, value1.Z / value2);

    /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3Double left, Vector3Double right) => left.Equals(right);

    /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3Double left, Vector3Double right) => !left.Equals(right);

    /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator *(Vector3Double left, Vector3Double right) => new Vector3Double(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator *(Vector3Double left, double right) => new Vector3Double(left.X * right, left.Y * right, left.Z * right);

    /// <summary>Multiplies the scalar value by the specified vector.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator *(double left, Vector3Double right) => right * left;

    /// <summary>Subtracts the second vector from the first.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator -(Vector3Double left, Vector3Double right) => new Vector3Double(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>Negates the specified vector.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double operator -(Vector3Double value) => new Vector3Double(-value.X, -value.Y, -value.Z);

    /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double Abs(Vector3Double value) => new Vector3Double(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));

    /// <summary>Adds two vectors together.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double Add(Vector3Double left, Vector3Double right) => left + right;

    /// <summary>Restricts a vector between a minimum and a maximum value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double Clamp(Vector3Double value1, Vector3Double min, Vector3Double max)
    {
        return new Vector3Double(
            Math.Max(min.X, Math.Min(max.X, value1.X)),
            Math.Max(min.Y, Math.Min(max.Y, value1.Y)),
            Math.Max(min.Z, Math.Min(max.Z, value1.Z))
        );
    }

    /// <summary>Computes the cross product of two vectors.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3Double Cross(Vector3Double vector1, Vector3Double vector2)
    {
        return new Vector3Double(
            (vector1.Y * vector2.Z) - (vector1.Z * vector2.Y),
            (vector1.Z * vector2.X) - (vector1.X * vector2.Z),
            (vector1.X * vector2.Y) - (vector1.Y * vector2.X)
        );
    }

    /// <summary>Computes the Euclidean distance between the two given points.</summary>
    public static double Distance(Vector3Double value1, Vector3Double value2) => Math.Sqrt(DistanceSquared(value1, value2));

    /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
    public static double DistanceSquared(Vector3Double value1, Vector3Double value2) => (value1 - value2).LengthSquared();

    /// <summary>Divides the first vector by the second.</summary>
    public static Vector3Double Divide(Vector3Double left, Vector3Double right) => left / right;

    /// <summary>Divides the specified vector by a specified scalar value.</summary>
    public static Vector3Double Divide(Vector3Double left, double divisor) => left / divisor;

    /// <summary>Returns the dot product of two vectors.</summary>
    public static double Dot(Vector3Double vector1, Vector3Double vector2) => (vector1.X * vector2.X) + (vector1.Y * vector2.Y) + (vector1.Z * vector2.Z);

    /// <summary>Performs a linear interpolation between two vectors.</summary>
    public static Vector3Double Lerp(Vector3Double value1, Vector3Double value2, double amount) => value1 + ((value2 - value1) * amount);

    /// <summary>Returns a vector whose elements are the maximum of each of the pairs of elements in two specified vectors.</summary>
    public static Vector3Double Max(Vector3Double value1, Vector3Double value2)
    {
        return new Vector3Double(
            Math.Max(value1.X, value2.X),
            Math.Max(value1.Y, value2.Y),
            Math.Max(value1.Z, value2.Z)
        );
    }

    /// <summary>Returns a vector whose elements are the minimum of each of the pairs of elements in two specified vectors.</summary>
    public static Vector3Double Min(Vector3Double value1, Vector3Double value2)
    {
        return new Vector3Double(
            Math.Min(value1.X, value2.X),
            Math.Min(value1.Y, value2.Y),
            Math.Min(value1.Z, value2.Z)
        );
    }

    /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
    public static Vector3Double Multiply(Vector3Double left, Vector3Double right) => left * right;

    /// <summary>Multiplies a vector by a specified scalar.</summary>
    public static Vector3Double Multiply(Vector3Double left, double right) => left * right;

    /// <summary>Multiplies a scalar value by a specified vector.</summary>
    public static Vector3Double Multiply(double left, Vector3Double right) => left * right;

    /// <summary>Negates a specified vector.</summary>
    public static Vector3Double Negate(Vector3Double value) => -value;

    /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
    public static Vector3Double Normalize(Vector3Double value) => value / value.Length();

    /// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
    public static Vector3Double Reflect(Vector3Double vector, Vector3Double normal)
    {
        double dot = Dot(vector, normal);
        return vector - (2.0 * dot * normal);
    }

    /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
    public static Vector3Double SquareRoot(Vector3Double value) => new Vector3Double(Math.Sqrt(value.X), Math.Sqrt(value.Y), Math.Sqrt(value.Z));

    /// <summary>Subtracts the second vector from the first.</summary>
    public static Vector3Double Subtract(Vector3Double left, Vector3Double right) => left - right;

    // NOTE: The Transform methods below assume the existence of double-precision
    // Matrix4x4Double and QuaternionDouble structs. The logic is a direct
    // translation of standard transformation mathematics.

    /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
    public static Vector3Double Transform(Vector3Double position, Matrix4x4Double matrix)
    {
        return new Vector3Double(
            (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
            (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
            (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43
        );
    }

    /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
    public static Vector3Double Transform(Vector3Double value, QuaternionDouble rotation)
    {
        double x2 = rotation.X + rotation.X;
        double y2 = rotation.Y + rotation.Y;
        double z2 = rotation.Z + rotation.Z;

        double wx2 = rotation.W * x2;
        double wy2 = rotation.W * y2;
        double wz2 = rotation.W * z2;
        double xx2 = rotation.X * x2;
        double xy2 = rotation.X * y2;
        double xz2 = rotation.X * z2;
        double yy2 = rotation.Y * y2;
        double yz2 = rotation.Y * z2;
        double zz2 = rotation.Z * z2;

        return new Vector3Double(
            (value.X * (1.0 - yy2 - zz2)) + (value.Y * (xy2 - wz2)) + (value.Z * (xz2 + wy2)),
            (value.X * (xy2 + wz2)) + (value.Y * (1.0 - xx2 - zz2)) + (value.Z * (yz2 - wx2)),
            (value.X * (xz2 - wy2)) + (value.Y * (yz2 + wx2)) + (value.Z * (1.0 - xx2 - yy2))
        );
    }

    /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
    public static Vector3Double TransformNormal(Vector3Double normal, Matrix4x4Double matrix)
    {
        return new Vector3Double(
            (normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
            (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
            (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33)
        );
    }

    /// <summary>Copies the elements of the vector to a specified array.</summary>
    public readonly void CopyTo(double[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Length < Count) throw new ArgumentException("Destination array is not long enough.", nameof(array));
        CopyTo(array, 0);
    }

    /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
    public readonly void CopyTo(double[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if ((uint)index >= (uint)array.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if ((array.Length - index) < Count) throw new ArgumentException("Destination array is not long enough.", nameof(array));

        array[index] = X;
        array[index + 1] = Y;
        array[index + 2] = Z;
    }

    /// <summary>Copies the vector to the given <see cref="Span{T}" />.</summary>
    public readonly void CopyTo(Span<double> destination)
    {
        if (destination.Length < Count) throw new ArgumentException("Destination span is not long enough.", nameof(destination));

        // Copy elements explicitly for clarity and safety.
        destination[0] = X;
        destination[1] = Y;
        destination[2] = Z;
    }

    /// <summary>Attempts to copy the vector to the given <see cref="Span{Double}" />.</summary>
    public readonly bool TryCopyTo(Span<double> destination)
    {
        if (destination.Length < Count) return false;

        destination[0] = X;
        destination[1] = Y;
        destination[2] = Z;
        return true;
    }

    /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector3Double other) && Equals(other);

    /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
    public readonly bool Equals(Vector3Double other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    /// <summary>Returns the hash code for this instance.</summary>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <summary>Returns the length of this vector object.</summary>
    public readonly double Length() => Math.Sqrt(LengthSquared());

    /// <summary>Returns the length of the vector squared.</summary>
    public readonly double LengthSquared() => Dot(this, this);

    /// <summary>Returns the string representation of the current instance using default formatting.</summary>
    public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

    /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

    /// <summary>Returns the string representation of the current instance using the specified format string and format provider.</summary>
    public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
    }
}
