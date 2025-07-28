using System;
using System.Collections.Generic;
using System.Linq;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// Represents a compiled, ready-to-use query that can be iterated over.
    /// This struct is designed to be used in a foreach loop for high-performance, allocation-free iteration.
    /// </summary>
    public readonly ref struct QueryEnumerable
    {
        private readonly World _world;
        // This is the new, unified driver for the query. It can be populated from components OR relationships.
        private readonly ReadOnlySpan<Entity> _driverEntities;
        private readonly IComponentStorage[]? _otherWithStorages;
        private readonly IComponentStorage[]? _withoutStorages;
        private readonly List<IFilter>? _filters;
        private readonly List<(Type type, Entity target)>? _withRelationships;

        internal QueryEnumerable(World world, List<Type> withTypes, List<Type> withoutTypes, List<IFilter> filters, List<(Type type, Entity target)> withRelationships)
        {
            _world = world;
            _filters = filters.Count > 0 ? filters : null;
            _withRelationships = withRelationships.Count > 0 ? withRelationships : null;

            // --- The Core Optimization ---
            // It now intelligently determines the best set of entities to iterate over.

            // **FIXED**: Re-introduced the .Where() clause to filter out relationship types from the 'withTypes' list
            // before trying to get their (non-existent) component storages. This is the critical fix.
            var componentStorages = withTypes
                .Where(t => !t.IsAssignableTo(typeof(IRelationship)))
                .Select(world.GetStorageUnsafe).ToList();

            IComponentStorage? componentDriver = componentStorages.OrderBy(s => s.Count).FirstOrDefault();

            List<Entity>? relationshipDriver = null;
            if (_withRelationships != null)
            {
                // Find the smallest set of source entities from all relationship filters to act as a potential driver.
                foreach (var (relType, target) in _withRelationships)
                {
                    var storage = world.GetRelationshipStorageUnsafe(relType);
                    // This is the key: we get the sources from the relationship's reverse map.
                    var sources = storage.GetSources(target).ToList();
                    if (relationshipDriver == null || sources.Count < relationshipDriver.Count)
                    {
                        relationshipDriver = sources;
                    }
                }
            }

            // --- Determine the query driver ---
            if (componentDriver == null && relationshipDriver == null)
            {
                throw new InvalidOperationException("Query must have at least one 'With' component or relationship specified.");
            }

            // Case 1: The best driver is a component storage.
            if (componentDriver != null && (relationshipDriver == null || componentDriver.Count <= relationshipDriver.Count))
            {
                var driverSpan = componentDriver.PackedEntities;
                var entities = new Entity[driverSpan.Length];
                for (int i = 0; i < driverSpan.Length; i++)
                {
                    entities[i] = world.GetEntityFromId(driverSpan[i]);
                }
                _driverEntities = new ReadOnlySpan<Entity>(entities);
                componentStorages.Remove(componentDriver);
            }
            // Case 2: The best driver is a relationship's source list.
            else
            {
                _driverEntities = new ReadOnlySpan<Entity>(relationshipDriver!.ToArray());
            }

            _otherWithStorages = componentStorages.Count > 0 ? [.. componentStorages] : null;

            if (withoutTypes.Count > 0)
            {
                // **FIXED**: Ensure we don't try to get storage for relationship types in the 'without' clause either.
                _withoutStorages = withoutTypes
                    .Where(t => !t.IsAssignableTo(typeof(IRelationship)))
                    .Select(t => world.GetStorageUnsafe(t)).ToArray();
            }
            else
            {
                _withoutStorages = null;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of entities.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_world, _driverEntities, _otherWithStorages, _withoutStorages, _filters, _withRelationships);
        }

        /// <summary>
        /// The custom enumerator for a query. Implemented as a ref struct to avoid heap allocations.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly World _world;
            private readonly ReadOnlySpan<Entity> _driverEntities;
            private readonly IComponentStorage[]? _otherWithStorages;
            private readonly IComponentStorage[]? _withoutStorages;
            private readonly List<IFilter>? _filters;
            private readonly List<(Type type, Entity target)>? _withRelationships;
            private int _index;

            internal Enumerator(World world, ReadOnlySpan<Entity> driverEntities, IComponentStorage[]? otherWithStorages, IComponentStorage[]? withoutStorages, List<IFilter>? filters, List<(Type, Entity)>? withRelationships)
            {
                _world = world;
                _driverEntities = driverEntities;
                _otherWithStorages = otherWithStorages;
                _withoutStorages = withoutStorages;
                _filters = filters;
                _withRelationships = withRelationships;
                _index = -1;
                Current = default;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                while (_index < _driverEntities.Length)
                {
                    var entity = _driverEntities[_index];
                    if (!entity.IsAlive())
                    {
                        _index++;
                        continue;
                    }

                    // Check if the entity has all the other required components
                    if (_otherWithStorages != null)
                    {
                        bool allFound = true;
                        foreach (var storage in _otherWithStorages)
                        {
                            if (!storage.Has(entity.Id))
                            {
                                allFound = false;
                                break;
                            }
                        }
                        if (!allFound)
                        {
                            _index++;
                            continue;
                        }
                    }

                    // Check if the entity has any of the excluded components
                    if (_withoutStorages != null)
                    {
                        bool anyFound = false;
                        foreach (var storage in _withoutStorages)
                        {
                            if (storage.Has(entity.Id))
                            {
                                anyFound = true;
                                break;
                            }
                        }
                        if (anyFound)
                        {
                            _index++;
                            continue;
                        }
                    }

                    // Check if the entity has all the required relationships.
                    if (_withRelationships != null)
                    {
                        bool allFound = true;
                        foreach (var (relType, target) in _withRelationships)
                        {
                            if (!_world.HasRelationship(entity, target, relType))
                            {
                                allFound = false;
                                break;
                            }
                        }
                        if (!allFound)
                        {
                            _index++;
                            continue;
                        }
                    }

                    // Check if the entity's data matches all filters
                    if (_filters != null)
                    {
                        bool allMatch = true;
                        foreach (var filter in _filters)
                        {
                            if (!filter.Matches(entity)) { allMatch = false; break; }
                        }
                        if (!allMatch) { _index++; continue; }
                    }

                    Current = entity;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            public Entity Current { get; private set; }
        }
    }
}
