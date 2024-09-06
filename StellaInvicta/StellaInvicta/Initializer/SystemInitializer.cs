using System;
using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the ECSSystem annotation will be automatically added as
    /// a system for the world
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ECSSystemAttribute : Attribute
    {
        // You can add properties here if you need to store additional data with the trait
    }

    /// <summary>
    /// The trait initalizer is used to easily write traits and automatically add them
    /// to the gameWorld
    /// </summary>
    public static class SystemInitializer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var systemTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(ECSSystemAttribute), false).Length > 0);


            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING SYSTEMS ...");

            Console.ForegroundColor = originalColor;

            foreach (var systemType in systemTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var systemInstance = Activator.CreateInstance(systemType);

                    // Log before initialization
                    Console.WriteLine($"Trying to add system: {systemType.Name}");

                    systemType.GetMethod("Init")?.Invoke(systemInstance, parameters);
                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;
                    // Log successful initialization
                    Console.WriteLine($"system {systemType.Name} successfully added.");
                    Console.ForegroundColor = originalColor;

                }
                catch (Exception ex)
                {
                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Red;
                    // Log the error with more details
                    Console.WriteLine($"Error adding system {systemType.Name}: {ex.Message}");
                    Console.ForegroundColor = originalColor;

                }
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING SYSTEMS");

            Console.ForegroundColor = originalColor;
        }
    }
}