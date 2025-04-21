using System;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents an event that occurs when the window is resized.
/// </summary>
/// <param name="Width">The new width of the window.</param>
/// <param name="Height">The new height of the window.</param>
public record struct WindowResize(int Width, int Height);
