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
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.dll",
            EnableRaisingEvents = false // We will enable it manually.
        };

        // Hook up the event handlers.
        _watcher.Created += OnPluginFileCreated;
        _watcher.Changed += OnPluginFileChanged;
        _watcher.Deleted += OnPluginFileDeleted;
        _watcher.Renamed += OnPluginFileRenamed;
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
    /// Loads all existing *.dll files in the plugin directory.
    /// This should be called once at startup.
    /// </summary>
    public void LoadAllExistingPlugins()
    {
        Console.WriteLine("[PluginLoader] Loading all existing plugins...");
        foreach (var file in Directory.GetFiles(_pluginDirectory, "*.dll"))
        {
            LoadPlugin(file);
        }
    }

    /// <summary>
    /// Atomically loads a single plugin assembly into a collectible context.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)] // Important to keep this method from being inlined by the JIT
    private void LoadPlugin(string path)
    {
        // First, if a version of this plugin is already loaded, unload it.
        // This handles both initial loading and reloading.
        if (_loadedPlugins.ContainsKey(path))
        {
            Console.WriteLine($"[PluginLoader] Reloading plugin: {Path.GetFileName(path)}");
            UnloadPlugin(path);
        }
        else
        {
            Console.WriteLine($"[PluginLoader] Loading new plugin: {Path.GetFileName(path)}");
        }

        var pluginName = Path.GetFileNameWithoutExtension(path);

        // Create a new, collectible AssemblyLoadContext for this plugin.
        var loadContext = new AssemblyLoadContext(pluginName, isCollectible: true);

        try
        {
            // Load the assembly from a stream to avoid locking the file on disk.
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assembly pluginAssembly = loadContext.LoadFromStream(fileStream);

            // Find all types in the DLL that implement our IPlugin interface.
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
            Console.WriteLine($"[ERROR] Failed to load plugin {pluginName}.dll: {e.Message}");
            // If we failed, unload the context to clean up.
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

    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected change in: {e.Name}. Reloading...");
        // A change event means we should reload the plugin.
        // We add a small delay to ensure the file write is complete.
        Thread.Sleep(100); // Wait 100ms
        LoadPlugin(e.FullPath);
    }

    private void OnPluginFileCreated(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected new file: {e.Name}. Loading...");
        // A new file was dropped in, load it.
        Thread.Sleep(100); // Wait 100ms
        LoadPlugin(e.FullPath);
    }

    private void OnPluginFileDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected deletion of: {e.Name}. Unloading...");
        // The DLL was deleted, so we should unload it.
        UnloadPlugin(e.FullPath);
    }

    private void OnPluginFileRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected rename from '{e.OldName}' to '{e.Name}'.");
        // Treat a rename as an unload of the old name and a load of the new name.
        UnloadPlugin(e.OldFullPath);
        LoadPlugin(e.FullPath);
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