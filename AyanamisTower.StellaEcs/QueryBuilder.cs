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
        private readonly List<Type> _withTypes = new();
        private readonly List<Type> _withoutTypes = new();

        internal QueryBuilder(World world)
        {
            _world = world;
        }

        /// <summary>
        /// Specifies that the query should only include entities that have a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type to require.</typeparam>
        /// <returns>The query builder instance for chaining.</returns>
        public QueryBuilder With<T>() where T : struct
        {
            _withTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Specifies that the query should exclude any entities that have a component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The component type to exclude.</typeparam>
        /// <returns>The query builder instance for chaining.</returns>
        public QueryBuilder Without<T>() where T : struct
        {
            _withoutTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Constructs the final, immutable query object based on the builder's configuration.
        /// The resulting query can be used in a foreach loop.
        /// </summary>
        /// <returns>A <see cref="QueryEnumerable"/> that iterates over the matched entities.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no 'With' components are specified.</exception>
        public QueryEnumerable Build()
        {
            if (_withTypes.Count == 0)
            {
                throw new InvalidOperationException("A query must have at least one 'With' component specified.");
            }

            return new QueryEnumerable(_world, _withTypes, _withoutTypes);
        }
    }
}
