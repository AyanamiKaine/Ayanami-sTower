using System;
using AyanamisTower.NihilEx.SDLWrapper;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents an event that occurs when the mouse enters a window.
/// </summary>
/// <param name="MouseButton">The mouse button flags indicating which button was pressed.</param>
/// <param name="LocalX">The X coordinate of the mouse relative to the window.</param>
/// <param name="LocalY">The Y coordinate of the mouse relative to the window.</param>
/// <param name="GlobalX">The X coordinate of the mouse in screen coordinates.</param>
/// <param name="GlobalY">The Y coordinate of the mouse in screen coordinates.</param>
public record struct WindowMouseLeaveEvent(
    MouseButton MouseButton,
    float LocalX,
    float LocalY,
    float GlobalX,
    float GlobalY
);
