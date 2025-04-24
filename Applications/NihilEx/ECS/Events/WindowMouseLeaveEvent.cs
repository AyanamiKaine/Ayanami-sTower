using System;
using AyanamisTower.NihilEx.SDLWrapper;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents an event that occurs when the mouse enters a window.
/// </summary>
/// <param name="MouseButton">The mouse button flags indicating which button was pressed.</param>
/// <param name="Down">Indicates whether the button is pressed down.</param>
/// <param name="X">The X coordinate of the mouse cursor. Relative to the mouse position on the screen.</param>
/// <param name="Y">The Y coordinate of the mouse cursor. Relative to the mouse position on the screen.</param>
/// <param name="Clicks">The number of clicks performed.</param>
public record struct WindowMouseLeaveEvent(MouseButton MouseButton, bool Down, float X, float Y, byte Clicks);