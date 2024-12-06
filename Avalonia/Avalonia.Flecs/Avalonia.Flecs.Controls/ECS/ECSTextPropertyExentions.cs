using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    /*
    DESIGN NOTES:

    Maybe we should instead have a general purpose extension method for all controls that have a text property. Or generally with a certain property. We would probably
    have to use StyledProperty.Find<T> to find the property. This would be a more general solution. But it would also be more complex and needs reflection.
    */

    /// <summary>
    /// To easily manipulate text properties in entities without first getting the control component with a text property.
    /// </summary>
    public static class ECSTextPropertyExentions
    {

        public static Entity SetInnerLeftContent(this Entity entity, object content)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().InnerLeftContent = content;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetInnerLeftContent));
        }

        public static object GetInnerLeftContent(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().InnerLeftContent!;

            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(GetInnerLeftContent));
        }

        public static Entity SetInnerRightContent(this Entity entity, object content)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().InnerRightContent = content;
                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetInnerRightContent));
        }

        public static object GetInnerRightContent(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().InnerRightContent!;

            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(GetInnerRightContent));
        }

        public static Entity SetText(this Entity entity, string text)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().Text = text;
                return entity;
            }
            else if (entity.Has<TextBlock>())
            {
                entity.Get<TextBlock>().Text = text;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Text", text);
            }

            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetText));

        }

        public static string GetText(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().Text!;

            else if (entity.Has<TextBlock>())
                return entity.Get<TextBlock>().Text!;

            throw new Exception("Entity does not have a control with a text property");
        }
    }
}