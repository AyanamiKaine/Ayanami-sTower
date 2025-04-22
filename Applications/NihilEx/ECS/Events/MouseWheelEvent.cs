using System;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents a mouse wheel event with scroll information.
/// </summary>
/// <param name="ScrollX">The horizontal scroll amount.</param>
/// <param name="ScrollY">The vertical scroll amount.</param>
/// <param name="Direction">The direction of the mouse wheel movement.</param>
public record struct MouseWheelEvent(float ScrollX, float ScrollY, SDL_MouseWheelDirection Direction);
