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
    public class SpeciesAttribute : Attribute
    {
    }

    public static class SpeciesInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var SpeciesTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(SpeciesAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING Species ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var SpeciesType in SpeciesTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var SpeciesInstance = Activator.CreateInstance(SpeciesType);

                    Console.WriteLine($"Trying to add Species entity: {SpeciesType.Name}");

                    SpeciesType.GetMethod("Init")?.Invoke(SpeciesInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Species entity {SpeciesType.Name} successfully added.");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
                catch (Exception ex)
                {

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Red;
                    // Log the error with more details
                    Console.WriteLine($"Error adding Species entity {SpeciesType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING Species ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}