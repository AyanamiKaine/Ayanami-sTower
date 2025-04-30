using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AyanamisTower.NihilEx;
using CSScriptLib;
using Flecs.NET.Core;

/// <summary>
/// Shared data (no change)
/// </summary>
public static class GlobalData
{
    /// <summary>
    /// WORLD
    /// </summary>
    public static World world;
}

internal static class Program
{
    public static void Main(string[] args)
    {
        GlobalData.world = World.Create();
        GlobalData.world.Import<Ecs.Stats>();

        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsSourceDir = Path.Combine(baseDirectory, "Mods");
        string compiledModsDir = Path.Combine(baseDirectory, "CompiledMods");

        Console.WriteLine("Initializing ModLoader...");
        ModLoader modLoader = new(GlobalData.world, modsSourceDir, compiledModsDir);
        modLoader.LoadAllMods(); // This does all the work!

        Console.WriteLine("\nHost application running after mod loading.");
        Console.WriteLine("--- Loaded Mod Metadata ---");
        if (modLoader.LoadedMods.Any())
        {
            foreach (var modInfo in modLoader.LoadedMods)
            {
                Console.WriteLine($" - {modInfo.Name} v{modInfo.Version} by {modInfo.Author}");
                Console.WriteLine($"     Desc: {modInfo.Description}");
                Console.WriteLine(
                    $"     Tags: {(modInfo.Tags != null ? string.Join(", ", modInfo.Tags) : "None")}"
                );
                Console.WriteLine(
                    $"     Deps: {(modInfo.Dependencies?.Any() == true ? string.Join(", ", modInfo.Dependencies) : "None")}"
                );
                Console.WriteLine($"     Priority: {modInfo.LoadPriority}");
            }
        }
        else
        {
            Console.WriteLine(" (No mods loaded successfully)");
        }
        Console.WriteLine("--------------------------");

        /*
        This actually works, because we have loaded the assembly using the CSScript.Evaluator
        and set it as a singleton, the namespace defined in the mod becomes available!
        */

        dynamic t = CSScript.Evaluator.LoadMethod(
            """
            using Flecs.NET.Core;

            public void Example(World world)
            {
                // Here we are using an component defined in a loaded mod, this then triggers
                // the execution of a system defined in the mod
                world.Entity("Hello-Script Entity").Set<HealthMod.Components.Health>(new(100,200));

                // We can even easily refrence static variables defined in the module.
                Console.WriteLine(HealthMod.Module.HealthModule.STATICVARIABLE);
            }
            """
        );
        t.Example(GlobalData.world);

        Console.WriteLine("Running Flecs world progression (press Ctrl+C to stop)...");
        GlobalData.world.App().EnableRest().Run();
    }
}
