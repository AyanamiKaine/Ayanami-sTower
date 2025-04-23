using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using AyanamisTower.NihilEx.ECS.Events;
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
    protected override bool OnInit(string[] args)
    {
        //SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnInit started.");

        try
        {
            Renderer!.VSync = true;
        }
        catch (Exception)
        {
            //SDL.LogError(SDL.LogCategory.Application, $"MinimalApp OnInit: Error creating renderer: {SDL.GetError()}");
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return false;
        }

        /* 
        Uncomment to see a rotating box
        */

        World.Entity("RotatingBox")
            .Set(new Position2D { Value = new Vector2(400, 300) })
            .Set(new Size2D { Value = new Vector2(100, 100) })
            .Set(new Orientation { Value = Quaternion.Identity })
            .Set(new RotationSpeed2D { Speed = 90 * (MathF.PI / 180.0f) })
            .Set(new RgbaColor(r: 255, g: 0, b: 0, a: 255));

        AppEntity.Observe((ref WindowResize windowResize) =>
        {
            Console.WriteLine($"Window was resized. New Height: {windowResize.Height} New Width: {windowResize.Width}");
        });

        AppEntity.Observe((ref KeyDownEvent keyDownEvent) =>
        {
            Console.WriteLine($"Key was pressed down: {keyDownEvent.Keycode}");
        });

        AppEntity.Observe((ref KeyUpEvent keyUpEvent) =>
        {
            Console.WriteLine($"Key was pressed up: {keyUpEvent.Keycode}");
        });

        AppEntity.Observe((ref MouseMotionEvent mouseMotionEvent) =>
        {
            Console.WriteLine($"Mouse is moving: X: {mouseMotionEvent.X} Y:{mouseMotionEvent.Y} YRel: {mouseMotionEvent.YRel} XRel: {mouseMotionEvent.XRel}");
        });

        AppEntity.Observe((ref MouseButtonDownEvent mouseButtonDownEvent) =>
        {
            Console.WriteLine($"Mouse button was pressed down: {mouseButtonDownEvent.MouseButton}");
        });

        AppEntity.Observe((ref MouseWheelEvent mouseWheelEvent) =>
        {
            Console.WriteLine($"Mouse wheel is moving:  DirectionType: {mouseWheelEvent.Direction} ScrollX: {mouseWheelEvent.ScrollX} ScrollY: {mouseWheelEvent.ScrollY}");
        });

        AppEntity.Observe((ref WindowMovedEvent windowMovedEvent) =>
        {
            Console.WriteLine($"Window was moved:  NewY: {windowMovedEvent.Y} NewX: {windowMovedEvent.X}");
        });


        AppEntity.Observe((ref WindowMouseEnterEvent windowMouseEnter) =>
        {
            Console.WriteLine($"Mouse enter the window at X:{windowMouseEnter.X} Y:{windowMouseEnter.Y}");
        });


        AppEntity.Observe((ref WindowMouseLeaveEvent windowMouseLeave) =>
        {
            Console.WriteLine($"Mouse leave the window at X:{windowMouseLeave.X} Y:{windowMouseLeave.Y}");
        });

        //SDL.LogInfo(SDL.LogCategory.Application, "MinimalApp OnInit finished successfully.");
        return true; // Signal success
    }

    /// <summary>
    /// Override OnQuit to clean up resources when the application exits.
    /// </summary>
    protected override void OnQuit()
    {
        Console.WriteLine("MinimalApp OnQuit started");
        Console.WriteLine("MinimalApp OnQuit finished.");
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