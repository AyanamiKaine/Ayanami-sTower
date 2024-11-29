using Avalonia.Controls;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{


    /*
    Design Note:
    This is mostly done so we can more easily manipulate controls in entities

    I personaly find the API better when we write 
    entity.method instead of entity.Get<Control>().method()

    Mhhh we could wrap the entity class into an UIWidget class and then expose the methods on it
    instead in the entity class. This way we can have a more clean API. This is also more in line with
    the mental model. Setting a row on an UIWidget is more intuitive than setting a row on an entity.

    But adding abstractions on abstractions is not always a good idea. 
    It can make the code harder to understand.
    */

    /// <summary>
    /// Here we define various methods to easily manipulate controls
    /// in entities without first getting the control component.
    /// </summary>
    public static class ECSControlExtensions
    {

        public static Entity SetOpacity(this Entity entity, double value)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().Opacity = value;
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");

        }

        public static double GetOpacity(this Entity entity)
        {
            if (entity.Has<Control>())
                return entity.Get<Control>().Opacity;

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

        /// <summary>
        /// Helper for setting Row property on a Control.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Entity SetRow(this Entity entity, int value)
        {
            //I wonder if we should add a check here for entity.Has<Control>() 
            //and throw an exception if it doesn't
            if (entity.Has<Control>())
            {
                Grid.SetRow(entity.Get<Control>(), value);
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetColumnSpan(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetColumnSpan(entity.Get<Control>(), value);
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetColumn(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetColumn(entity.Get<Control>(), value);
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }

        public static Entity SetRowSpan(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetRowSpan(entity.Get<Control>(), value);
                return entity;
            }
            throw new Exception("Entity does not have a Control component attached, try first setting a control element to the entity");
        }
    }
}