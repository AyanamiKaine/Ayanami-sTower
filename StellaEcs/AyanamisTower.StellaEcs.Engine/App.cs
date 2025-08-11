using System;
using AyanamisTower.NihilEx.SDLWrapper;
using AyanamisTower.StellaEcs.Api;

namespace AyanamisTower.StellaEcs.Engine;

/// <summary>
/// Represents the main application class.
/// </summary>
public class App
{
    private Window? _window;
    private Renderer? _renderer;
    private bool _shouldQuit = false; // Flag to signal exit request
    private World? _world;
    /// <summary>
    /// Init
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public bool Initialize(string[] args)
    {
        Console.WriteLine("AppInit callback started.");
        try
        {
            _world = new World();

            var pluginLoader = new HotReloadablePluginLoader(_world, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();
            _world.EnableRestApi();

            // SdlHost.RunApplication already initializes SDL subsystems specified
            // (or we could call SdlHost.Init here if RunApplication didn't)

            // Create Window and Renderer
            _window = new Window("SDL3 Callback Window", 800, 600, WindowFlags.Resizable);
            Console.WriteLine($"Window created with ID: {_window.Id}");

            _renderer = _window.CreateRenderer();
            Console.WriteLine($"Renderer created: {_renderer.Name}");

            Console.WriteLine("AppInit callback finished successfully.");
            return true; // Indicate success
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error during initialization: {ex}");
            Console.ResetColor();
            return false; // Indicate failure
        }
    }

    /// <summary>
    /// . Event Handling Callback
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    public bool HandleEvent(SdlEventArgs? evt)
    {
        // If evt is null, it's an unhandled event type by the wrapper, continue running
        if (evt == null)
            return !_shouldQuit;

        // Process known events
        switch (evt)
        {
            case QuitEventArgs:
                Console.WriteLine("Quit event received.");
                _shouldQuit = true;
                break;

            case WindowEventArgs windowEvt:
                // Check if the close button was clicked for *our* window
                if (
                    windowEvt.EventType == WindowEventType.CloseRequested
                    && _window != null
                    && // Ensure window exists
                    windowEvt.WindowId == _window.Id
                )
                {
                    Console.WriteLine("Window close requested.");
                    _shouldQuit = true;
                }
                // Optional: Handle other window events like resize
                if (windowEvt.EventType == WindowEventType.Resized)
                {
                    Console.WriteLine($"Window resized to {windowEvt.Data1}x{windowEvt.Data2}");
                }
                break;

            case KeyboardEventArgs keyEvt:
                // Example: Quit on Escape key press
                if (keyEvt.IsDown && keyEvt.Key == Key.Escape)
                {
                    Console.WriteLine("Escape key pressed (event).");
                    _shouldQuit = true;
                }
                // Console.WriteLine($"Key Event: Key={keyEvt.Key}, Mod={keyEvt.Modifiers}, Down={keyEvt.IsDown}");
                break;

                // Add other event handlers as needed
        }

        // Return false to signal quit, true to continue
        return !_shouldQuit;
    }

    /// <summary>
    /// . Update/Iteration Callback
    /// </summary>
    /// <returns></returns>
    public bool Update()
    {
        _world?.Update(1f / 60f);

        // Application logic (e.g., game state update) would go here

        // Rendering
        if (_renderer?.IsDisposed == false)
        {
            _renderer.DrawColor = new Color(100, 149, 237); // Cornflower Blue
            _renderer.Clear();

            // --- Add drawing code here ---

            _renderer.Present();
        }

        // Return false to signal quit, true to continue
        return !_shouldQuit;
    }

    /// <summary>
    /// . Quit Callback
    /// </summary>
    public void Cleanup()
    {
        Console.WriteLine("AppQuit callback started.");
        // Dispose resources in reverse order of creation
        _renderer?.Dispose();
        _window?.Dispose();
        Console.WriteLine("Window and Renderer disposed.");
        Console.WriteLine("AppQuit callback finished.");
        // SdlHost.Quit() is called automatically by the RunApplication wrapper's NativeAppQuit
    }
}

