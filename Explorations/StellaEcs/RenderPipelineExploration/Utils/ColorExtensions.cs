using System.Numerics;
using MoonWorks.Graphics;

namespace StellaInvicta.Utils
{
    /// <summary>
    /// Extension methods for working with colors.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Convert a MoonWorks Color to an ImGui-friendly Vector4 (RGBA 0..1).
        /// </summary>
        public static Vector4 ToImgui(this Color c)
        {
            return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        }

        /// <summary>
        /// Convert an ImGui-style Vector4 (RGBA 0..1) to a MoonWorks Color.
        /// </summary>
        public static Color FromImgui(this Vector4 v)
        {
            return new Color(v);
        }
    }
}
