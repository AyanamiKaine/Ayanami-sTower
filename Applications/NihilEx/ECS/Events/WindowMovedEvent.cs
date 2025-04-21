using System;
using static SDL3.SDL;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents an event that occurs when a window is moved to a new position.
/// </summary>
/// <param name="X">The new X coordinate of the window.</param>
/// <param name="Y">The new Y coordinate of the window.</param>
public record struct WindowMovedEvent(float X, float Y);
