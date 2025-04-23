using System;
using AyanamisTower.NihilEx.SDLWrapper;
using SDL3;

namespace AyanamisTower.NihilEx.ECS;

// Optional: Explicit layout can sometimes help with performance/interop,
// but is often not strictly necessary unless profiling shows a benefit.
// [StructLayout(LayoutKind.Sequential)]
/// <summary>
/// Represents a color in the Red, Green, Blue, Alpha (RGBA) color space.
/// Each component is stored as a byte (0-255).
/// </summary>
public struct RgbaColor : IEquatable<RgbaColor>
{
    /// <summary>The red component of the color.</summary>
    public byte R;
    /// <summary>The green component of the color.</summary>
    public byte G;
    /// <summary>The blue component of the color.</summary>
    public byte B;
    /// <summary>The alpha (transparency) component of the color.</summary>
    public byte A;

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct with the specified red, green, blue, and alpha components.
    /// </summary>
    /// <param name="r">The red component of the color (0-255).</param>
    /// <param name="g">The green component of the color (0-255).</param>
    /// <param name="b">The blue component of the color (0-255).</param>
    /// <param name="a">The alpha component of the color (0-255).</param>
    public RgbaColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaColor"/> struct with the specified red, green, and blue components.
    /// The alpha component is set to 255 (fully opaque).
    /// </summary>
    /// <param name="r">The red component of the color (0-255).</param>
    /// <param name="g">The green component of the color (0-255).</param>
    /// <param name="b">The blue component of the color (0-255).</param>
    public RgbaColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        A = 255;
    }

    // --- Convenience Static Properties for Common Colors ---

    /// <summary>Gets a system-defined color that has an RGBA value of #FF0000 (fully opaque red).</summary>
    public static RgbaColor Red => new(255, 0, 0);
    /// <summary>Gets a system-defined color that has an RGBA value of #00FF00 (fully opaque green).</summary>
    public static RgbaColor Green => new(0, 255, 0);
    /// <summary>Gets a system-defined color that has an RGBA value of #0000FF (fully opaque blue).</summary>
    public static RgbaColor Blue => new(0, 0, 255);
    /// <summary>Gets a system-defined color that has an RGBA value of #FFFF00 (fully opaque yellow).</summary>
    public static RgbaColor Yellow => new(255, 255, 0);
    /// <summary>Gets a system-defined color that has an RGBA value of #FF00FF (fully opaque magenta).</summary>
    public static RgbaColor Magenta => new(255, 0, 255);
    /// <summary>Gets a system-defined color that has an RGBA value of #00FFFF (fully opaque cyan).</summary>
    public static RgbaColor Cyan => new(0, 255, 255);
    /// <summary>Gets a system-defined color that has an RGBA value of #FFFFFF (fully opaque white).</summary>
    public static RgbaColor White => new(255, 255, 255);
    /// <summary>Gets a system-defined color that has an RGBA value of #000000 (fully opaque black).</summary>
    public static RgbaColor Black => new(0, 0, 0);
    /// <summary>Gets a system-defined color that has an RGBA value of #00000000 (fully transparent black).</summary>
    public static RgbaColor Transparent => new(0, 0, 0, 0);

    // --- Conversion Operators (Optional but helpful) ---

    /// <summary>
    /// Implicitly converts a <see cref="System.Drawing.Color"/> to an <see cref="RgbaColor"/>.
    /// </summary>
    /// <param name="color">The <see cref="System.Drawing.Color"/> to convert.</param>
    /// <returns>The equivalent <see cref="RgbaColor"/>.</returns>
    public static implicit operator RgbaColor(System.Drawing.Color color)
    {
        return new RgbaColor(color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// Implicitly converts an <see cref="RgbaColor"/> to a <see cref="System.Drawing.Color"/>.
    /// </summary>
    /// <param name="color">The <see cref="RgbaColor"/> to convert.</param>
    /// <returns>The equivalent <see cref="System.Drawing.Color"/>.</returns>
    public static implicit operator System.Drawing.Color(RgbaColor color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    /// <summary>
    /// Implicitly converts an <see cref="RgbaColor"/> to a <see cref="Color"/>.
    /// </summary>
    /// <param name="color">The <see cref="RgbaColor"/> to convert.</param>
    /// <returns>The equivalent <see cref="Color"/>.</returns>
    public static implicit operator Color(RgbaColor color)
    {
        return new(color.R, color.G, color.B, color.A);
    }

    // --- Equality Checks and HashCode (Good practice for structs) ---

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="RgbaColor"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="RgbaColor"/>.</param>
    /// <returns><c>true</c> if the specified object is an <see cref="RgbaColor"/> and has the same RGBA values as the current instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is RgbaColor other && Equals(other);
    }

    /// <summary>
    /// Determines whether the specified <see cref="RgbaColor"/> is equal to the current <see cref="RgbaColor"/>.
    /// </summary>
    /// <param name="other">The <see cref="RgbaColor"/> to compare with the current instance.</param>
    /// <returns><c>true</c> if the specified <see cref="RgbaColor"/> has the same RGBA values as the current instance; otherwise, <c>false</c>.</returns>
    public bool Equals(RgbaColor other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <summary>
    /// Determines whether two specified <see cref="RgbaColor"/> objects are equal.
    /// </summary>
    /// <param name="left">The first <see cref="RgbaColor"/> to compare.</param>
    /// <param name="right">The second <see cref="RgbaColor"/> to compare.</param>
    /// <returns><c>true</c> if the values of <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(RgbaColor left, RgbaColor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified <see cref="RgbaColor"/> objects are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="RgbaColor"/> to compare.</param>
    /// <param name="right">The second <see cref="RgbaColor"/> to compare.</param>
    /// <returns><c>true</c> if the values of <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(RgbaColor left, RgbaColor right)
    {
        return !(left == right);
    }

    // --- ToString for Debugging ---

    /// <summary>
    /// Returns a string representation of the color.
    /// </summary>
    /// <returns>A string in the format "Rgba(R, G, B, A)".</returns>
    public override string ToString()
    {
        return $"Rgba({R}, {G}, {B}, {A})";
    }

    // --- Optional: Methods for common operations ---

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="a">The starting color (corresponding to t=0).</param>
    /// <param name="b">The ending color (corresponding to t=1).</param>
    /// <param name="t">The interpolation factor, clamped between 0.0 and 1.0.</param>
    /// <returns>The interpolated <see cref="RgbaColor"/>.</returns>
    public static RgbaColor Lerp(RgbaColor a, RgbaColor b, float t)
    {
        t = Math.Clamp(t, 0.0f, 1.0f); // Ensure t is in [0, 1] range
        byte r = (byte)(a.R + (b.R - a.R) * t);
        byte g = (byte)(a.G + (b.G - a.G) * t);
        byte bl = (byte)(a.B + (b.B - a.B) * t); // Renamed 'b' parameter to 'bl' to avoid conflict
        byte alpha = (byte)(a.A + (b.A - a.A) * t);
        return new RgbaColor(r, g, bl, alpha);
    }
}
