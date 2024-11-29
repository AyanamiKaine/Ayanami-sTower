using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSHeaderedSelectingItemsControlExtensions
    {

        /// <summary>
        /// Set the header of the HeaderedSelectingItemsControl component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Entity SetHeader(this Entity entity, object? content)
        {
            if (entity.Has<HeaderedSelectingItemsControl>())
            {
                entity.Get<HeaderedSelectingItemsControl>().Header = content;
                return entity;
            }
            throw new Exception("Entity does not have a HeaderedSelectingItemsControl component. Try adding a control element that is an HeaderedSelectingItemsControl component to the entity.");
        }

        /// <summary>
        /// Get the header of the HeaderedSelectingItemsControl component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static object? GetHeader(this Entity entity)
        {
            if (entity.Has<HeaderedSelectingItemsControl>())
            {
                return entity.Get<HeaderedSelectingItemsControl>().Header;
            }
            throw new Exception("Entity does not have a HeaderedSelectingItemsControl component. Try adding a control element that is an HeaderedSelectingItemsControl component to the entity.");
        }
    }
}