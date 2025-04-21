using System.Drawing;
using System.Numerics;
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
    // State for color cycling (now instance members)
    private float _currentR;
    private float _currentG;
    private float _currentB;
    private float _deltaR;
    private float _deltaG;
    private float _deltaB;
    private readonly FPSCounter _fpsCounter = new();
    // Cube Geometry
    private Vector3[] _baseVertices = []; // Original cube vertices
    private Tuple<int, int>[] _edges = []; // Indices of vertices connected by edges

    // Transformed/Projected Vertices
    private Vector2[] _projectedVertices = []; // 2D screen coordinates

    // Rotation State
    private float _angleX = 0.0f;
    private float _angleY = 0.0f;
    private float _angleZ = 0.0f;
    private const float RotationSpeed = 0.8f; // Radians per second

    // Projection State
    private int _screenWidth = 800;
    private int _screenHeight = 600;
    private float _fieldOfView = 300.0f; // Affects perspective intensity
    private float _zOffset = 5.0f; // Move cube away from camera
    /// <summary>
    /// Override OnInit to create window, renderer, and initialize state.
    /// </summary>
    protected override SDL.AppResult OnInit(string[] args)
    {
        // Call base OnInit first to initialize SDL subsystems (optional but good practice)
        SDL.AppResult baseResult = base.SDLInit(args);
        if (baseResult != SDL.AppResult.Continue)
        {
            return baseResult; // Exit if base initialization failed
        }

        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit started.");

        // Configuration for window and color
        _currentR = 100f; // Start color
        _currentG = 149f;
        _currentB = 237f;
        _deltaR = 30.0f; // Change rates per second
        _deltaG = 50.0f;
        _deltaB = 70.0f;

        // Create Window and Renderer
        Window!.Title = "SDL3 Color Cycle (Framework)";
        Window!.Height = 600;
        Window!.Width = 800;

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

        DefineCube();
        _projectedVertices = new Vector2[_baseVertices.Length]; // Initialize array for 2D points
        SDL.LogInfo(SDL.LogCategory.Application, "MyColorApp OnInit finished successfully.");
        return SDL.AppResult.Continue; // Signal success
    }


    private void DefineCube()
    {
        // Define 8 vertices for a unit cube centered at the origin
        _baseVertices = [
                new(-1, -1, -1), // 0
                new( 1, -1, -1), // 1
                new( 1,  1, -1), // 2
                new(-1,  1, -1), // 3
                new(-1, -1,  1), // 4
                new( 1, -1,  1), // 5
                new( 1,  1,  1), // 6
                new(-1,  1,  1)  // 7
            ];

        // Define 12 edges connecting the vertices by index
        _edges = [
                // Back face
                Tuple.Create(0, 1), Tuple.Create(1, 2), Tuple.Create(2, 3), Tuple.Create(3, 0),
                // Front face
                Tuple.Create(4, 5), Tuple.Create(5, 6), Tuple.Create(6, 7), Tuple.Create(7, 4),
                // Connecting edges
                Tuple.Create(0, 4), Tuple.Create(1, 5), Tuple.Create(2, 6), Tuple.Create(3, 7)
            ];
    }
    /// <summary>
    /// Override OnIterate for update and rendering logic.
    /// </summary>
    protected override SDL.AppResult OnIterate(float deltaTime)
    {
        // --- Update Rotation ---
        _angleX += RotationSpeed * deltaTime * 0.8f; // Slightly different speeds
        _angleY += RotationSpeed * deltaTime;
        _angleZ += RotationSpeed * deltaTime * 1.2f;
        // --- End Update Rotation ---

        // --- Transformation and Projection ---
        // Create rotation matrices
        Matrix4x4 rotX = Matrix4x4.CreateRotationX(_angleX);
        Matrix4x4 rotY = Matrix4x4.CreateRotationY(_angleY);
        Matrix4x4 rotZ = Matrix4x4.CreateRotationZ(_angleZ);
        Matrix4x4 transform = rotX * rotY * rotZ; // Combine rotations

        float screenCenterX = _screenWidth / 2.0f;
        float screenCenterY = _screenHeight / 2.0f;

        // Rotate and project each vertex
        for (int i = 0; i < _baseVertices.Length; i++)
        {
            // Apply rotation
            Vector3 rotatedVertex = Vector3.Transform(_baseVertices[i], transform);

            // Apply simple perspective projection
            // Move cube away from camera along Z
            float zProjected = rotatedVertex.Z + _zOffset;

            // Basic perspective scaling factor (avoid division by zero or small numbers)
            float scale = (zProjected > 0.1f) ? (_fieldOfView / zProjected) : _fieldOfView / 0.1f;

            float projectedX = (rotatedVertex.X * scale) + screenCenterX;
            float projectedY = (rotatedVertex.Y * scale) + screenCenterY;

            _projectedVertices[i] = new Vector2(projectedX, projectedY);
        }

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
        Renderer!.DrawColor = (ECS.RgbaColor)Color.FromArgb(255, (int)_currentR, (int)_currentG, (int)_currentB);

        Renderer?.Clear();

        World.Progress(deltaTime);
        _fpsCounter.Update();

        Renderer!.DrawColor = (ECS.RgbaColor)Color.FromArgb(255, (int)(255 - _currentR), (int)(255 - _currentG), (int)(255 - _currentB));

        Renderer?.ShowDebugText(10, 10, $"FPS: {_fpsCounter.FPS}");

        Renderer!.DrawColor = (ECS.RgbaColor)Color.FromArgb(255, 255, 255, 255);

        // Draw the edges
        foreach (var edge in _edges)
        {
            Vector2 p1 = _projectedVertices[edge.Item1];
            Vector2 p2 = _projectedVertices[edge.Item2];
            // SDL.RenderLine expects integers
            Renderer?.RenderLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
        }

        Renderer?.Present();
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
            _screenWidth = e.Window.Data1; // Update screen dimensions
            _screenHeight = e.Window.Data2;
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

        // Destroy resources created in OnInit
        Renderer?.Dispose();
        Window?.Dispose();

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
        var myApp = new ColorApp();

        // Run the application - the base App class handles the rest.
        myApp.Run([]);
        return;
    }
}