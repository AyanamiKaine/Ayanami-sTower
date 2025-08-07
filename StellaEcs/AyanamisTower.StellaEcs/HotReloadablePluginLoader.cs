using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

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

    // --- DEBOUNCE FIX ---
    // Dictionary to hold timers for debouncing file system events.
    private readonly Dictionary<string, Timer> _debounceTimers = [];
    // Time in milliseconds to wait after the last file event before reloading.
    private const int DebounceTimeMs = 500;

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

        _watcher = new FileSystemWatcher(_pluginDirectory)
        {
            IncludeSubdirectories = true,
            // Watch for changes to the last write time and file/directory name changes.
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
        foreach (var file in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                var tempContext = new AssemblyLoadContext("PluginInspector", isCollectible: true);
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                var assembly = tempContext.LoadFromStream(fs);
                bool hasPluginInterface = assembly.GetTypes().Any(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
                tempContext.Unload();

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
        if (_loadedPlugins.ContainsKey(path))
        {
            UnloadPlugin(path);
        }

        Console.WriteLine($"[PluginLoader] Loading plugin from: {Path.GetFileName(path)}");
        var loadContext = new PluginLoadContext(path);

        try
        {
            Assembly pluginAssembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
            foreach (var type in pluginAssembly.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface))
            {
                if (Activator.CreateInstance(type) is IPlugin pluginInstance)
                {
                    Console.WriteLine($"--- Initializing Plugin: {pluginInstance.Name} ---");

                    // Register the plugin with the world for tracking
                    _world.RegisterPlugin(pluginInstance);

                    pluginInstance.Initialize(_world);
                    _loadedPlugins[path] = new PluginState(pluginInstance, loadContext, path);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] Failed to load plugin {Path.GetFileName(path)}: {e.Message}\n{e.StackTrace}");
            loadContext.Unload();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void UnloadPlugin(string path)
    {
        if (!_loadedPlugins.TryGetValue(path, out var pluginState)) return;

        Console.WriteLine($"--- Uninitializing Plugin: {pluginState.Plugin.Name} ---");
        try
        {
            pluginState.Plugin.Uninitialize(_world);

            // Unregister the plugin from the world tracking
            _world.UnregisterPlugin(pluginState.Plugin);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] Error during Uninitialize for {pluginState.Plugin.Name}: {e.Message}");
        }

        var loadContext = pluginState.Context;
        _loadedPlugins.Remove(path);
        loadContext.Unload();
        Console.WriteLine($"[PluginLoader] Unloaded {Path.GetFileName(path)}.");
    }

    // --- FileSystemWatcher Event Handlers with Debounce ---

    private void OnPluginFileEvent(object sender, FileSystemEventArgs e)
    {
        string? directory = Path.GetDirectoryName(e.FullPath);
        if (directory == null) return;

        lock (_debounceTimers)
        {
            if (_debounceTimers.TryGetValue(directory, out var existingTimer))
            {
                // If a timer already exists for this directory, reset it.
                existingTimer.Change(DebounceTimeMs, Timeout.Infinite);
            }
            else
            {
                // If no timer exists, create a new one that will fire once after the debounce period.
                var newTimer = new Timer(HandleReload, directory, DebounceTimeMs, Timeout.Infinite);
                _debounceTimers[directory] = newTimer;
            }
        }
    }

    /// <summary>
    /// This callback is executed by the debounce timer after the file events have settled.
    /// </summary>
    private void HandleReload(object? state)
    {
        var directory = (string)state!;

        // Clean up the timer for this directory.
        lock (_debounceTimers)
        {
            if (_debounceTimers.Remove(directory, out var timer))
            {
                timer.Dispose();
            }
        }

        Console.WriteLine($"[FSWatcher] Debounce time elapsed. Reloading plugin in: {Path.GetFileName(directory)}");
        var mainDllPath = FindMainPluginDll(directory);
        if (mainDllPath != null)
        {
            LoadPlugin(mainDllPath);
        }
    }

    private void OnPluginFileDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"[FSWatcher] Detected deletion of: {e.Name}.");
        string? deletedInDirectory = Path.GetDirectoryName(e.FullPath);
        if (deletedInDirectory == null) return;

        var pluginToUnload = _loadedPlugins.Values
            .FirstOrDefault(state => Path.GetDirectoryName(state.FilePath) == deletedInDirectory);

        if (pluginToUnload != null)
        {
            Console.WriteLine($"[PluginLoader] A file related to plugin '{pluginToUnload.Plugin.Name}' was deleted. Unloading the plugin.");
            UnloadPlugin(pluginToUnload.FilePath);
        }
    }

    private void OnPluginFileRenamedEvent(object sender, RenamedEventArgs e)
    {
        // A rename is like a delete from the old path and a create at the new path.
        // The existing delete/create handlers with debouncing will handle this gracefully.
        OnPluginFileDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath)!, e.OldName));
        OnPluginFileEvent(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath)!, e.Name));
    }

    /// <summary>
    /// Ensures all plugins are unloaded and the watcher is stopped when the loader is disposed.
    /// </summary>
    public void Dispose()
    {
        StopWatching();
        _watcher.Dispose();

        // Dispose all active debounce timers.
        lock (_debounceTimers)
        {
            foreach (var timer in _debounceTimers.Values)
            {
                timer.Dispose();
            }
            _debounceTimers.Clear();
        }

        // Unload all plugins.
        foreach (var path in _loadedPlugins.Keys.ToList())
        {
            UnloadPlugin(path);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
