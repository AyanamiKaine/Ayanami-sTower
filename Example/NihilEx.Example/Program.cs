using SDL3;

namespace NihilEx.Example;


/// <summary>
/// Example application demonstrating color cycling, inheriting from the App base class.
/// </summary>
public class MyColorApp : App // Inherit from App
{
    // Store SDL Window and Renderer as instance members
    private nint _window;
    private nint _renderer;

    // State for color cycling (now instance members)
    private float _currentR;
    private float _currentG;
    private float _currentB;
    private float _deltaR;
    private float _deltaG;
    private float _deltaB;

    /// <summary>
    /// Override OnInit to create window, renderer, and initialize state.
    /// </summary>
    protected override SDL.AppResult OnInit(string[] args)
    {
        // Call base OnInit first to initialize SDL subsystems (optional but good practice)
        SDL.AppResult baseResult = base.OnInit(args);
        if (baseResult != SDL.AppResult.Continue)
        {
            return baseResult; // Exit if base initialization failed
        }

        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit started.");

        // Configuration for window and color
        const int width = 800;
        const int height = 600;
        const string title = "SDL3 Color Cycle (Framework)";
        _currentR = 100f; // Start color
        _currentG = 149f;
        _currentB = 237f;
        _deltaR = 30.0f; // Change rates per second
        _deltaG = 50.0f;
        _deltaB = 70.0f;

        // Create Window and Renderer
        if (!SDL.CreateWindowAndRenderer(title, width, height, SDL.WindowFlags.Resizable, out _window, out _renderer))
        {
            SDL.LogError(SDL.LogCategory.Application, $"MyColorApp OnInit: Error creating window/renderer: {SDL.GetError()}");
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return SDL.AppResult.Failure;
        }

        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit finished successfully.");
        return SDL.AppResult.Continue; // Signal success
    }

    /// <summary>
    /// Override OnIterate for update and rendering logic.
    /// </summary>
    protected override SDL.AppResult OnIterate(float deltaTime)
    {
        // --- Color Update Logic ---
        _currentR += _deltaR * deltaTime;
        if (_currentR > 255.0f) { _currentR = 255.0f; _deltaR *= -1.0f; }
        else if (_currentR < 0.0f) { _currentR = 0.0f; _deltaR *= -1.0f; }

        _currentG += _deltaG * deltaTime;
        if (_currentG > 255.0f) { _currentG = 255.0f; _deltaG *= -1.0f; }
        else if (_currentG < 0.0f) { _currentG = 0.0f; _deltaG *= -1.0f; }

        _currentB += _deltaB * deltaTime;
        if (_currentB > 255.0f) { _currentB = 255.0f; _deltaB *= -1.0f; }
        else if (_currentB < 0.0f) { _currentB = 0.0f; _deltaB *= -1.0f; }
        // --- End Color Update Logic ---

        // --- Rendering ---
        SDL.SetRenderDrawColor(_renderer, (byte)_currentR, (byte)_currentG, (byte)_currentB, 255);
        SDL.RenderClear(_renderer);
        SDL.RenderPresent(_renderer);
        // --- End Rendering ---

        return SDL.AppResult.Continue; // Keep iterating
    }

    /// <summary>
    /// Override OnEvent to handle application-specific events.
    /// </summary>
    protected override SDL.AppResult OnEvent(ref SDL.Event e)
    {
        // Handle window resize event specifically
        if (e.Type == (uint)SDL.EventType.WindowResized)
        {
            return SDL.AppResult.Continue;
        }

        // Call the base OnEvent implementation to handle default events (like Quit)
        return base.OnEvent(ref e);
    }

    /// <summary>
    /// Override OnQuit to clean up resources created in OnInit.
    /// </summary>
    protected override void OnQuit(SDL.AppResult result)
    {
        SDL.LogInfo(SDL.LogCategory.Application, $"MyColorApp OnQuit started with result: {result}");

        // Destroy resources created in OnInit
        if (_renderer != 0)
        {
            SDL.DestroyRenderer(_renderer);
            SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnQuit: Renderer destroyed.");
        }
        if (_window != 0)
        {
            SDL.DestroyWindow(_window);
            SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnQuit: Window destroyed.");
        }

        // Call base OnQuit *after* cleaning up derived class resources
        // to ensure SDL subsystems are shut down last.
        base.OnQuit(result);

        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnQuit finished.");
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Create an instance of your application class
        var myApp = new MyColorApp();

        // Run the application - the base App class handles the rest.
        myApp.Run([]);
        return;
    }
}