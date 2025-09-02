using System.Numerics;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;
using Hexa.NET.ImGui;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Rendering;

/// <summary>
/// Debug renderer that draws UI via ImGui's foreground draw list. Replace with a MoonWorks renderer later.
/// </summary>
public sealed class DebugImGuiUIRenderer : IUIRenderer
{
    /// <inheritdoc/>
    public void Begin() { }
    /// <inheritdoc/>
    public void End() { }
    /// <summary>
    /// Draws a panel with the specified rectangle and style.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="style"></param>
    public void DrawPanel(UIRect rect, UIStyle style)
    {
        var draw = ImGui.GetForegroundDrawList();
        var p0 = new Vector2(rect.Left, rect.Top);
        var p1 = new Vector2(rect.Right, rect.Bottom);
        uint bg = ImGui.ColorConvertFloat4ToU32(style.BackgroundColor);
        uint bd = ImGui.ColorConvertFloat4ToU32(style.BorderColor);
        draw.AddRectFilled(p0, p1, bg, style.CornerRadius);
        if (style.BorderThickness > 0.01f)
        {
            draw.AddRect(p0, p1, bd, style.CornerRadius, 0, style.BorderThickness);
        }
    }
    /// <summary>
    /// Draws a button with the specified text, color, and font size.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="style"></param>
    /// <param name="text"></param>
    /// <param name="textColor"></param>
    /// <param name="fontSize"></param>
    public void DrawButton(UIRect rect, UIStyle style, string text, Vector4 textColor, float fontSize)
    {
        DrawPanel(rect, style);
        DrawLabel(rect, text, textColor, fontSize);
    }
    /// <summary>
    /// Draws a label with the specified text, color, and font size.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="text"></param>
    /// <param name="color"></param>
    /// <param name="fontSize"></param>
    public void DrawLabel(UIRect rect, string text, Vector4 color, float fontSize)
    {
        var draw = ImGui.GetForegroundDrawList();
        uint col = ImGui.ColorConvertFloat4ToU32(color);
        float pad = 6f;
        draw.AddText(new Vector2(rect.Left + pad, rect.Top + pad), col, text);
    }
}
