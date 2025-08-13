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
                                       // ECS-rendered lit meshes
                                       // Solar system
        private Mesh? sunMesh;
        private Texture? sunTexture;
        private Entity sunEntity;
        private readonly List<Planet> planets = new();
        private readonly List<Texture> planetTextures = new();
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
        private Vector3 lightPos = new(2f, 3f, 2f); // Store light position for debug display
        // Camera controls
        private float mouseSensitivity = 0.0015f; // Reduced for relative mouse mode
        private float moveSpeed = 15f;

        private sealed class Planet
        {
            public string Name = string.Empty;
            public float Radius;            // world units
            public float OrbitRadius;       // world units
            public float AngularSpeed;      // radians per second (orbit)
            public float SelfSpin;          // radians per second (rotation)
            public float Phase;             // initial orbit angle
            public string? Parent;          // parent planet name (for moons)
            public Mesh? Mesh;
            public Texture? Texture;
            public Entity Entity;
        }

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

            // Set up camera position and orientation
            Camera.Position = new Vector3(0, 2, 5);
            Camera.LookAt(Vector3.Zero);
            Camera.Far = 200f; // Increase default far clip so distant objects don't vanish

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
            // Set a simple sun-like point light
            defaultRenderer.SetPointLight(lightPos, new Vector3(1f, 1f, 0.95f), 0.15f);

            // Configure shadow mapping for solar system scale (planets orbit out to ~21 units)
            defaultRenderer.SetShadows(farPlane: 50f, depthBias: 0.02f);

            // Bridge ECS -> Renderer: register sync system
            world.RegisterSystem(new RenderSyncSystem3DLit(defaultRenderer));
            world.RegisterSystem(new RenderSyncSystem3DTexturedLit(defaultRenderer));

            // Build a small solar system using textures in Assets/
            // Sun
            sunTexture = TryLoadTextureAny("Assets/Sun");
            if (sunTexture != null)
            {
                sunMesh = Mesh.CreateSphere3DTexturedLit(GraphicsDevice, radius: 1.5f, slices: 128, stacks: 64);
                sunEntity = world.CreateEntity()
                    .Set(new Position3D(0, 0, 0))
                    .Set(new Mesh3D { Mesh = sunMesh })
                    .Set(new Texture2DRef { Texture = sunTexture })
                    .Set(new RenderTexturedLit3D())
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(new Vector3(0f, 0.15f, 0f)));
                planetTextures.Add(sunTexture);
                lightPos = Vector3.Zero; // put the light at the sun
            }
            // Planets (sizes/distances are artistic, not to scale)
            AddPlanet("Mercury", baseName: "Mercury", radius: 0.18f, orbitRadius: 3.0f, orbitSpeed: 0.09f, selfSpin: 0.4f, phase: 0.0f);
            AddPlanet("Venus", baseName: "Venus", radius: 0.25f, orbitRadius: 5.0f, orbitSpeed: 0.05f, selfSpin: 0.2f, phase: 0.5f);
            AddPlanet("Earth", baseName: "Earth", radius: 0.27f, orbitRadius: 7.0f, orbitSpeed: 0.04f, selfSpin: 1.2f, phase: 0.2f);
            AddPlanet("Moon", baseName: "Moon", radius: 0.07f, orbitRadius: 0.6f, orbitSpeed: 1.0f, selfSpin: 0.6f, phase: 1.2f, parent: "Earth");
            AddPlanet("Mars", baseName: "Mars", radius: 0.22f, orbitRadius: 9.0f, orbitSpeed: 0.032f, selfSpin: 0.8f, phase: 2.0f);
            AddPlanet("Jupiter", baseName: "Jupiter", radius: 0.9f, orbitRadius: 12.0f, orbitSpeed: 0.018f, selfSpin: 2.0f, phase: 1.0f);
            AddPlanet("Saturn", baseName: "Saturn", radius: 0.8f, orbitRadius: 15.0f, orbitSpeed: 0.014f, selfSpin: 1.8f, phase: 2.5f);
            AddPlanet("Uranus", baseName: "Uranus", radius: 0.5f, orbitRadius: 18.0f, orbitSpeed: 0.010f, selfSpin: 1.2f, phase: 0.3f);
            AddPlanet("Neptune", baseName: "Neptune", radius: 0.48f, orbitRadius: 21.0f, orbitSpeed: 0.008f, selfSpin: 1.0f, phase: 1.7f);


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
                batch.Add(uiFont!, "Right Click: Capture Mouse for Camera", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "WASD+QE: Move Camera (FPS-style)", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "IJKL+UO: Move Light", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "PgUp/PgDn: Far Clip +/-", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, "Click square to toggle color", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, $"FPS: {(int)fps}", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                y += 20f;
                batch.Add(uiFont!, $"Mouse: {(MouseCaptured ? "CAPTURED" : "Free")}", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), MouseCaptured ? accent : white);
                y += 20f;
                batch.Add(uiFont!, $"Light: ({lightPos.X:F1}, {lightPos.Y:F1}, {lightPos.Z:F1})", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), accent);
                y += 20f;
                batch.Add(uiFont!, $"Near/Far: {Camera.Near:F2} / {Camera.Far:F0}", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), accent);
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
            // Esc now releases mouse capture or quits (handled by App base class)

            // Toggle overlay with F1
            if (Inputs.Keyboard.IsPressed(KeyCode.F1))
            {
                showOverlay = !showOverlay;
            }

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
                    Camera.Rotate(mouseDelta.X * mouseSensitivity, -mouseDelta.Y * mouseSensitivity);
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

            time += (float)delta.TotalSeconds;

            // FPS-style camera movement (WASD + QE for up/down)
            float deltaSpeed = moveSpeed * (float)delta.TotalSeconds;
            if (Inputs.Keyboard.IsDown(KeyCode.W)) Camera.MoveRelative(deltaSpeed, 0, 0);    // Forward
            if (Inputs.Keyboard.IsDown(KeyCode.S)) Camera.MoveRelative(-deltaSpeed, 0, 0);   // Backward
            if (Inputs.Keyboard.IsDown(KeyCode.A)) Camera.MoveRelative(0, -deltaSpeed, 0);   // Left
            if (Inputs.Keyboard.IsDown(KeyCode.D)) Camera.MoveRelative(0, deltaSpeed, 0);    // Right
            if (Inputs.Keyboard.IsDown(KeyCode.Q)) Camera.MoveRelative(0, 0, -deltaSpeed);   // Down
            if (Inputs.Keyboard.IsDown(KeyCode.E)) Camera.MoveRelative(0, 0, deltaSpeed);    // Up

            // IJKL controls for moving the light
            var lightSpeed = 2f * (float)delta.TotalSeconds;
            if (Inputs.Keyboard.IsDown(KeyCode.I)) lightPos.Z -= lightSpeed;
            if (Inputs.Keyboard.IsDown(KeyCode.K)) lightPos.Z += lightSpeed;
            if (Inputs.Keyboard.IsDown(KeyCode.J)) lightPos.X -= lightSpeed;
            if (Inputs.Keyboard.IsDown(KeyCode.L)) lightPos.X += lightSpeed;
            if (Inputs.Keyboard.IsDown(KeyCode.U)) lightPos.Y += lightSpeed;
            if (Inputs.Keyboard.IsDown(KeyCode.O)) lightPos.Y -= lightSpeed;

            // Update light position in renderer
            defaultRenderer?.SetPointLight(lightPos, new Vector3(1f, 1f, 0.95f), 0.15f);
            // Animate planet orbits; sun is at origin
            if (planets.Count > 0)
            {
                foreach (var p in planets)
                {
                    var pos = GetOrbitPosition(p, time);
                    if (p.Entity.IsValid())
                    {
                        p.Entity.Set(new Position3D(pos.X, pos.Y, pos.Z));
                    }
                }
            }
            // Rotation is now handled by RotationSystem3D (CorePlugin)

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
            /*
            if (Inputs.Mouse.RightButton.IsPressed && MouseToPlaneZ(out var wp, planeZ: 0f))
            {
                cubePos = wp;
            }
            */
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
            world.Update((float)delta.TotalSeconds); // Update with delta time
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
            try { sunMesh?.Dispose(); } catch { }
            try { sunTexture?.Dispose(); } catch { }
            foreach (var t in planetTextures) { try { t.Dispose(); } catch { } }
            base.Destroy();
        }

        private Texture? LoadTextureFromAssets(string relativePath)
        {
            // Read bytes via RootTitleStorage
            if (!RootTitleStorage.Exists(relativePath))
            {
                Logger.LogWarn($"Texture not found: {relativePath}");
                return null;
            }
            if (!RootTitleStorage.GetFileSize(relativePath, out var size)) return null;
            var bytes = new byte[size];
            RootTitleStorage.ReadFile(relativePath, bytes);

            // Decode with StbImageSharp into RGBA8
            using var ms = new MemoryStream(bytes);
            var image = ImageResult.FromStream(ms, ColorComponents.RedGreenBlueAlpha);
            if (image == null)
            {
                Logger.LogWarn($"Failed to decode image: {relativePath}");
                return null;
            }
            // Create MoonWorks texture and upload
            return defaultRenderer!.CreateTextureFromRgba8Pixels((uint)image.Width, (uint)image.Height, image.Data, srgb: true);
        }

        private Texture? TryLoadTextureAny(string basePathWithoutExt)
        {
            var candidates = new[] { ".jpg", ".png", ".jpeg", ".JPG", ".PNG" };
            foreach (var ext in candidates)
            {
                var tex = LoadTextureFromAssets(basePathWithoutExt + ext);
                if (tex != null) return tex;
            }
            return null;
        }

        private void AddPlanet(string name, string baseName, float radius, float orbitRadius, float orbitSpeed, float selfSpin, float phase, string? parent = null)
        {
            var tex = TryLoadTextureAny($"Assets/{baseName}");
            if (tex == null)
            {
                Logger.LogWarn($"Planet texture not found for {name} (Assets/{baseName}.[jpg|png]) — skipping.");
                return;
            }
            var mesh = Mesh.CreateSphere3DTexturedLit(GraphicsDevice, radius: radius, slices: 96, stacks: 48);
            var entity = world.CreateEntity()
                .Set(new Position3D(0, 0, 0))
                .Set(new Mesh3D { Mesh = mesh })
                .Set(new Texture2DRef { Texture = tex })
                .Set(new RenderTexturedLit3D())
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(new Vector3(0f, selfSpin, 0f)));

            planets.Add(new Planet
            {
                Name = name,
                Radius = radius,
                OrbitRadius = orbitRadius,
                AngularSpeed = orbitSpeed,
                SelfSpin = selfSpin,
                Phase = phase,
                Parent = parent,
                Mesh = mesh,
                Texture = tex,
                Entity = entity
            });
            planetTextures.Add(tex);
        }

        private Vector3 GetOrbitPosition(Planet p, float t)
        {
            var angle = p.Phase + t * p.AngularSpeed;
            var pos = new Vector3(MathF.Cos(angle) * p.OrbitRadius, 0f, MathF.Sin(angle) * p.OrbitRadius);
            if (!string.IsNullOrEmpty(p.Parent))
            {
                var parent = planets.Find(x => x.Name == p.Parent);
                if (parent != null)
                {
                    return GetOrbitPosition(parent, t) + pos;
                }
            }
            return pos;
        }
    }
}
