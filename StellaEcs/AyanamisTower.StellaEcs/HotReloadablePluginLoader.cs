using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// A helper class to manage the state of a loaded plugin,
/// including its instance and the AssemblyLoadContext it lives in.
/// </summary>
public class PluginState(IPlugin plugin, AssemblyLoadContext context, string filePath)
{
    /// <summary>
    /// The plugin instance itself, implementing IPlugin.
    /// </summary>
    public IPlugin Plugin { get; } = plugin;
    /// <summary>
    /// The AssemblyLoadContext that this plugin is loaded into.
    /// </summary>
    public AssemblyLoadContext Context { get; } = context;
    /// <summary>
    /// The file path of the plugin assembly.
    /// </summary>
    public string FilePath { get; } = filePath;
}

/// <summary>
/// Manages loading, unloading, and hot-reloading of plugins from a directory.
/// This loader uses collectible AssemblyLoadContexts to allow for true unloading of plugin DLLs.
/// </summary>
public class HotReloadablePluginLoader : IDisposable
{
    private readonly World _world;
    private readonly string _pluginDirectory;
    private readonly FileSystemWatcher _watcher;

    // We store the state of each plugin, keyed by its full file path.
    private readonly Dictionary<string, PluginState> _loadedPlugins = [];
    /// <summary>
    /// Manages loading, unloading, and hot-reloading of plugins from a directory.
    /// This loader uses collectible AssemblyLoadContexts to allow for true unloading of plugin DLLs.
    /// </summary>
    public HotReloadablePluginLoader(World world, string pluginPath = "Plugins")
    {
        _world = world;
        _pluginDirectory = Path.GetFullPath(pluginPath);

        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
        }

        // Set up the watcher to monitor the plugin directory for changes.
        _watcher = new FileSystemWatcher(_pluginDirectory)
        {
            // We tell the watcher to look inside the sub-folders as well.
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.dll",
            EnableRaisingEvents = false
        };

        // Hook up the event handlers.
        _watcher.Created += OnPluginFileEvent;
        _watcher.Changed += OnPluginFileEvent;
        _watcher.Deleted += OnPluginFileDeleted;
        _watcher.Renamed += OnPluginFileRenamedEvent;
    }

    /// <summary>
    /// Starts monitoring the plugin directory for changes.
    /// </summary>
    public void StartWatching()
    {
        Console.WriteLine($"[PluginLoader] Now watching for plugins in: {_pluginDirectory}");
        _watcher.EnableRaisingEvents = true;
    }


    /// <summary>
    /// Stops monitoring the plugin directory.
    /// </summary>
    public void StopWatching()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Loads all existing plugins in the plugin directory.
    /// This should be called once at startup.
    /// </summary>
    public void LoadAllExistingPlugins()
    {
        Console.WriteLine("[PluginLoader] Loading all existing plugins...");
        foreach (var directory in Directory.GetDirectories(_pluginDirectory))
        {
            var mainDllPath = FindMainPluginDll(directory);
            if (mainDllPath != null)
            {
                LoadPlugin(mainDllPath);
            }
        }
    }

    private static string? FindMainPluginDll(string directory)
    {
        // A simple heuristic: find the first DLL that contains an IPlugin implementation.
        // This could be made more robust (e.g., looking for a specific file name).
        foreach (var file in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                // We must load the assembly into a temporary context to inspect its types
                // without locking it or loading it into our main application.
                var tempContext = new AssemblyLoadContext("PluginInspector", isCollectible: true);
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var assembly = tempContext.LoadFromStream(fs);
                bool hasPluginInterface = assembly.GetTypes().Any(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
                tempContext.Unload(); // Unload the inspector context immediately

                if (hasPluginInterface)
                {
                    return file;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] Could not inspect file {Path.GetFileName(file)}: {ex.Message}");
            }
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LoadPlugin(string path)
    {
        // Unload any existing version first
        if (_loadedPlugins.ContainsKey(path))
        {
            UnloadPlugin(path);
        }

        Console.WriteLine($"[PluginLoader] Loading plugin from: {Path.GetFileName(path)}");

        // CHANGE: Use our custom PluginLoadContext
        var loadContext = new PluginLoadContext(path);

        try
        {
            // The rest is the same, but now dependencies will be resolved correctly!
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));

            foreach (var type in pluginAssembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface))
            {
                IPlugin? pluginInstance = (IPlugin)Activator.CreateInstance(type)!;
                if (pluginInstance == null) continue;

                Console.WriteLine($"--- Initializing Plugin: {pluginInstance.Name} ---");
                pluginInstance.Initialize(_world);
                _loadedPlugins[path] = new PluginState(pluginInstance, loadContext, path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] Failed to load plugin {Path.GetFileName(path)}: {e.Message}\n{e.StackTrace}");
            loadContext.Unload();
        }
    }

    /// <summary>
    /// Atomically unloads a plugin, tells it to unregister its systems,
    /// and marks its AssemblyLoadContext for collection by the GC.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnloadPlugin(string path)
    {
        if (!_loadedPlugins.TryGetValue(path, out var pluginState)) return;

        Console.WriteLine($"--- Uninitializing Plugin: {pluginState.Plugin.Name} ---");

        try
        {
            // The crucial cleanup step!
            pluginState.Plugin.Uninitialize(_world);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] Error during Uninitialize for {pluginState.Plugin.Name}: {e.Message}");
        }

        // Get a reference to the context before removing the state
        var loadContext = pluginState.Context;

        // Remove the plugin from our tracking dictionary.
        _loadedPlugins.Remove(path);

        // This is the magic call. It starts the unload process.
        loadContext.Unload();
        Console.WriteLine($"[PluginLoader] Unloaded {Path.GetFileName(path)}.");
    }

    // --- FileSystemWatcher Event Handlers ---

    private void OnPluginFileEvent(object sender, FileSystemEventArgs e)
    {
        // When any DLL changes, we need to find the main plugin DLL for that directory and reload it.
        // A small delay helps avoid issues with files being locked during writing.
        Thread.Sleep(250);
        string? directory = Path.GetDirectoryName(e.FullPath);
        if (directory == null) return;

        var mainDllPath = FindMainPluginDll(directory);
        if (mainDllPath != null)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Console.WriteLine($"[FSWatcher] Plugin directory {Path.GetFileName(directory)} is empty. Unloading.");
                UnloadPlugin(mainDllPath);
            }
            else
            {
                Console.WriteLine($"[FSWatcher] Detected change in {Path.GetFileName(directory)}. Reloading plugin.");
                LoadPlugin(mainDllPath);
            }
        }
    }

    private void OnPluginFileDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected deletion of: {e.Name}.");
        Thread.Sleep(150);

        // Find which loaded plugin this deleted file belonged to by checking its directory.
        string? deletedInDirectory = Path.GetDirectoryName(e.FullPath);
        if (deletedInDirectory == null) return;

        // Find the plugin state where the main DLL's directory matches the directory of the deleted file.
        // We use ToList() to create a copy, allowing us to safely modify the original collection.
        var pluginToUnload = _loadedPlugins.Values
            .FirstOrDefault(state => Path.GetDirectoryName(state.FilePath) == deletedInDirectory);

        if (pluginToUnload != null)
        {
            Console.WriteLine($"[PluginLoader] A file related to plugin '{pluginToUnload.Plugin.Name}' was deleted. Unloading the plugin.");
            // We have the state, so we know the exact path to its main DLL to pass to UnloadPlugin.
            UnloadPlugin(pluginToUnload.FilePath);
        }
    }

    private void OnPluginFileRenamedEvent(object sender, RenamedEventArgs e)
    {
        // This logic can get complex, a simple approach is to reload both old and new directories.
        Thread.Sleep(250);
        string? oldDirectory = Path.GetDirectoryName(e.OldFullPath);
        if (oldDirectory != null)
        {
            var mainDll = FindMainPluginDll(oldDirectory);
            if (mainDll != null) UnloadPlugin(mainDll);
        }

        string? newDirectory = Path.GetDirectoryName(e.FullPath);
        if (newDirectory != null)
        {
            var mainDll = FindMainPluginDll(newDirectory);
            if (mainDll != null) LoadPlugin(mainDll);
        }
    }

    /// <summary>
    /// Ensures all plugins are unloaded and the watcher is stopped when the loader is disposed.
    /// </summary>
    public void Dispose()
    {
        StopWatching();
        _watcher.Dispose();

        // Create a copy of the keys to avoid modification during iteration
        var pluginPaths = _loadedPlugins.Keys.ToList();
        foreach (var path in pluginPaths)
        {
            UnloadPlugin(path);
        }

        // This is a good practice to encourage the GC to run and collect the unloaded contexts.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}