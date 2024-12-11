using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the ContentControl component
    /// </summary>
    public static class ECSContentControlExtensions
    {
        /// <summary>
        /// Used to manually set the content of a ContentControl component.
        /// </summary>
        /// <remarks>
        /// Usually you should not use this to add content to a ContentControl component.
        /// instead you should add the entity as a child of the ContentControl entity.
        /// </remarks>
        /// <param name="entity"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetContent(this Entity entity, object? control)
        {
            if (entity.Has<ContentControl>())
            {
                entity.Get<ContentControl>().Content = control;
                return entity;
            }
            if (entity.Has<ToolTip>())
            {
                entity.Get<ToolTip>().Content = control;
                return entity;
            }
            if (entity.Has<object>())
            {
                return entity.SetProperty("Content", control);
            }
            throw new ComponentNotFoundException(entity, typeof(ContentControl), nameof(SetContent));
        }
        /// <summary>
        /// Helper function to get the content of a ContentControl component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static object? GetContent(this Entity entity)
        {
            if (entity.Has<ContentControl>())
            {
                return entity.Get<ContentControl>().Content;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<object>("Content");
            }
            throw new ComponentNotFoundException(entity, typeof(ContentControl), nameof(SetContent));
        }
    }
}