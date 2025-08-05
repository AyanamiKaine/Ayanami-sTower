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

        internal QueryEnumerable(World world, List<Type> withTypes, List<Type> withoutTypes, List<IFilter> filters)
        {
            _world = world;
            _filters = filters.Count > 0 ? filters : null;

            // --- 1. Get Component Storages for 'With' Types ---
            var componentStorages = withTypes.ConvertAll(world.GetStorageUnsafe);

            // A query must have at least one 'With' component to iterate over
            if (componentStorages.Count == 0)
            {
                throw new InvalidOperationException("A query must have at least one 'With' component specified.");
            }

            // --- 2. Choose the Best Driver (Smallest Component Storage) ---
            // The core optimization is to iterate over the smallest possible set of entities
            var componentDriver = componentStorages.OrderBy(s => s.Count).First();

            // Convert entity IDs to Entity handles for the driver
            var driverSpan = componentDriver.PackedEntities;
            var entities = new Entity[driverSpan.Length];
            for (int i = 0; i < driverSpan.Length; i++)
            {
                entities[i] = world.GetEntityFromId(driverSpan[i]);
            }
            _driverEntities = new ReadOnlySpan<Entity>(entities);

            // The remaining component storages become secondary checks
            componentStorages.Remove(componentDriver);
            _otherWithStorages = componentStorages.Count > 0 ? [.. componentStorages] : null;

            // --- 3. Configure 'Without' Checks ---
            if (withoutTypes.Count > 0)
            {
                _withoutStorages = [.. withoutTypes.Select(world.GetStorageUnsafe)];
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
            return new Enumerator(_world, _driverEntities, _otherWithStorages, _withoutStorages, _filters);
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
            private int _index;

            internal Enumerator(World world, ReadOnlySpan<Entity> driverEntities, IComponentStorage[]? otherWithStorages, IComponentStorage[]? withoutStorages, List<IFilter>? filters)
            {
                _world = world;
                _driverEntities = driverEntities;
                _otherWithStorages = otherWithStorages;
                _withoutStorages = withoutStorages;
                _filters = filters;
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
