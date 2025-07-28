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
        public void Add(Entity source, Entity target, object data);
        public void Remove(Entity source, Entity target);
        public bool Has(Entity source, Entity target);
        public IEnumerable<Entity> GetTargets(Entity source);
        public bool TryGetData(Entity source, Entity target, out object data);

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
        private readonly Dictionary<int, Dictionary<Entity, T>> _forwardMap = [];
        // Reverse mapping: Target Entity ID -> List of Source Entities
        private readonly Dictionary<int, List<Entity>> _reverseMap = [];

        void IRelationshipStorage.Add(Entity source, Entity target, object data) => Add(source, target, (T)data);

        public void Add(Entity source, Entity target, T data)
        {
            // Add to forward map
            if (!_forwardMap.TryGetValue(source.Id, out var targets))
            {
                targets = [];
                _forwardMap[source.Id] = targets;
            }
            targets[target] = data; // Set or update the relationship data

            // Add to reverse map
            if (!_reverseMap.TryGetValue(target.Id, out var sources))
            {
                sources = [];
                _reverseMap[target.Id] = sources;
            }
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
                if (targets.Remove(target) && targets.Count == 0)
                {
                    _forwardMap.Remove(source.Id);
                }
            }

            // Remove from reverse map
            if (_reverseMap.TryGetValue(target.Id, out var sources))
            {
                sources.Remove(source);
                if (sources.Count == 0) _reverseMap.Remove(target.Id);
            }
        }

        public bool TryGetData(Entity source, Entity target, out T data)
        {
            if (_forwardMap.TryGetValue(source.Id, out var targets) && targets.TryGetValue(target, out data))
            {
                return true;
            }

            data = default;
            return false;
        }

        bool IRelationshipStorage.TryGetData(Entity source, Entity target, out object data)
        {
            if (TryGetData(source, target, out T typedData))
            {
                data = typedData;
                return true;
            }
            data = default!;
            return false;
        }


        public bool Has(Entity source, Entity target)
        {
            return _forwardMap.TryGetValue(source.Id, out var targets) && targets.ContainsKey(target);
        }

        public IEnumerable<Entity> GetTargets(Entity source)
        {
            // The targets are the keys of the inner dictionary
            return _forwardMap.TryGetValue(source.Id, out var targets) ? targets.Keys : [];
        }

        public IEnumerable<Entity> GetSources(Entity target) =>
            _reverseMap.TryGetValue(target.Id, out var sources) ? sources : Enumerable.Empty<Entity>();


        public void RemoveAll(int entityId)
        {
            // Same logic as before
            if (_forwardMap.TryGetValue(entityId, out var targets))
            {
                foreach (var target in targets.Keys)
                {
                    if (_reverseMap.TryGetValue(target.Id, out var reverseSources))
                    {
                        reverseSources.RemoveAll(e => e.Id == entityId);
                        if (reverseSources.Count == 0) _reverseMap.Remove(target.Id);
                    }
                }
                _forwardMap.Remove(entityId);
            }
            if (_reverseMap.TryGetValue(entityId, out var sources))
            {
                // For each entity that was pointing to this one...
                foreach (var source in sources)
                {
                    // ...go to its forward map and remove the entry for this entity.
                    if (_forwardMap.TryGetValue(source.Id, out var sourceTargets))
                    {
                        // --- THE FIX IS HERE ---
                        // 1. Find all keys that match the entityId to be destroyed.
                        var keysToRemove = sourceTargets.Keys.Where(k => k.Id == entityId).ToList();

                        // 2. Iterate over the temporary list to safely remove from the dictionary.
                        foreach (var key in keysToRemove)
                        {
                            sourceTargets.Remove(key);
                        }

                        if (sourceTargets.Count == 0)
                        {
                            _forwardMap.Remove(source.Id);
                        }
                    }
                }
                _reverseMap.Remove(entityId);
            }
        }
    }
}
