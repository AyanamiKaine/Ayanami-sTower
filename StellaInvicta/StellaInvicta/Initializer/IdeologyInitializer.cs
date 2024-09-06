using System;
using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the Ideology annotation will be automatically added to the ECS world
    /// as a Ideology entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IdeologyAttribute : Attribute
    {
    }

    public static class IdeologyInitializer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var IdeologyTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(IdeologyAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING Ideology ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var IdeologyType in IdeologyTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var IdeologyInstance = Activator.CreateInstance(IdeologyType);

                    Console.WriteLine($"Trying to add Ideology entity: {IdeologyType.Name}");

                    IdeologyType.GetMethod("Init")?.Invoke(IdeologyInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Ideology entity {IdeologyType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding Ideology entity {IdeologyType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING Ideology ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}