using System;
using System.Diagnostics.CodeAnalysis;

namespace AyanamisTower.StellaEcs.HighPrecisionMath;

/// <summary>Represents a 4x4 matrix using double-precision floating-point numbers.</summary>
public struct Matrix4x4Double : IEquatable<Matrix4x4Double>
{
    /// <summary>
    /// The element at row 1, column 1.
    /// </summary>
    public double M11;
    /// <summary>
    /// The element at row 1, column 2.
    /// </summary>
    public double M12;
    /// <summary>
    /// The element at row 1, column 3.
    /// </summary>
    public double M13;
    /// <summary>
    /// The element at row 1, column 4.
    /// </summary>
    public double M14;
    /// <summary>
    /// The element at row 2, column 1.
    /// </summary>
    public double M21;
    /// <summary>
    /// The element at row 2, column 2.
    /// </summary>
    public double M22;
    /// <summary>
    /// The element at row 2, column 3.
    /// </summary>
    public double M23;
    /// <summary>
    /// The element at row 2, column 4.
    /// </summary>
    public double M24;

    /// <summary>
    /// The element at row 3, column 1.
    /// </summary>
    public double M31;
    /// <summary>
    /// The element at row 3, column 2.
    /// </summary>
    public double M32;
    /// <summary>
    /// The element at row 3, column 3.
    /// </summary>
    public double M33;
    /// <summary>
    /// The element at row 3, column 4.
    /// </summary>
    public double M34;
    /// <summary>
    /// The element at row 4, column 1.
    /// </summary>
    public double M41;
    /// <summary>
    /// The element at row 4, column 2.
    /// </summary>
    public double M42;
    /// <summary>
    /// The element at row 4, column 3.
    /// </summary>
    public double M43;
    /// <summary>
    /// The element at row 4, column 4.
    /// </summary>
    public double M44;

    /// <summary>Gets the multiplicative identity matrix.</summary>
    public static Matrix4x4Double Identity => new Matrix4x4Double(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    );

    /// <summary>Indicates whether the current matrix is the identity matrix.</summary>
    public readonly bool IsIdentity => this.Equals(Identity);

    /// <summary>Gets or sets the translation component of this matrix.</summary>
    public Vector3Double Translation
    {
        readonly get => new Vector3Double(M41, M42, M43);
        set
        {
            M41 = value.X;
            M42 = value.Y;
            M43 = value.Z;
        }
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix4x4Double"/> struct.
    /// </summary>
    /// <param name="m11"></param>
    /// <param name="m12"></param>
    /// <param name="m13"></param>
    /// <param name="m14"></param>
    /// <param name="m21"></param>
    /// <param name="m22"></param>
    /// <param name="m23"></param>
    /// <param name="m24"></param>
    /// <param name="m31"></param>
    /// <param name="m32"></param>
    /// <param name="m33"></param>
    /// <param name="m34"></param>
    /// <param name="m41"></param>
    /// <param name="m42"></param>
    /// <param name="m43"></param>
    /// <param name="m44"></param>
    public Matrix4x4Double(double m11, double m12, double m13, double m14,
                           double m21, double m22, double m23, double m24,
                           double m31, double m32, double m33, double m34,
                           double m41, double m42, double m43, double m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }
    /// <summary>
    /// Adds two matrices.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static Matrix4x4Double operator +(Matrix4x4Double value1, Matrix4x4Double value2)
    {
        return new Matrix4x4Double(
            value1.M11 + value2.M11, value1.M12 + value2.M12, value1.M13 + value2.M13, value1.M14 + value2.M14,
            value1.M21 + value2.M21, value1.M22 + value2.M22, value1.M23 + value2.M23, value1.M24 + value2.M24,
            value1.M31 + value2.M31, value1.M32 + value2.M32, value1.M33 + value2.M33, value1.M34 + value2.M34,
            value1.M41 + value2.M41, value1.M42 + value2.M42, value1.M43 + value2.M43, value1.M44 + value2.M44
        );
    }
    /// <summary>
    /// Determines whether two matrices are equal.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static bool operator ==(Matrix4x4Double value1, Matrix4x4Double value2) => value1.Equals(value2);
    /// <summary>
    /// Determines whether two matrices are not equal.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static bool operator !=(Matrix4x4Double value1, Matrix4x4Double value2) => !value1.Equals(value2);
    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static Matrix4x4Double operator *(Matrix4x4Double value1, Matrix4x4Double value2)
    {
        return new Matrix4x4Double(
            value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41,
            value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42,
            value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43,
            value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 + value1.M14 * value2.M44,
            value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41,
            value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42,
            value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43,
            value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 + value1.M24 * value2.M44,
            value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41,
            value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42,
            value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43,
            value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 + value1.M34 * value2.M44,
            value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41,
            value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42,
            value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43,
            value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value1.M44 * value2.M44
        );
    }
    /// <summary>
    /// Multiplies a matrix by a scalar value.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static Matrix4x4Double operator *(Matrix4x4Double value1, double value2)
    {
        return new Matrix4x4Double(
            value1.M11 * value2, value1.M12 * value2, value1.M13 * value2, value1.M14 * value2,
            value1.M21 * value2, value1.M22 * value2, value1.M23 * value2, value1.M24 * value2,
            value1.M31 * value2, value1.M32 * value2, value1.M33 * value2, value1.M34 * value2,
            value1.M41 * value2, value1.M42 * value2, value1.M43 * value2, value1.M44 * value2
        );
    }
    /// <summary>
    /// Subtracts two matrices.
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static Matrix4x4Double operator -(Matrix4x4Double value1, Matrix4x4Double value2)
    {
        return new Matrix4x4Double(
           value1.M11 - value2.M11, value1.M12 - value2.M12, value1.M13 - value2.M13, value1.M14 - value2.M14,
           value1.M21 - value2.M21, value1.M22 - value2.M22, value1.M23 - value2.M23, value1.M24 - value2.M24,
           value1.M31 - value2.M31, value1.M32 - value2.M32, value1.M33 - value2.M33, value1.M34 - value2.M34,
           value1.M41 - value2.M41, value1.M42 - value2.M42, value1.M43 - value2.M43, value1.M44 - value2.M44
       );
    }
    /// <summary>
    /// Negates the specified matrix.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Matrix4x4Double operator -(Matrix4x4Double value) => value * -1;

    /// <summary>Creates a rotation matrix from the specified Quaternion rotation value.</summary>
    public static Matrix4x4Double CreateFromQuaternion(QuaternionDouble quaternion)
    {
        Matrix4x4Double result = Identity;

        double xx = quaternion.X * quaternion.X;
        double yy = quaternion.Y * quaternion.Y;
        double zz = quaternion.Z * quaternion.Z;
        double xy = quaternion.X * quaternion.Y;
        double zw = quaternion.Z * quaternion.W;
        double zx = quaternion.Z * quaternion.X;
        double yw = quaternion.Y * quaternion.W;
        double yz = quaternion.Y * quaternion.Z;
        double xw = quaternion.X * quaternion.W;

        result.M11 = 1.0 - (2.0 * (yy + zz));
        result.M12 = 2.0 * (xy + zw);
        result.M13 = 2.0 * (zx - yw);
        result.M21 = 2.0 * (xy - zw);
        result.M22 = 1.0 - (2.0 * (zz + xx));
        result.M23 = 2.0 * (yz + xw);
        result.M31 = 2.0 * (zx + yw);
        result.M32 = 2.0 * (yz - xw);
        result.M33 = 1.0 - (2.0 * (yy + xx));

        return result;
    }

    /// <summary>Creates a translation matrix from the specified 3-dimensional vector.</summary>
    public static Matrix4x4Double CreateTranslation(Vector3Double position)
    {
        Matrix4x4Double result = Identity;
        result.M41 = position.X;
        result.M42 = position.Y;
        result.M43 = position.Z;
        return result;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Matrix4x4Double other) && Equals(other);

    /// <inheritdoc/>
    public readonly bool Equals(Matrix4x4Double other)
    {
        return M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) && M14.Equals(other.M14) &&
               M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) && M24.Equals(other.M24) &&
               M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33) && M34.Equals(other.M34) &&
               M41.Equals(other.M41) && M42.Equals(other.M42) && M43.Equals(other.M43) && M44.Equals(other.M44);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(M11); hash.Add(M12); hash.Add(M13); hash.Add(M14);
        hash.Add(M21); hash.Add(M22); hash.Add(M23); hash.Add(M24);
        hash.Add(M31); hash.Add(M32); hash.Add(M33); hash.Add(M34);
        hash.Add(M41); hash.Add(M42); hash.Add(M43); hash.Add(M44);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return $"{{ {{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} {{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} {{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} {{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} }}";
    }
}
