using System;
using AyanamisTower.NihilEx.SDLWrapper;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents a SDL mouse button down event.
/// </summary>
/// <param name="MouseButton">Which button was pressed</param>
/// <param name="X">The X coordinate where the mouse button was pressed.</param>
/// <param name="Y">The Y coordinate where the mouse button was pressed.</param>
/// <param name="Clicks">The number of clicks (1 for single-click, 2 for double-click, etc).</param>
public record struct MouseButtonDownEvent(MouseButton MouseButton, float X, float Y, byte Clicks);
