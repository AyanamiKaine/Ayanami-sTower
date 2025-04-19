using System.Drawing;
using System.Numerics;
using Flecs.NET.Core;
using SDL3;

namespace AyanamisTower.NihilEx.MinimalExample;


/// <summary>
/// Represents the minimal application example demonstrating the SDL3 framework integration.
/// </summary>
public class MinimalApp : App // Inherit from App
{
    private Renderer? _renderer;

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

        SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnInit started.");

        // Create Window and Renderer
        Window!.Title = "SDL3 Minimal Example";
        Window!.Height = 600;
        Window!.Width = 800;

        try
        {
            _renderer = Window?.CreateRenderer();
            _renderer!.VSync = true;
            _renderer!.DrawColor = Color.Bisque;

            World.Set(_renderer);
        }
        catch (Exception)
        {
            SDL.LogError(SDL.LogCategory.Application, $"MinimalApp OnInit: Error creating renderer: {SDL.GetError()}");
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return SDL.AppResult.Failure;
        }

        SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnInit finished successfully.");
        return SDL.AppResult.Continue; // Signal success
    }


    /*
    We probably add some on event delegates, so people can subscribe to an event
    like OnWindowResized. And here we simply fire the event.
    */

    /// <summary>
    /// Override OnEvent to handle application-specific events.
    /// </summary>
    protected override SDL.AppResult OnEvent(ref SDL.Event e)
    {
        // Handle window resize event specifically
        if (e.Type == (uint)SDL.EventType.WindowResized)
        {
            SDL.LogInfo(SDL.LogCategory.Application, $"MinimalApp OnEvent: Window Resized (from event data) to: {Window?.Width} x {Window?.Height}");

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
        SDL.LogInfo(SDL.LogCategory.Application, $"MinimalApp OnQuit started with result: {result}");

        // Destroy resources created in OnInit
        _renderer?.Dispose();
        Window?.Dispose();

        // Call base OnQuit *after* cleaning up derived class resources
        // to ensure SDL subsystems are shut down last.
        base.OnQuit(result);

        SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnQuit finished.");
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Create an instance of your application class
        var myApp = new MinimalApp();

        // Run the application - the base App class handles the rest.
        myApp.Run([]);
    }
}