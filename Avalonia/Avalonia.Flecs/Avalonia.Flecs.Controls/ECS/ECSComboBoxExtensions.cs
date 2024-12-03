using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    public static class ECSComboBoxExtensions
    {
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