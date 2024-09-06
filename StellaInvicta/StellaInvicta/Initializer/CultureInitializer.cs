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
    public class CultureAttribute : Attribute
    {
    }

    public static class CultureInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var CultureTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(CultureAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING Culture ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var CultureType in CultureTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var CultureInstance = Activator.CreateInstance(CultureType);

                    Console.WriteLine($"Trying to add Culture entity: {CultureType.Name}");

                    CultureType.GetMethod("Init")?.Invoke(CultureInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Culture entity {CultureType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding Culture entity {CultureType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING Culture ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}