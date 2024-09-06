using System;
using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the Education annotation will be automatically added to the ECS world
    /// as a Education entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EducationAttribute : Attribute
    {
    }

    public static class EducationInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var educationTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(EducationAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING EDUCATION ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var educationType in educationTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var educationInstance = Activator.CreateInstance(educationType);

                    Console.WriteLine($"Trying to add education entity: {educationType.Name}");

                    educationType.GetMethod("Init")?.Invoke(educationInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Education entity {educationType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding education entity {educationType.Name}: {ex.Message}");
                    // Reset the color back to the original
                    Console.ForegroundColor = originalColor;
                }
            }

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING EDUCATION ENTITIES!");

            Console.ForegroundColor = originalColor;
        }
    }
}