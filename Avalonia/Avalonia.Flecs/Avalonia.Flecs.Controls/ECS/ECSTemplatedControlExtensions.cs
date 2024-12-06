using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to TemplatedControl components
    /// </summary>
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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Padding", padding);
            }
            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(SetPadding));
        }

        /// <summary>
        /// Helper function to set the padding of a TemplatedControl control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetPadding(this Entity entity, double padding)
        {
            if (entity.Has<TemplatedControl>())
            {
                entity.Get<TemplatedControl>().Padding = new Thickness(padding);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Padding", new Thickness(padding));
            }
            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(SetPadding));
        }

        /// <summary>
        /// Helper function to set the padding of a TemplatedControl control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="horizontalPadding"></param>
        /// <param name="verticalPadding"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetPadding(this Entity entity, double horizontalPadding, double verticalPadding)
        {
            if (entity.Has<TemplatedControl>())
            {
                entity.Get<TemplatedControl>().Padding = new Thickness(horizontal: horizontalPadding, vertical: verticalPadding);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Padding", new Thickness(horizontal: horizontalPadding, vertical: verticalPadding));
            }
            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(SetPadding));
        }

        /// <summary>
        /// Helper function to set the padding of a TemplatedControl control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="leftPadding"></param>
        /// <param name="topPadding"></param>
        /// <param name="rightPadding"></param>
        /// <param name="bottomPadding"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetPadding(this Entity entity, double leftPadding, double topPadding, double rightPadding, double bottomPadding)
        {
            if (entity.Has<TemplatedControl>())
            {
                entity.Get<TemplatedControl>().Padding = new Thickness(left: leftPadding, top: topPadding, right: rightPadding, bottom: bottomPadding);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Padding", new Thickness(left: leftPadding, top: topPadding, right: rightPadding, bottom: bottomPadding));
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
            else if (entity.Has<object>())
            {
                return entity.GetProperty<Thickness>("Padding");
            }
            throw new ComponentNotFoundException(entity, typeof(TemplatedControl), nameof(GetPadding));
        }
    }
}
