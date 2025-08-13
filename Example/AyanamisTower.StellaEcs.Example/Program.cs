using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine;
using AyanamisTower.StellaEcs.Engine.Graphics;
using AyanamisTower.StellaEcs.Engine.DefaultRenderer;
using AyanamisTower.StellaEcs.Engine.Rendering;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;

namespace AyanamisTower.StellaEcs.Example;


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
        private Mesh rectMesh;
        private Vector3 rectColor = new(1f, 0f, 0f); // Start as red
        private float time;
        private float rectSize = 150f; // pixels
        // Add: FPS counter & window title
        private readonly string baseTitle = "Hello MoonWorks - Shaders";
        private float fpsTimer;
        private int fpsFrames;
        private double fps;
        // Add: rectangle hover state and currently displayed color (may be highlighted)
        private bool rectHovered;
        private Vector3 displayedRectColor;
        // Add: text overlay
        private Font? uiFont; // MSDF font loaded from Shaders/
        private bool showOverlay = true;
        private Vector3 cubePos = new(0, 0, 0);

        public HelloGame() : base("Hello MoonWorks - Shaders", 800, 480, debugMode: true)
        {
            // Customize defaults
            ClearColor = new Color(10, 20, 40);

            var pluginLoader = new HotReloadablePluginLoader(world, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();

            world.CreateEntity()
                .Set(new Position2D(0, 0))
                .Set(new Velocity2D(1, 1));

            world.EnableRestApi();

            // Camera is provided by App base class (this.Camera)

            // Create 2D rectangle mesh (pixel space)
            rectMesh = Mesh.CreateQuad(GraphicsDevice, rectSize, rectSize, rectColor);
            displayedRectColor = rectColor;

            // Try to load an MSDF font from Assets/
            // Provide the path to your MSDF font (.ttf/.otf with matching .json and .png atlas next to it)
            // Example expected files: Assets/Roboto-Regular.ttf, Assets/Roboto-Regular.json, Assets/Roboto-Regular.png
            uiFont = Font.Load(GraphicsDevice, RootTitleStorage, "Assets/Roboto-Regular.ttf");
            if (uiFont == null)
            {
                Logger.LogWarn("MSDF font not found in Assets/. Place .ttf/.otf with matching .json and .png next to it (msdf-atlas-gen output). Overlay will be disabled.");
                showOverlay = false;
            }

            // Attach high-level renderer and register objects
            defaultRenderer = UseDefaultRenderer();
            // Use a flat-colored 3D cube (cyan)
            defaultRenderer.AddCube(
                () => Matrix4x4.CreateFromYawPitchRoll(time, time * 0.7f, 0) * Matrix4x4.CreateTranslation(cubePos),
                new Vector3(0f, 1f, 1f), // flat color
                0.7f
            );
            // Keep the 2D rect in pixel space
            defaultRenderer.AddQuad2D(
                () => rectMesh,
                () => Matrix4x4.CreateTranslation(new Vector3(MainWindow.Width / 2f, MainWindow.Height / 2f, 0))
            );
            defaultRenderer.SetOverlayBuilder(batch =>
            {
                if (!showOverlay || uiFont == null) return false;
                const int size = 18;
                const float x = 12f;
                float y = 22f;
                var white = new Color(230, 235, 245);
                var accent = new Color(170, 200, 255);

                batch.Add(uiFont!, "MoonWorks Example", size + 2, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), accent);
                y += 26f;
                batch.Add(uiFont!, "Esc: Quit", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "F11: Toggle Fullscreen", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "Mouse Wheel: Zoom Camera", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "Click square to toggle color", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, $"FPS: {(int)fps}", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                return true;
            });
        }

        private readonly struct Vertex(System.Numerics.Vector2 pos, float r, float g, float b)
        {
            public readonly System.Numerics.Vector2 Pos = pos;
            public readonly float R = r;
            public readonly float G = g;
            public readonly float B = b;
        }

        protected override void OnUpdate(TimeSpan delta)
        {
            // Esc and F11 are handled by App by default

            // Toggle overlay with F1
            if (Inputs.Keyboard.IsPressed(KeyCode.F1))
            {
                showOverlay = !showOverlay;
            }

            // Mouse wheel zoom: adjust camera FOV
            if (Inputs.Mouse.Wheel != 0)
            {
                var newFov = Camera.Fov - (Inputs.Mouse.Wheel * 0.05f);
                if (newFov < 0.3f) newFov = 0.3f;
                else if (newFov > 1.6f) newFov = 1.6f;
                Camera.Fov = newFov;
            }

            time += (float)delta.TotalSeconds;

            // Simple WASD camera movement
            float moveSpeed = 2.5f * (float)delta.TotalSeconds;
            var move = Vector3.Zero;
            if (Inputs.Keyboard.IsDown(KeyCode.W)) move += new Vector3(0, 0, -moveSpeed);
            if (Inputs.Keyboard.IsDown(KeyCode.S)) move += new Vector3(0, 0, moveSpeed);
            if (Inputs.Keyboard.IsDown(KeyCode.A)) move += new Vector3(-moveSpeed, 0, 0);
            if (Inputs.Keyboard.IsDown(KeyCode.D)) move += new Vector3(moveSpeed, 0, 0);
            Camera.Move(move);

            // Rectangle hover + click detection (pixel space)
            {
                int mouseX = Inputs.Mouse.X;
                int mouseY = Inputs.Mouse.Y;
                float cx = MainWindow.Width / 2f;
                float cy = MainWindow.Height / 2f;
                float half = rectSize / 2f;
                float left = cx - half;
                float right = cx + half;
                float top = cy - half;
                float bottom = cy + half;

                bool inside = mouseX >= left && mouseX <= right && mouseY >= top && mouseY <= bottom;

                // Hover highlight: only rebuild mesh when visible color changes
                if (inside != rectHovered)
                {
                    rectHovered = inside;
                    var targetColor = rectHovered ? Saturate(rectColor * 1.35f) : rectColor;
                    if (!ApproximatelyEqual(displayedRectColor, targetColor))
                    {
                        displayedRectColor = targetColor;
                        rectMesh.Dispose();
                        rectMesh = Mesh.CreateQuad(GraphicsDevice, rectSize, rectSize, displayedRectColor);
                    }
                }

                if (inside && Inputs.Mouse.LeftButton.IsPressed)
                {
                    // Toggle color between red and green, keep hover highlight
                    rectColor = (rectColor.X == 1f && rectColor.Y == 0f)
                        ? new Vector3(0f, 1f, 0f)
                        : new Vector3(1f, 0f, 0f);

                    var targetColor = rectHovered ? Saturate(rectColor * 1.35f) : rectColor;
                    if (!ApproximatelyEqual(displayedRectColor, targetColor))
                    {
                        displayedRectColor = targetColor;
                        rectMesh.Dispose();
                        rectMesh = Mesh.CreateQuad(GraphicsDevice, rectSize, rectSize, displayedRectColor);
                    }
                }
            }

            // Place cube on plane Z=0 at mouse position when right-clicking
            if (Inputs.Mouse.RightButton.IsPressed && MouseToPlaneZ(out var wp, planeZ: 0f))
            {
                cubePos = wp;
            }

            // Update FPS in window title about twice a second
            fpsFrames++;
            fpsTimer += (float)delta.TotalSeconds;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                var ms = 1000.0 / global::System.Math.Max(fps, 0.0001);
                MainWindow.SetTitle($"{baseTitle} | FPS {fps:0} ({ms:0.0} ms) | F11 Fullscreen | Wheel Zoom | Esc Quit | Click square | F1 Help");
                fpsFrames = 0;
                fpsTimer = 0f;
            }
            world.Update(1f / 60f); // Update with delta time
        }

        private static Vector3 Saturate(Vector3 v)
        {
            return new Vector3(MathF.Min(v.X, 1f), MathF.Min(v.Y, 1f), MathF.Min(v.Z, 1f));
        }

        private static bool ApproximatelyEqual(Vector3 a, Vector3 b)
        {
            const float eps = 1e-3f;
            return MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps && MathF.Abs(a.Z - b.Z) < eps;
        }

        // Ensure rect mesh is disposed when the app shuts down
        protected override void Destroy()
        {
            try { rectMesh.Dispose(); } catch { }
            base.Destroy();
        }
    }
}
