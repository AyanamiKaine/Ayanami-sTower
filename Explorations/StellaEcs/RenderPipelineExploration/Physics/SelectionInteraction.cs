using System;
using System.Collections.Generic;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// Service that lets entities register callbacks for when they are selected
/// (e.g., via a selection rectangle). Program code calls NotifySelected to trigger them.
/// </summary>
public sealed class SelectionInteractionService
{
    private readonly Dictionary<Entity, Action<Entity>> _onSelected = new();

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
    /// Notify a list of entities have been selected.
    /// </summary>
    public void NotifySelected(IEnumerable<Entity> entities)
    {
        foreach (var e in entities)
        {
            NotifySelected(e);
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
}
