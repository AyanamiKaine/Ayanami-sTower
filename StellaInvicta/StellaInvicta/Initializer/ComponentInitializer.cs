using System.Reflection;
using Flecs.NET.Core;

namespace StellaInvicta.Initializer
{
    /// <summary>
    /// Classed with the Component annotation will be automatically added to the ECS world
    /// as a registered component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentAttribute : Attribute
    {
        // You can add properties here if you need to store additional data with the trait
    }

    /// <summary>
    /// The component initalizer is used to easily write components and automatically add them
    /// to the gameWorld
    /// </summary>
    public static class ComponentInitializer // You might want to rename this class to reflect the change
    {
        public static void Run(World world)
        {
            var allTypes = Assembly.GetExecutingAssembly().GetTypes();

            // Filter types with the ComponentAttribute
            var componentTypes = allTypes.Where(t => t.GetCustomAttributes(typeof(ComponentAttribute), false).Length > 0);

            ConsoleColor originalColor = Console.ForegroundColor;

            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("INITIALIZING COMPONENTS ...");

            Console.ForegroundColor = originalColor;

            foreach (var componentType in componentTypes)
            {
                try
                {
                    object[] parameters = [world];
                    var componentInstance = Activator.CreateInstance(componentType);

                    Console.WriteLine($"Trying to register component: {componentType.Name}");

                    componentType.GetMethod("Init")?.Invoke(componentInstance, parameters);

                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Component {componentType.Name} successfully register.");
                    Console.ForegroundColor = originalColor;

                }
                catch (Exception ex)
                {
                    // Log the error with more details
                    // Store the original console color

                    // Set the color to red
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error registering component {componentType.Name}: {ex.Message}");
                    Console.ForegroundColor = originalColor;

                }
            }
            // Set the color to red
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("DONE INITIALIZING COMPONENTS");

            Console.ForegroundColor = originalColor;


        }
    }
}