using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSLayoutableExtensions
    {

        /// <summary>
        /// Set the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMaxWidth(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().MaxWidth = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMaxWidth));
        }

        /// <summary>
        /// Get the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static double GetMaxWidth(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().MaxWidth;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetMaxWidth));
        }

        /// <summary>
        /// Set the min height of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMaxHeight(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().MaxHeight = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMaxHeight));
        }

        /// <summary>
        /// Get the min height of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static double GetMaxHeight(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().MaxHeight;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetMaxHeight));
        }




        /// <summary>
        /// Set the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetVerticalAlignment(this Entity entity, VerticalAlignment value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().VerticalAlignment = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetVerticalAlignment));
        }

        /// <summary>
        /// Get the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static VerticalAlignment GetVerticalAlignment(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().VerticalAlignment;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetVerticalAlignment));
        }

        /// <summary>
        /// Set the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetHorizontalAlignment(this Entity entity, HorizontalAlignment value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().HorizontalAlignment = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetHorizontalAlignment));
        }

        /// <summary>
        /// Get the min width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static HorizontalAlignment GetHorizontalAlignment(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().HorizontalAlignment;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetHorizontalAlignment));
        }


        /// <summary>
        /// Set the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMargin(this Entity entity, Thickness value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Margin = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMargin));
        }

        /// <summary>
        /// Get the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Thickness GetMargin(this Entity entity, Thickness value)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().Margin;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetMargin));
        }

        /// <summary>
        /// Set the height of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetHeight(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Height = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetHeight));
        }

        /// <summary>
        /// Get the height of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static double GetHeight(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().Height;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetHeight));
        }

        /// <summary>
        /// Set the width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetWidth(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Width = value;
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetWidth));

        }

        /// <summary>
        /// Get the width of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static double GetWidth(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().Width;

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetWidth));
        }

    }
}