using System;
using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the Trait annotation will be automatically added to the ECS world
    /// as a trait entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TraitAttribute : Attribute
    {
        // You can add properties here if you need to store additional data with the trait
    }

    /// <summary>
    /// The trait initalizer is used to easily write traits and automatically add them
    /// to the gameWorld
    /// </summary>
    public static class TraitInitializer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            // Filter types with the TraitAttribute
            var traitTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(TraitAttribute), false).Length > 0);

            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING TRAIT ENTITIES ...");

            Console.ForegroundColor = originalColor;

            // Create instances and call Init
            foreach (var traitType in traitTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var traitInstance = Activator.CreateInstance(traitType);

                    // Log before initialization
                    Console.WriteLine($"Trying to add trait entity: {traitType.Name}");

                    traitType.GetMethod("Init")?.Invoke(traitInstance, parameters);


                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;
                    // Log successful initialization
                    Console.WriteLine($"Trait entity {traitType.Name} successfully added.");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
                catch (Exception ex)
                {
                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Red;

                    // Print the error message
                    Console.WriteLine($"ERROR adding trait entity {traitType.Name}: {ex.Message}");

                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }
            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING TRAIT ENTITIES");

            Console.ForegroundColor = originalColor;
        }
    }
}