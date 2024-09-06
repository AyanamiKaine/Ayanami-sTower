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
    public class PopulationPrefabAttribute : Attribute
    {
    }

    public static class PopulationPrefabInitalizer
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            var populationPrefabTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(PopulationPrefabAttribute), false).Length > 0);

            // Log the error with more details
            // Store the original console color
            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING PopulationPrefab ENTITIES...");

            Console.ForegroundColor = originalColor;
            // Create instances and call Init
            foreach (var populationPrefabType in populationPrefabTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var populationPrefabInstance = Activator.CreateInstance(populationPrefabType);

                    Console.WriteLine($"Trying to add PopulationPrefab entity: {populationPrefabType.Name}");

                    populationPrefabType.GetMethod("Init")?.Invoke(populationPrefabInstance, parameters);

                    // Log the error with more details
                    // Store the original console color
                    originalColor = Console.ForegroundColor;

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"PopulationPrefab entity {populationPrefabType.Name} successfully added.");
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
                    Console.WriteLine($"Error adding PopulationPrefab entity {populationPrefabType.Name}: {ex.Message}");
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