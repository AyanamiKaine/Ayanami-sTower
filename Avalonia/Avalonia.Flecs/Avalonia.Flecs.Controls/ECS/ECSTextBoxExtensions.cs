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
    public static class ECSTextBoxExtensions
    {

        /// <summary>
        /// Helper function to set the text of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Entity SetWatermark(this Entity entity, string text)
        {
            if (entity.Has<TextBox>())
            {
                entity.Get<TextBox>().Watermark = text;
                return entity;
            }
            else if (entity.Has<object>())
            {
                return entity.SetProperty("Watermark", text);
            }
            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetWatermark));

        }

        /// <summary>
        /// Helper function to get the text of a TextBox control component of an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetWatermark(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().Watermark!;

            throw new ComponentNotFoundException(entity, typeof(TextBox), nameof(SetWatermark));
        }
    }
}