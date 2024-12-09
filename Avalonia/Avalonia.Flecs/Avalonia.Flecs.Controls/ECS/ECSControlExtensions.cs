using Avalonia.Controls;
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

    /*
    (IMPORTANT!) RAGARDING PROPERTY ACCESS:
    We should probably always implement a fallback case where we try using reflection to get/set the property. So users that added properties to the control can still use the API without having to modify the ECSControlExtensions class.
    */

    /// <summary>
    /// Here we define various methods to easily manipulate controls
    /// in entities without first getting the control component.
    /// </summary>
    public static class ECSControlExtensions
    {
        /// <summary>
        /// Helper for setting Row property on a Control.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetRow(this Entity entity, int value)
        {
            //I wonder if we should add a check here for entity.Has<Control>() 
            //and throw an exception if it doesn't
            if (entity.Has<Control>())
            {
                Grid.SetRow(entity.Get<Control>(), value);
                return entity;
            }

            throw new ComponentNotFoundException(entity, typeof(Control), nameof(SetRow));
        }

        /// <summary>
        /// Helper function to set the ColumnSpan property 
        /// on a Control component that is attach to an entitiy.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetColumnSpan(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetColumnSpan(entity.Get<Control>(), value);
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(SetColumnSpan));
        }

        /// <summary>
        /// Helper function to set the Column property of a control component attached to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetColumn(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetColumn(entity.Get<Control>(), value);
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(SetColumn));
        }

        /// <summary>
        /// Helper function to set the RowSpan property of a control component attached to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetRowSpan(this Entity entity, int value)
        {
            if (entity.Has<Control>())
            {
                Grid.SetRowSpan(entity.Get<Control>(), value);
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(SetRowSpan));
        }
    }
}