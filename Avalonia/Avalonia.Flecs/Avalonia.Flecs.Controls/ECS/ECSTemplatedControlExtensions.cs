using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSTemplatedControlExtensions
    {

        public static Entity SetPadding(this Entity entity, Thickness padding)
        {
            if (entity.Has<TemplatedControl>())
            {
                entity.Get<TemplatedControl>().Padding = padding;
                return entity;
            }
            throw new Exception("Entity does not have a TemplatedControl component with a Padding property");
        }

        public static Thickness GetPadding(this Entity entity)
        {
            if (entity.Has<TemplatedControl>())
                return entity.Get<TemplatedControl>().Padding;

            throw new Exception("Entity does not have a TemplatedControl component with a Padding property");
        }
    }
}
