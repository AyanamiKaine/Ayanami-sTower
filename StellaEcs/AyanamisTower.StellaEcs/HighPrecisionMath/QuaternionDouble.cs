using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

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

    /// <summary>Gets a quaternion that represents zero.</summary>
    public static QuaternionDouble Zero => new(0.0, 0.0, 0.0, 0.0);

    /// <summary>Gets or sets the element at the specified index.</summary>
    public double this[int index]
    {
        get => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            3 => W,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                case 3: W = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <inheritdoc/>
    public QuaternionDouble(double x, double y, double z, double w)
    {
        X = x; Y = y; Z = z; W = w;
    }

    /// <summary>
    /// Implicitly convert a double-precision quaternion to System.Numerics.Quaternion (float-based).
    /// The quaternion is normalized during conversion to avoid passing degenerate rotations to float systems.
    /// </summary>
    public static implicit operator Quaternion(QuaternionDouble q)
    {
        double lenSq = q.LengthSquared();
        if (lenSq <= double.Epsilon)
            return Quaternion.Identity;
        double invLen = 1.0 / Math.Sqrt(lenSq);
        return new Quaternion((float)(q.X * invLen), (float)(q.Y * invLen), (float)(q.Z * invLen), (float)(q.W * invLen));
    }

    /// <summary>
    /// Implicitly convert a System.Numerics.Quaternion (float-based) to a double-precision quaternion.
    /// </summary>
    public static implicit operator QuaternionDouble(Quaternion q) => new QuaternionDouble(q.X, q.Y, q.Z, q.W);

    /// <inheritdoc/>
    public QuaternionDouble(Vector3Double vectorPart, double scalarPart)
    {
        X = vectorPart.X; Y = vectorPart.Y; Z = vectorPart.Z; W = scalarPart;
    }

    /// <inheritdoc/>
    public static QuaternionDouble operator +(QuaternionDouble value1, QuaternionDouble value2) => new QuaternionDouble(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z, value1.W + value2.W);

    /// <inheritdoc/>
    public static QuaternionDouble operator /(QuaternionDouble value1, QuaternionDouble value2) => value1 * Inverse(value2);


    /// <summary>Creates a quaternion from the specified components.</summary>
    internal static QuaternionDouble Create(double x, double y, double z, double w) => new QuaternionDouble(x, y, z, w);

    /// <summary>Creates a quaternion from the specified vector and scalar parts.</summary>
    internal static QuaternionDouble Create(Vector3Double vectorPart, double scalarPart) => new QuaternionDouble(vectorPart.X, vectorPart.Y, vectorPart.Z, scalarPart);

    /// <summary>Creates a quaternion from an axis and an angle in radians. Axis must be normalized.</summary>
    public static QuaternionDouble CreateFromAxisAngle(Vector3Double axis, double angle)
    {
        double half = angle * 0.5;
        double s = Math.Sin(half);
        double c = Math.Cos(half);
        return new QuaternionDouble(axis.X * s, axis.Y * s, axis.Z * s, c);
    }

    /// <summary>Creates a quaternion from the specified rotation matrix.</summary>
    public static QuaternionDouble CreateFromRotationMatrix(Matrix4x4Double matrix)
    {
        double trace = matrix.M11 + matrix.M22 + matrix.M33;

        QuaternionDouble q = default;

        if (trace > 0.0)
        {
            double s = Math.Sqrt(trace + 1.0);
            q.W = s * 0.5;
            s = 0.5 / s;
            q.X = (matrix.M23 - matrix.M32) * s;
            q.Y = (matrix.M31 - matrix.M13) * s;
            q.Z = (matrix.M12 - matrix.M21) * s;
        }
        else
        {
            if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
            {
                double s = Math.Sqrt(1.0 + matrix.M11 - matrix.M22 - matrix.M33);
                double invS = 0.5 / s;
                q.X = 0.5 * s;
                q.Y = (matrix.M12 + matrix.M21) * invS;
                q.Z = (matrix.M13 + matrix.M31) * invS;
                q.W = (matrix.M23 - matrix.M32) * invS;
            }
            else if (matrix.M22 > matrix.M33)
            {
                double s = Math.Sqrt(1.0 + matrix.M22 - matrix.M11 - matrix.M33);
                double invS = 0.5 / s;
                q.X = (matrix.M21 + matrix.M12) * invS;
                q.Y = 0.5 * s;
                q.Z = (matrix.M32 + matrix.M23) * invS;
                q.W = (matrix.M31 - matrix.M13) * invS;
            }
            else
            {
                double s = Math.Sqrt(1.0 + matrix.M33 - matrix.M11 - matrix.M22);
                double invS = 0.5 / s;
                q.X = (matrix.M31 + matrix.M13) * invS;
                q.Y = (matrix.M32 + matrix.M23) * invS;
                q.Z = 0.5 * s;
                q.W = (matrix.M12 - matrix.M21) * invS;
            }
        }

        return q;
    }

    /// <summary>Creates a new quaternion from yaw (Y), pitch (X) and roll (Z) angles (radians).</summary>
    public static QuaternionDouble CreateFromYawPitchRoll(double yaw, double pitch, double roll)
    {
        double halfRoll = roll * 0.5;
        double halfPitch = pitch * 0.5;
        double halfYaw = yaw * 0.5;

        double sinR = Math.Sin(halfRoll);
        double cosR = Math.Cos(halfRoll);
        double sinP = Math.Sin(halfPitch);
        double cosP = Math.Cos(halfPitch);
        double sinY = Math.Sin(halfYaw);
        double cosY = Math.Cos(halfYaw);

        QuaternionDouble result;
        result.X = cosY * sinP * cosR + sinY * cosP * sinR;
        result.Y = sinY * cosP * cosR - cosY * sinP * sinR;
        result.Z = cosY * cosP * sinR - sinY * sinP * cosR;
        result.W = cosY * cosP * cosR + sinY * sinP * sinR;
        return result;
    }

    /// <summary>Returns the quaternion that results from concatenating two rotations (value1 followed by value2).</summary>
    public static QuaternionDouble Concatenate(QuaternionDouble value1, QuaternionDouble value2) => value2 * value1;

    /// <summary>Divides two quaternions.</summary>
    public static QuaternionDouble Divide(QuaternionDouble value1, QuaternionDouble value2) => value1 / value2;

    /// <summary>Calculates the dot product of two quaternions.</summary>
    public static double Dot(QuaternionDouble quaternion1, QuaternionDouble quaternion2) => (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);

    /// <summary>Performs a linear interpolation between two quaternions and normalizes the result.</summary>
    public static QuaternionDouble Lerp(QuaternionDouble quaternion1, QuaternionDouble quaternion2, double amount)
    {
        // Ensure shortest path
        double dot = Dot(quaternion1, quaternion2);
        QuaternionDouble q2 = quaternion2;
        if (dot < 0.0)
        {
            q2 = -quaternion2;
            dot = -dot;
        }

        QuaternionDouble result = new QuaternionDouble(
            quaternion1.X * (1.0 - amount) + q2.X * amount,
            quaternion1.Y * (1.0 - amount) + q2.Y * amount,
            quaternion1.Z * (1.0 - amount) + q2.Z * amount,
            quaternion1.W * (1.0 - amount) + q2.W * amount
        );

        return Normalize(result);
    }

    /// <summary>Returns the quaternion resulting from scaling all components by a scalar.</summary>
    public static QuaternionDouble Multiply(QuaternionDouble value1, double value2) => value1 * value2;

    /// <summary>Returns the quaternion product.</summary>
    public static QuaternionDouble Multiply(QuaternionDouble value1, QuaternionDouble value2) => value1 * value2;

    /// <summary>Negates the quaternion.</summary>
    public static QuaternionDouble Negate(QuaternionDouble value) => -value;

    /// <summary>Normalizes the quaternion.</summary>
    public static QuaternionDouble Normalize(QuaternionDouble value)
    {
        double len = value.Length();
        if (len > 0.0) return value * (1.0 / len);
        return Identity;
    }

    /// <summary>Spherical linear interpolation between two quaternions.</summary>
    public static QuaternionDouble Slerp(QuaternionDouble quaternion1, QuaternionDouble quaternion2, double amount)
    {
        const double SlerpEpsilon = 1e-12;

        double cosOmega = Dot(quaternion1, quaternion2);
        double sign = 1.0;

        if (cosOmega < 0.0)
        {
            cosOmega = -cosOmega;
            sign = -1.0;
        }

        double s1, s2;

        if (cosOmega > (1.0 - SlerpEpsilon))
        {
            // Too close, use linear interpolation
            s1 = 1.0 - amount;
            s2 = amount * sign;
        }
        else
        {
            double omega = Math.Acos(cosOmega);
            double invSinOmega = 1.0 / Math.Sin(omega);

            s1 = Math.Sin((1.0 - amount) * omega) * invSinOmega;
            s2 = Math.Sin(amount * omega) * invSinOmega * sign;
        }

        return new QuaternionDouble(
            (quaternion1.X * s1) + (quaternion2.X * s2),
            (quaternion1.Y * s1) + (quaternion2.Y * s2),
            (quaternion1.Z * s1) + (quaternion2.Z * s2),
            (quaternion1.W * s1) + (quaternion2.W * s2)
        );
    }

    /// <summary>Subtracts two quaternions.</summary>
    public static QuaternionDouble Subtract(QuaternionDouble value1, QuaternionDouble value2) => value1 - value2;

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

