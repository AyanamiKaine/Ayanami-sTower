using Avalonia.Controls;
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
        /// Returns the current selected item or items
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static T GetSelectedItem<T>(this Entity entity)
        {
            if (entity.Has<ListBox>())
            {
                if (entity.Get<ListBox>().SelectedItem != null)
                {
                    return (T)entity.Get<ListBox>().SelectedItem!;
                }
                throw new NullReferenceException(nameof(GetSelectedItem));
            }
            else if (entity.Has<object>())
            {
                return (T)entity.GetProperty<object>("SelectedItem");
            }

            throw new ComponentNotFoundException(entity, typeof(ListBox), nameof(GetSelectedItem));
        }

        /// <summary>
        /// Sets the selection mode of a listbox component
        /// NEEDED COMPONENTS: ListBox or an object with a SelectionMode property
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
            else if (entity.Has<object>())
            {
                return entity.SetProperty("SelectionMode", mode);
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
            {
                return entity.Get<ListBox>().SelectionMode;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<SelectionMode>("SelectionMode");
            }

            throw new ComponentNotFoundException(entity, typeof(ListBox), nameof(GetSelectionMode));
        }
    }
}