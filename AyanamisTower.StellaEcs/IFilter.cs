using System;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// Internal interface for a type-erased filter condition that can be applied to an entity.
    /// </summary>
    internal interface IFilter
    {
        /// <summary>
        /// Checks if the entity's component data matches the filter's predicate.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity's data matches the condition; otherwise, false.</returns>
        bool Matches(Entity entity);
    }

    /// <summary>
    /// A concrete implementation of a filter for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter on.</typeparam>
    internal class Filter<T>(Func<T, bool> predicate) : IFilter where T : struct
    {
        private readonly Func<T, bool> _predicate = predicate;

        /// <summary>
        /// Checks if the entity has the component and if its data satisfies the predicate.
        /// </summary>
        public bool Matches(Entity entity)
        {
            // This check is implicitly safe because the query will only pass entities
            // that are guaranteed to have this component type.
            // We get a readonly reference to avoid copying the struct.
            ref readonly var component = ref entity.Get<T>();
            return _predicate(component);
        }
    }
}
