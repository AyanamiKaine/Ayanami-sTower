using System;
using AyanamisTower.NihilEx.SDLWrapper;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents a mouse motion event with information about the mouse state and position.
/// </summary>
/// <param name="MouseState">The state of the mouse buttons.</param>
/// <param name="X">The X coordinate of the mouse cursor.</param>
/// <param name="Y">The Y coordinate of the mouse cursor.</param>
/// <param name="XRel">The relative motion in the X direction.</param>
/// <param name="YRel">The relative motion in the Y direction.</param>
public record struct MouseMotionEvent(
    MouseButton MouseState,
    float X,
    float Y,
    float XRel,
    float YRel
);
