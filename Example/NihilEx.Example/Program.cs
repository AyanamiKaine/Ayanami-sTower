using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using Flecs.NET.Core;
using SDL3;

namespace AyanamisTower.NihilEx.SpinningCubeExample;

internal record struct Position2D(float X, float Y);
internal class FPSCounter
{
    private ulong _lastTime = SDL.GetPerformanceCounter();
    private int _frameCount;
    private double _fps;

    public void Update()
    {
        _frameCount++;
        var currentTime = SDL.GetPerformanceCounter();
        var elapsedTime = (currentTime - _lastTime) / (double)SDL.GetPerformanceFrequency();

        if (!(elapsedTime >= 0.1)) return;

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
    protected override SDL.AppResult OnInit(string[] args)
    {
        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit started.");

        try
        {
            Renderer!.VSync = true;
        }
        catch (Exception)
        {
            SDL.LogError(SDL.LogCategory.Application, $"MyColorApp OnInit: Error creating renderer: {SDL.GetError()}");
            // No need to call SDL.Quit() here, base.OnQuit will handle subsystem cleanup if necessary.
            return SDL.AppResult.Failure;
        }
        // --- Create Singletons ---
        // 1. Camera Singleton
        float initialFovRadians = 60.0f * (MathF.PI / 180.0f); // e.g., 60 degrees FOV
        float aspectRatio = (float)InitalWidth / InitalHeight; // Use initial size
        var initialCamera = new Camera(
            position: new Vector3(0, 0, 5),  // Move camera back along Z
            lookAtTarget: Vector3.Zero,      // Look at the origin where the cube is
            worldUp: Vector3.UnitY,
            aspectRatio: aspectRatio,
            nearPlane: 0.1f,
            farPlane: 100.0f                 // Adjust far plane as needed
        );
        initialCamera.FieldOfViewRadians = initialFovRadians;
        World.Set(initialCamera); // Set as singleton


        /*
        // --- Create the Cube Entity ---
        var cubeEntity = World.Entity("Custom Mesh Example");

        // Define Geometry
        Vector3[] baseVertices = [
            new(-1, -1, -1), new( 1, -1, -1), new( 1,  1, -1), new(-1,  1, -1), // Back face
                new(-1, -1,  1), new( 1, -1,  1), new( 1,  1,  1), new(-1,  1,  1)  // Front face
        ];
        Edge[] edges = [
           // Back face
           new(0, 1), new(1, 2), new(2, 3), new(3, 0),
                // Front face
                new(4, 5), new(5, 6), new(6, 7), new(7, 4),
                // Connecting edges
                new(0, 4), new(1, 5), new(2, 6), new(3, 7)
        ];

        
        cubeEntity.Set(new MeshGeometry(baseVertices, edges))
                  .Set(new ProjectedMesh { ProjectedVertices = new Vector2[baseVertices.Length] }) // Initialize array
                  .Set(new Position3D(Vector3.Zero)) // Place cube at origin
                  .Set(new Orientation(Quaternion.Identity)) // Start with no rotation
                  .Set(new RotationSpeed3D(new Vector3( // Different speeds per axis (radians/sec)
                      0.8f * 0.8f,
                      0.8f * 1.0f,
                      0.8f * 1.2f
                  )))
                  .Set(RgbaColor.Red); // Cube lines are white

        */

        World.Entity("SpinningCube")
            .Add<Box>()                             // Add the Box tag
            .Set(new Size3D(new Vector3(2, 2, 2)))  // Width=2, Height=2, Depth=2
            .Set(new Position3D(new(0, 0, 0)))
            .Set(new Orientation(Quaternion.Identity))
            .Set(new RotationSpeed3D(new Vector3(0.8f * 0.8f, 0.8f * 1.0f, 0.8f * 1.2f)))
            .Set(RgbaColor.Red);

        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit finished successfully.");
        return SDL.AppResult.Continue; // Signal success
    }

    /// <summary>
    /// Override OnEvent to handle application-specific events.
    /// </summary>
    protected override SDL.AppResult OnEvent(ref SDL.Event e)
    {
        // Handle window resize event specifically
        if (e.Type == (uint)SDL.EventType.WindowResized)
        {
            SDL.LogInfo(SDL.LogCategory.Application, $"MyColorApp OnEvent: Window Resized (from event data) to: {Window?.Width} x {Window?.Height}");

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
        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnQuit finished.");
    }
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
        return;
    }
}