using System;
using System.Collections;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// A high-performance view over entities that have two specific component types.
    /// Provides direct access to component storages without query building overhead.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    public readonly ref struct View<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        private readonly World _world;
        private readonly ComponentStorage<T1> _storage1;
        private readonly ComponentStorage<T2> _storage2;
        private readonly ComponentStorage<T1> _driverStorage; // The smaller storage drives iteration

        internal View(World world, ComponentStorage<T1> storage1, ComponentStorage<T2> storage2)
        {
            _world = world;
            _storage1 = storage1;
            _storage2 = storage2;

            // Use the smaller storage as the driver for better performance
            _driverStorage = storage1.Count <= storage2.Count ? storage1 : storage1;
        }

        /// <summary>
        /// Gets the estimated number of entities that might have both components.
        /// This is based on the smaller component storage.
        /// </summary>
        public int EstimatedCount => _driverStorage.Count;

        /// <summary>
        /// Refreshes the view. Since views are always up-to-date with the storage,
        /// this is a no-op but provided for interface compatibility.
        /// </summary>
        public void Refresh()
        {
            // Views are always current with the underlying storage
        }

        /// <summary>
        /// Gets the actual count by iterating through and checking both components.
        /// This is more expensive than EstimatedCount but gives the exact count.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                var entities = _driverStorage.PackedEntities;
                for (int i = 0; i < entities.Length; i++)
                {
                    if (_storage1.Has(entities[i]) && _storage2.Has(entities[i]))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Checks if a specific entity has both components.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity has both components; otherwise, false.</returns>
        public bool Contains(Entity entity)
        {
            return _world.IsAlive(entity) &&
                   _storage1.Has(entity.Id) &&
                   _storage2.Has(entity.Id);
        }

        /// <summary>
        /// Returns an enumerator that iterates through entities that have both components.
        /// </summary>
        public Enumerator GetEnumerator() => new(_world, _storage1, _storage2);

        /// <summary>
        /// High-performance enumerator for iterating over entities that have both components.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly World _world;
            private readonly ComponentStorage<T1> _storage1;
            private readonly ComponentStorage<T2> _storage2;
            private readonly ReadOnlySpan<int> _driverEntities;
            private int _index;

            internal Enumerator(World world, ComponentStorage<T1> storage1, ComponentStorage<T2> storage2)
            {
                _world = world;
                _storage1 = storage1;
                _storage2 = storage2;

                // Use the smaller storage as the driver
                _driverEntities = storage1.Count <= storage2.Count ?
                    storage1.PackedEntities : storage2.PackedEntities;

                _index = -1;
                Current = default;
            }

            /// <summary>
            /// Gets the current entity.
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// Advances the enumerator to the next entity that has both components.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                while (_index < _driverEntities.Length)
                {
                    var entityId = _driverEntities[_index];

                    // Check if this entity has both components
                    if (_storage1.Has(entityId) && _storage2.Has(entityId))
                    {
                        Current = _world.GetEntityFromId(entityId);
                        return true;
                    }

                    _index++;
                }
                return false;
            }

            /// <summary>
            /// Resets the enumerator to its initial position.
            /// </summary>
            public void Reset()
            {
                _index = -1;
                Current = default;
            }
        }
    }

    /// <summary>
    /// A high-performance view over entities that have three specific component types.
    /// </summary>
    /// <typeparam name="T1">The first component type.</typeparam>
    /// <typeparam name="T2">The second component type.</typeparam>
    /// <typeparam name="T3">The third component type.</typeparam>
    public readonly ref struct View<T1, T2, T3>
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        private readonly World _world;
        private readonly ComponentStorage<T1> _storage1;
        private readonly ComponentStorage<T2> _storage2;
        private readonly ComponentStorage<T3> _storage3;
        private readonly IComponentStorage _driverStorage;

        internal View(World world, ComponentStorage<T1> storage1, ComponentStorage<T2> storage2, ComponentStorage<T3> storage3)
        {
            _world = world;
            _storage1 = storage1;
            _storage2 = storage2;
            _storage3 = storage3;

            // Find the smallest storage to drive iteration
            _driverStorage = storage1.Count <= storage2.Count && storage1.Count <= storage3.Count ? storage1 :
                            storage2.Count <= storage3.Count ? storage2 : storage3;
        }

        /// <summary>
        /// Gets the estimated number of entities based on the smallest component storage.
        /// </summary>
        public int EstimatedCount => _driverStorage.Count;

        /// <summary>
        /// Gets the actual count by checking all three components.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                var entities = _driverStorage.PackedEntities;
                for (int i = 0; i < entities.Length; i++)
                {
                    var entityId = entities[i];
                    if (_storage1.Has(entityId) && _storage2.Has(entityId) && _storage3.Has(entityId))
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        public void Refresh()
        {
            // Views are always current with the underlying storage
        }

        /// <summary>
        /// Checks if a specific entity has all three components.
        /// </summary>
        public bool Contains(Entity entity)
        {
            return _world.IsAlive(entity) &&
                   _storage1.Has(entity.Id) &&
                   _storage2.Has(entity.Id) &&
                   _storage3.Has(entity.Id);
        }

        /// <summary>
        /// Returns an enumerator for entities with all three components.
        /// </summary>
        public Enumerator GetEnumerator() => new(_world, _storage1, _storage2, _storage3, _driverStorage);

        /// <summary>
        /// High-performance enumerator for three-component views.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly World _world;
            private readonly ComponentStorage<T1> _storage1;
            private readonly ComponentStorage<T2> _storage2;
            private readonly ComponentStorage<T3> _storage3;
            private readonly ReadOnlySpan<int> _driverEntities;
            private int _index;

            internal Enumerator(World world, ComponentStorage<T1> storage1, ComponentStorage<T2> storage2,
                ComponentStorage<T3> storage3, IComponentStorage driverStorage)
            {
                _world = world;
                _storage1 = storage1;
                _storage2 = storage2;
                _storage3 = storage3;
                _driverEntities = driverStorage.PackedEntities;
                _index = -1;
                Current = default;
            }

            /// <summary>
            /// Gets the current entity.
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// Advances to the next entity with all three components.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                while (_index < _driverEntities.Length)
                {
                    var entityId = _driverEntities[_index];

                    if (_storage1.Has(entityId) && _storage2.Has(entityId) && _storage3.Has(entityId))
                    {
                        Current = _world.GetEntityFromId(entityId);
                        return true;
                    }

                    _index++;
                }
                return false;
            }

            /// <summary>
            /// Resets the enumerator.
            /// </summary>
            public void Reset()
            {
                _index = -1;
                Current = default;
            }
        }
    }
}
