using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSWindowExtensions
    {
        /// <summary>
        /// Helper function to set the title of a window component on an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="title"></param>
        /// <returns></returns>
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
        /// Helper function to open a window attached to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Entity ShowWindow(this Entity entity)
        {
            if (entity.Has<Window>())
            {
                entity.Get<Window>().Show();
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Window), nameof(ShowWindow));
        }

    }
}