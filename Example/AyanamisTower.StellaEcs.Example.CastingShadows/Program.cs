using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine;
using AyanamisTower.StellaEcs.Engine.Graphics;
using AyanamisTower.StellaEcs.Engine.DefaultRenderer;
using AyanamisTower.StellaEcs.Engine.Rendering;
using AyanamisTower.StellaEcs.Engine.Ecs;
using AyanamisTower.StellaEcs.Engine.Components;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;
using StbImageSharp;
using System.Drawing;
using Color = MoonWorks.Graphics.Color;

namespace AyanamisTower.StellaEcs.Example.CastingShadows;


internal static class Program
{
    public static void Main()
    {
        var game = new HelloGame();
        game.Run();
    }
    private sealed class HelloGame : App
    {
        private World world = new();
        // Renderer and meshes
        private DefaultRenderer? defaultRenderer;
        private Vector3 lightPos = new(-5f, 0f, 0f); // Store light position for debug display

        public HelloGame() : base("Hello MoonWorks - Shaders", 800, 480, debugMode: true)
        {
            // Customize defaults
            ClearColor = new MoonWorks.Graphics.Color(10, 20, 40);

            var pluginLoader = new HotReloadablePluginLoader(world, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();

            world.CreateEntity()
                .Set(new Position2D(0, 0))
                .Set(new Velocity2D(1, 1));

            world.EnableRestApi();

            // Set up camera position and orientation
            Camera.Position = new Vector3(0, 2, 5);
            Camera.LookAt(Vector3.Zero);
            Camera.Far = 200f; // Increase default far clip so distant objects don't vanish

            // Attach high-level renderer and register objects
            defaultRenderer = UseDefaultRenderer();
            // Set a simple sun-like point light
            defaultRenderer.SetPointLight(lightPos, new Vector3(1f, 1f, 0.95f), 0.1f);

            // Configure shadow mapping for solar system scale (planets orbit out to ~21 units)
            // Use near=0.5 (half Sun radius) and higher bias to prevent shadow acne
            defaultRenderer.SetShadows(nearPlane: 0.5f, farPlane: 50f, depthBias: 0.008f);

            // Bridge ECS -> Renderer: register sync system
            world.RegisterSystem(new RenderSyncSystem3DLit(defaultRenderer));
            world.RegisterSystem(new RenderSyncSystem3DTexturedLit(defaultRenderer));

            /*
            Testing shadow casting onto the plane below, currently this does not work and no real shadows are cast.
            */

            world.CreateEntity()
                .Set(new Position3D(0, -3, 0))
                .Set(new Mesh3D { Mesh = Mesh.CreatePlane3DLit(GraphicsDevice, 1f, 1f, new(1, 1, 1)) })
                .Set(new RenderLit3D())
                .Set(Rotation3D.Identity)
                .Set(new Size3D(100f, 1f, 100f)) // 1x1x1 cube
                .Set(new AngularVelocity3D(Vector3.Zero)); // Static cube

            world.CreateEntity()
                    .Set(new Position3D(2, -2.5f, 2))
                    .Set(new Mesh3D { Mesh = Mesh.CreateBox3D(GraphicsDevice, 1f, new(1, 1, 1)) })
                    .Set(new RenderLit3D())
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(new Vector3(0f, 0.25f, 0f)));

            world.CreateEntity()
                    .Set(new Position3D(0, 0, 0))
                    .Set(new Mesh3D { Mesh = Mesh.CreateBox3D(GraphicsDevice, 1f, new(1, 1, 1)) })
                    .Set(new RenderLit3D())
                    .Set(Rotation3D.Identity)
                    .Set(new Size3D(2f, 10f, 2f)) // 1x1x1 cube
                    .Set(new AngularVelocity3D(Vector3.Zero));

            world.CreateEntity()
                    .Set(new Position3D(0, -2.5f, 0))
                    .Set(new Mesh3D { Mesh = Mesh.CreateSphere3DLit(GraphicsDevice, 0.1f, new(1, 1, 1), 4) })
                    .Set(new RenderLit3D())
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(new Vector3(0f, 0.25f, 0f)));
        }

        protected override void OnUpdate(TimeSpan delta)
        {
            // Right click to capture/release mouse for camera control
            if (Inputs.Mouse.RightButton.IsPressed)
            {
                if (MouseCaptured)
                    ReleaseMouse();
                else
                    CaptureMouse();
            }

            // Mouse look when captured
            if (MouseCaptured)
            {
                var mouseDelta = GetMouseDelta();
                if (mouseDelta != Vector2.Zero)
                {
                    // Positive yaw when moving mouse right; keep pitch inverted for natural feel
                    Camera.Rotate(mouseDelta.X * 0.0015f, -mouseDelta.Y * 0.0015f);
                }
            }

            // Mouse wheel zoom: adjust camera FOV (only when not captured)
            if (!MouseCaptured && Inputs.Mouse.Wheel != 0)
            {
                var newFov = Camera.Fov - (Inputs.Mouse.Wheel * 0.05f);
                if (newFov < 0.3f) newFov = 0.3f;
                else if (newFov > 1.6f) newFov = 1.6f;
                Camera.Fov = newFov;
            }

            // Adjust far clipping distance on the fly
            if (Inputs.Keyboard.IsPressed(KeyCode.PageUp))
            {
                Camera.Far = MathF.Min(Camera.Far * 1.5f, 10000f);
            }
            if (Inputs.Keyboard.IsPressed(KeyCode.PageDown))
            {
                var minFar = MathF.Max(Camera.Near + 0.1f, 1f);
                Camera.Far = MathF.Max(Camera.Far / 1.5f, minFar);
            }


            float deltaSpeed = 15f * (float)delta.TotalSeconds;
            if (Inputs.Keyboard.IsDown(KeyCode.W)) Camera.MoveRelative(deltaSpeed, 0, 0);    // Forward
            if (Inputs.Keyboard.IsDown(KeyCode.S)) Camera.MoveRelative(-deltaSpeed, 0, 0);   // Backward
            if (Inputs.Keyboard.IsDown(KeyCode.A)) Camera.MoveRelative(0, -deltaSpeed, 0);   // Left
            if (Inputs.Keyboard.IsDown(KeyCode.D)) Camera.MoveRelative(0, deltaSpeed, 0);    // Right
            if (Inputs.Keyboard.IsDown(KeyCode.Q)) Camera.MoveRelative(0, 0, -deltaSpeed);   // Down
            if (Inputs.Keyboard.IsDown(KeyCode.E)) Camera.MoveRelative(0, 0, deltaSpeed);    // Up

            world.Update((float)delta.TotalSeconds); // Update with delta time
        }
    }
}
