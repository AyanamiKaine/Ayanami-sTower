using Avalonia.Controls;
using Avalonia.Media;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /*
    DESIGN NOTES:

    Maybe we should instead have a general purpose extension method for all controls that have a text property. Or generally with a certain property. We would probably
    have to use StyledProperty.Find<T> to find the property. This would be a more general solution. But it would also be more complex and needs reflection.
    */

    /// <summary>
    /// To easily manipulate TextBox properties in entities without first getting the control component with a text property.
    /// </summary>
    public static class ECSTextPropertyExentions
    {
        /// <summary>
        /// Sets the inner left content of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetInnerLeftContent(this Entity entity, object content)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().InnerLeftContent = content;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("InnerLeftContent", content);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetInnerLeftContent));
        }

        /// <summary>
        /// Sets the inner left content of a TextBox control component of an entity.
        /// To the control component of another entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityToSet"></param>
        /// <returns></returns>
        public static Entity SetInnerLeftContent(this Entity entity, Entity entityToSet)
        {
            return entity.SetInnerLeftContent(entityToSet.Get<Control>());
        }

        /// <summary>
        /// Gets the inner left content of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static object GetInnerLeftContent(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().InnerLeftContent!;
            else if (entity.Has<object>())
                return entity.GetProperty<object>("InnerLeftContent");
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(GetInnerLeftContent));
        }

        /// <summary>
        /// Sets the inner right content of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetInnerRightContent(this Entity entity, object content)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().InnerRightContent = content;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("InnerRightContent", content);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetInnerRightContent));
        }

        /// <summary>
        /// Sets the inner right content of a TextBox control component of an entity.
        /// To the control component of another entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityToSet"></param>
        /// <returns></returns>
        public static Entity SetInnerRightContent(this Entity entity, Entity entityToSet)
        {
            return entity.SetInnerRightContent(entityToSet.Get<Control>());
        }

        /// <summary>
        /// Gets the inner right content of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static object GetInnerRightContent(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().InnerRightContent!;
            else if (entity.Has<object>())
                return entity.GetProperty<object>("InnerRightContent");

            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(GetInnerRightContent));
        }

        /// <summary>
        /// Sets the text of a TextBox or TextBlock control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
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

        /// <summary>
        /// Gets the text of a TextBox or TextBlock control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetText(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().Text!;
            else if (entity.Has<TextBlock>())
                return entity.Get<TextBlock>().Text!;
            else if (entity.Has<object>())
                return entity.GetProperty<string>("Text");
            throw new Exception("Entity does not have a control with a text property");
        }


        /// <summary>
        /// Sets the FontWeight when the entity has a object attached with the property FontWeight.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fontWeight"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetFontWeight(this Entity entity, FontWeight fontWeight)
        {
            if (entity.Has<TextBlock>())
            {
                entity.Get<TextBlock>().FontWeight = fontWeight;
                return entity;
            }
            else if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().FontWeight = fontWeight;
                return entity;
            }
            else if (entity.Has<Button>())
            {
                entity.Get<Button>().FontWeight = fontWeight;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("FontWeight", fontWeight);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(SetFontWeight));
        }

        /// <summary>
        /// Gets the FontWeight when the entity has a object attached with the property FontWeight.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static FontWeight GetFontWeight(this Entity entity)
        {
            if (entity.Has<TextBlock>())
                return entity.Get<TextBlock>().FontWeight;
            else if (entity.Has<TextBox>())
                return entity.Get<TextBox>().FontWeight;
            else if (entity.Has<Button>())
                return entity.Get<Button>().FontWeight;
            else if (entity.Has<object>())
                return entity.GetProperty<FontWeight>("FontWeight");
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(GetFontWeight));
        }

        /// <summary>
        /// Sets the TextWrapping when the entity has a object attached with the property TextWrapping.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="textWrapping"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetTextWrapping(this Entity entity, TextWrapping textWrapping)
        {
            if (entity.Has<TextBlock>())
            {
                entity.Get<TextBlock>().TextWrapping = textWrapping;
                return entity;
            }
            else if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().TextWrapping = textWrapping;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("TextWrapping", textWrapping);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(SetTextWrapping));
        }

        /// <summary>
        /// Gets the TextWrapping when the entity has a object attached with the property TextWrapping.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static TextWrapping GetTextWrapping(this Entity entity)
        {
            if (entity.Has<TextBlock>())
                return entity.Get<TextBlock>().TextWrapping;
            else if (entity.Has<TextBox>())
                return entity.Get<TextBox>().TextWrapping;
            else if (entity.Has<object>())
                return entity.GetProperty<TextWrapping>("TextWrapping");
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(GetTextWrapping));
        }

        /// <summary>
        /// Sets the TextTrimming when the entity has a object attached with the property TextTrimming.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="textTrimming"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static Entity SetTextTrimming(this Entity entity, TextTrimming textTrimming)
        {
            if (entity.Has<TextBlock>())
            {
                entity.Get<TextBlock>().TextTrimming = textTrimming;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("TextTrimming", textTrimming);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(SetTextTrimming));
        }

        /// <summary>
        /// Gets the TextTrimming when the entity has a object attached with the property TextTrimming.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ComponentNotFoundException"></exception>
        public static TextTrimming GetTextTrimming(this Entity entity)
        {
            if (entity.Has<TextBlock>())
                return entity.Get<TextBlock>().TextTrimming;
            else if (entity.Has<object>())
                return entity.GetProperty<TextTrimming>("TextTrimming");
            throw new ComponentNotFoundException(entity, typeof(TextBlock), nameof(GetTextTrimming));
        }
    }
}