using System;
using SDL3;

namespace AyanamisTower.NihilEx.ECS.Events;

/// <summary>
/// Represents a key down event.
/// </summary>
/// <param name="Keycode">The keycode of the pressed key.</param>
/// <param name="Modifiers">The keyboard modifiers active during the key press.</param>
/// <param name="IsRepeat">Indicates whether this is a repeat key press.</param>
public record struct KeyDownEvent(SDLWrapper.Key Keycode, SDLWrapper.KeyModifier Modifiers, bool IsRepeat);