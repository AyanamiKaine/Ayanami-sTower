using System;
using SDL3;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents a key up event triggered when a keyboard key is released.
/// </summary>
/// <param name="Keycode">The code of the released key.</param>
/// <param name="Modifiers">The modifier keys active during the event.</param>
public record struct KeyUpEvent(SDL.SDL_Keycode Keycode, SDL.SDL_Keymod Modifiers);