using System;
using System.Collections.Generic;
using System.Linq;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// A marker interface to define a relationship type.
    /// Relationship tags should be empty structs that implement this interface.
    /// Example: public struct IsChildOf : IRelationship {}
    /// </summary>
    public interface IRelationship { }

    /// <summary>
    /// A marker interface to define a bidirectional relationship type.
    /// This inherits from IRelationship and signals to the World that when a relationship
    /// A -> B of this type is added, the corresponding B -> A relationship should also be added automatically.
    /// Likewise, removing one side of the relationship will also remove the other.
    /// <example>
    /// public struct IsMarriedTo : IBidirectionalRelationship { }
    /// </example>
    /// </summary>
    public interface IBidirectionalRelationship : IRelationship { }

    /// <summary>
    /// Defines the non-generic API for a relationship storage.
    /// </summary>
    public interface IRelationshipStorage
    {
        // **MODIFIED**: Methods now use the full Entity struct for the source.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void Add(Entity source, Entity target);
        public void Remove(Entity source, Entity target);
        public bool Has(Entity source, Entity target);
        public IEnumerable<Entity> GetTargets(Entity source);

        // This can remain as-is since it just needs the ID for cleanup.
        public void RemoveAll(int entityId);
        public IEnumerable<Entity> GetSources(Entity target);
    }

    /// <summary>
    /// Stores relationships of a specific type <typeparamref name="T"/>.
    /// It uses two dictionaries to maintain forward (source -> targets) and reverse (target -> sources) mappings for efficient lookups.
    /// </summary>
    internal class RelationshipStorage<T> : IRelationshipStorage where T : struct, IRelationship
    {
        // Forward mapping: Source Entity ID -> List of Target Entities
        private readonly Dictionary<int, List<Entity>> _forwardMap = new();
        // Reverse mapping: Target Entity ID -> List of Source Entities
        private readonly Dictionary<int, List<Entity>> _reverseMap = new();

        public void Add(Entity source, Entity target)
        {
            // Add to forward map
            if (!_forwardMap.TryGetValue(source.Id, out var targets))
            {
                targets = [];
                _forwardMap[source.Id] = targets;
            }
            if (!targets.Contains(target))
            {
                targets.Add(target);
            }

            // Add to reverse map
            if (!_reverseMap.TryGetValue(target.Id, out var sources))
            {
                sources = [];
                _reverseMap[target.Id] = sources;
            }
            // **MODIFIED**: Store the full, correct source entity handle.
            if (!sources.Contains(source))
            {
                sources.Add(source);
            }
        }

        public void Remove(Entity source, Entity target)
        {
            // Remove from forward map
            if (_forwardMap.TryGetValue(source.Id, out var targets))
            {
                targets.Remove(target);
                if (targets.Count == 0) _forwardMap.Remove(source.Id);
            }

            // Remove from reverse map
            if (_reverseMap.TryGetValue(target.Id, out var sources))
            {
                // **MODIFIED**: Use the full entity for removal.
                sources.Remove(source);
                if (sources.Count == 0) _reverseMap.Remove(target.Id);
            }
        }

        public bool Has(Entity source, Entity target)
        {
            return _forwardMap.TryGetValue(source.Id, out var targets) && targets.Contains(target);
        }

        public IEnumerable<Entity> GetTargets(Entity source)
        {
            return _forwardMap.TryGetValue(source.Id, out var targets) ? targets : Enumerable.Empty<Entity>();
        }

        public IEnumerable<Entity> GetSources(Entity target)
        {
            return _reverseMap.TryGetValue(target.Id, out var sources) ? sources : Enumerable.Empty<Entity>();
        }

        public void RemoveAll(int entityId)
        {
            // Remove all relationships where the entity was a source
            if (_forwardMap.TryGetValue(entityId, out var targets))
            {
                // For each entity this one was pointing to...
                foreach (var target in targets)
                {
                    // ...go to its reverse map and remove the entry for this entity.
                    if (_reverseMap.TryGetValue(target.Id, out var reverseSources))
                    {
                        reverseSources.RemoveAll(e => e.Id == entityId);
                        if (reverseSources.Count == 0) _reverseMap.Remove(target.Id);
                    }
                }
                _forwardMap.Remove(entityId);
            }

            // Remove all relationships where the entity was a target
            if (_reverseMap.TryGetValue(entityId, out var sources))
            {
                // For each entity that was pointing to this one...
                foreach (var source in sources)
                {
                    // ...go to its forward map and remove the entry for this entity.
                    if (_forwardMap.TryGetValue(source.Id, out var sourceTargets))
                    {
                        sourceTargets.RemoveAll(e => e.Id == entityId);
                        if (sourceTargets.Count == 0) _forwardMap.Remove(source.Id);
                    }
                }
                _reverseMap.Remove(entityId);
            }
        }
    }
}
