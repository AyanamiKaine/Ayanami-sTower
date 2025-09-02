using System.Numerics;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Components;

/// <summary>
/// Marks an entity as a UI node and links it to a parent UI entity (or default for root).
/// </summary>
public struct UIElement
{
    /// <summary>
    /// Parent UI entity. default(Entity) indicates a root element attached to the screen/canvas.
    /// </summary>
    public Entity Parent;

    /// <summary>
    /// Draw order inside the same parent. Higher draws later (on top).
    /// </summary>
    public int ZIndex;

    /// <summary>
    /// If false, input and rendering are skipped for this node and its children (but still laid out).
    /// </summary>
    public bool Visible;
}

/// <summary>
/// Basic rectangular layout transform for UI in screen space.
/// Anchors are in [0,1] relative to parent rect. Offsets are in pixels.
/// </summary>
public struct RectTransform
{
    /// <summary>Lower-left anchor relative to parent rectangle (0..1)</summary>
    public Vector2 AnchorMin;   // 0..1
    /// <summary>Upper-right anchor relative to parent rectangle (0..1)</summary>
    public Vector2 AnchorMax;   // 0..1
    /// <summary>Pixel offset added to the anchored minimum corner</summary>
    public Vector2 OffsetMin;   // pixels
    /// <summary>Pixel offset added to the anchored maximum corner</summary>
    public Vector2 OffsetMax;   // pixels
    /// <summary>Pivot for alignment and potential rotation (0..1)</summary>
    public Vector2 Pivot;       // 0..1 (for content positioning; not fully used yet)

    /// <summary>
    /// Computed absolute screen-space rectangle for this element.
    /// </summary>
    public UIRect Computed;
}

/// <summary>
/// Simple screen-space rectangle.
/// </summary>
public struct UIRect
{
    /// <summary>Left coordinate in pixels</summary>
    public float X;
    /// <summary>Top coordinate in pixels</summary>
    public float Y;
    /// <summary>Width in pixels</summary>
    public float Width;
    /// <summary>Height in pixels</summary>
    public float Height;

    /// <summary>Create from position and size.</summary>
    public UIRect(float x, float y, float w, float h)
    {
        X = x; Y = y; Width = w; Height = h;
    }

    /// <summary>Left edge</summary>
    public float Left => X;
    /// <summary>Top edge</summary>
    public float Top => Y;
    /// <summary>Right edge</summary>
    public float Right => X + Width;
    /// <summary>Bottom edge</summary>
    public float Bottom => Y + Height;

    /// <summary>Position vector (X,Y)</summary>
    public Vector2 Position => new(X, Y);
    /// <summary>Size vector (Width,Height)</summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>True if point lies inside rectangle</summary>
    public bool Contains(Vector2 p) => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;

    /// <summary>Create rectangle from min/max edges</summary>
    public static UIRect FromMinMax(float left, float top, float right, float bottom)
        => new(left, top, right - left, bottom - top);
}

/// <summary>
/// Visual style for simple panels and buttons.
/// </summary>
public struct UIStyle
{
    /// <summary>Background color RGBA (0..1)</summary>
    public Vector4 BackgroundColor;   // RGBA 0..1
    /// <summary>Border color RGBA (0..1)</summary>
    public Vector4 BorderColor;       // RGBA 0..1
    /// <summary>Border thickness in pixels</summary>
    public float BorderThickness;
    /// <summary>Corner radius in pixels</summary>
    public float CornerRadius;

    /// <summary>Default look for panels.</summary>
    public static UIStyle DefaultPanel => new()
    {
        BackgroundColor = new Vector4(0.10f, 0.12f, 0.16f, 0.92f),
        BorderColor = new Vector4(0f, 0f, 0f, 0.55f),
        BorderThickness = 1f,
        CornerRadius = 4f
    };

    /// <summary>Default look for buttons.</summary>
    public static UIStyle DefaultButton => new()
    {
        BackgroundColor = new Vector4(0.18f, 0.22f, 0.28f, 0.95f),
        BorderColor = new Vector4(0.06f, 0.65f, 0.85f, 1f),
        BorderThickness = 1.5f,
        CornerRadius = 6f
    };
}

/// <summary>
/// Simple visual components.
/// </summary>
/// <summary>Visual panel marker with a style.</summary>
public struct UIPanel
{
    /// <summary>Panel style</summary>
    public UIStyle Style;
}

/// <summary>Clickable button. Emits UIButtonClicked event via UIInputSystem.</summary>
public struct UIButton
{
    /// <summary>Button visual style</summary>
    public UIStyle Style;
    /// <summary>Optional command identifier</summary>
    public string Command;
}

/// <summary>Simple text label.</summary>
public struct UILabel
{
    /// <summary>Text content</summary>
    public string Text;
    /// <summary>Text color</summary>
    public Vector4 Color;
    /// <summary>Font size in points/pixels</summary>
    public float FontSize;
}
