using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Rendering;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Systems;

/// <summary>
/// Renders UI elements using the provided IUIRenderer.
/// </summary>
public sealed class UIDrawSystem : ISystem
{
    private readonly IUIRenderer _renderer;
    /// <summary>
    /// Constructor for UIDrawSystem.
    /// </summary>
    public UIDrawSystem(IUIRenderer renderer)
    {
        _renderer = renderer;
    }
    /// <summary>
    /// Gets or sets the name of the system.
    /// </summary>
    public string Name { get; set; } = "UIDrawSystem";
    /// <summary>
    /// Gets or sets a value indicating whether the system is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Updates the UIDrawSystem.
    /// </summary>
        public void Update(World world, float delta)
    {
        // Gather drawables
        var items = new List<(int z, Entity e)>();
        foreach (var e in world.GetAllEntities())
        {
            if (!e.Has<UIElement>() || !e.Has<RectTransform>()) continue;
            var u = e.GetCopy<UIElement>();
            // Visibility is controlled via the Invisible tag: presence = hidden
            if (e.Has<Invisible>()) continue;
            items.Add((u.ZIndex, e));
        }
        items.Sort((a, b) => a.z.CompareTo(b.z));

        _renderer.Begin();
        foreach (var (_, e) in items)
        {
            var rect = e.GetCopy<RectTransform>().Computed;
            if (e.Has<UIPanel>())
            {
                var style = e.GetCopy<UIPanel>().Style;
                _renderer.DrawPanel(rect, style);
            }
            if (e.Has<UIButton>())
            {
                var b = e.GetCopy<UIButton>();
                Vector4 textColor = new(1, 1, 1, 1);
                float font = 16f;
                if (e.Has<UILabel>())
                {
                    var l = e.GetCopy<UILabel>();
                    textColor = l.Color; font = l.FontSize;
                }
                _renderer.DrawButton(rect, b.Style, e.Has<UILabel>() ? e.GetCopy<UILabel>().Text : string.Empty, textColor, font);
            }
            else if (e.Has<UILabel>())
            {
                var l = e.GetCopy<UILabel>();
                _renderer.DrawLabel(rect, l.Text, l.Color, l.FontSize);
            }
        }
        _renderer.End();
    }
}
