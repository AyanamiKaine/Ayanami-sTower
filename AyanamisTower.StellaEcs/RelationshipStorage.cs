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
    /// Defines the non-generic API for a relationship storage.
    /// </summary>
    internal interface IRelationshipStorage
    {
        void Add(int sourceId, Entity target);
        void Remove(int sourceId, Entity target);
        bool Has(int sourceId, Entity target);
        IEnumerable<Entity> GetTargets(int sourceId);
        void RemoveAll(int entityId);
        IEnumerable<Entity> GetSources(Entity target);
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

        public void Add(int sourceId, Entity target)
        {
            // Add to forward map
            if (!_forwardMap.TryGetValue(sourceId, out var targets))
            {
                targets = new List<Entity>();
                _forwardMap[sourceId] = targets;
            }
            if (!targets.Contains(target))
            {
                targets.Add(target);
            }

            // Add to reverse map
            if (!_reverseMap.TryGetValue(target.Id, out var sources))
            {
                sources = new List<Entity>();
                _reverseMap[target.Id] = sources;
            }
            var sourceEntity = new Entity(sourceId, 0, null); // We only need the ID for the source list
            if (!sources.Any(e => e.Id == sourceId))
            {
                sources.Add(sourceEntity);
            }
        }

        public void Remove(int sourceId, Entity target)
        {
            // Remove from forward map
            if (_forwardMap.TryGetValue(sourceId, out var targets))
            {
                targets.Remove(target);
                if (targets.Count == 0) _forwardMap.Remove(sourceId);
            }

            // Remove from reverse map
            if (_reverseMap.TryGetValue(target.Id, out var sources))
            {
                sources.RemoveAll(e => e.Id == sourceId);
                if (sources.Count == 0) _reverseMap.Remove(target.Id);
            }
        }

        public bool Has(int sourceId, Entity target)
        {
            return _forwardMap.TryGetValue(sourceId, out var targets) && targets.Contains(target);
        }

        public IEnumerable<Entity> GetTargets(int sourceId)
        {
            return _forwardMap.TryGetValue(sourceId, out var targets) ? targets : Enumerable.Empty<Entity>();
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
                foreach (var target in targets)
                {
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
                foreach (var source in sources)
                {
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
