using System;
using System.Collections.Generic;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta
{
    /// <summary>
    /// Lightweight service to register per-Entity mouse interaction handlers.
    /// It does not try to resolve pick results itself; Program.cs should call
    /// <see cref="NotifyHover"/> and <see cref="NotifyClick"/> with the resolved
    /// hovered/clicked entity (or null).
    /// </summary>
    public sealed class MouseInteractionService
    {
        private class Entry
        {
            public Action<Entity>? OnClick;
            public Action<Entity>? OnEnter;
            public Action<Entity>? OnExit;
            public bool IsHovered;
        }

        private readonly Dictionary<Entity, Entry> _entries = new();
        private Entity? _currentHover;

        /// <summary>
        /// Register a callback invoked when the given entity is clicked. The callback
        /// receives the entity that triggered the event.
        /// </summary>
        public void RegisterClick(Entity e, Action<Entity> handler)
        {
            if (!_entries.TryGetValue(e, out var en))
            {
                en = new Entry();
                _entries[e] = en;
            }
            en.OnClick += handler;
        }

        /// <summary>
        /// Register a callback invoked when the mouse begins hovering the given entity.
        /// The callback receives the entity that triggered the event.
        /// </summary>
        public void RegisterMouseEnter(Entity e, Action<Entity> handler)
        {
            if (!_entries.TryGetValue(e, out var en))
            {
                en = new Entry();
                _entries[e] = en;
            }
            en.OnEnter += handler;
        }

        /// <summary>
        /// Register a callback invoked when the mouse stops hovering the given entity.
        /// The callback receives the entity that triggered the event.
        /// </summary>
        public void RegisterMouseExit(Entity e, Action<Entity> handler)
        {
            if (!_entries.TryGetValue(e, out var en))
            {
                en = new Entry();
                _entries[e] = en;
            }
            en.OnExit += handler;
        }

        /// <summary>
        /// Remove all mouse handlers registered for the given entity.
        /// </summary>
        public void UnregisterAllHandlers(Entity e)
        {
            _entries.Remove(e);
        }

        /// <summary>
        /// Call this every frame (or whenever you sample the mouse) with the currently hovered entity
        /// (or null). The service will fire Enter/Exit callbacks as the hovered entity changes.
        /// </summary>
        /// <summary>
        /// Call this every frame (or whenever you sample the mouse) with the currently hovered entity
        /// (or null). The service will fire Enter/Exit callbacks as the hovered entity changes.
        /// </summary>
        public void NotifyHover(Entity? hovered)
        {
            if (_currentHover == hovered) return;

            // Fire exit on previous
            if (_currentHover.HasValue && _entries.TryGetValue(_currentHover.Value, out var prev))
            {
                if (prev.IsHovered)
                {
                    prev.IsHovered = false;
                    try { prev.OnExit?.Invoke(_currentHover.Value); } catch { }
                }
            }

            // Fire enter on new
            if (hovered.HasValue && _entries.TryGetValue(hovered.Value, out var next))
            {
                if (!next.IsHovered)
                {
                    next.IsHovered = true;
                    try { next.OnEnter?.Invoke(hovered.Value); } catch { }
                }
            }

            _currentHover = hovered;
        }

        /// <summary>
        /// Notify that an entity was clicked. Will invoke the registered click handler if present.
        /// </summary>
        /// <summary>
        /// Notify that an entity was clicked. Will invoke the registered click handler if present.
        /// </summary>
        public void NotifyClick(Entity? clicked)
        {
            if (!clicked.HasValue) return;
            if (_entries.TryGetValue(clicked.Value, out var en))
            {
                try { en.OnClick?.Invoke(clicked.Value); } catch { }
            }
        }
    }

    /// <summary>
    /// Convenience extension methods for registering mouse handlers on entities.
    /// </summary>
    public static class EntityMouseExtensions
    {
        /// <summary>Register a click handler that receives the entity when invoked.</summary>
        public static void OnClick(this Entity e, MouseInteractionService svc, Action<Entity> handler) => svc.RegisterClick(e, handler);

        /// <summary>Register a mouse-enter handler that receives the entity when invoked.</summary>
        public static void OnMouseEnter(this Entity e, MouseInteractionService svc, Action<Entity> handler) => svc.RegisterMouseEnter(e, handler);

        /// <summary>Register a mouse-exit handler that receives the entity when invoked.</summary>
        public static void OnMouseExit(this Entity e, MouseInteractionService svc, Action<Entity> handler) => svc.RegisterMouseExit(e, handler);

        /// <summary>Remove all mouse handlers previously registered on this entity via the given service.</summary>
        public static void RemoveMouseHandlers(this Entity e, MouseInteractionService svc) => svc.UnregisterAllHandlers(e);
    }
}
