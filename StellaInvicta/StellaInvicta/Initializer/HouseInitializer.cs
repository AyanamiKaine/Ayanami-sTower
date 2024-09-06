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
    public class HouseAttribute : Attribute
    {
    }

    public static class HouseInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var HouseTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(HouseAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING House ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var HouseType in HouseTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var HouseInstance = Activator.CreateInstance(HouseType);

                    Console.WriteLine($"Trying to add House entity: {HouseType.Name}");

                    HouseType.GetMethod("Init")?.Invoke(HouseInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"House entity {HouseType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding House entity {HouseType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING House ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}