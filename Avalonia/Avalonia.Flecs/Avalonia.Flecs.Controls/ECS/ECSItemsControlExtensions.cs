using Avalonia.Controls;
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
        /// Gets or sets a collection used to generate the content of the <see cref="ItemsControl"/>.
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
            entity.Get<ItemsControl>().ItemsSource = collection;
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
            entity.Get<ItemsControl>().ItemTemplate = template;
            return entity;
        }
    }
}