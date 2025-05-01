using CSScriptLib;
using Flecs.NET.Core;

/// <summary>
/// We want to create a way we can easily debug and overwrite exisiting systems
/// we could simply load method and execute it. We could also define a simply interface
/// for the method call. But there is one big question I have and that is performance.
///
/// When a system gets executed how big is the performance impact when it was created
/// in a script? We will need to create a benchmark project for that.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        World world = World.Create();

        CSScript.EvaluatorConfig.DebugBuild = true;

        dynamic script = CSScript
            .CodeDomEvaluator.ReferenceAssemblyByNamespace("Flecs")
            .LoadMethod(
                """
                using Flecs.NET.Core;
                public void CreateSystem(World world)
                {
                    world
                    .System("Example Script System")
                    .Each(
                        () =>
                        {
                            Console.WriteLine($"This system should always run, for each world.Progress()");
                        }
                    );
                }
                """
            );

        script.CreateSystem(world);
        world.Progress();

        dynamic scriptB = CSScript
            .CodeDomEvaluator.ReferenceAssemblyByNamespace("Flecs")
            .LoadMethod(
                """
                using Flecs.NET.Core;
                public void OverwriteSystem(World world)
                {
                    world
                    .System("Example Script System")
                    .Each(
                        () =>
                        {
                            Console.WriteLine($"Creating a system with the same name actually overwrites it!");
                        }
                    );
                }
                """
            );

        scriptB.OverwriteSystem(world);
        world.Progress();
    }
}
