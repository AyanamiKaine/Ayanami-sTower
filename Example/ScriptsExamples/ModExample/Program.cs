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
                world.Entity("Hello-Script Entity").Set<HealthMod.Components.Health>(new(100,200));

                // We can even easily refrence static variables defined in the module.
                Console.WriteLine(HealthMod.Module.HealthModule.STATICVARIABLE);
            }
            """
        );
        t.Example(GlobalData.world);

        /*
        // --- Configuration ---
        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsSourceDir = Path.GetFullPath(Path.Combine(baseDirectory, "Mods")); // Where user .cs files are
        string compiledModsDir = Path.GetFullPath(Path.Combine(baseDirectory, "CompiledMods")); // Where DLLs will go

        CSScript.GlobalSettings.AddSearchDir(compiledModsDir);

        Directory.CreateDirectory(compiledModsDir);

        Console.WriteLine($"Scanning for mods in: {modsSourceDir}");
        Console.WriteLine($"Outputting compiled mods to: {compiledModsDir}");

        // Store loaded module assemblies and types for potential use
        var loadedModuleTypes = new List<Type>();

        // --- Discover and Process Mods ---
        if (Directory.Exists(modsSourceDir))
        {
            // Find all potential mod folders (subdirectories)
            foreach (var modDir in Directory.GetDirectories(modsSourceDir))
            {
                string modName = Path.GetFileName(modDir);
                Console.WriteLine($"\nProcessing Mod: {modName}");

                string componentsSourcePath = Path.Combine(modDir, "Components.cs");
                string moduleSourcePath = Path.Combine(modDir, "Module.cs");

                string componentsDllPath = Path.Combine(
                    compiledModsDir,
                    $"{modName}.Components.dll"
                );
                string moduleDllPath = Path.Combine(compiledModsDir, $"{modName}.Module.dll");

                Assembly? componentsAssembly = null;
                Assembly? moduleAssembly = null;

                // --- Step 1: Compile Components Assembly ---
                if (File.Exists(componentsSourcePath))
                {
                    Console.WriteLine(
                        $"  Compiling Components: {Path.GetFileName(componentsSourcePath)} -> {Path.GetFileName(componentsDllPath)}"
                    );
                    try
                    {
                        IEvaluator componentCompiler = CSScript.Evaluator.Clone(
                            copyRefAssemblies: false
                        );
                        // Reference essential assemblies IF components need them (e.g., Flecs.NET if components implement IFlecsComponentLifecycle)
                        componentCompiler.ReferenceAssembly(typeof(System.Object).Assembly); // System.Private.CoreLib
                        componentCompiler.ReferenceAssembly(typeof(Console).Assembly); // System.Console
                        componentCompiler.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET (Optional here, but safe)

                        componentCompiler.CompileAssemblyFromFile(
                            componentsSourcePath,
                            componentsDllPath
                        );
                        Console.WriteLine("  Components compiled successfully.");
                        componentsAssembly = Assembly.LoadFrom(componentsDllPath); // Load immediately to use for Module compilation
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR compiling components for {modName}: {ex}");
                        continue; // Skip to next mod if components fail
                    }
                }
                else
                {
                    Console.WriteLine($"  Skipping components compile (Components.cs not found).");
                }

                // --- Step 2: Compile Module Assembly ---
                if (File.Exists(moduleSourcePath))
                {
                    Console.WriteLine(
                        $"  Compiling Module: {Path.GetFileName(moduleSourcePath)} -> {Path.GetFileName(moduleDllPath)}"
                    );
                    try
                    {
                        IEvaluator moduleCompiler = CSScript.Evaluator.Clone(
                            copyRefAssemblies: false
                        ); // Use configured engine

                        // *** Reference dependencies needed to COMPILE the Module ***
                        moduleCompiler.ReferenceAssembly(Assembly.GetExecutingAssembly()); // Host (for GlobalData maybe)
                        moduleCompiler.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET is required
                        if (componentsAssembly != null)
                        {
                            moduleCompiler.ReferenceAssembly(componentsAssembly); // CRUCIAL: Reference the Components assembly!
                        }
                        else if (File.Exists(componentsDllPath)) // Fallback if not loaded above
                        {
                            moduleCompiler.ReferenceAssembly(componentsDllPath);
                        }

                        moduleCompiler.CompileAssemblyFromFile(moduleSourcePath, moduleDllPath);
                        Console.WriteLine("  Module compiled successfully.");
                        moduleAssembly = Assembly.LoadFrom(moduleDllPath); // Load the module assembly
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR compiling module for {modName}: {ex}");
                        // Optionally log CompilerLastOutput
                        continue; // Skip loading this module
                    }
                }
                else
                {
                    Console.WriteLine($"  Skipping module compile (Module.cs not found).");
                }

                // --- Step 3: Find and Load IFlecsModule ---
                if (moduleAssembly != null)
                {
                    try
                    {
                        // Find the class implementing IFlecsModule in the Module assembly
                        Type? moduleType = moduleAssembly
                            .GetTypes()
                            .FirstOrDefault(t =>
                                typeof(IFlecsModule).IsAssignableFrom(t)
                                && !t.IsInterface
                                && !t.IsAbstract
                            );

                        if (moduleType != null)
                        {
                            Console.WriteLine(
                                $"  Found IFlecsModule implementation: {moduleType.FullName}"
                            );
                            // Import the module into the Flecs world using reflection with generics
                            // Need to get the generic Import<T> method first
                            MethodInfo? importMethod = typeof(World).GetMethod(
                                "Import",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                            MethodInfo genericImportMethod = importMethod!.MakeGenericMethod(
                                moduleType
                            );

                            // Invoke world.Import<FoundModuleType>();
                            genericImportMethod.Invoke(GlobalData.world, null);

                            Console.WriteLine(
                                $"  Successfully imported {modName} into Flecs world."
                            );
                            loadedModuleTypes.Add(moduleType);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"  WARNING: No public class implementing IFlecsModule found in {Path.GetFileName(moduleDllPath)}."
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"  ERROR loading or importing Flecs module for {modName}: {ex}"
                        );
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Mods source directory not found, skipping mod loading.");
        }

        // --- Host Application Continues ---
        Console.WriteLine($"\nHost application finished loading {loadedModuleTypes.Count} mod(s).");

        // Example: Add an entity in the host and see if systems from mods affect it
        /*
        var hostEntity = GlobalData
            .world.Entity("HostTestEntity")
            .Set<HealthMod.Components.Health>(
                new HealthMod.Components.Health { Current = 5, Max = 10 }
            ); // Use component from mod
        */

        Console.WriteLine("Running Flecs world progression (press Ctrl+C to stop)...");

        // Basic loop to see systems run (replace with your actual game loop)
        // while (GlobalData.world.Progress()) // Use Progress() in a real loop
        // {
        //      System.Threading.Thread.Sleep(16); // Simulate frame delay
        // }

        // Or run the REST API for inspection
        GlobalData.world.App().EnableRest().Run();
    }
}
