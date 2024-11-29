using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSContentControlExtensions
    {
        /// <summary>
        /// Used to manually set the content of a ContentControl component.
        /// </summary>
        /// <remarks>
        /// Usually you should not use this to add content to a ContentControl component.
        /// instead you should add the entity as a child of the ContentControl entity.
        /// </remaks>
        /// <param name="entity"></param>
        /// <param name="control"></param>
        /// <returns></returns>
        public static Entity SetContent(this Entity entity, object? control)
        {
            if (entity.Has<ContentControl>())
            {
                entity.Get<ContentControl>().Content = control;
                return entity;
            }
            throw new Exception("Entity does not have a ContentControl component. Try adding a control element that is an ContentControl component to the entity.");
        }

        public static object? GetContent(this Entity entity)
        {
            if (entity.Has<ContentControl>())
            {
                return entity.Get<ContentControl>().Content;
            }
            throw new Exception("Entity does not have a ContentControl component. Try adding a control element that is an ContentControl component to the entity.");
        }
    }
}