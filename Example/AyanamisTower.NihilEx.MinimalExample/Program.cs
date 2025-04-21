using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using Flecs.NET.Core;
using SDL3;

namespace AyanamisTower.NihilEx.MinimalExample;


/// <summary>
/// Represents the minimal application example demonstrating the SDL3 framework integration.
/// </summary>
public class MinimalApp : App // Inherit from App
{
    /// <summary>
    /// Override OnInit to create window, renderer, and initialize state.
    /// </summary>
    protected override SDL.AppResult OnInit(string[] args)
    {
        SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnInit started.");

        try
        {
            Renderer!.VSync = true;
        }
        catch (Exception)
        {
            SDL.LogError(SDL.LogCategory.Application, $"MinimalApp OnInit: Error creating renderer: {SDL.GetError()}");
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return SDL.AppResult.Failure;
        }

        /* 
        Uncomment to see a rotating box

        World.Entity("RotatingBox")
            .Set(new Position2D { Value = new Vector2(400, 300) })
            .Set(new ECS.Size { Value = new Vector2(100, 100) })
            .Set(new Orientation { Value = Quaternion.Identity })
            .Set(new RotationSpeed { Speed = 90 * (MathF.PI / 180.0f) })
            .Set(new RgbaColor(r: 255, g: 0, b: 0, a: 255));

        */
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
        SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnQuit finished.");
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Create an instance of your application class
        var myApp = new MinimalApp()
        {
            InitalTitle = "SDL3 Minimal Example",
            InitalWidth = 400,
            InitalHeight = 400,
        };

        // Run the application - the base App class handles the rest.
        myApp.Run([]);
    }
}