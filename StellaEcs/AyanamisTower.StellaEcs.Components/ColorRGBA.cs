using System.Globalization;

namespace AyanamisTower.StellaEcs.Components;
#pragma warning disable CS1591

/// <summary>
/// Simple RGBA color component.
/// Stored as bytes 0..255 and convertible to/from hex (#RRGGBB or #RRGGBBAA).
/// </summary>
public struct ColorRGBA(byte r = 255, byte g = 255, byte b = 255, byte a = 255)
{
    public byte R = r;
    public byte G = g;
    public byte B = b;
    public byte A = a;

    public string ToHex(bool includeAlpha = true)
    {
        return includeAlpha
            ? $"#{R:X2}{G:X2}{B:X2}{A:X2}"
            : $"#{R:X2}{G:X2}{B:X2}";
    }

    public static ColorRGBA FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return new ColorRGBA();
        hex = hex.Trim();
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length is not (6 or 8)) return new ColorRGBA();
        byte r = byte.Parse(hex[..2], NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        byte a = (hex.Length == 8) ? byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) : (byte)255;
        return new ColorRGBA(r, g, b, a);
    }
}
