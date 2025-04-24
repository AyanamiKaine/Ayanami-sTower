using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using Flecs.NET.Core;
using SDL3;

namespace AyanamisTower.NihilEx.SpinningCubeExample;

internal record struct Position2D(float X, float Y);

internal class FPSCounter
{
    private ulong _lastTime = SDL.SDL_GetPerformanceCounter();
    private int _frameCount;
    private double _fps;

    public void Update()
    {
        _frameCount++;
        var currentTime = SDL.SDL_GetPerformanceCounter();
        var elapsedTime = (currentTime - _lastTime) / (double)SDL.SDL_GetPerformanceFrequency();

        if (!(elapsedTime >= 0.1))
            return;

        _fps = _frameCount / elapsedTime;
        _frameCount = 0;
        _lastTime = currentTime;
    }

    public double FPS => _fps;
}

/// <summary>
/// Example application demonstrating color cycling, inheriting from the App base class.
/// </summary>
public class ColorApp : App // Inherit from App
{
    private readonly FPSCounter _fpsCounter = new();

    // Cube Geometry
    private Vector3[] _baseVertices = []; // Original cube vertices
    private Tuple<int, int>[] _edges = []; // Indices of vertices connected by edges

    // Transformed/Projected Vertices
    private Vector2[] _projectedVertices = []; // 2D screen coordinates

    /// <summary>
    /// Override OnInit to create window, renderer, and initialize state.
    /// </summary>
    protected override bool OnInit(string[] args)
    {
        SDL.SDL_LogInfo(
            (int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
            "MyColorApp OnInit started."
        );

        try
        {
            Renderer!.VSync = true;
        }
        catch (Exception)
        {
            SDL.SDL_LogError(
                (int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
                $"MyColorApp OnInit: Error creating renderer: {SDL.SDL_GetError()}"
            );
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return false;
        }
        // --- Create Singletons ---
        // 1. Camera Singleton
        float initialFovRadians = 60.0f * (MathF.PI / 180.0f); // e.g., 60 degrees FOV
        float aspectRatio = (float)InitalWidth / InitalHeight; // Use initial size
        var initialCamera = new Camera(
            position: new Vector3(0, 0, 5), // Move camera back along Z
            lookAtTarget: Vector3.Zero, // Look at the origin where the cube is
            worldUp: Vector3.UnitY,
            aspectRatio: aspectRatio,
            nearPlane: 0.1f,
            farPlane: 100.0f // Adjust far plane as needed
        );
        initialCamera.FieldOfViewRadians = initialFovRadians;
        World.Set(initialCamera); // Set as singleton

        World
            .Entity("SpinningCube")
            .Add<Box>() // Add the Box tag
            .Set(new Size3D(new Vector3(2))) // Width=2, Height=2, Depth=2
            .Set(new Position3D(new(0, 0, 0)))
            .Set(new Orientation(Quaternion.Identity))
            .Set(new RotationSpeed3D(new Vector3(0.8f * 0.8f, 0.8f * 1.0f, 0.8f * 1.2f)))
            .Set(RgbaColor.Red);

        SDL.SDL_LogInfo(
            (int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION,
            "MyColorApp OnInit finished successfully."
        );
        return true; // Signal success
    }

    /// <summary>
    /// Gets called when the app wants to quit
    /// </summary>
    protected override void OnQuit() { }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Create an instance of your application class
        var myApp = new ColorApp()
        {
            InitalTitle = "SDL3 Color Cycle (Framework)",
            InitalHeight = 600,
            InitalWidth = 800,
        };
        // Run the application - the base App class handles the rest.
        myApp.Run([]);
    }
}
