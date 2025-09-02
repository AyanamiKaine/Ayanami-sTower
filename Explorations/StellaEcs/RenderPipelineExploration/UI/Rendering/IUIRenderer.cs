using System.Numerics;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Rendering;

/// <summary>
/// Abstraction over the engine's 2D renderer for UI. Implement this using MoonWorks to draw quads, borders, and text.
/// </summary>
public interface IUIRenderer
{
    /// <summary>
    /// Begins a new UI rendering batch.
    /// </summary>
    void Begin();
    /// <summary>
    /// Ends the current UI rendering batch.
    /// </summary>
    void End();
    /// <summary>
    /// Draws a panel background.
    /// </summary>
    void DrawPanel(UIRect rect, UIStyle style);
    /// <summary>
    /// Draws a button.
    /// </summary>
    void DrawButton(UIRect rect, UIStyle style, string text, Vector4 textColor, float fontSize);
    /// <summary>
    /// Draws a label.
    /// </summary>
    void DrawLabel(UIRect rect, string text, Vector4 color, float fontSize);
}
