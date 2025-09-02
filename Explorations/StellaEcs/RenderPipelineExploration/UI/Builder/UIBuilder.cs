using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Builder;

/// <summary>
/// Fluent builder for declarative UI creation on top of ECS entities.
/// Each call creates an entity and attaches UI components.
/// </summary>
public sealed class UIBuilder
{
    private readonly World _world;
    private readonly Entity _parent;

    /// <summary>
    /// Creates a builder with the provided parent. Use default(Entity) for root.
    /// </summary>
    public UIBuilder(World world, Entity parent = default)
    {
        _world = world;
        _parent = parent;
    }

    /// <summary>
    /// Adds a panel element and returns a nested builder to add children.
    /// </summary>
    public UIBuilder Panel(out Entity entity, Action<UIBuilder>? children = null, RectTransform? rect = null, UIStyle? style = null, int z = 0, bool visible = true)
    {
        entity = _world.CreateEntity()
        .Set(new UIElement { Parent = _parent, ZIndex = z, Visible = visible })
        .Set(new UIPanel { Style = style ?? UIStyle.DefaultPanel })
        .Set(rect ?? DefaultFill());

        if (children != null)
        {
            var childBuilder = new UIBuilder(_world, entity);
            children(childBuilder);
        }
        return this;
    }

    /// <summary>
    /// Adds a label.
    /// </summary>
    public UIBuilder Label(string text, out Entity entity, RectTransform? rect = null, Vector4? color = null, float fontSize = 16f, int z = 0, bool visible = true)
    {
        entity = _world.CreateEntity()
            .Set(new UIElement { Parent = _parent, ZIndex = z, Visible = visible })
            .Set(rect ?? DefaultWrapContent())
            .Set(new UILabel { Text = text, Color = color ?? new Vector4(1, 1, 1, 1), FontSize = fontSize });
        return this;
    }

    /// <summary>
    /// Adds a button with optional command name.
    /// </summary>
    public UIBuilder Button(string text, out Entity entity, string command = "", RectTransform? rect = null, UIStyle? style = null, int z = 0, bool visible = true)
    {
        entity = _world.CreateEntity()
            .Set(new UIElement { Parent = _parent, ZIndex = z, Visible = visible })
            .Set(rect ?? DefaultHugTop(120, 32))
            .Set(new UIButton { Style = style ?? UIStyle.DefaultButton, Command = command })
            .Set(new UILabel { Text = text, Color = new Vector4(1, 1, 1, 1), FontSize = 16f });
        return this;
    }

    /// <summary>
    /// Utility: creates a RectTransform that fills its parent.
    /// </summary>
    public static RectTransform DefaultFill()
    {
        return new RectTransform
        {
            AnchorMin = new Vector2(0, 0),
            AnchorMax = new Vector2(1, 1),
            OffsetMin = Vector2.Zero,
            OffsetMax = Vector2.Zero,
            Pivot = new Vector2(0.5f, 0.5f)
        };
    }

    /// <summary>
    /// Utility: wraps content (anchors at top-left with size via offsets as width/height); no text measurement yet.
    /// </summary>
    public static RectTransform DefaultWrapContent(float width = 64, float height = 20)
    {
        return new RectTransform
        {
            AnchorMin = new Vector2(0, 0),
            AnchorMax = new Vector2(0, 0),
            OffsetMin = Vector2.Zero,
            OffsetMax = new Vector2(width, height),
            Pivot = new Vector2(0, 0)
        };
    }

    /// <summary>
    /// Utility: aligns to top with a fixed size.
    /// </summary>
    public static RectTransform DefaultHugTop(float width, float height, float margin = 8)
    {
        return new RectTransform
        {
            AnchorMin = new Vector2(0, 0),
            AnchorMax = new Vector2(0, 0),
            OffsetMin = new Vector2(margin, margin),
            OffsetMax = new Vector2(margin + width, margin + height),
            Pivot = new Vector2(0, 0)
        };
    }
}
