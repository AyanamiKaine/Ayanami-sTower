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
        private readonly IComponentStorage _driverStorage; // The smallest set to iterate over
        private readonly IComponentStorage[]? _otherWithStorages;
        private readonly IComponentStorage[]? _withoutStorages;
        private readonly List<IFilter>? _filters;

        internal QueryEnumerable(World world, List<Type> withTypes, List<Type> withoutTypes, List<IFilter> filters)
        {
            _world = world;
            _filters = filters.Count > 0 ? filters : null;

            // --- The Core Optimization ---
            // 1. Get all 'With' storages and find the one with the fewest components.
            var withStorages = withTypes.ConvertAll(world.GetStorageUnsafe);
            _driverStorage = withStorages.OrderBy(s => s.Count).First();

            // 2. The rest of the 'With' storages are collected for checking.
            withStorages.Remove(_driverStorage);
            _otherWithStorages = withStorages.Count > 0 ? [.. withStorages] : null;

            // 3. Collect all 'Without' storages.
            if (withoutTypes.Count > 0)
            {
                _withoutStorages = withoutTypes.Select(t => world.GetStorageUnsafe(t)).ToArray();
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
            // Pass the fields directly to the enumerator's constructor
            // to avoid passing a reference to 'this' ref struct.
            return new Enumerator(_world, _driverStorage, _otherWithStorages, _withoutStorages, _filters);
        }

        /// <summary>
        /// The custom enumerator for a query. Implemented as a ref struct to avoid heap allocations.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly World _world;
            private readonly ReadOnlySpan<int> _driverEntityIds;
            private readonly IComponentStorage[]? _otherWithStorages;
            private readonly IComponentStorage[]? _withoutStorages;
            private readonly List<IFilter>? _filters;
            private int _index;

            // The constructor now takes the required data directly, not a reference to the QueryEnumerable.
            internal Enumerator(World world, IComponentStorage driverStorage, IComponentStorage[]? otherWithStorages, IComponentStorage[]? withoutStorages, List<IFilter>? filters)
            {
                _world = world;
                // This is the fix: Access PackedEntities through the interface directly, removing the 'dynamic' call.
                _driverEntityIds = driverStorage.PackedEntities;
                _otherWithStorages = otherWithStorages;
                _withoutStorages = withoutStorages;
                _index = -1;
                Current = default;
                _filters = filters;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                while (_index < _driverEntityIds.Length)
                {
                    int entityId = _driverEntityIds[_index];
                    var entity = _world.GetEntityFromId(entityId);

                    // Check if the entity has all the other required components
                    if (_otherWithStorages != null)
                    {
                        bool allFound = true;
                        foreach (var storage in _otherWithStorages)
                        {
                            if (!storage.Has(entityId))
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
                            if (storage.Has(entityId))
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

                    if (_filters != null)
                    {
                        bool allMatch = true;
                        foreach (var filter in _filters)
                        {
                            if (!filter.Matches(entity)) { allMatch = false; break; }
                        }
                        if (!allMatch) { _index++; continue; }
                    }

                    // If all checks pass, this is our entity.
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
