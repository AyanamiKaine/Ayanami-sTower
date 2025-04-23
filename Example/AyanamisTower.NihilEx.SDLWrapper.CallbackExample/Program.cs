using System;
using System.Threading; // For Thread.Sleep (might not be needed with callbacks)

namespace AyanamisTower.NihilEx.SDLWrapper.CallbackExample;


/// <summary>
/// Basic example using callbacks instead of the mainloop
/// </summary>
public class CallbackApplication
{
    private Window? _window;
    private Renderer? _renderer;
    private bool _shouldQuit = false; // Flag to signal exit request

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
        if (evt == null) return !_shouldQuit;

        // Process known events
        switch (evt)
        {
            case QuitEventArgs:
                Console.WriteLine("Quit event received.");
                _shouldQuit = true;
                break;

            case WindowEventArgs windowEvt:
                // Check if the close button was clicked for *our* window
                if (windowEvt.EventType == WindowEventType.CloseRequested &&
                    _window != null && // Ensure window exists
                    windowEvt.WindowId == _window.Id)
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
/// <summary>
/// Program
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [STAThread]
    public static int Main(string[] args)
    {
        Console.WriteLine("Starting SDL3 Callback Application Example...");

        var app = new CallbackApplication();

        // Use the SdlHost.RunApplication method, passing the instance methods
        // Note: SdlHost.RunApplication handles SDL_Init and SDL_Quit internally via the callbacks.
        int exitCode = SdlHost.RunApplication(
            app.Initialize,
            app.Update,
            app.HandleEvent,
            app.Cleanup,
            args // Pass command line args
        );

        Console.WriteLine($"Application finished with exit code: {exitCode}");
        return exitCode;
    }
}
