using System;
using System.Collections.Generic;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// Service that tracks a selection set and lets entities register callbacks for when they are selected.
/// Supports replace/add/subtract semantics when applying a selection operation.
/// </summary>
public sealed class SelectionInteractionService
{
    /// <summary>
    /// How a new selection should be applied to the current selection set.
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>Replace current selection with new candidates.</summary>
        Replace,
        /// <summary>Add new candidates to the current selection.</summary>
        Add,
        /// <summary>Remove candidates from the current selection.</summary>
        Subtract
    }

    private readonly Dictionary<Entity, Action<Entity>> _onSelected = new();
    private readonly Dictionary<Entity, Action<Entity>> _onDeselected = new();
    private readonly HashSet<Entity> _selected = new();

    /// <summary>
    /// Gets the current selection set.
    /// </summary>
    public IReadOnlyCollection<Entity> CurrentSelection => _selected;

    /// <summary>
    /// Register a callback invoked when the entity is selected.
    /// Multiple registrations append; handlers are invoked in registration order.
    /// </summary>
    public void RegisterSelection(Entity e, Action<Entity> handler)
    {
        if (_onSelected.TryGetValue(e, out var existing))
        {
            existing += handler;
            _onSelected[e] = existing;
        }
        else
        {
            _onSelected[e] = handler;
        }
    }

    /// <summary>
    /// Remove all selection handlers for an entity.
    /// </summary>
    public void UnregisterAll(Entity e) => _onSelected.Remove(e);
    /// <summary>
    /// Remove all deselection handlers for an entity.
    /// </summary>
    public void UnregisterDeselection(Entity e) => _onDeselected.Remove(e);

    /// <summary>
    /// Notify that an entity has been selected.
    /// </summary>
    public void NotifySelected(Entity e)
    {
        if (_onSelected.TryGetValue(e, out var handlers))
        {
            try { handlers?.Invoke(e); } catch { /* swallow handler errors */ }
        }
    }

    /// <summary>
    /// Register a callback invoked when the entity is deselected.
    /// Multiple registrations append; handlers are invoked in registration order.
    /// </summary>
    public void RegisterDeselection(Entity e, Action<Entity> handler)
    {
        if (_onDeselected.TryGetValue(e, out var existing))
        {
            existing += handler;
            _onDeselected[e] = existing;
        }
        else
        {
            _onDeselected[e] = handler;
        }
    }

    /// <summary>
    /// Notify that an entity has been deselected.
    /// </summary>
    public void NotifyDeselected(Entity e)
    {
        if (_onDeselected.TryGetValue(e, out var handlers))
        {
            try { handlers?.Invoke(e); } catch { /* swallow handler errors */ }
        }
    }

    /// <summary>
    /// Clears the current selection set.
    /// If notify is true, invokes deselection handlers for entities that were selected.
    /// </summary>
    public void ClearSelection(bool notify = true)
    {
        if (notify && _selected.Count > 0)
        {
            // Create a snapshot to avoid modifying during enumeration.
            var removed = new List<Entity>(_selected);
            foreach (var e in removed)
            {
                NotifyDeselected(e);
            }
        }
        _selected.Clear();
    }

    /// <summary>
    /// Apply a selection operation over the current selection using the specified mode.
    /// </summary>
    public void ApplySelection(IEnumerable<Entity> candidates, SelectionMode mode)
    {
        switch (mode)
        {
            case SelectionMode.Replace:
                {
                    // Build new set
                    var newSet = new HashSet<Entity>();
                    foreach (var e in candidates)
                    {
                        newSet.Add(e);
                    }

                    // Compute removed (present before, absent now) and added (absent before, present now)
                    var removed = new List<Entity>();
                    foreach (var prev in _selected)
                    {
                        if (!newSet.Contains(prev))
                        {
                            removed.Add(prev);
                        }
                    }

                    var added = new List<Entity>();
                    foreach (var now in newSet)
                    {
                        if (!_selected.Contains(now))
                        {
                            added.Add(now);
                        }
                    }

                    // Notify deselections first, then apply new set, then notify selections
                    foreach (var e in removed) NotifyDeselected(e);
                    _selected.Clear();
                    foreach (var e in newSet) _selected.Add(e);
                    foreach (var e in added) NotifySelected(e);
                    break;
                }
            case SelectionMode.Add:
                {
                    foreach (var e in candidates)
                    {
                        if (_selected.Add(e))
                        {
                            NotifySelected(e);
                        }
                    }
                    break;
                }
            case SelectionMode.Subtract:
                {
                    foreach (var e in candidates)
                    {
                        if (_selected.Remove(e))
                        {
                            NotifyDeselected(e);
                        }
                    }
                    break;
                }
        }
    }
}

/// <summary>
/// Convenience extensions for selection registration.
/// </summary>
public static class EntitySelectionExtensions
{
    /// <summary>
    /// Register a callback to be invoked when this entity is selected.
    /// </summary>
    public static void OnSelection(this Entity e, SelectionInteractionService svc, Action<Entity> handler)
        => svc.RegisterSelection(e, handler);

    /// <summary>
    /// Remove all selection handlers that were registered for this entity.
    /// </summary>
    public static void RemoveSelectionHandlers(this Entity e, SelectionInteractionService svc)
        => svc.UnregisterAll(e);

    /// <summary>
    /// Returns true if this entity is currently selected.
    /// </summary>
    public static bool IsSelected(this Entity e, SelectionInteractionService svc)
        => svc.CurrentSelection.Contains(e);

    /// <summary>
    /// Register a callback to be invoked when this entity is deselected.
    /// </summary>
    public static void OnDeselection(this Entity e, SelectionInteractionService svc, Action<Entity> handler)
        => svc.RegisterDeselection(e, handler);

    /// <summary>
    /// Remove all deselection handlers that were registered for this entity.
    /// </summary>
    public static void RemoveDeselectionHandlers(this Entity e, SelectionInteractionService svc)
        => svc.UnregisterDeselection(e);
}
