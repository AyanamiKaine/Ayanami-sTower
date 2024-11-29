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

        public static Entity SetWatermark(this Entity entity, string text)
        {
            if (entity.Has<TextBox>())
                entity.Get<TextBox>().Watermark = text;
            return entity;
        }

        public static string GetWatermark(this Entity entity)
        {
            if (entity.Has<TextBox>())
                return entity.Get<TextBox>().Watermark!;

            throw new Exception("Entity does not have a TextBox with a Watermark property");
        }
    }
}