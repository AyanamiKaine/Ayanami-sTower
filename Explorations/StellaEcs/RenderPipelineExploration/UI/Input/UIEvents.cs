using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta.UI.Input;

/// <summary>
/// Event triggered when a button is clicked.
/// </summary>
public readonly struct UIButtonClicked
{
    /// <summary>
    /// The entity that was clicked.
    /// </summary>
    public readonly Entity Entity;
    /// <summary>
    /// The command associated with the button click.
    /// </summary>
    public readonly string Command;
    /// <summary>
    /// Creates a new instance of the <see cref="UIButtonClicked"/> struct.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="command"></param>
    public UIButtonClicked(Entity e, string command) { Entity = e; Command = command; }
}
/// <summary>
/// Interface for handling UI events.
/// </summary>
public interface IUIEventSink
{
    /// <summary>
    /// Emits an event.
    /// </summary>
    void Emit<T>(in T evt);
}

/// <summary>
/// Event bus for UI events.
/// </summary>
public sealed class UIEventBus : IUIEventSink
{
    /// <summary>
    /// Event triggered when a button is clicked.
    /// </summary>
    public event Action<UIButtonClicked>? OnButtonClicked;
    /// <summary>
    /// Emits an event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="evt"></param>
    public void Emit<T>(in T evt)
    {
        if (evt is UIButtonClicked bc) OnButtonClicked?.Invoke(bc);
    }
}
