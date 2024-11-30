using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    /// <summary>
    /// Here we define various methods to easily manipulate SelectingItemsControl
    /// in entities without first getting the SelectingItemsControl component.
    /// </summary>
    public static class ECSListBoxExtensions
    {
        /// <summary>
        /// Sets the slection mode of a listbox component
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Entity SetSelectionMode(this Entity entity, SelectionMode mode)
        {
            if (entity.Has<ListBox>())
            {
                entity.Get<ListBox>().SelectionMode = mode;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(ListBox), nameof(SetSelectionMode));
        }

        /// <summary>
        /// Sets the slection mode of a listbox component
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static SelectionMode GetSelectionMode(this Entity entity, SelectionMode mode)
        {
            if (entity.Has<ListBox>())
                return entity.Get<ListBox>().SelectionMode;

            throw new ComponentNotFoundException(entity, typeof(ListBox), nameof(GetSelectionMode));
        }
    }
}