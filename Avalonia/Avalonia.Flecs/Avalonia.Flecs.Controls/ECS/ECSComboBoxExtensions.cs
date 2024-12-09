using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This extension methods all relate to ComboBox components
    /// </summary>
    public static class ECSComboBoxExtensions
    {
        /// <summary>
        /// Sets the placeholder text of a ComboBox component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="placeholderText"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetPlaceholderText(this Entity entity, string placeholderText)
        {
            if (entity.Has<ComboBox>())
            {
                entity.Get<ComboBox>().PlaceholderText = placeholderText;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("PlaceholderText", placeholderText);
            }
            throw new ComponentNotFoundException(entity, typeof(ComboBox), nameof(SetPlaceholderText));
        }

        /// <summary>
        /// Helper function to get the placeholder text of a ComboBox component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static string? GetPlaceholderText(this Entity entity)
        {
            if (entity.Has<ComboBox>())
                return entity.Get<ComboBox>().PlaceholderText;
            else if (entity.Has<object>())
            {
                return entity.GetProperty<string>("PlaceholderText");
            }
            throw new ComponentNotFoundException(entity, typeof(ComboBox), nameof(GetPlaceholderText));
        }
    }
}