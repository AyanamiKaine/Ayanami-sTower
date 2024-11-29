using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    /// <summary>
    /// Here we define various methods to easily manipulate itemscontrols
    /// in entities without first getting the itemscontrol component.
    /// </summary>
    public static class ECSItemsControlExtensions
    {

        /// <summary>
        /// Adds an item to the ItemsControl of an entity.
        /// </summary>
        /// <param name="entity">entity with an ItemsControl component</param>
        /// <param name="value">object to be added to the items control</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Entity AddItem(this Entity entity, object? value)
        {
            var index = entity.Get<ItemsControl>().Items.Add(value);
            if (index == -1)
            {
                throw new Exception("Failed to add item to ItemsControl");
            }
            return entity;
        }
    }
}