using Avalonia.Controls.Primitives;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    /// <summary>
    /// This component is used to add a header to a SelectingItemsControl.
    /// </summary>
    public static class ECSHeaderedSelectingItemsControlExtensions
    {

        /// <summary>
        /// Set the header property of components that have it.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetHeader(this Entity entity, object? content)
        {
            if (entity.Has<HeaderedSelectingItemsControl>())
            {
                entity.Get<HeaderedSelectingItemsControl>().Header = content;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Header", content);
            }
            throw new ComponentNotFoundException(entity, typeof(HeaderedSelectingItemsControl), nameof(SetHeader));

        }

        /// <summary>
        /// Get the header of the HeaderedSelectingItemsControl component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static object? GetHeader(this Entity entity)
        {
            if (entity.Has<HeaderedSelectingItemsControl>())
            {
                return entity.Get<HeaderedSelectingItemsControl>().Header;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<object>("Header");
            }
            throw new ComponentNotFoundException(entity, typeof(HeaderedSelectingItemsControl), nameof(GetHeader));

        }
    }
}