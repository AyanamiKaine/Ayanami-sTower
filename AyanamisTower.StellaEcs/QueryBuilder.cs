using System;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// A fluent builder for creating complex entity queries.
    /// This class is the entry point for defining which components an entity must have or must not have.
    /// </summary>
    public class QueryBuilder
    {
        private readonly World _world;
        private readonly List<Type> _withTypes = [];
        private readonly List<Type> _withoutTypes = [];
        private readonly List<IFilter> _filters = [];
        private readonly List<(Type type, Entity target)> _withRelationships = [];

        internal QueryBuilder(World world)
        {
            _world = world;
        }

        /// <summary>
        /// Specifies that the query should only include entities that have a relationship of type <typeparamref name="T"/> pointing to a specific target entity.
        /// </summary>
        public QueryBuilder With<T>(Entity target) where T : struct, IRelationship
        {
            _withRelationships.Add((typeof(T), target));
            return this;
        }

        /// <summary>
        /// Specifies that the query should only include entities that have a component of type <typeparamref name="T"/>.
        /// </summary>
        public QueryBuilder With<T>() where T : struct => With(typeof(T));

        /// <summary>
        /// Specifies that the query should exclude any entities that have a component of type <typeparamref name="T"/>.
        /// </summary>
        public QueryBuilder Without<T>() where T : struct => Without(typeof(T));

        /// <summary>
        /// Specifies that the query should only include entities that have a component of the given type.
        /// </summary>
        public QueryBuilder With(Type componentType)
        {
            _withTypes.Add(componentType);
            return this;
        }

        /// <summary>
        /// Specifies that the query should exclude any entities that have a component of the given type.
        /// </summary>
        public QueryBuilder Without(Type componentType)
        {
            _withoutTypes.Add(componentType);
            return this;
        }

        /// <summary>
        /// Adds a data-based filter to the query for a specific component type.
        /// The query will only return entities where the component's data satisfies the predicate.
        /// This automatically adds a `With T ()` condition if it doesn't already exist.
        /// </summary>
        /// <typeparam name="T">The component type to filter on.</typeparam>
        /// <param name="predicate">A function that takes the component data and returns true if it matches the filter.</param>
        /// <returns>The query builder instance for chaining.</returns>
        public QueryBuilder Where<T>(Func<T, bool> predicate) where T : struct
        {
            // A 'Where' clause implies a 'With' clause.
            With<T>();
            _filters.Add(new Filter<T>(predicate));
            return this;
        }

        /// <summary>
        /// Constructs the final, immutable query object based on the builder's configuration.
        /// </summary>
        public QueryEnumerable Build()
        {
            if (_withTypes.Count == 0)
            {
                throw new InvalidOperationException("A query must have at least one 'With' component specified.");
            }

            return new QueryEnumerable(_world, _withTypes, _withoutTypes, _filters);
        }
    }
}
