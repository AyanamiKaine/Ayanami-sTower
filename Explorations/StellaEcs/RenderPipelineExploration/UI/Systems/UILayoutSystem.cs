using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Systems;

/// <summary>
/// Computes RectTransform.Computed for all UI nodes each frame. Anchors relative to parent.
/// Assumes screen size is provided externally.
/// </summary>
public sealed class UILayoutSystem : ISystem
{
    private readonly Func<Vector2> _getScreenSize;

    /// <inheritdoc/>
    public string Name { get; set; } = "UILayoutSystem";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Creates a new instance of the <see cref="UILayoutSystem"/> class.
    /// </summary>
    /// <param name="getScreenSize"></param>
    public UILayoutSystem(Func<Vector2> getScreenSize)
    {
        _getScreenSize = getScreenSize;
    }
    /// <summary>
    /// Updates the layout system.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="delta"></param>
    public void Update(World world, float delta)
    {
        var screen = _getScreenSize();

        // Build children lists per parent
        var children = new Dictionary<Entity, List<Entity>>();
        foreach (var e in world.GetAllEntities())
        {
            if (!e.Has<UIElement>() || !e.Has<RectTransform>()) continue;
            var u = e.GetCopy<UIElement>();
            if (!children.TryGetValue(u.Parent, out var list))
            {
                list = new List<Entity>(8);
                children[u.Parent] = list;
            }
            list.Add(e);
        }

        // Roots are entries under default(Entity)
        var rootParent = default(Entity);
        if (!children.TryGetValue(rootParent, out var roots)) return;

        // Layout DFS
        foreach (var root in roots)
        {
            LayoutNode(world, root, new UIRect(0, 0, screen.X, screen.Y), children);
        }
    }

    private static void LayoutNode(World world, Entity e, UIRect parentRect, Dictionary<Entity, List<Entity>> children)
    {
        ref var rt = ref e.GetMut<RectTransform>();
        var anchorMin = rt.AnchorMin; var anchorMax = rt.AnchorMax;
        var offMin = rt.OffsetMin; var offMax = rt.OffsetMax;

        float left = parentRect.Left + parentRect.Width * anchorMin.X + offMin.X;
        float top = parentRect.Top + parentRect.Height * anchorMin.Y + offMin.Y;
        float right = parentRect.Left + parentRect.Width * anchorMax.X + offMax.X;
        float bottom = parentRect.Top + parentRect.Height * anchorMax.Y + offMax.Y;

        rt.Computed = UIRect.FromMinMax(left, top, right, bottom);

        if (children.TryGetValue(e, out var kids))
        {
            foreach (var child in kids)
            {
                LayoutNode(world, child, rt.Computed, children);
            }
        }
    }
}
