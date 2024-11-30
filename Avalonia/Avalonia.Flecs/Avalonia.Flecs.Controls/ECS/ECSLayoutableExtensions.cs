using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class ECSLayoutableExtensions
    {

        public static Entity SetMaxWidth(this Entity entity, double value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().MaxWidth = value;
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static double GetMaxWidth(this Entity entity)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().MaxWidth;
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetMaxHeight(this Entity entity, double value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().MaxHeight = value;
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static double GetMaxHeight(this Entity entity)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().MaxHeight;
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }





        public static Entity SetVerticalAlignment(this Entity entity, VerticalAlignment value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().VerticalAlignment = value;
                return entity;
            }

            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static VerticalAlignment GetVerticalAlignment(this Entity entity)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().VerticalAlignment;

            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetHorizontalAlignment(this Entity entity, HorizontalAlignment value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().HorizontalAlignment = value;
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static HorizontalAlignment GetHorizontalAlignment(this Entity entity)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().HorizontalAlignment;
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }


        public static Entity SetMargin(this Entity entity, Thickness value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().Margin = value;
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Thickness GetMargin(this Entity entity, Thickness value)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().Margin;
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetHeight(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Height = value;
                return entity;
            }
            throw new Exception("Entity does not have a Layoutable component attached, try first setting a Layoutable element to the entity");
        }

        public static double GetHeight(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().Height;
            throw new Exception("Entity does not have a Layoutable component attached, try first setting a Layoutable element to the entity");
        }

        public static Entity SetWidth(this Entity entity, double value)
        {
            if (entity.Has<Layoutable>())
            {
                entity.Get<Layoutable>().Width = value;
                return entity;
            }
            throw new Exception("Entity does not have a Layoutable component attached, try first setting a Layoutable element to the entity");
        }

        public static double GetWidth(this Entity entity)
        {
            if (entity.Has<Layoutable>())
                return entity.Get<Layoutable>().Width;
            throw new Exception("Entity does not have a Layoutable component attached, try first setting a Layoutable element to the entity");
        }

    }
}