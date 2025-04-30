using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CSScriptLib; // For CS-Script classes
using Flecs.NET.Core; // For Flecs types

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents metadata and runtime information about a mod.
/// </summary>
/// <remarks>
/// ModInfo contains all the necessary information to identify, load, and manage a mod,
/// including its metadata (name, author, version), dependencies, and runtime components.
/// </remarks>
public class ModInfo
{
    /// <summary>
    /// Name of the mod
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Author of the mod
    /// </summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// Version string of the mod in semantic versioning format
    /// </summary>
    public string Version { get; set; } = "0.0.0";

    /// <summary>
    /// Detailed description of the mod's functionality and purpose
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// List of other mods that this mod requires to function properly
    /// </summary>
    public IReadOnlyList<ModDependency> Dependencies { get; set; } = []; // Changed type

    /// <summary>
    /// Determines the loading order of the mod; mods with higher priority are loaded first
    /// </summary>
    public int LoadPriority { get; set; } = 0;

    /// <summary>
    /// Collection of categorization tags associated with this mod
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// The Type representing the main mod class that implements the IMod interface
    /// </summary>
    public Type? ModuleType { get; set; }

    /// <summary>
    /// The Assembly containing the mod's main implementation
    /// </summary>
    public Assembly? ModuleAssembly { get; set; }

    /// <summary>
    /// Optional Assembly containing additional components for the mod
    /// </summary>
    public Assembly? ComponentsAssembly { get; set; }

    /// <summary>
    /// File system directory path where the mod's source files are located
    /// </summary>
    public string SourceDirectory { get; set; } = "";

    /// <summary>
    /// The instantiated mod object implementing the IMod interface
    /// </summary>
    public IMod? Instance { get; set; }

    /// <summary>
    /// Stores the path of the components dll for recompilation
    /// </summary>
    public string ComponentsDllPath { get; set; } = "";

    /// <summary>
    /// Stores the path of the module dll for recompilation
    /// </summary>
    public string ModuleDllPath { get; set; } = "";

    /// <summary>
    /// Checks if the dependencies can be met
    /// </summary>
    /// <param name="availableMods"></param>
    /// <returns></returns>
    public bool AreDependenciesMet(IReadOnlyDictionary<string, ModInfo> availableMods)
    {
        if (Dependencies?.Any() != true)
            return true; // No dependencies to meet

        foreach (var dep in Dependencies)
        {
            if (!availableMods.TryGetValue(dep.ModName, out var availableDepMod))
            {
                Console.WriteLine(
                    $"  Dependency Check Failed for '{Name}': Required mod '{dep.ModName}' not found."
                );
                return false; // Required mod isn't loaded/available
            }

            // --- Version Checking Logic ---
            // This needs to be implemented based on your versioning scheme!
            if (!IsVersionCompatible(availableDepMod.Version, dep.RequiredVersion))
            {
                Console.WriteLine(
                    $"  Dependency Check Failed for '{Name}': Mod '{dep.ModName}' version '{availableDepMod.Version}' is not compatible with required version '{dep.RequiredVersion}'."
                );
                return false;
            }
        }
        return true; // All dependencies met
    }

    // --- !!! Placeholder - Implement Robust Version Checking !!! ---
    private bool IsVersionCompatible(string availableVersionStr, string requiredVersionStr)
    {
        // VERY Simple Example: Exact match or basic minimum version (>=)
        // You SHOULD replace this with proper semantic version parsing and comparison
        // using System.Version or a NuGet-like library if needed.
        Console.WriteLine(
            $"    Checking compatibility: Available='{availableVersionStr}' vs Required='{requiredVersionStr}'"
        );
        try
        {
            if (requiredVersionStr.StartsWith(">="))
            {
                var requiredMin = new Version(requiredVersionStr[2..].Trim());
                var available = new Version(availableVersionStr);
                return available >= requiredMin;
            }
            else if (requiredVersionStr.StartsWith("==")) // Explicit exact match
            {
                return availableVersionStr == requiredVersionStr.Substring(2).Trim();
            }
            else // Default to exact match if no operator
            {
                return availableVersionStr == requiredVersionStr;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"    WARN: Could not parse/compare versions ('{availableVersionStr}', '{requiredVersionStr}'): {ex.Message}"
            );
            return false; // Fail safe if versions can't be compared
        }
    }
}

/*
Design Notes:

The main goal is the following:
- Users can create mods that can create their own components and systems
- Users can refrence components created by other users.
- Users have the ability to use the main ecs world.

*/

/// <summary>
/// Mod loader that is able to load custom mods.
/// Compiling mods written in C#
/// </summary>
/// <summary>
/// Handles discovery, compilation, dependency resolution, and loading of mods.
/// </summary>
public class ModLoader
{
    private readonly string _modsSourceDir;
    private readonly string _compiledModsDir;
    private readonly World _world;
    private readonly List<ModInfo> _discoveredModInfo;
    private readonly List<ModInfo> _loadedMods;

    /// <summary>Gets metadata for all mods discovered and successfully compiled/instantiated.</summary>
    public IReadOnlyList<ModInfo> DiscoveredModInfo => _discoveredModInfo.AsReadOnly();

    /// <summary>Gets metadata for mods that were successfully loaded into the Flecs world after dependency checks.</summary>
    public IReadOnlyList<ModInfo> LoadedMods => _loadedMods.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModLoader"/> class.
    /// </summary>
    /// <param name="world">The Flecs world instance.</param>
    /// <param name="modsSourceDirectory">The directory containing mod source subdirectories.</param>
    /// <param name="compiledModsOutputDirectory">The directory where compiled mod DLLs will be stored.</param>
    public ModLoader(World world, string modsSourceDirectory, string compiledModsOutputDirectory)
    {
        _world = world;
        _modsSourceDir = Path.GetFullPath(modsSourceDirectory);
        _compiledModsDir = Path.GetFullPath(compiledModsOutputDirectory);
        _discoveredModInfo = [];
        _loadedMods = [];

        Directory.CreateDirectory(_compiledModsDir);

        // Ensure CS-Script knows where to find compiled DLLs referenced by name
        if (
            !CSScript.GlobalSettings.SearchDirs.Contains(
                _compiledModsDir,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            CSScript.GlobalSettings.AddSearchDir(_compiledModsDir);
            Console.WriteLine($"ModLoader: Added to CS-Script search paths: {_compiledModsDir}");
        }
    }

    /// <summary>
    /// Discovers mods, compiles them, reads metadata, resolves dependencies, and loads them into Flecs.
    /// </summary>
    public void LoadAllMods()
    {
        Console.WriteLine($"ModLoader: Scanning for mods in: {_modsSourceDir}");
        if (!Directory.Exists(_modsSourceDir))
        {
            Console.WriteLine("ModLoader: Mods source directory not found, skipping mod loading.");
            return;
        }

        _discoveredModInfo.Clear();
        _loadedMods.Clear();

        // --- Phase 1: Discovery and Compilation ---
        var compiledModData = DiscoverAndCompileMods();

        // --- Phase 2: Instantiation and Metadata Reading ---
        InstantiateModsAndReadMetadata(compiledModData);

        // --- Phase 3: Dependency Resolution and Flecs Loading ---
        ResolveDependenciesAndLoadModules();

        Console.WriteLine(
            $"\nModLoader: Finished processing. Successfully loaded {LoadedMods.Count} / {_discoveredModInfo.Count} discovered mod(s)."
        );
    }

    /// <summary>
    /// Discovers mod folders and compiles their Components.cs and Module.cs files.
    /// </summary>
    /// <returns>A dictionary mapping mod names to their compiled assembly data.</returns>
    private Dictionary<
        string,
        (
            Assembly? ComponentsAssembly,
            Assembly? ModuleAssembly,
            string ComponentsDllPath,
            string ModuleDllPath,
            string SourceDir
        )
    > DiscoverAndCompileMods()
    {
        var compiledData = new Dictionary<string, (Assembly?, Assembly?, string, string, string)>();
        var discoveredMods =
            new List<(
                string ModName,
                string ModDir,
                string ModuleSourcePath,
                string ComponentsSourcePath
            )>();

        foreach (var modDir in Directory.GetDirectories(_modsSourceDir))
        {
            string modName = Path.GetFileName(modDir);
            string moduleSourcePath = Path.Combine(modDir, "Module.cs");
            string componentsSourcePath = Path.Combine(modDir, "Components.cs");

            if (File.Exists(moduleSourcePath)) // A valid mod must have at least Module.cs
            {
                discoveredMods.Add((modName, modDir, moduleSourcePath, componentsSourcePath));
            }
            else
            {
                Console.WriteLine($"ModLoader: Skipping directory {modName} (no Module.cs found).");
            }
        }

        foreach (var mod in discoveredMods)
        {
            Console.WriteLine($"\nModLoader: Compiling Mod -> {mod.ModName}");
            string componentsDllPath = Path.Combine(
                _compiledModsDir,
                $"{mod.ModName}.Components.dll"
            );
            string moduleDllPath = Path.Combine(_compiledModsDir, $"{mod.ModName}.Module.dll");

            Assembly? componentsAssembly = CompileComponents(
                mod.ModName,
                mod.ComponentsSourcePath,
                componentsDllPath
            );
            Assembly? moduleAssembly = null;
            if (componentsAssembly != null || !File.Exists(mod.ComponentsSourcePath))
            {
                moduleAssembly = CompileModule(
                    mod.ModName,
                    mod.ModuleSourcePath,
                    moduleDllPath,
                    componentsAssembly,
                    componentsDllPath
                );
            }
            else
            {
                Console.WriteLine(
                    $"  Skipping module compilation for {mod.ModName} because components failed."
                );
            }
            // Store the original ModDir along with other data
            compiledData[mod.ModName] = (
                componentsAssembly!,
                moduleAssembly!,
                componentsDllPath,
                moduleDllPath,
                mod.ModDir
            );
        }
        return compiledData;
    }

    /// <summary>
    /// Instantiates compiled mods and reads their metadata via the IMod interface.
    /// </summary>
    /// <param name="compiledModData">Data containing compiled assemblies for each mod.</param>
    private void InstantiateModsAndReadMetadata(
        Dictionary<
            string,
            (
                Assembly? ComponentsAssembly,
                Assembly? ModuleAssembly,
                string ComponentsDllPath,
                string ModuleDllPath,
                string SourceDir
            )
        > compiledModData
    )
    {
        Console.WriteLine("\nModLoader: Instantiating mods and reading metadata...");
        foreach (var modName in compiledModData.Keys)
        {
            var compiledData = compiledModData[modName];
            if (compiledData.ModuleAssembly != null)
            {
                try
                {
                    Type? moduleType = compiledData
                        .ModuleAssembly.GetTypes()
                        .FirstOrDefault(t =>
                            typeof(IMod).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract
                        );

                    if (moduleType != null)
                    {
                        if (Activator.CreateInstance(moduleType) is IMod instance)
                        {
                            var info = new ModInfo
                            {
                                Name = instance.Name ?? modName,
                                Author = instance.Author ?? "Unknown",
                                Version = instance.Version ?? "0.0.0",
                                Description = instance.Description ?? "",
                                Dependencies =
                                    instance.Dependencies ?? Array.Empty<ModDependency>(),
                                LoadPriority = instance.LoadPriority,
                                Tags = instance.Tags ?? Array.Empty<string>(),
                                ModuleType = moduleType,
                                ModuleAssembly = compiledData.ModuleAssembly,
                                ComponentsAssembly = compiledData.ComponentsAssembly,
                                SourceDirectory = compiledData.SourceDir, // Or pass from discovery
                                Instance = instance,
                                ComponentsDllPath = compiledData.ComponentsDllPath, // Store path
                                ModuleDllPath =
                                    compiledData.ModuleDllPath // Store path
                                ,
                            };
                            _discoveredModInfo.Add(info);
                            Console.WriteLine($"  Read metadata for: {info.Name} v{info.Version}");
                        }
                        else
                        {
                            Console.WriteLine(
                                $"  ERROR: Could not instantiate type {moduleType.FullName} as IMod for {modName}. Does it have a public parameterless constructor?"
                            );
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"  WARNING: No public class implementing IMod found in {Path.GetFileName(compiledData.ModuleAssembly.Location)} for {modName}."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"  ERROR instantiating or reading metadata for {modName}: {ex}"
                    );
                }
            }
        }
    }

    /// <summary>
    /// Resolves dependencies, sorts mods, and loads them into the Flecs world.
    /// </summary>
    private void ResolveDependenciesAndLoadModules()
    {
        Console.WriteLine("\nModLoader: Resolving dependencies and loading modules...");
        var modsToLoad = new List<ModInfo>(_discoveredModInfo);
        var availableModsDict = _discoveredModInfo.ToDictionary(
            m => m.Name,
            StringComparer.OrdinalIgnoreCase
        ); // Case-insensitive lookup
        var successfullyLoaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int loadedCount = 0;
        int iteration = 0;
        int maxIterations = (modsToLoad.Count * modsToLoad.Count) + 1; // Safety break

        while (modsToLoad.Count > 0 && iteration++ < maxIterations)
        {
            var loadableThisPass = new List<ModInfo>();

            // Find mods whose dependencies are already met in the 'successfullyLoaded' set
            foreach (var modInfo in modsToLoad)
            {
                bool dependenciesMet = true;
                if (modInfo.Dependencies is { Count: > 0 })
                {
                    foreach (var dep in modInfo.Dependencies)
                    {
                        if (!availableModsDict.TryGetValue(dep.ModName, out var availableDepMod))
                        {
                            // This dependency mod doesn't even exist in the discovered set
                            Console.WriteLine(
                                $"  Dependency Check Failed for '{modInfo.Name}': Required mod '{dep.ModName}' was not found or failed compilation."
                            );
                            dependenciesMet = false;
                            break;
                        }
                        if (!successfullyLoaded.Contains(dep.ModName))
                        {
                            // Dependency exists but hasn't been loaded yet in a previous pass
                            dependenciesMet = false;
                            break;
                        }
                        // Check version compatibility ONLY if the dependency is already loaded
                        if (!modInfo.AreDependenciesMet(availableModsDict)) // Reuse the version check logic
                        {
                            dependenciesMet = false;
                            break;
                        }
                    }
                }

                if (dependenciesMet)
                {
                    loadableThisPass.Add(modInfo);
                }
            }

            if (!loadableThisPass.Any())
            {
                // If no mods can be loaded in this pass, log remaining and break
                Console.WriteLine(
                    $"  ERROR: Cannot resolve dependencies for remaining mods: {string.Join(", ", modsToLoad.Select(m => m.Name))}"
                );
                break;
            }

            // Sort the loadable mods by priority for this pass
            loadableThisPass.Sort((a, b) => a.LoadPriority.CompareTo(b.LoadPriority));

            // Load the mods identified for this pass
            foreach (var modInfo in loadableThisPass)
            {
                Console.WriteLine(
                    $"  Loading Flecs module for: {modInfo.Name} v{modInfo.Version} (Priority: {modInfo.LoadPriority})"
                );
                try
                {
                    if (modInfo.Instance == null)
                        throw new InvalidOperationException("Mod instance is null.");

                    modInfo.Instance.InitModule(_world); // Call IFlecsModule.Load

                    _loadedMods.Add(modInfo); // Add to the final list
                    successfullyLoaded.Add(modInfo.Name); // Mark as successfully loaded for dependency checks
                    modsToLoad.Remove(modInfo); // Remove from the pending list
                    loadedCount++;
                    Console.WriteLine($"  Successfully imported {modInfo.Name} into Flecs world.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR importing Flecs module for {modInfo.Name}: {ex}");
                    if (ex is TargetInvocationException tie && tie.InnerException != null)
                    {
                        Console.WriteLine($"  Inner Exception: {tie.InnerException}");
                    }
                    // Optionally remove from modsToLoad to prevent retrying a failed mod,
                    // or implement retry logic. For now, it will block others if it's a dependency.
                    modsToLoad.Remove(modInfo); // Assume failure means it can't load this time
                }
            }
        } // End while loop

        if (modsToLoad.Count > 0 && iteration >= maxIterations)
        {
            Console.WriteLine(
                $"  ERROR: Possible circular dependency or unresolved dependency detected. Cannot load: {string.Join(", ", modsToLoad.Select(m => m.Name))}"
            );
        }
    }

    // --- Compilation Helper Methods ---

    private Assembly? CompileComponents(string modName, string sourcePath, string outputPath)
    {
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"  Components source not found: {Path.GetFileName(sourcePath)}");
            return null;
        }

        Console.WriteLine(
            $"  Compiling Components: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(outputPath)}"
        );
        try
        {
            IEvaluator compiler = CSScript.Evaluator.Clone(copyRefAssemblies: false);
            // Reference assemblies needed by Components.cs
            compiler.ReferenceAssembly(typeof(System.Object).Assembly); // mscorlib/System.Private.CoreLib
            compiler.ReferenceAssembly(typeof(Console).Assembly); // System.Console
            compiler.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET
            compiler.ReferenceAssembly(typeof(IMod).Assembly); // Assembly containing IMod, ModDependency

            compiler.CompileAssemblyFromFile(sourcePath, outputPath);
            Console.WriteLine("  Components compiled successfully.");
            return Assembly.LoadFrom(outputPath); // Load the compiled assembly
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR compiling components for {modName}: {ex.Message}");
            if (CSScript.EvaluatorConfig.Engine == EvaluatorEngine.CodeDom)
            {
                Console.WriteLine(
                    $"  Compiler Output:\n{CSScriptLib.CodeDomEvaluator.CompilerLastOutput}"
                );
            }
            return null;
        }
    }

    private Assembly? CompileModule(
        string modName,
        string sourcePath,
        string outputPath,
        Assembly? componentsAssembly,
        string componentsDllPath
    )
    {
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"  Module source not found: {Path.GetFileName(sourcePath)}");
            return null;
        }

        Console.WriteLine(
            $"  Compiling Module: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(outputPath)}"
        );
        try
        {
            IEvaluator compiler = CSScript.Evaluator.Clone(copyRefAssemblies: false);

            // Reference assemblies needed by Module.cs
            compiler.ReferenceAssembly(Assembly.GetExecutingAssembly()); // Host application (for GlobalData etc.)
            compiler.ReferenceAssembly(typeof(World).Assembly); // Flecs.NET
            compiler.ReferenceAssembly(typeof(IMod).Assembly); // Assembly containing IMod, ModDependency

            // Reference the Components assembly for this mod
            if (componentsAssembly != null)
            {
                compiler.ReferenceAssembly(componentsAssembly); // Prefer loaded assembly
            }
            else if (File.Exists(componentsDllPath))
            {
                compiler.ReferenceAssembly(componentsDllPath); // Fallback to path
            }
            else if (File.Exists(Path.Combine(_modsSourceDir, modName, "Components.cs"))) // Only warn if Components.cs existed but failed compile
            {
                Console.WriteLine(
                    $"  Warning: Cannot reference components assembly for {modName}. Module compilation might fail."
                );
            }

            // Compile the module script
            compiler.CompileAssemblyFromFile(sourcePath, outputPath);

            Console.WriteLine("  Module compiled successfully.");
            return Assembly.LoadFrom(outputPath); // Load the compiled assembly
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR compiling module for {modName}: {ex.Message}");
            if (CSScript.EvaluatorConfig.Engine == EvaluatorEngine.CodeDom)
            {
                Console.WriteLine(
                    $"  Compiler Output:\n{CSScriptLib.CodeDomEvaluator.CompilerLastOutput}"
                );
            }
            return null;
        }
    }
}
