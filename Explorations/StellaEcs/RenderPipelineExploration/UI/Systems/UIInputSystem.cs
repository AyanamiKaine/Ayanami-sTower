using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Components;
using AyanamisTower.StellaEcs.StellaInvicta.UI.Input;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Systems;

/// <summary>
/// Basic mouse input routing for UI. You provide current mouse position and click state.
/// </summary>
public sealed class UIInputSystem : ISystem
{
    private readonly Func<Vector2> _getMouse;
    private readonly Func<bool> _getMousePressed;
    private readonly Func<bool> _getMouseReleased;
    private readonly IUIEventSink _events;

    private Entity _pressedOn = default;
    /// <summary>
    /// Constructor for UIInputSystem.
    /// </summary>
    public UIInputSystem(Func<Vector2> getMouse, Func<bool> getMousePressed, Func<bool> getMouseReleased, IUIEventSink events)
    {
        _getMouse = getMouse;
        _getMousePressed = getMousePressed;
        _getMouseReleased = getMouseReleased;
        _events = events;
    }
    
    /// <inheritdoc/>
    public string Name { get; set; } = "UIInputSystem";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc/>
        public void Update(World world, float delta)
    {
        var mouse = _getMouse();
        bool pressed = _getMousePressed();
        bool released = _getMouseReleased();

        // Collect hit-testable elements sorted by Z (topmost last)
        var stack = new List<(int z, Entity e, UIRect r)>();
        foreach (var e in world.GetAllEntities())
        {
            if (!e.Has<UIElement>() || !e.Has<RectTransform>()) continue;
            var u = e.GetCopy<UIElement>();
            // Skip invisible elements (Invisible tag means hidden)
            if (e.Has<Invisible>()) continue;
            var r = e.GetCopy<RectTransform>().Computed;
            stack.Add((u.ZIndex, e, r));
        }
        stack.Sort((a, b) => a.z.CompareTo(b.z));

        Entity hit = default;
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (stack[i].r.Contains(mouse)) { hit = stack[i].e; break; }
        }

        if (pressed)
        {
            _pressedOn = hit;
        }
        if (released)
        {
            if (_pressedOn != default && hit == _pressedOn && hit.Has<UIButton>())
            {
                var btn = hit.GetCopy<UIButton>();
                _events.Emit(new UIButtonClicked(hit, btn.Command));
            }
            _pressedOn = default;
        }
    }
}
