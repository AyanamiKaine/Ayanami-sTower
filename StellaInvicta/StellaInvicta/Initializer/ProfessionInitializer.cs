using System;
using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the Profession annotation will be automatically added to the ECS world
    /// as a Profession entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProfessionAttribute : Attribute
    {
    }

    public static class ProfessionInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var professionTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(ProfessionAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING PROFESSION ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var professionType in professionTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var professionInstance = Activator.CreateInstance(professionType);

                    Console.WriteLine($"Trying to add profession entity: {professionType.Name}");

                    professionType.GetMethod("Init")?.Invoke(professionInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Profession entity {professionType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding profession entity {professionType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING PROFESSION ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}