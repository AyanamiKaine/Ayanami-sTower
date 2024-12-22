using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to StackPanel components
    /// </summary>
    public static class ECSStackPanelExtensions
    {
        /// <summary>
        /// Helper function to set the orientation of a StackPanel control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetOrientation(this Entity entity, Orientation orientation)
        {
            if (entity.Has<StackPanel>())
            {
                entity.Get<StackPanel>().Orientation = orientation;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Orientation", orientation);
            }
            throw new ComponentNotFoundException(entity, typeof(StackPanel), nameof(SetOrientation));
        }

        /// <summary>
        /// Sets the spacing of a component
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="spacing"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetSpacing(this Entity entity, double spacing)
        {
            if (entity.Has<StackPanel>())
            {
                entity.Get<StackPanel>().Spacing = spacing;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Spacing", spacing);
            }
            throw new ComponentNotFoundException(entity, typeof(StackPanel), nameof(SetSpacing));
        }
    }
}
