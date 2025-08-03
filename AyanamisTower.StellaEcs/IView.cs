using System;

namespace AyanamisTower.StellaEcs
{
    /// <summary>
    /// Base interface for all views in the ECS system.
    /// Views provide high-performance, direct access to component data without query overhead.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Gets the number of entities in this view.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Refreshes the view to ensure it reflects the current state of the world.
        /// This may be needed if components have been added/removed since the view was created.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// Interface for views that can be enumerated.
    /// </summary>
    /// <typeparam name="T">The type of items yielded during enumeration.</typeparam>
    public interface IEnumerableView<out T> : IView
    {
        /// <summary>
        /// Gets an enumerator for this view.
        /// </summary>
        /// <returns>An enumerator that yields items of type T.</returns>
        System.Collections.Generic.IEnumerator<T> GetEnumerator();
    }
}
