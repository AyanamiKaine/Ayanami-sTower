using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
        /// Sets the items source of an ItemsControl component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetItemsSource(this Entity entity, System.Collections.IEnumerable? collection)
        {
            if (entity.Has<ItemsControl>())
            {
                entity.Get<ItemsControl>().ItemsSource = collection;
                return entity;
            }
            else if (entity.Has<MenuFlyout>())
            {
                entity.Get<MenuFlyout>().ItemsSource = collection;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ItemsSource", collection);
            }
            throw new ComponentNotFoundException(entity, typeof(ItemsControl), nameof(SetItemsSource));
        }

        /// <summary>
        /// Gets the items source of an ItemsControl component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static System.Collections.IEnumerable? GetItemsSource(this Entity entity)
        {
            if (entity.Has<ItemsControl>())
            {
                return entity.Get<ItemsControl>().ItemsSource;
            }
            else if (entity.Has<MenuFlyout>())
            {
                return entity.Get<MenuFlyout>().ItemsSource;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<System.Collections.IEnumerable>("ItemsSource");
            }

            throw new ComponentNotFoundException(entity, typeof(ItemsControl), nameof(GetItemsSource));
        }

        /// <summary>
        /// Sets the data template used to display the items in the ItemsControl.
        /// </summary>
        /// <param name="entity">Entity with an ItemsControl Component</param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Entity SetItemTemplate(this Entity entity, IDataTemplate template)
        {
            if (entity.Has<ItemsControl>())
            {
                entity.Get<ItemsControl>().ItemTemplate = template;
                return entity;
            }
            else if (entity.Has<MenuFlyout>())
            {
                entity.Get<MenuFlyout>().ItemTemplate = template;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ItemTemplate", template);
            }

            throw new ComponentNotFoundException("Component ItemsControl or Component MenuFlyout NOT FOUND", typeof(ItemsControl), entity);
        }

        /// <summary>
        /// Removes the data template used to display the items in the ItemsControl.
        /// </summary>
        /// <param name="entity">Entity with an ItemsControl Component</param>
        /// <returns></returns>
        public static Entity RemoveItemTemplate(this Entity entity)
        {
            if (entity.Has<ItemsControl>())
            {
                entity.Get<ItemsControl>().ItemTemplate = null;
                return entity;
            }
            else if (entity.Has<MenuFlyout>())
            {
                entity.Get<MenuFlyout>().ItemTemplate = null;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("ItemTemplate", null);
            }

            throw new ComponentNotFoundException("Component ItemsControl or Component MenuFlyout NOT FOUND", typeof(ItemsControl), entity);
        }

        /// <summary>
        /// Gets the data template used in the ItemTemplate property.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static IDataTemplate? GetItemTemplate(this Entity entity)
        {
            if (entity.Has<ItemsControl>())
            {
                return entity.Get<ItemsControl>().ItemTemplate;
            }
            else if (entity.Has<MenuFlyout>())
            {
                return entity.Get<MenuFlyout>().ItemTemplate;
            }
            else if (entity.Has<object>())
            {
                return entity.GetProperty<IDataTemplate>("ItemTemplate");
            }

            throw new ComponentNotFoundException("Component ItemsControl or Component MenuFlyout NOT FOUND", typeof(ItemsControl), entity);
        }
    }
}