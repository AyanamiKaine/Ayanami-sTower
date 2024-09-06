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
    public class ReligionAttribute : Attribute
    {
    }

    public static class ReligionInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var ReligionTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(ReligionAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING Religion ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var ReligionType in ReligionTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var ReligionInstance = Activator.CreateInstance(ReligionType);

                    Console.WriteLine($"Trying to add Religion entity: {ReligionType.Name}");

                    ReligionType.GetMethod("Init")?.Invoke(ReligionInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Religion entity {ReligionType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding Religion entity {ReligionType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING Religion ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}