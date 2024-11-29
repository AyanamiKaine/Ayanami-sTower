using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
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
        /// Sets a collection used to generate the content of the <see cref="ItemsControl"/>.
        /// If An entity has an ItemsControl component or the component itself has an itemscontrol property, this property can be used to set the collection
        /// </summary>
        /// <remarks>
        /// A common scenario is to use an <see cref="ItemsControl"/> such as a 
        /// <see cref="ListBox"/> to display a data collection, or to bind an
        /// <see cref="ItemsControl"/> to a collection object. To bind an <see cref="ItemsControl"/>
        /// to a collection object, use the <see cref="ItemsSource"/> property.
        /// 
        /// When the <see cref="ItemsSource"/> property is set, the <see cref="Items"/> collection
        /// is made read-only and fixed-size.
        ///
        /// When <see cref="ItemsSource"/> is in use, setting the property to null removes the
        /// collection and restores usage to <see cref="Items"/>, which will be an empty 
        /// <see cref="ItemCollection"/>.
        /// </remarks>
        public static Entity SetItemsSource(this Entity entity, System.Collections.IEnumerable? collection)
        {
            if (entity.Has<ItemsControl>())
                entity.Get<ItemsControl>().ItemsSource = collection;
            else if (entity.Has<MenuFlyout>())
                entity.Get<MenuFlyout>().ItemsSource = collection;
            return entity;
        }

        public static System.Collections.IEnumerable? GetItemsSource(this Entity entity)
        {
            return entity.Get<ItemsControl>().ItemsSource;
        }

        /// <summary>
        /// Sets the data template used to display the items in the ItemsControl.
        /// </summary>
        /// <param name="entity">Entity with an ItemsControl Component</param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Entity SetItemTemplate(this Entity entity, DataTemplate? template)
        {
            if (entity.Has<ItemsControl>())
                entity.Get<ItemsControl>().ItemTemplate = template;
            else if (entity.Has<MenuFlyout>())
                entity.Get<MenuFlyout>().ItemTemplate = template;
            return entity;
        }
    }
}