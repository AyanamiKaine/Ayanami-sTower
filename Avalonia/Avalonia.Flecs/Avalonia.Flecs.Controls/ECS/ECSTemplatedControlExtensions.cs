using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSTemplatedControlExtensions
    {

        /// <summary>
        /// Helper function to set the padding of a TemplatedControl control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Entity SetPadding(this Entity entity, Thickness padding)
        {
            if (entity.Has<TemplatedControl>())
            {
                entity.Get<TemplatedControl>().Padding = padding;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(SetPadding));
        }

        /// <summary>
        /// Helper function to get the padding of a TemplatedControl control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Thickness GetPadding(this Entity entity)
        {
            if (entity.Has<TemplatedControl>())
                return entity.Get<TemplatedControl>().Padding;

            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(GetPadding));
        }
    }
}
