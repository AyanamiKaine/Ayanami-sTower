using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to Window components
    /// </summary>
    public static class ECSWindowExtensions
    {
        /// <summary>
        /// Helper function to set the title of a window component on an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetWindowTitle(this Entity entity, string title)
        {
            if (entity.Has<Window>())
            {
                entity.Get<Window>().Title = title;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Title", title);
            }
            throw new ComponentNotFoundException(entity, typeof(Window), nameof(SetWindowTitle));
        }

        /// <summary>
        /// Returns the window title if the window title
        /// itself is null it returns an empty string instead.
        /// If a window component is not found it will throw.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static string GetWindowTitle(this Entity entity)
        {
            if (entity.Has<Window>())
            {
                return entity.Get<Window>().Title ?? string.Empty;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<string?>("Title") ?? string.Empty;
            }
            throw new ComponentNotFoundException(entity, typeof(Window), nameof(GetWindowTitle));
        }

        /// <summary>
        /// Helper function to open a window attached to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity ShowWindow(this Entity entity)
        {
            if (entity.Has<Window>())
            {
                entity.Get<Window>().Show();
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Window), nameof(ShowWindow));
        }

        /// <summary>
        /// Helper function to close a window attached to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity CloseWindow(this Entity entity)
        {
            if (entity.Has<object>())
            {
                return entity.RunMethod("Close");
            }
            throw new ComponentNotFoundException(entity, typeof(Window), nameof(CloseWindow));
        }
    }
}