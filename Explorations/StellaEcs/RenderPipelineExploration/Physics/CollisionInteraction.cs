using System;
using System.Collections.Generic;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// Service to register per-Entity collision callbacks. It listens to a
/// <see cref="PhysicsManager"/>'s collision events and forwards them to
/// per-entity handlers registered here.
/// </summary>
public sealed class CollisionInteractionService : IDisposable
{
    private class Entry
    {
        public Action<Entity, Entity>? OnEnter;
        public Action<Entity, Entity>? OnStay;
        public Action<Entity, Entity>? OnExit;
    }

    private readonly Dictionary<Entity, Entry> _entries = new();
    private readonly PhysicsManager _physicsManager;
    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionInteractionService"/> class.
    /// </summary>
    /// <param name="physicsManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CollisionInteractionService(PhysicsManager physicsManager)
    {
        _physicsManager = physicsManager ?? throw new ArgumentNullException(nameof(physicsManager));
        _physicsManager.OnCollisionEnter += HandleEnter;
        _physicsManager.OnCollisionStay += HandleStay;
        _physicsManager.OnCollisionExit += HandleExit;
    }

    private void HandleEnter(Entity a, Entity b)
    {
        if (_entries.TryGetValue(a, out var ea))
        {
            try { ea.OnEnter?.Invoke(a, b); } catch { }
        }
        if (_entries.TryGetValue(b, out var eb))
        {
            try { eb.OnEnter?.Invoke(a, b); } catch { }
        }
    }

    private void HandleStay(Entity a, Entity b)
    {
        if (_entries.TryGetValue(a, out var ea))
        {
            try { ea.OnStay?.Invoke(a, b); } catch { }
        }
        if (_entries.TryGetValue(b, out var eb))
        {
            try { eb.OnStay?.Invoke(a, b); } catch { }
        }
    }

    private void HandleExit(Entity a, Entity b)
    {
        if (_entries.TryGetValue(a, out var ea))
        {
            try { ea.OnExit?.Invoke(a, b); } catch { }
        }
        if (_entries.TryGetValue(b, out var eb))
        {
            try { eb.OnExit?.Invoke(a, b); } catch { }
        }
    }
    /// <summary>
    /// Registers a handler for the "enter" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="handler"></param>
    public void RegisterEnter(Entity e, Action<Entity, Entity> handler)
    {
        if (!_entries.TryGetValue(e, out var en))
        {
            en = new Entry();
            _entries[e] = en;
        }
        en.OnEnter += handler;
    }
    /// <summary>
    /// Registers a handler for the "stay" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="handler"></param>
    public void RegisterStay(Entity e, Action<Entity, Entity> handler)
    {
        if (!_entries.TryGetValue(e, out var en))
        {
            en = new Entry();
            _entries[e] = en;
        }
        en.OnStay += handler;
    }
    /// <summary>
    /// Registers a handler for the "exit" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="handler"></param>
    public void RegisterExit(Entity e, Action<Entity, Entity> handler)
    {
        if (!_entries.TryGetValue(e, out var en))
        {
            en = new Entry();
            _entries[e] = en;
        }
        en.OnExit += handler;
    }

    /// <summary>
    /// Remove all collision handlers for the given entity.
    /// </summary>
    public void UnregisterAllHandlers(Entity e)
    {
        _entries.Remove(e);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _physicsManager.OnCollisionEnter -= HandleEnter;
        _physicsManager.OnCollisionStay -= HandleStay;
        _physicsManager.OnCollisionExit -= HandleExit;
        _entries.Clear();
    }
}

/// <summary>
/// Convenience extension methods for registering collision handlers on entities.
/// Handlers receive both entities participating in the collision (order: A,B as
/// raised by the PhysicsManager).
/// </summary>
public static class EntityCollisionExtensions
{
    /// <summary>
    /// Registers a handler for the "enter" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="svc"></param>
    /// <param name="handler"></param>
    public static void OnCollisionEnter(this Entity e, CollisionInteractionService svc, Action<Entity, Entity> handler) => svc.RegisterEnter(e, handler);
    /// <summary>
    /// Registers a handler for the "stay" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="svc"></param>
    /// <param name="handler"></param>
    public static void OnCollisionStay(this Entity e, CollisionInteractionService svc, Action<Entity, Entity> handler) => svc.RegisterStay(e, handler);
    /// <summary>
    /// Registers a handler for the "exit" collision event.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="svc"></param>
    /// <param name="handler"></param>
    public static void OnCollisionExit(this Entity e, CollisionInteractionService svc, Action<Entity, Entity> handler) => svc.RegisterExit(e, handler);
    /// <summary>
    /// Removes all collision handlers for the given entity.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="svc"></param>
    public static void RemoveCollisionHandlers(this Entity e, CollisionInteractionService svc) => svc.UnregisterAllHandlers(e);
}
