using System;
using System.IO;
using System.Reflection;
using CSScripting;
using CSScriptLib; // For CSScript, IEvaluator
using Flecs.NET.Core; // Assuming Flecs is used

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

        // --- Configuration ---
        // We still need an engine for the COMPILE step
        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom; // Or Roslyn
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string scriptsDir = Path.GetFullPath(Path.Combine(baseDirectory, "scripts"));
        string compiledOutputDir = Path.GetFullPath(Path.Combine(baseDirectory, "CompiledScripts")); // Output for ALL DLLs
        Directory.CreateDirectory(compiledOutputDir); // Ensure the output directory exists

        // --- Step 1: Compile ModuleA.cs to ModuleA.dll ---
        string moduleASourcePath = Path.Combine(scriptsDir, "ModuleA.cs");
        string moduleADllPath = Path.Combine(compiledOutputDir, "ModuleA.dll");
        Console.WriteLine(
            $"Compiling {Path.GetFileName(moduleASourcePath)} -> {Path.GetFileName(moduleADllPath)}..."
        );
        try
        {
            // Use a clean evaluator instance for module compilation
            IEvaluator moduleCompiler = CSScript.Evaluator.Clone(copyRefAssemblies: false);
            moduleCompiler.CompileAssemblyFromFile(moduleASourcePath, moduleADllPath);
            Console.WriteLine("ModuleA compiled successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: Failed to compile ModuleA: {ex}");
            return;
        }

        // --- Step 2: Compile ScriptA.cs to ScriptA.dll ---
        string scriptASourcePath = Path.Combine(scriptsDir, "ScriptA.cs");
        string scriptADllPath = Path.Combine(compiledOutputDir, "ScriptA.dll");
        Console.WriteLine(
            $"Compiling {Path.GetFileName(scriptASourcePath)} -> {Path.GetFileName(scriptADllPath)}..."
        );
        try
        {
            IEvaluator scriptCompilerA = CSScript.Evaluator.Clone(); // Use configured engine

            // *** Reference dependencies needed to COMPILE ScriptA ***
            scriptCompilerA.ReferenceAssembly(Assembly.GetExecutingAssembly()); // Host
            scriptCompilerA.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET
            scriptCompilerA.ReferenceAssembly(moduleADllPath); // The ModuleA DLL we just created!

            scriptCompilerA.CompileAssemblyFromFile(scriptASourcePath, scriptADllPath);
            Console.WriteLine("ScriptA compiled successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compiling ScriptA: {ex}");
            // Optionally log CodeDomEvaluator.CompilerLastOutput
            return; // Or handle error appropriately
        }

        // --- Step 2 (cont.): Compile ScriptB.cs to ScriptB.dll ---
        string scriptBSourcePath = Path.Combine(scriptsDir, "ScriptB.cs");
        string scriptBDllPath = Path.Combine(compiledOutputDir, "ScriptB.dll");
        Console.WriteLine(
            $"Compiling {Path.GetFileName(scriptBSourcePath)} -> {Path.GetFileName(scriptBDllPath)}..."
        );
        try
        {
            IEvaluator scriptCompilerB = CSScript.Evaluator.Clone(); // Use configured engine

            // *** Reference dependencies needed to COMPILE ScriptB ***
            scriptCompilerB.ReferenceAssembly(Assembly.GetExecutingAssembly()); // Host
            scriptCompilerB.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET
            scriptCompilerB.ReferenceAssembly(moduleADllPath); // The ModuleA DLL!

            scriptCompilerB.CompileAssemblyFromFile(scriptBSourcePath, scriptBDllPath);
            Console.WriteLine("ScriptB compiled successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compiling ScriptB: {ex}");
            // Optionally log CodeDomEvaluator.CompilerLastOutput
            return; // Or handle error appropriately
        }

        // --- Step 3 & 4: Load and Invoke Assemblies ---
        Console.WriteLine("\n--- Running Compiled Assemblies ---");

        // Run ScriptA from its DLL
        try
        {
            Console.WriteLine($"Loading and running {Path.GetFileName(scriptADllPath)}...");
            Assembly scriptAAssembly = Assembly.LoadFrom(scriptADllPath); // Standard .NET load
            // Use CS-Script helper for easy instantiation + dynamic invocation
            dynamic scriptAInstance = scriptAAssembly.CreateObject("ScriptA"); // Use actual class name
            scriptAInstance.Run();
            Console.WriteLine("ScriptA DLL finished.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running ScriptA DLL: {ex}");
        }

        // Run ScriptB from its DLL
        try
        {
            Console.WriteLine($"\nLoading and running {Path.GetFileName(scriptBDllPath)}...");
            Assembly scriptBAssembly = Assembly.LoadFrom(scriptBDllPath);
            dynamic scriptBInstance = scriptBAssembly.CreateObject("ScriptB"); // Use actual class name
            scriptBInstance.PerformAction();
            Console.WriteLine("ScriptB DLL finished.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running ScriptB DLL: {ex}"); // Should not get cast/file not found error here
        }

        Console.WriteLine("\nHost application finished invoking assemblies.");
        // Optional: Keep Flecs world running if needed
        GlobalData.world.App().EnableRest().Run();
    }
}
