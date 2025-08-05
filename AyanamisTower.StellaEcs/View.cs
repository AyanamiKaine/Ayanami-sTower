using System;
using System.Collections;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// A high-performance view over entities that have a specific component type.
    /// Provides direct access to component storage without query building overhead.
    /// </summary>
    /// <typeparam name="T">The component type to view.</typeparam>
    public readonly ref struct View<T>
        where T : struct
    {
        private readonly World _world;
        private readonly ComponentStorage<T> _storage;

        internal View(World world, ComponentStorage<T> storage)
        {
            _world = world;
            _storage = storage;
        }

        /// <summary>
        /// Gets the number of entities that have this component.
        /// </summary>
        public int Count => _storage.Count;

        /// <summary>
        /// Gets a read-only span of entity IDs that have this component.
        /// This provides the fastest possible iteration over entities.
        /// </summary>
        public ReadOnlySpan<int> EntityIds => _storage.PackedEntities;

        /// <summary>
        /// Gets a read-only span of the component data.
        /// The indices correspond to the EntityIds span.
        /// </summary>
        public ReadOnlySpan<T> Components => _storage.PackedComponents;

        /// <summary>
        /// Gets an entity by its index in the view.
        /// </summary>
        /// <param name="index">The index in the view (0 to Count-1).</param>
        /// <returns>The entity at the specified index.</returns>
        public Entity GetEntity(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {Count})");

            return _world.GetEntityFromId(_storage.PackedEntities[index]);
        }

        /// <summary>
        /// Gets a read-only reference to the component at the specified index.
        /// </summary>
        /// <param name="index">The index in the view (0 to Count-1).</param>
        /// <returns>A read-only reference to the component.</returns>
        public ref readonly T GetComponent(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {Count})");

            return ref _storage.PackedComponents[index];
        }

        /// <summary>
        /// Gets a mutable reference to the component at the specified index.
        /// </summary>
        /// <param name="index">The index in the view (0 to Count-1).</param>
        /// <returns>A mutable reference to the component.</returns>
        public ref T GetComponentMutable(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {Count})");

            return ref _storage.PackedComponentsMutable[index];
        }

        /// <summary>
        /// Gets both the entity and component at the specified index.
        /// </summary>
        /// <param name="index">The index in the view (0 to Count-1).</param>
        /// <returns>A tuple containing the entity and the component value.</returns>
        public (Entity Entity, T Component) Get(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {Count})");

            var entity = _world.GetEntityFromId(_storage.PackedEntities[index]);
            var component = _storage.PackedComponents[index];
            return (entity, component);
        }

        /// <summary>
        /// Checks if a specific entity is contained in this view.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity has the component; otherwise, false.</returns>
        public bool Contains(Entity entity)
        {
            return _world.IsAlive(entity) && _storage.Has(entity.Id);
        }

        /// <summary>
        /// Refreshes the view. Since views are always up-to-date with the storage,
        /// this is a no-op but provided for consistency.
        /// </summary>
        public void Refresh()
        {
            // Views are always current with the underlying storage
        }

        /// <summary>
        /// Returns an enumerator that iterates through the entities in this view.
        /// </summary>
        public Enumerator GetEnumerator() => new(_world, _storage);

        /// <summary>
        /// High-performance enumerator for iterating over entities in a view.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly World _world;
            private readonly ComponentStorage<T> _storage;
            private readonly ReadOnlySpan<int> _entityIds;
            private int _index;

            internal Enumerator(World world, ComponentStorage<T> storage)
            {
                _world = world;
                _storage = storage;
                _entityIds = storage.PackedEntities;
                _index = -1;
                Current = default;
            }

            /// <summary>
            /// Gets the current entity.
            /// </summary>
            public Entity Current { get; private set; }

            /// <summary>
            /// Advances the enumerator to the next entity.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                if (_index < _entityIds.Length)
                {
                    Current = _world.GetEntityFromId(_entityIds[_index]);
                    return true;
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
}
