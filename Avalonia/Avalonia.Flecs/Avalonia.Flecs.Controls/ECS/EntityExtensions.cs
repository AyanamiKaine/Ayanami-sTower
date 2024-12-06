using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{

    public static class EntityExtensions
    {

        public static Entity CreateOrLookup(this World world, string name)
        {
            var entity = world.Lookup("." + name);
            if (entity != 0)
            {
                return entity;
            }

            return world.Entity(name);

        }

        /// <summary>
        /// Returns a string where each component is comma separated each with a newline.
        /// EXAMPLE:
        /// Position,
        /// Velocity,
        /// ...
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string GetAllComponents(this Entity entity)
        {
            var inputString = entity.Type().Str();
            string[] components = inputString.Split(',');
            string formattedString = string.Join(
                ",\n  ",
                components.Select(c => c.Trim())
            );
            return formattedString;
        }
    }
}