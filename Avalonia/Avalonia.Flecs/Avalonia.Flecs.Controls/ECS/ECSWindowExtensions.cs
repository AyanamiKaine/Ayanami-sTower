using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSWindowExtensions
    {
        public static Entity SetWindowTitle(this Entity entity, string title)
        {
            if (entity.Has<Window>())
            {
                entity.Get<Window>().Title = title;
            }
            return entity;
        }


        public static Entity ShowWindow(this Entity entity)
        {
            if (entity.Has<Window>())
            {
                entity.Get<Window>().Show();
                return entity;
            }
            throw new Exception("Entity does not have a Window component");
        }

    }
}