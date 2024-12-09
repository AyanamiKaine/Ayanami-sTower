using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension class is used to work with Layoutable properties of a component  attached to the Entity class.
    /// </summary>
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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("MaxWidth", value);
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
            {
                return entity.Get<Layoutable>().MaxWidth;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<double>("MaxWidth");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("MaxHeight", value);
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
            {
                return entity.Get<Layoutable>().MaxHeight;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<double>("MaxHeight");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("VerticalAlignment", value);
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
            {
                return entity.Get<Layoutable>().VerticalAlignment;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<VerticalAlignment>("VerticalAlignment");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("HorizontalAlignment", value);
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
            {
                return entity.Get<Layoutable>().HorizontalAlignment;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<HorizontalAlignment>("HorizontalAlignment");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Margin", value);
            }
            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMargin));
        }

        /// <summary>
        /// Set the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMargin(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Margin = new Thickness(value);
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Margin", new Thickness(value));
            }
            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMargin));
        }

        /// <summary>
        /// Set the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="horizontalMargin"></param>
        /// <param name="verticalMargin"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMargin(this Entity entity, double horizontalMargin, double verticalMargin)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Margin = new Thickness(horizontalMargin, verticalMargin);

                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Margin", new Thickness(horizontalMargin, verticalMargin));
            }
            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMargin));
        }

        /// <summary>
        /// Set the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="leftMargin"></param>
        /// <param name="topMargin"></param>
        /// <param name="rightMargin"></param>
        /// <param name="bottomMargin"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetMargin(this Entity entity, double leftMargin, double topMargin, double rightMargin, double bottomMargin)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Margin = new Thickness(
                                                    left: leftMargin,
                                                    top: topMargin,
                                                    right: rightMargin,
                                                    bottom: bottomMargin);

                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Margin", new Thickness(
                                                    left: leftMargin,
                                                    top: topMargin,
                                                    right: rightMargin,
                                                    bottom: bottomMargin));
            }
            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(SetMargin));
        }
        /// <summary>
        /// Get the margin of the Layoutable component.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Thickness GetMargin(this Entity entity)
        {
            if (entity.Has<Layoutable>())
            {
                return entity.Get<Layoutable>().Margin;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<Thickness>("Margin");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Height", value);
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
            {
                return entity.Get<Layoutable>().Height;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<double>("Height");
            }

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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Width", value);
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
            {
                return entity.Get<Layoutable>().Width;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<double>("Width");
            }

            throw new ComponentNotFoundException(entity, typeof(Layoutable), nameof(GetWidth));
        }
    }
}