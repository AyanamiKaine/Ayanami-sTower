using System.Numerics;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using AyanamisTower.StellaEcs.StellaInvicta.Graphics;
using AyanamisTower.StellaEcs.StellaInvicta.Physics;
using AyanamisTower.StellaEcs.StellaInvicta.Components;
using BepuPhysics;
using BepuUtilities.Memory;
using BepuUtilities;
using BepuPhysics.Collidables;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using Mesh = AyanamisTower.StellaEcs.StellaInvicta.Graphics.Mesh;
using AyanamisTower.StellaEcs.StellaInvicta.Assets;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using static SDL3.SDL;
using SDL3;

namespace AyanamisTower.StellaEcs.StellaInvicta;


internal static class Program
{
    public static void Main()
    {
        var game = new StellaInvicta();
        game.Run();
    }

    private sealed class StellaInvicta : Game
    {
        private ImGuiIOPtr io;
        private bool _imguiEnabled = false;
        // BepuPhysics v2 Simulation
        private readonly PhysicsManager _physicsManager;
        private MousePicker _mousePicker = null!;
        // Debug: draw axes at entity centers
        private bool _debugDrawAxesAll = false;
        // Double-click detection state for pick-to-focus
        private double _lastClickTime = 0.0;
        private int _lastClickX = -9999;
        private int _lastClickY = -9999;

        // Deterministic fixed-step simulation accumulator
        // The simulation will always step in discrete, fixed-sized steps so
        // identical inputs produce identical results regardless of frame timing.
        private double _simulationAccumulator = 0.0;
        private const double _fixedSimulationStepSeconds = 1.0 / 60.0; // 60 Hz deterministic step
        private const int _maxSimulationStepsPerFrame = 16; // safety clamp to avoid spiral-of-death
        /// <summary>
        /// Represents the current game world.
        /// </summary>
        public readonly World World = new(10000000);
        // Camera
        private Camera _camera = null!;
        private SpaceStrategyCameraController _cameraController = null!;
        // When true, render everything relative to the camera (subtract camera position
        // when building model matrices / line vertices) instead of performing world rebasing.
        // This reduces the need to move world objects and avoids rebase-induced jitter.
        private bool _useCameraRelativeRendering = true;

        // Lines (debug/overlay) further than this distance from the camera will be skipped.
        // Tune this value for your scene scale. Units are world units (same as _camera.Position).
        private double _maxDebugLineDistance = 100000.0;

        // GPU resources
        private GraphicsPipeline? _pipeline;
        // rotation
        private float _angle;
        // textures
        private Texture? _whiteTexture;
        private Texture? _checkerTexture;

        private SampleCount _msaaSamples = SampleCount.Four;
        private Texture? _msaaColor; // The offscreen texture for MSAA rendering
        private Texture? _msaaDepth; // The offscreen depth buffer (MSAA)
                                     // Skybox resources
        private GraphicsPipeline? _skyboxPipeline;
        private GraphicsPipeline? _skyboxCubePipeline;
        private Texture? _skyboxTexture;
        private GpuMesh? _skyboxMesh;
        private float _skyboxScale = 50f;
        private bool _skyboxEnabled;
        // Lines
        private GraphicsPipeline? _linePipeline;
        private LineBatch3D? _lineBatch;
        // Debug: visualize physics colliders with wireframes
        private bool _debugDrawColliders = false;
        // If true, also draw ECS-declared collider poses (helps spot divergence). Off by default.
        private bool _debugDrawEcsColliderPoses = false;

        // Frustum culling controls
        // When true, perform frustum culling on GPU-mesh entities; can be toggled at runtime.
        private bool _enableFrustumCulling = true;
        // Scale applied to bounding sphere radii used for culling. Useful for tuning.
        private float _cullingRadiusScale = 1.0f;
        // Size-based visibility: objects with projected screen size above this threshold will be forced visible
        private bool _enableSizeVisibility = true;
        private float _minScreenPixelSize = 6.0f; // default minimum pixel radius to consider "visible"

        // Floating origin system
        private FloatingOriginManager? _floatingOriginManager;
        // Input manager to simplify checked inputs
        private InputManager _inputManager = new();
        // FPS counter for window title
        private readonly string _baseTitle = "Stella Invicta";
        private float _fpsTimer = 0f;
        private int _fpsFrames = 0;
        private double _fps = 0.0;
        // Track last draw time to measure actual render FPS (not update rate)
        private DateTime _lastDrawTime = DateTime.UtcNow;
        // Currently selected entity in the ImGui entities window
        private Entity _selectedEntity = default;
        public StellaInvicta() : base(
            new AppInfo("Ayanami", "Stella Invicta Demo"),
            new WindowCreateInfo("Stella Invicta", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(60, 360),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            _physicsManager = new PhysicsManager(World);
            InitializeScene();
            EnableImgui();
        }

        protected override unsafe void OnSdlEvent(SDL.SDL_Event evt)
        {
            // Only forward to ImGui when our program has enabled it.
            if (_imguiEnabled)
            {
                SDLEvent* pev = (SDLEvent*)&evt;                 // address-of local
                SDLEventPtr ptr = new(pev);
                ImGuiImplSDL3.ProcessEvent(ptr);
            }
            // Leave the rest of event handling to the base Game class (it will handle window events, etc.)
        }

        public unsafe void EnableImgui()
        {
            if (_imguiEnabled) return; // already enabled

            var ctx = ImGui.CreateContext();
            ImGui.SetCurrentContext(ctx);
            io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
                            | ImGuiConfigFlags.NavEnableGamepad
                            | ImGuiConfigFlags.DockingEnable;

            ImGui.StyleColorsDark();
            var style = ImGui.GetStyle();


            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }


            ImGuiImplSDL3.SetCurrentContext(ctx);
            ImGuiImplSDL3.InitForSDLGPU((SDLWindow*)MainWindow.Handle);

            ImGuiImplSDLGPU3InitInfo initInfo = new()
            {

                Device = (SDLGPUDevice*)GraphicsDevice.Handle,
                ColorTargetFormat = (int)SDL_GetGPUSwapchainTextureFormat(GraphicsDevice.Handle, MainWindow.Handle),
                // Imgui gpu info does not have a depth stencil field, so a render pipeline used for imgui cannot use one too, other wise you will get validation errors.
                // DepthStencilFormat = (int)GraphicsDevice.SupportedDepthStencilFormat,
                MSAASamples = (int)_msaaSamples
            };
            ImGuiImplSDL3.SDLGPU3Init(&initInfo);

            _imguiEnabled = true;

        }

        public void DisableImgui()
        {
            if (!_imguiEnabled) return;

            // Shutdown backend and SDLGPU integration, then destroy context
            ImGuiImplSDL3.Shutdown();
            ImGuiImplSDL3.SDLGPU3Shutdown();
            ImGui.DestroyContext();

            io = default;
            _imguiEnabled = false;
        }

        // Safely get a BodyReference; returns false if the handle is invalid (e.g., removed or not yet added)
        private bool TryGetBodyRef(BodyHandle handle, out BodyReference bodyRef)
        {
            try
            {
                bodyRef = _physicsManager.Simulation.Bodies.GetBodyReference(handle);
                return true;
            }
            catch
            {
                bodyRef = default;
                return false;
            }
        }

        private bool TryGetStaticRef(StaticHandle handle, out StaticReference staticRef)
        {
            try
            {
                staticRef = _physicsManager.Simulation.Statics[handle];
                return true;
            }
            catch
            {
                staticRef = default;
                return false;
            }
        }

        /// <summary>
        /// Helper: returns the best available world-space position for an entity.
        /// Prefers the physics pose (dynamic/kinematic/static) then falls back to Position3D.
        /// Returns Vector3.Zero if no position is available.
        /// </summary>
        private Vector3Double GetEntityWorldPosition(Entity e)
        {
            if (e.Has<Position3D>()) return e.GetCopy<Position3D>().Value;
            return Vector3Double.Zero;
        }

        // Render arbitrary objects into ImGui in a readable, expandable form.
        // Handles primitives, enums, strings, IEnumerable (lists/arrays), and nested objects via reflection.
        private void RenderObjectForImGui(object? obj, int depth = 0)
        {
            const int MAX_DEPTH = 5;
            if (obj == null)
            {
                ImGui.Text("(null)");
                return;
            }
            if (depth > MAX_DEPTH)
            {
                ImGui.Text("(max depth)");
                return;
            }

            var type = obj.GetType();

            // Simple scalars and enums
            if (type.IsPrimitive || obj is string || obj is decimal || type.IsEnum)
            {
                ImGui.Text(obj.ToString() ?? "(null)");
                return;
            }

            // IEnumerable (but not string)
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                if (ImGui.TreeNode($"{type.Name}"))
                {
                    int i = 0;
                    foreach (var item in enumerable)
                    {
                        ImGui.PushID(i);
                        RenderObjectForImGui(item, depth + 1);
                        ImGui.PopID();
                        i++;
                    }
                    ImGui.TreePop();
                }
                return;
            }

            // Complex object: show properties and fields
            if (ImGui.TreeNode(type.Name))
            {
                // Properties
                var props = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var p in props)
                {
                    try
                    {
                        // Skip indexers
                        if (p.GetIndexParameters().Length != 0) continue;
                        object? val = null;
                        try { val = p.GetValue(obj); } catch { val = "(ex)"; }
                        ImGui.PushID(p.Name);
                        if (val == null)
                        {
                            ImGui.Text($"{p.Name}: (null)");
                        }
                        else if (p.PropertyType.IsPrimitive || val is string || val is decimal || p.PropertyType.IsEnum)
                        {
                            ImGui.Text($"{p.Name}: {val}");
                        }
                        else
                        {
                            if (ImGui.TreeNode(p.Name))
                            {
                                RenderObjectForImGui(val, depth + 1);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.PopID();
                    }
                    catch { /* ignore property reflection issues */ }
                }

                // Fields
                var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var f in fields)
                {
                    try
                    {
                        object? val = null;
                        try { val = f.GetValue(obj); } catch { val = "(ex)"; }
                        ImGui.PushID(f.Name);
                        if (val == null)
                        {
                            ImGui.Text($"{f.Name}: (null)");
                        }
                        else if (f.FieldType.IsPrimitive || val is string || val is decimal || f.FieldType.IsEnum)
                        {
                            ImGui.Text($"{f.Name}: {val}");
                        }
                        else
                        {
                            if (ImGui.TreeNode(f.Name))
                            {
                                RenderObjectForImGui(val, depth + 1);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.PopID();
                    }
                    catch { /* ignore field reflection issues */ }
                }

                ImGui.TreePop();
            }
        }

        /// <summary>
        /// Draws RGB axes (X=red, Y=green, Z=blue) centered at the given world position.
        /// </summary>
        private void DrawEntityAxes(Vector3Double center, double length = 1.0f)
        {
            if (_lineBatch == null) return;

            var xEnd = center + new Vector3Double(length, 0, 0);
            var yEnd = center + new Vector3Double(0, length, 0);
            var zEnd = center + new Vector3Double(0, 0, length);

            // Red for X
            _lineBatch.AddLine(center, xEnd, new Color(255, 64, 64, 255));
            // Green for Y
            _lineBatch.AddLine(center, yEnd, new Color(64, 255, 64, 255));
            // Blue for Z
            _lineBatch.AddLine(center, zEnd, new Color(64, 64, 255, 255));
        }

        private int WorldEntityCount()
        {
            try
            {
                return World != null ? World.ActiveEntityCount : 0;
            }
            catch
            {
                return 0;
            }
        }

        // Renders the custom in-game ImGui debug window
        private void RenderImguiDebugWindow()
        {
            ImGui.Begin("StellaInvicta Debug");
            if (_camera != null)
            {
                ImGui.Text($"Camera Position: {_camera.Position.X:F2}, {_camera.Position.Y:F2}, {_camera.Position.Z:F2}");
            }
            // Mouse position from the input system
            try
            {
                ImGui.Text($"Mouse: {Inputs.Mouse.X}, {Inputs.Mouse.Y}");
            }
            catch { ImGui.Text("Mouse: N/A"); }

            // Window size
            try
            {
                ImGui.Text($"Window: {MainWindow.Width} x {MainWindow.Height}");
            }
            catch { ImGui.Text("Window: N/A"); }
            ImGui.Text($"Entities: {WorldEntityCount()}");
            ImGui.Separator();
            ImGui.Checkbox("Debug Draw Colliders", ref _debugDrawColliders);
            ImGui.Checkbox("Debug Draw Axes (All)", ref _debugDrawAxesAll);
            ImGui.Checkbox("Frustum Culling", ref _enableFrustumCulling);
            ImGui.SliderFloat("Culling Radius Scale", ref _cullingRadiusScale, 0.1f, 10f);
            ImGui.End();
        }

        // Grouped call for all ImGui windows we want to show
        private void RenderImguiWindows()
        {
            RenderImguiDebugWindow();
            RenderImguiEntitiesWindow();
        }

        private void RenderImguiEntitiesWindow()
        {
            ImGui.Begin("Entities");
            ImGui.Text($"Total Entities: {WorldEntityCount()}");
            ImGui.Separator();

            // Left: list of entities
            ImGui.BeginChild(ImGui.GetID("entity_list"), new System.Numerics.Vector2(320, 400), ImGuiChildFlags.None);
            foreach (var e in World.GetAllEntities())
            {
                string label = $"E{e.Id} (Gen {e.Generation})";
                bool isSelected = !_selectedEntity.Equals(default) && _selectedEntity.Equals(e);
                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedEntity = e;
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            // Right: details for selected entity
            ImGui.BeginGroup();
            if (_selectedEntity != default && _selectedEntity != Entity.Null)
            {
                ImGui.Text($"Selected: E{_selectedEntity.Id} (Gen {_selectedEntity.Generation})");

                if (_selectedEntity.Has<Position3D>())
                {
                    var pos = _selectedEntity.GetCopy<Position3D>().Value;
                    ImGui.Text($"Position: {pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}");
                }
                else
                {
                    ImGui.Text("Position: (no Position3D)");
                }

                if (ImGui.Button("Jump to position"))
                {
                    var worldPos = GetEntityWorldPosition(_selectedEntity);
                    // Compute a reasonable distance based on Size3D when available
                    double distance = _cameraController.MinDistance;
                    if (_selectedEntity.Has<Size3D>())
                    {
                        var s = _selectedEntity.GetMut<Size3D>().Value;
                        double maxAxis = Math.Max(s.X, Math.Max(s.Y, s.Z));
                        double boundingRadius = Math.Max(0.01, maxAxis * 0.6);
                        double fovHalfTan = Math.Tan(_camera.Fov * 0.5f);
                        if (fovHalfTan > 1e-6f)
                        {
                            distance = Math.Max(boundingRadius / (0.35 * fovHalfTan), _cameraController.MinDistance);
                        }
                        distance = Math.Clamp(distance, _cameraController.MinDistance, _cameraController.MaxDistance);
                    }
                    _cameraController.SetFocus(worldPos, distance);
                }

                ImGui.Separator();
                ImGui.Text("Components:");
                ImGui.BeginChild(ImGui.GetID("entity_components"), new System.Numerics.Vector2(0, 200), ImGuiChildFlags.None);
                foreach (var (type, data) in World.GetComponentsForEntityAsObjects(_selectedEntity))
                {
                    ImGui.PushID(type.FullName);
                    if (ImGui.TreeNode(type.Name))
                    {
                        if (data != null)
                        {
                            // Use the recursive ImGui renderer for better visibility of fields
                            RenderObjectForImGui(data);
                        }
                        else
                        {
                            ImGui.Text("(null)");
                        }
                        ImGui.TreePop();
                    }
                    ImGui.PopID();
                }
                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("No entity selected");
            }

            ImGui.EndGroup();
            ImGui.End();
        }

        public void EnableMSAA(SampleCount sampleCount)
        {


        }

        public void DisableMSAA()
        {

        }

        public void EnableVSync()
        {
            GraphicsDevice.SetSwapchainParameters(MainWindow, MainWindow.SwapchainComposition, PresentMode.VSync);

            // Can improve the frame latency
            // GraphicsDevice.SetAllowedFramesInFlight(1);
        }

        public void DisableVSync()
        {
            GraphicsDevice.SetSwapchainParameters(MainWindow, MainWindow.SwapchainComposition, PresentMode.Immediate);
        }

        private void InitializeScene()
        {
            var pluginLoader = new HotReloadablePluginLoader(World, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();

            World.EnableRestApi();

            //EnableVSync();

            // Initialize floating origin system
            _floatingOriginManager = new FloatingOriginManager(World, _physicsManager.Simulation, 200.0);

            // Camera setup
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _camera = new Camera(new Vector3(0, 2, 6), Vector3.Zero, Vector3.UnitY)
            {
                Aspect = aspect,
                Near = 0.1f,
                Far = 100f,
                Fov = MathF.PI / 3f
            };
            _cameraController = new SpaceStrategyCameraController(_camera);

            _mousePicker = new MousePicker(_camera, _physicsManager.Simulation)
            {
                UseCameraRelativeRendering = _useCameraRelativeRendering
                ,
                FloatingOriginManager = _floatingOriginManager
            };

            // Register input handlers with InputManager so adding new inputs is easy
            _inputManager.RegisterKeyPressed(KeyCode.F1, () =>
            {
                _debugDrawAxesAll = !_debugDrawAxesAll;
                Console.WriteLine($"[Debug] Draw axes for all entities: {_debugDrawAxesAll}");
            }, "ToggleAxesAll");

            _inputManager.RegisterKeyPressed(KeyCode.F2, () =>
            {
                _debugDrawColliders = !_debugDrawColliders;
                Console.WriteLine($"[Debug] Draw colliders for all entities: {_debugDrawColliders}");
            }, "ToggleColliders");

            // Mouse pick (edge on left mouse)
            _inputManager.RegisterLeftMousePressed(() =>
            {
                if (_mousePicker.Pick(Inputs.Mouse, (int)MainWindow.Width, (int)MainWindow.Height, out var result))
                {
                    // Keep the existing pick/double-click logic but readonly capture necessary variables
                    double now = DateTime.UtcNow.TimeOfDay.TotalSeconds;
                    bool isDoubleClick = false;
                    const double DOUBLE_CLICK_TIME = 0.35; // seconds
                    const int DOUBLE_CLICK_PIXEL_RADIUS = 6;
                    if (now - _lastClickTime <= DOUBLE_CLICK_TIME)
                    {
                        int dx = Inputs.Mouse.X - _lastClickX;
                        int dy = Inputs.Mouse.Y - _lastClickY;
                        if ((dx * dx) + (dy * dy) <= DOUBLE_CLICK_PIXEL_RADIUS * DOUBLE_CLICK_PIXEL_RADIUS)
                        {
                            isDoubleClick = true;
                        }
                    }
                    _lastClickTime = now;
                    _lastClickX = Inputs.Mouse.X;
                    _lastClickY = Inputs.Mouse.Y;

                    if (isDoubleClick)
                    {
                        // inline ComputeDesiredDistanceForEntity
                        double ComputeDesiredDistanceForEntity(Entity? entity, float hitDistance)
                        {
                            const float targetScreenFraction = 0.35f;
                            double desired = hitDistance * 0.6f;
                            if (entity.HasValue && entity.Value != default && entity.Value.Has<Size3D>())
                            {
                                var s = entity.Value.GetMut<Size3D>().Value;
                                double maxAxis = Math.Max(s.X, Math.Max(s.Y, s.Z));
                                double boundingRadius = Math.Max(0.01f, maxAxis * 0.6f);
                                double fovHalfTan = Math.Tan(_camera.Fov * 0.5f);
                                if (fovHalfTan > 1e-6f)
                                {
                                    double fromSize = boundingRadius / (targetScreenFraction * fovHalfTan);
                                    desired = Math.Max(fromSize, _cameraController.MinDistance);
                                }
                            }
                            desired = Math.Clamp(desired, _cameraController.MinDistance, _cameraController.MaxDistance);
                            return desired;
                        }

                        if (result.Collidable.Mobility == CollidableMobility.Static)
                        {
                            if (TryGetStaticRef(result.Collidable.StaticHandle, out var staticBody))
                            {
                                Entity? owner = null;
                                foreach (var e in World.Query(typeof(PhysicsStatic)))
                                {
                                    if (e.GetMut<PhysicsStatic>().Handle.Equals(result.Collidable.StaticHandle))
                                    {
                                        owner = e;
                                        break;
                                    }
                                }
                                double distance = ComputeDesiredDistanceForEntity(owner, result.Distance);
                                _cameraController.SetFocus(staticBody.Pose.Position, distance);
                                Console.WriteLine($"Double-click focus STATIC at {staticBody.Pose.Position} (distance={distance:F2})");
                            }
                            else
                            {
                                double distance = ComputeDesiredDistanceForEntity(null, result.Distance);
                                _cameraController.SetFocus(result.HitLocation, distance);
                                Console.WriteLine($"Double-click focus at hit location {result.HitLocation} (distance={distance:F2})");
                            }
                        }
                        else
                        {
                            var bh = result.Collidable.BodyHandle;
                            Entity? found = null;
                            foreach (var e in World.Query(typeof(PhysicsBody)))
                            {
                                if (e.GetMut<PhysicsBody>().Handle.Equals(bh))
                                {
                                    found = e;
                                    break;
                                }
                            }

                            if (found != null)
                            {
                                var followEntity = found.Value;
                                double distance = ComputeDesiredDistanceForEntity(followEntity, result.Distance);
                                _cameraController.SetFocusProvider(() => GetEntityWorldPosition(followEntity), distance);
                                Console.WriteLine($"Double-click follow ENTITY {followEntity.Id} (distance={distance:F2})");
                            }
                            else
                            {
                                double distance = ComputeDesiredDistanceForEntity(null, result.Distance);
                                _cameraController.SetFocus(result.HitLocation, distance);
                                Console.WriteLine($"Double-click focus at hit location {result.HitLocation} (distance={distance:F2})");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("PICK: Missed. No object was hit.");
                }
            }, "MousePick");

            // Culling toggles and adjustments
            _inputManager.RegisterKeyPressed(KeyCode.C, () =>
            {
                _enableFrustumCulling = !_enableFrustumCulling;
                Console.WriteLine($"[Culling] Frustum culling: {(_enableFrustumCulling ? "ENABLED" : "DISABLED")}");
            }, "ToggleCulling");

            _inputManager.RegisterKeyPressed(KeyCode.V, () =>
            {
                _enableSizeVisibility = !_enableSizeVisibility;
                Console.WriteLine($"[Culling] Size-based visibility: {(_enableSizeVisibility ? "ENABLED" : "DISABLED")}");
            }, "ToggleSizeVisibility");

            _inputManager.RegisterKeyHeld(KeyCode.N, () =>
            {
                _cullingRadiusScale = MathF.Max(0.1f, _cullingRadiusScale - 0.1f);
                Console.WriteLine($"[Culling] Radius scale: {_cullingRadiusScale:F2}");
            }, "DecreaseCullingRadius");

            _inputManager.RegisterKeyHeld(KeyCode.M, () =>
            {
                _cullingRadiusScale = MathF.Min(10f, _cullingRadiusScale + 0.1f);
                Console.WriteLine($"[Culling] Radius scale: {_cullingRadiusScale:F2}");
            }, "IncreaseCullingRadius");

            _inputManager.RegisterKeyHeld(KeyCode.LeftBracket, () =>
            {
                _minScreenPixelSize = MathF.Max(0.5f, _minScreenPixelSize - 0.5f);
                Console.WriteLine($"[Culling] Min pixel size: {_minScreenPixelSize:F1}");
            }, "DecreaseMinPixelSize");

            _inputManager.RegisterKeyHeld(KeyCode.RightBracket, () =>
            {
                _minScreenPixelSize = MathF.Min(200f, _minScreenPixelSize + 0.5f);
                Console.WriteLine($"[Culling] Min pixel size: {_minScreenPixelSize:F1}");
            }, "IncreaseMinPixelSize");

            // MSAA targets are (re)created in Draw to match the actual swapchain size.
            _msaaColor = null;
            _msaaDepth = null;

            // Compile shaders from HLSL source via ShaderCross
            ShaderCross.Initialize();

            var vs = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/Basic3DObjectRenderer.hlsl",
                "VSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Vertex,
                false,
                "Basic3DObjectRendererVS"
            );

            // See https://github.com/libsdl-org/SDL/issues/12085, when enabling
            // debug shader information and why a validation error will be thrown
            var fs = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/Basic3DObjectRenderer.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "Basic3DObjectRendererPS"
            );
            var vsSkyCube = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/SkyboxCube.hlsl",
                "VSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Vertex,
                false,
                "SkyboxCubeVS"
            );
            var fsSkyCube = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/SkyboxCube.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "SkyboxCubePS"
            );
            // Create simple textures
            _whiteTexture = AssetManager.CreateSolidTexture(this, 255, 255, 255, 255);
            _checkerTexture = AssetManager.CreateCheckerboardTexture(this, 256, 256, 8,
                (230, 230, 230, 255), (40, 40, 40, 255));



            var vertexInput = VertexInputState.CreateSingleBinding<Vertex>(0);

            // Pipeline (no depth for minimal sample -> use depth if needed)
            _pipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vs,
                FragmentShader = fs,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = new RasterizerState { CullMode = CullMode.Back, FrontFace = FrontFace.CounterClockwise },
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = true,
                    EnableDepthWrite = true,
                    CompareOp = CompareOp.LessOrEqual,
                    CompareMask = 0xFF,
                    WriteMask = 0xFF,
                    EnableStencilTest = false
                },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            // Must match the swapchain format for the active window
                            Format = MainWindow.SwapchainFormat,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "Basic3DObjectRenderer"
            });

            // Dedicated skybox pipeline: no depth write/test, no culling (render inside of sphere)
            _skyboxPipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vs,
                FragmentShader = fs,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    CompareOp = CompareOp.Always,
                    CompareMask = 0xFF,
                    WriteMask = 0x00,
                    EnableStencilTest = false
                },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            Format = MainWindow.SwapchainFormat,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "SkyboxRenderer"
            });

            _skyboxCubePipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vsSkyCube,
                FragmentShader = fsSkyCube,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    CompareOp = CompareOp.Always,
                    CompareMask = 0xFF,
                    WriteMask = 0x00,
                    EnableStencilTest = false
                },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            Format = MainWindow.SwapchainFormat,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "SkyboxCubeRenderer"
            });

            // Line pipeline setup (position+color, line list)
            var vsLine = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/LineColor.hlsl",
                "VSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Vertex,
                false,
                "LineVS"
            );
            var fsLine = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                AssetManager.AssetFolderName + "/LineColor.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "LinePS"
            );
            var lineVertexInput = VertexInputState.CreateSingleBinding<LineBatch3D.LineVertex>(0);
            _linePipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vsLine,
                FragmentShader = fsLine,
                VertexInputState = lineVertexInput,
                PrimitiveType = PrimitiveType.LineList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = true,
                    EnableDepthWrite = false, // lines typically don't write depth
                    CompareOp = CompareOp.LessOrEqual,
                    CompareMask = 0xFF,
                    WriteMask = 0x00,
                    EnableStencilTest = false
                },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            Format = MainWindow.SwapchainFormat,
                            // Use standard non-premultiplied alpha blending for line transparency
                            BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "LineColorRenderer"
            });

            _lineBatch = new LineBatch3D(GraphicsDevice, 409600);

            World.OnSetPost((Entity entity, in Mesh _, in Mesh mesh, bool _) =>
            {
                entity.Set(GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "Cube"));
            });

            World.OnSetPost((Entity entity, in CollisionShape _, in CollisionShape collisionShape, bool _) =>
            {
                TypedIndex? shapeIndex = null; // Default to invalid index
                // Add concrete shape type; Shapes.Add<T>() requires an unmanaged struct, not the IShape interface.
                switch (collisionShape.Shape)
                {
                    case Sphere sphere:
                        shapeIndex = _physicsManager.Simulation.Shapes.Add(sphere);
                        break;
                    case Box box:
                        shapeIndex = _physicsManager.Simulation.Shapes.Add(box);
                        break;
                    case Capsule capsule:
                        shapeIndex = _physicsManager.Simulation.Shapes.Add(capsule);
                        break;
                    case Cylinder cylinder:
                        shapeIndex = _physicsManager.Simulation.Shapes.Add(cylinder);
                        break;
                    default:
                        Console.WriteLine($"[Physics] Unsupported collision shape type: {collisionShape.Shape?.GetType().Name ?? "null"}");
                        return;
                }

                // Pull initial transform
                var pos = entity.Has<Position3D>() ? entity.GetMut<Position3D>().Value : Vector3Double.Zero;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;

                if (entity.Has<Kinematic>())
                {
                    // Create a kinematic body
                    var pose = new RigidPose(pos, rot);
                    var collidable = new CollidableDescription((TypedIndex)shapeIndex!, 0.1f);
                    var activity = new BodyActivityDescription(0.01f);
                    var bodyDesc = BodyDescription.CreateKinematic(pose, collidable, activity);
                    var bodyHandle = _physicsManager.Simulation.Bodies.Add(bodyDesc);
                    entity.Set(new PhysicsBody { Handle = bodyHandle });
                }
                else
                {
                    // Create a static collider
                    var staticDescription = new StaticDescription(HighPrecisionConversions.ToVector3(pos), (TypedIndex)shapeIndex!);
                    var staticHandle = _physicsManager.Simulation.Statics.Add(staticDescription);
                    entity.Set(new PhysicsStatic { Handle = staticHandle });
                }
            });

            // Create the default star system at world origin (extracted to a helper to allow multiple spawns)
            CreateStarSystem(new Vector3(0f, 0f, 0f), 80.0f);

            SpawnGalaxies(25);

            // Example usage:
            // SetSkybox("AssetManager.AssetFolderName + "/skybox.jpg", 50f);
            // Or, for cubemap:
            SetSkyboxCube([
                AssetManager.AssetFolderName + "/Sky/px.png",
                AssetManager.AssetFolderName + "/Sky/nx.png",
                AssetManager.AssetFolderName + "/Sky/py.png",
                AssetManager.AssetFolderName + "/Sky/ny.png",
                AssetManager.AssetFolderName + "/Sky/pz.png",
                AssetManager.AssetFolderName + "/Sky/nz.png"
            ]);
        }

        // Creates an XZ-plane grid centered at origin by spawning Line3D + Color entities.
        // halfLines: number of lines to each side of origin (total = halfLines*2 + 1 per axis)
        // step: spacing between neighboring lines
        // y: elevation of the grid
        // color: line color
        private void SpawnGridXZ(int halfLines, float step, float y, Color color)
        {
            if (halfLines <= 0 || step <= 0f) { return; }

            float extent = halfLines * step;
            for (int i = -halfLines; i <= halfLines; i++)
            {
                float x = i * step;
                // Lines parallel to Z (varying X)
                World.CreateEntity()
                    .Set(new Line3D(new Vector3Double(x, y, -extent), new Vector3Double(x, y, extent)))
                    .Set(color);

                float z = i * step;
                // Lines parallel to X (varying Z)
                World.CreateEntity()
                    .Set(new Line3D(new Vector3Double(-extent, y, z), new Vector3Double(extent, y, z)))
                    .Set(color);
            }
        }

        /// <summary>
        /// Creates a large textured sphere centered on the camera and draws it as a skybox.
        /// Pass a 2D panoramic/equirectangular image (JPG/PNG) or DDS. Call once to set/replace.
        /// </summary>
        private void SetSkybox(string path, float scale = 50f)
        {
            var tex = AssetManager.LoadTextureFromFile(this, path);
            if (tex == null)
            {
                Console.WriteLine($"[SetSkybox] Failed to load skybox texture: {path}");
                _skyboxEnabled = false;
                return;
            }

            _skyboxTexture?.Dispose();
            _skyboxTexture = tex;
            _skyboxScale = scale;

            if (_skyboxMesh == null)
            {
                var mesh = Mesh.CreateSphere3D();
                _skyboxMesh = GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "SkyboxSphere");
            }

            _skyboxEnabled = true;
        }


        /// <summary>
        /// Sets a cubemap skybox using 6 face file paths in the order:
        /// +X, -X, +Y, -Y, +Z, -Z. Builds a cube mesh if needed and binds a cubemap pipeline.
        /// </summary>
        private void SetSkyboxCube(string[] facePaths, float scale = 50f)
        {
            if (facePaths == null || facePaths.Length != 6)
            {
                Console.WriteLine("[SetSkyboxCube] Provide exactly 6 file paths: +X,-X,+Y,-Y,+Z,-Z");
                _skyboxEnabled = false;
                return;
            }

            var cubeTex = AssetManager.CreateCubemapFromSixFiles(this, facePaths[0], facePaths[1], facePaths[2], facePaths[3], facePaths[4], facePaths[5]);
            if (cubeTex == null)
            {
                Console.WriteLine("[SetSkyboxCube] Failed to create cubemap");
                _skyboxEnabled = false;
                return;
            }

            _skyboxTexture?.Dispose();
            _skyboxTexture = cubeTex;
            _skyboxScale = scale;

            // Use a cube mesh so positions map to cube directions cleanly
            if (_skyboxMesh == null)
            {
                var cube = Mesh.CreateBox3D(1, 1, 1);
                _skyboxMesh = GpuMesh.Upload(GraphicsDevice, cube.Vertices.AsSpan(), cube.Indices.AsSpan(), "SkyboxCubeMesh");
            }

            _skyboxEnabled = true;
        }




        /// <summary>
        /// Spawns a large number of planet-like entities for stress testing.
        /// Entities are distributed on random circular orbits on the XZ plane with slight Y jitter.
        /// Optionally attaches static sphere colliders so they can be ray-picked.
        ///
        /// Creates a star system (sun + planets + moon + station + asteroid belt) centered at the given origin.
        /// Returns the created sun entity.
        /// </summary>
        private Entity CreateStarSystem(Vector3 origin, float auScale = 80.0f)
        {
            // Local helper to convert AU offsets to world positions relative to origin
            Vector3 AuToWorld(float auX, float auY, float auZ) => origin + new Vector3(auX * auScale, auY * auScale, auZ * auScale);

            // --- CELESTIAL BODY CREATION ---

            var sun = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(origin.X, origin.Y, origin.Z))
                .Set(new Size3D(10.0f)) // Artistically scaled size
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.001f, 0f)) // Slow rotation for effect
                .Set(new CollisionShape(new Sphere(10.0f * 0.6f)))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            // Mercury: 0.39 AU from the Sun.
            var mercury = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(0.39f, 0, 0).X, AuToWorld(0.39f, 0, 0).Y, AuToWorld(0.39f, 0, 0).Z)) // distance = 0.39 AU
                .Set(new Size3D(0.38f)) // size = 0.38x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.38f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.048f, 0f)) // speed relative to Earth (fastest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            mercury.Set(new OrbitCircle(sun, 0.39f * auScale, new Color(200, 200, 200, 96), segments: 128));

            // Venus: 0.72 AU from the Sun.
            var venus = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(0.72f, 0, 0).X, AuToWorld(0.72f, 0, 0).Y, AuToWorld(0.72f, 0, 0).Z))
                .Set(new Size3D(0.95f)) // size = 0.95x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.95f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.016f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            venus.Set(new OrbitCircle(sun, 0.72f * auScale, new Color(200, 200, 200, 96), segments: 128));

            // Earth: 1.0 AU from the Sun (our baseline).
            var earth = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(1.0f, 0, 0).X, AuToWorld(1.0f, 0, 0).Y, AuToWorld(1.0f, 0, 0).Z))
                .Set(new Size3D(1.0f)) // size = 1.0x (baseline)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(1.0f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed = baseline
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            earth.Set(new OrbitCircle(sun, 1.0f * auScale, new Color(180, 220, 255, 96), segments: 128));

            // The Moon: Positioned relative to Earth.
            var earthPos = earth.GetCopy<Position3D>().Value;
            var moonLocalOffset = new Vector3Double(2, 0, 0); // distance from Earth in world units
            var moonWorldPos = earthPos + moonLocalOffset;

            var moonEntity = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(moonWorldPos.X, moonWorldPos.Y, moonWorldPos.Z))
                .Set(new LocalPosition3D(moonLocalOffset.X, moonLocalOffset.Y, moonLocalOffset.Z))
                .Set(new Size3D(0.27f)) // size = 0.27x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.27f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.012f, 0f)) // Orbits Earth faster
                .Set(new Parent(earth))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            // Space station
            var spaceStationLocalOffset = new Vector3Double(1f, 0f, 0f); // distance from Earth in world units
            var spaceStationWorldPos = earthPos + spaceStationLocalOffset;

            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(spaceStationWorldPos.X, spaceStationWorldPos.Y, spaceStationWorldPos.Z))
                .Set(new LocalPosition3D(spaceStationLocalOffset.X, spaceStationLocalOffset.Y, spaceStationLocalOffset.Z))
                .Set(new Size3D(0.01f)) // size = 0.01x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.01f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // Orbits Earth faster
                .Set(new Parent(earth))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            // Moon orbit indicator
            if (moonEntity != default)
            {
                moonEntity.Set(new OrbitCircle(earth, moonLocalOffset.Length(), new Color(180, 180, 200, 120), segments: 64));
            }

            // Mars: 1.52 AU from the Sun.
            var mars = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(1.52f, 0, 0).X, AuToWorld(1.52f, 0, 0).Y, AuToWorld(1.52f, 0, 0).Z)) // distance = 1.52 AU
                .Set(new Size3D(0.53f)) // size = 0.53x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.53f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            mars.Set(new OrbitCircle(sun, 1.52f * auScale, new Color(220, 160, 120, 96), segments: 128));

            // Jupiter: 5.20 AU from the Sun.
            var jupiter = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(5.20f, 0, 0).X, AuToWorld(5.20f, 0, 0).Y, AuToWorld(5.20f, 0, 0).Z)) // distance = 5.20 AU
                .Set(new Size3D(4.5f)) // size = 11.2x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(4.5f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            jupiter.Set(new OrbitCircle(sun, 5.20f * auScale, new Color(240, 200, 160, 96), segments: 160));

            // Saturn: 9.58 AU from the Sun.
            var saturn = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(9.58f, 0, 0).X, AuToWorld(9.58f, 0, 0).Y, AuToWorld(9.58f, 0, 0).Z)) // distance = 9.58 AU
                .Set(new Size3D(4.0f)) // size = 9.45x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(4.0f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            saturn.Set(new OrbitCircle(sun, 9.58f * auScale, new Color(240, 220, 200, 96), segments: 160));

            // Uranus: 19.22 AU from the Sun.
            var uranus = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(19.22f, 0, 0).X, AuToWorld(19.22f, 0, 0).Y, AuToWorld(19.22f, 0, 0).Z)) // distance = 19.22 AU
                .Set(new Size3D(2.5f)) // size = 4.0x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(2.5f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            uranus.Set(new OrbitCircle(sun, 19.22f * auScale, new Color(200, 220, 240, 96), segments: 160));

            // Neptune: 30.05 AU from the Sun. (Added for completeness)
            var neptune = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(30.05f, 0, 0).X, AuToWorld(30.05f, 0, 0).Y, AuToWorld(30.05f, 0, 0).Z)) // distance = 30.05 AU
                .Set(new Size3D(2.4f)) // size = 3.88x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(2.4f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            neptune.Set(new OrbitCircle(sun, 30.05f * auScale, new Color(180, 200, 240, 96), segments: 160));

            // Pluto: 39.48 AU from the Sun (average).
            var pluto = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(39.48f, 0, 0).X, AuToWorld(39.48f, 0, 0).Y, AuToWorld(39.48f, 0, 0).Z)) // distance = 39.48 AU
                .Set(new Size3D(0.18f)) // size = 0.18x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.18f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed relative to Earth (slowest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            pluto.Set(new OrbitCircle(sun, 39.48f * auScale, new Color(200, 180, 200, 96), segments: 160));

            // Classic main-belt style asteroid belt between Mars and Jupiter (~2.2 - 3.2 AU)
            SpawnAsteroidBelt(origin,
                parent: sun,
                count: 5,
                innerRadius: 2.2f * auScale,
                outerRadius: 3.2f * auScale,
                minSize: 0.02f,
                maxSize: 0.05f,
                inclinationDegrees: 1.5f,
                seed: 8675309,
                addStaticCollider: false);

            return sun;
        }
        /// <summary>
        /// Spawns a large number of planet-like entities for stress testing.
        /// Entities are distributed on random circular orbits on the XZ plane with slight Y jitter.
        /// Optionally attaches static sphere colliders so they can be ray-picked.
        /// </summary>
        /// <param name="count">How many entities to spawn.</param>
        /// <param name="minOrbitRadius">Minimum orbit radius from origin.</param>
        /// <param name="maxOrbitRadius">Maximum orbit radius from origin.</param>
        /// <param name="minScale">Minimum uniform scale (planet radius equivalent).</param>
        /// <param name="maxScale">Maximum uniform scale.</param>
        /// <param name="seed">Random seed.</param>
        /// <param name="addStaticCollider">If true, creates Bepu static sphere colliders for each spawned entity.</param>
        private void SpawnPlanetStressTest(
            int count,
            float minOrbitRadius = 10f,
            float maxOrbitRadius = 300f,
            float minScale = 0.1f,
            float maxScale = 1.5f,
            int seed = 1337,
            bool addStaticCollider = false)
        {
            if (count <= 0) { return; }
            if (minOrbitRadius < 0f) minOrbitRadius = 0f;
            if (maxOrbitRadius < minOrbitRadius) maxOrbitRadius = minOrbitRadius + 1f;
            if (minScale <= 0f) minScale = 0.05f;
            if (maxScale < minScale) maxScale = minScale;

            var rng = new Random(seed);

            // Ensure we have at least a fallback texture
            var tex = _checkerTexture ?? _whiteTexture ?? AssetManager.CreateSolidTexture(this, 255, 255, 255, 255);

            // Pre-create a GPU mesh for spheres to avoid duplicating per-entity CPU meshes
            var sharedSphere = Mesh.CreateSphere3D();

            for (int i = 0; i < count; i++)
            {
                // Random orbit radius and angle
                float r = minOrbitRadius + ((float)rng.NextDouble() * (maxOrbitRadius - minOrbitRadius));
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float yJitter = ((float)rng.NextDouble() - 0.5f) * 2f; // [-1,1]

                var pos = new Vector3(MathF.Cos(angle) * r, yJitter, MathF.Sin(angle) * r);
                float s = minScale + ((float)rng.NextDouble() * (maxScale - minScale));

                // Small random angular velocity
                var angVel = new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * 0.02f,
                    ((float)rng.NextDouble() - 0.5f) * 0.02f,
                    ((float)rng.NextDouble() - 0.5f) * 0.02f
                );

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere) // use the same CPU-side mesh data
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(angVel))
                    .Set(new Texture2DRef { Texture = tex });

                // Optional: physics collider so raycast picks them.
                if (addStaticCollider)
                {
                    var sphereShape = new Sphere(s * 0.5f);
                    var shapeIndex = _physicsManager.Simulation.Shapes.Add(sphereShape);
                    var staticDesc = new StaticDescription(pos, shapeIndex);
                    _physicsManager.Simulation.Statics.Add(staticDesc);
                }
            }
        }

        /// <summary>
        /// Spawns an asteroid field centered at `center` using simple sphere meshes and optional static colliders.
        /// </summary>
        /// <param name="center">World-space center of the asteroid field.</param>
        /// <param name="count">Number of asteroids to spawn.</param>
        /// <param name="fieldRadius">Radius of the spherical region to distribute asteroids in.</param>
        /// <param name="minSize">Minimum asteroid uniform scale.</param>
        /// <param name="maxSize">Maximum asteroid uniform scale.</param>
        /// <param name="seed">Random seed for reproducible fields.</param>
        /// <param name="addStaticCollider">If true, adds a static sphere collider for each asteroid.</param>
        /// <param name="parent">The parent entity to attach the asteroids to.</param>
        /// <param name="planeThickness">Thickness of the asteroid field plane.</param>
        private void SpawnAsteroidField(Vector3 center, Entity parent, int count = 200, float fieldRadius = 100f, float minSize = 0.2f, float maxSize = 2.0f, float planeThickness = 0.5f, int seed = 424242, bool addStaticCollider = false)
        {
            if (count <= 0) return;
            var rng = new Random(seed);

            var tex = _checkerTexture ?? _whiteTexture ?? AssetManager.CreateSolidTexture(this, 255, 255, 255, 255);

            // Shared CPU mesh for all asteroids
            var sharedSphere = Mesh.CreateSphere3D();

            for (int i = 0; i < count; i++)
            {
                // Disk/cylindrical distribution on XZ plane (uniform within disk).
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                // use sqrt(U) to produce uniform distribution over disk area
                float r = (float)Math.Sqrt(rng.NextDouble()) * fieldRadius;
                var x = center.X + (MathF.Cos(angle) * r);
                var z = center.Z + (MathF.Sin(angle) * r);
                // Small Y jitter around center.Y so asteroids lie close to the orbital plane
                float y = center.Y + (((float)rng.NextDouble() - 0.5f) * planeThickness);

                var pos = new Vector3(x, y, z);

                float s = minSize + ((float)rng.NextDouble() * (maxSize - minSize));

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere)
                    .Set(new Kinematic())
                    .Set(new Parent(parent))
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(new CollisionShape(new Sphere(s)))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(0, 0.1f, 0))
                    .Set(new Texture2DRef { Texture = tex });

                if (addStaticCollider)
                {
                    var sphereShape = new Sphere(s * 0.5f);
                    var shapeIndex = _physicsManager.Simulation.Shapes.Add(sphereShape);
                    var staticDesc = new StaticDescription(pos, shapeIndex);
                    _physicsManager.Simulation.Statics.Add(staticDesc);
                }
            }
        }

        /// <summary>
        /// Spawns an annular asteroid belt around a given center. Distributes asteroids between innerRadius and outerRadius
        /// on the XZ plane with small random inclinations to give a torus-like appearance.
        /// </summary>
        /// <param name="center">Center of the belt (usually the star position)</param>
        /// <param name="parent">Parent entity (usually the star entity)</param>
        /// <param name="count">Total asteroids to spawn</param>
        /// <param name="innerRadius">Inner radius of belt</param>
        /// <param name="outerRadius">Outer radius of belt</param>
        /// <param name="minSize">Minimum asteroid size</param>
        /// <param name="maxSize">Maximum asteroid size</param>
        /// <param name="inclinationDegrees">Max inclination in degrees applied as small tilt from XZ plane</param>
        /// <param name="seed">Random seed</param>
        /// <param name="addStaticCollider">If true, adds static colliders for picking</param>
        private void SpawnAsteroidBelt(Vector3 center, Entity parent, int count = 1000, float innerRadius = 200f, float outerRadius = 400f, float minSize = 0.2f, float maxSize = 1.5f, float inclinationDegrees = 2.0f, int seed = 424242, bool addStaticCollider = false)
        {
            if (count <= 0) return;
            if (innerRadius < 0f) innerRadius = 0f;
            if (outerRadius < innerRadius) outerRadius = innerRadius + 1f;

            var rng = new Random(seed);
            var tex = _checkerTexture ?? _whiteTexture ?? AssetManager.CreateSolidTexture(this, 255, 255, 255, 255);
            var sharedSphere = Mesh.CreateSphere3D();

            // convert inclination to radians
            float inclRad = MathF.Abs(inclinationDegrees) * (MathF.PI / 180f);

            for (int i = 0; i < count; i++)
            {
                // pick a radius with probability proportional to area (so uniform density across annulus)
                float u = (float)rng.NextDouble();
                float r = MathF.Sqrt((u * ((outerRadius * outerRadius) - (innerRadius * innerRadius))) + (innerRadius * innerRadius));
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);

                // small inclination: tilt by a small angle around random node line
                float tilt = (((float)rng.NextDouble() * 2f) - 1f) * inclRad; // [-inclRad, inclRad]
                // choose random longitude of ascending node
                float node = (float)(rng.NextDouble() * Math.PI * 2.0);

                // base position in XZ
                float x = center.X + (MathF.Cos(angle) * r);
                float z = center.Z + (MathF.Sin(angle) * r);
                // apply inclination by rotating point about node axis roughly (approx)
                // We'll compute small Y displacement using sin(tilt) * r * sin(some offset)
                float y = center.Y + (MathF.Sin(tilt) * r * MathF.Sin(angle - node));

                var pos = new Vector3(x, y, z);

                float s = minSize + ((float)rng.NextDouble() * (maxSize - minSize));

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere)
                    .Set(new Kinematic())
                    .Set(new Parent(parent))
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(new CollisionShape(new Sphere(s)))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(0, 0.0025f, 0))
                    .Set(new Texture2DRef { Texture = tex });

                if (addStaticCollider)
                {
                    var sphereShape = new Sphere(s * 0.5f);
                    var shapeIndex = _physicsManager.Simulation.Shapes.Add(sphereShape);
                    var staticDesc = new StaticDescription(pos, shapeIndex);
                    _physicsManager.Simulation.Statics.Add(staticDesc);
                }
            }
        }

        // New helper: spawn multiple galaxies
        /// <summary>
        /// Spawns <paramref name="count"/> galaxies (star systems) randomly distributed within a sphere of
        /// radius <paramref name="maxRadius"/>. Ensures each spawned galaxy center is at least
        /// <paramref name="minSeparation"/> units from any other. Returns a list of the created star (sun) entities.
        /// </summary>
        private System.Collections.Generic.List<Entity> SpawnGalaxies(int count, float minSeparation = 50000f, float maxRadius = 200000f, float auScale = 80.0f, int seed = 12345)
        {
            var result = new System.Collections.Generic.List<Entity>();
            if (count <= 0) return result;

            var rng = new Random(seed);
            var centers = new System.Collections.Generic.List<Vector3>();
            float minSepSq = minSeparation * minSeparation;
            const int maxAttemptsPerGalaxy = 1000;

            for (int i = 0; i < count; i++)
            {
                int attempts = 0;
                Vector3 pos;
                bool tooClose;

                do
                {
                    // sample a random direction and radius within sphere
                    var dir = new Vector3((float)((rng.NextDouble() * 2.0) - 1.0), (float)((rng.NextDouble() * 2.0) - 1.0), (float)((rng.NextDouble() * 2.0) - 1.0));
                    if (dir.LengthSquared() <= 1e-6f) dir = new Vector3(1f, 0f, 0f);
                    dir = Vector3.Normalize(dir);
                    float r = (float)rng.NextDouble() * maxRadius;
                    pos = dir * r;

                    tooClose = false;
                    for (int j = 0; j < centers.Count; j++)
                    {
                        if (Vector3.DistanceSquared(centers[j], pos) < minSepSq)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    attempts++;
                } while (tooClose && attempts < maxAttemptsPerGalaxy);

                // accept position even if it hit the attempts limit
                centers.Add(pos);
                // Create a star system at the chosen position. Use the provided auScale.
                result.Add(CreateStarSystem(pos, auScale));
            }

            return result;
        }


        protected unsafe override void Update(TimeSpan delta)
        {
            // Snapshot inputs early so edge/held queries are stable during the frame
            _inputManager.Update(Inputs);

            _angle += (float)delta.TotalSeconds * 0.7f;
            // Keep camera aspect up-to-date on resize
            _camera.Aspect = (float)((float)MainWindow.Width / MainWindow.Height);
            // Camera update moved to AFTER the physics timestep so any live-follow providers
            // sample the physics bodies' post-step poses. This prevents a one-frame lag
            // when the simulation time scale is increased which caused the camera to
            // trail the physics objects and accumulate an offset.


            // NOTE: floating-origin update is performed after the camera update below
            // so it uses the camera's post-update position.

            // Ensure any entities with CollisionShape have corresponding physics objects even if OnSetPost timing missed
            foreach (var e in World.Query(typeof(CollisionShape)))
            {
                bool isKinematic = e.Has<Kinematic>();
                bool hasBody = e.Has<PhysicsBody>();
                bool hasStatic = e.Has<PhysicsStatic>();
                if (isKinematic && !hasBody)
                {
                    var cs = e.GetMut<CollisionShape>();
                    // Build shape index from ECS shape
                    TypedIndex? sidx = null;
                    switch (cs.Shape)
                    {
                        case Sphere s: sidx = _physicsManager.Simulation.Shapes.Add(s); break;
                        case Box b: sidx = _physicsManager.Simulation.Shapes.Add(b); break;
                        case Capsule c: sidx = _physicsManager.Simulation.Shapes.Add(c); break;
                        case Cylinder cy: sidx = _physicsManager.Simulation.Shapes.Add(cy); break;
                    }
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3Double.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                    if (sidx.HasValue)
                    {
                        var pose = new RigidPose(pos, rot);
                        var collidable = new CollidableDescription(sidx.Value, 0.1f);
                        var activity = new BodyActivityDescription(0.01f);
                        var bodyDesc = BodyDescription.CreateKinematic(pose, collidable, activity);
                        var handle = _physicsManager.Simulation.Bodies.Add(bodyDesc);
                        e.Set(new PhysicsBody { Handle = handle });
                    }
                }
                else if (!isKinematic && !hasStatic)
                {
                    var cs = e.GetMut<CollisionShape>();
                    TypedIndex? sidx = null;
                    switch (cs.Shape)
                    {
                        case Sphere s: sidx = _physicsManager.Simulation.Shapes.Add(s); break;
                        case Box b: sidx = _physicsManager.Simulation.Shapes.Add(b); break;
                        case Capsule c: sidx = _physicsManager.Simulation.Shapes.Add(c); break;
                        case Cylinder cy: sidx = _physicsManager.Simulation.Shapes.Add(cy); break;
                    }
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3Double.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                    if (sidx.HasValue)
                    {
                        var sdesc = new StaticDescription(HighPrecisionConversions.ToVector3(pos), sidx.Value) { }; // orientation not in this overload; use position-only static
                        var sh = _physicsManager.Simulation.Statics.Add(sdesc);
                        e.Set(new PhysicsStatic { Handle = sh });
                    }
                }
            }

            // Ensure kinematic bodies follow ECS transforms BEFORE stepping the simulation
            foreach (var entity in World.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(Position3D)))
            {
                var body = entity.GetMut<PhysicsBody>();
                var pos = entity.GetMut<Position3D>().Value;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    bodyRef.Pose = new RigidPose(pos, rot);
                    // Ensure the body is awake and its broadphase bounds reflect the new pose for accurate raycasts
                    // Awake must be set to true otherwise it simply wont work
                    bodyRef.Awake = true;
                    _physicsManager.Simulation.Bodies.UpdateBounds(body.Handle);
                }
                else
                {
                    // Optional: log once per entity if needed; avoided here to prevent spam.
                }
            }

            // Accumulate scaled real seconds
            _simulationAccumulator += delta.TotalSeconds;

            // Limit accumulated time to avoid spiral-of-death after freezes/hangs
            var maxAccum = _fixedSimulationStepSeconds * _maxSimulationStepsPerFrame;
            if (_simulationAccumulator > maxAccum)
            {
                _simulationAccumulator = maxAccum;
            }

            for (int steps = 0; _simulationAccumulator >= _fixedSimulationStepSeconds && steps < _maxSimulationStepsPerFrame; steps++)
            {
                // Step physics with a fixed, deterministic timestep
                _physicsManager.Step((float)_fixedSimulationStepSeconds);

                // Advance the ECS world by the same fixed timestep
                World.Update((float)_fixedSimulationStepSeconds);

                _simulationAccumulator -= _fixedSimulationStepSeconds;
            }


            // NOTE: floating-origin update is intentionally performed after the camera update below
            // so it uses the camera's post-update position when deciding to rebase. The actual
            // rebase is executed after `_cameraController.Update(...)` to avoid mixed pre/post
            // rebase state within a single frame which can cause visible teleports.

            // Input checks (F1,F2, mouse pick, culling toggles) are registered with _inputManager in InitializeScene

            // Note: World.Update was already called inside the fixed-step loop above.
            // Update camera via controller abstraction AFTER physics step so any live
            // focus providers (which read physics poses) get the latest, post-step
            // positions. This avoids the camera lagging behind fast-moving objects
            // when the simulation is sped up.
            _cameraController.Update(Inputs, MainWindow, delta);

            // Update floating origin system (check if rebase is needed).
            // When using camera-relative rendering we *don't* rebase the world because
            // rendering already keeps active coordinates near the origin. Rebasing while
            // rendering camera-relative causes visible jumps and line/orbit mismatch.
            // Always consider rebasing the world when the camera drifts far from origin.
            // Keeping ECS/physics positions small in magnitude prevents single-precision
            // precision loss and visible jitter when converting to floats for rendering.
            if (_floatingOriginManager != null && _floatingOriginManager.Update(_camera.Position, out var rebaseOffset))
            {
                // Keep the camera near origin too so it doesn't immediately trigger another rebase
                _camera.Position -= rebaseOffset;
                // Also adjust camera controller internals so focus/distance remain valid after the world shift
                _cameraController.ApplyOriginShift(rebaseOffset);
            }

            foreach (var entity in World.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(Position3D)))
            {
                var body = entity.GetMut<PhysicsBody>();
                var pos = entity.GetMut<Position3D>().Value;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    bodyRef.Pose = new RigidPose(pos, rot);
                    bodyRef.Awake = true;
                    _physicsManager.Simulation.Bodies.UpdateBounds(body.Handle);
                }
            }

            // Debug: Print floating origin info (uncomment for debugging)
            // if (_floatingOriginManager != null)
            // {
            //     var origin = _floatingOriginManager.CurrentOrigin;
            //     var camDist = new Vector3d(_camera.Position.X, _camera.Position.Y, _camera.Position.Z).Length();
            //     Console.WriteLine($"[FloatingOrigin] Current Origin: {origin}, Camera Distance from Origin: {camDist:F1}");
            // }
        }


        protected unsafe override void Draw(double alpha)
        {
            var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            var backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer == null)
            {
                GraphicsDevice.Submit(cmdbuf);
                return;
            }
            // Measure draw FPS (time between Draw calls) and update window title periodically
            var now = DateTime.UtcNow;
            var dt = (now - _lastDrawTime).TotalSeconds;
            _lastDrawTime = now;
            // protect against zero/very small dt
            if (dt > 0)
            {
                _fps = 1.0 / dt;
            }
            _fpsFrames++;
            _fpsTimer += (float)dt;
            if (_fpsTimer >= 0.5f)
            {
                var avgFps = _fpsFrames / Math.Max(_fpsTimer, 1e-6f);
                var ms = 1000.0 / Math.Max(avgFps, 0.0001);
                MainWindow.SetTitle($"{_baseTitle} | FPS {avgFps:0} ({ms:0.0} ms) | F11 Fullscreen | Esc Quit");
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }
            // Compute single-precision view/projection and extract frustum once per frame.
            var viewDouble = _camera.GetViewMatrix();
            var projDouble = _camera.GetProjectionMatrix();
            var viewMat = HighPrecisionConversions.ToMatrix(viewDouble);
            var projMat = HighPrecisionConversions.ToMatrix(projDouble);
            // When using camera-relative rendering, remove the camera translation from the view
            // (we already subtract the camera from model positions). This prevents double-subtraction
            // and keeps the camera effectively at the origin for rendering.
            if (_useCameraRelativeRendering)
            {
                viewMat.Translation = Vector3.Zero;
            }
            var viewProj = viewMat * projMat;
            var frustum = ExtractFrustumPlanes(viewProj);
            // Runtime culling adjustments are handled by _inputManager registrations in InitializeScene
            // Prepare line batch for this frame and upload any lines
            _lineBatch?.Begin();
            // Example lines: axes at origin
            if (_lineBatch != null)
            {

                // Add all lines, converting to camera-relative positions if enabled.
                // Do the subtraction in double precision then convert to float to avoid
                // large-value precision loss when converting directly to float first.
                var camPosDouble = _camera.Position; // Vector3Double
                double maxDistSq = _maxDebugLineDistance * _maxDebugLineDistance;
                foreach (var entity in World.Query(typeof(Line3D), typeof(Color)))
                {
                    var line = entity.GetMut<Line3D>();
                    var color = entity.GetMut<Color>();

                    // Cull lines whose both endpoints are beyond the max debug distance.
                    // Compute squared distances in double precision for accuracy.
                    var da = Vector3Double.DistanceSquared(line.Start, camPosDouble);
                    var db = Vector3Double.DistanceSquared(line.End, camPosDouble);
                    if (da > maxDistSq && db > maxDistSq)
                    {
                        continue;
                    }

                    Vector3 aVec, bVec;
                    if (_useCameraRelativeRendering)
                    {
                        var aRel = line.Start - camPosDouble;
                        var bRel = line.End - camPosDouble;
                        aVec = HighPrecisionConversions.ToVector3(aRel);
                        bVec = HighPrecisionConversions.ToVector3(bRel);
                    }
                    else
                    {
                        aVec = HighPrecisionConversions.ToVector3(line.Start);
                        bVec = HighPrecisionConversions.ToVector3(line.End);
                    }
                    _lineBatch.AddLine(aVec, bVec, color);
                }

                // Draw orbit circles for entities that have OrbitCircle component
                foreach (var ocEntity in World.Query(typeof(OrbitCircle)))
                {
                    var oc = ocEntity.GetMut<OrbitCircle>();
                    // Get current parent world position
                    var center = GetEntityWorldPosition(oc.Parent);

                    // Cull entire orbit if center is too far from camera (fast-path)
                    if (Vector3Double.DistanceSquared(center, camPosDouble) > maxDistSq)
                    {
                        continue;
                    }

                    if (_useCameraRelativeRendering)
                    {
                        center = new Vector3Double(center.X - camPosDouble.X, center.Y - camPosDouble.Y, center.Z - camPosDouble.Z);
                    }
                    int segs = Math.Max(4, oc.Segments);
                    double angleStep = MathF.Tau / segs;
                    Vector3Double prev = center + new Vector3Double(oc.Radius, 0f, 0f);
                    for (int i = 1; i <= segs; i++)
                    {
                        double a = i * angleStep;
                        Vector3Double p = center + new Vector3Double(Math.Cos(a) * oc.Radius, 0f, Math.Sin(a) * oc.Radius);
                        var p0 = HighPrecisionConversions.ToVector3(prev);
                        var p1 = HighPrecisionConversions.ToVector3(p);
                        _lineBatch.AddLine(p0, p1, oc.Color);
                        prev = p;
                    }
                }

                // Add physics collider wireframes before uploading
                if (_debugDrawColliders)
                {
                    DebugDrawColliders();
                }


                // Draw axes for entities that request it, or for all entities when toggled.
                if (_debugDrawAxesAll)
                {
                    foreach (var e in World.Query(typeof(Position3D)))
                    {
                        var ent = e; // capture
                        var pos = ent.Has<Position3D>() ? ent.GetMut<Position3D>().Value : Vector3Double.Zero;

                        // Cull axes if center is too far
                        if (Vector3Double.DistanceSquared(pos, camPosDouble) > maxDistSq) continue;

                        if (_useCameraRelativeRendering)
                        {
                            pos = new Vector3Double(pos.X - camPosDouble.X, pos.Y - camPosDouble.Y, pos.Z - camPosDouble.Z);
                        }
                        DrawEntityAxes(pos, 1.0f);
                    }
                }
                else
                {
                    foreach (var e in World.Query(typeof(DebugAxes), typeof(Position3D)))
                    {
                        var ent = e; // capture
                        var pos = ent.GetMut<Position3D>().Value;

                        // Cull axes if center is too far
                        if (Vector3Double.DistanceSquared(pos, camPosDouble) > maxDistSq) continue;

                        if (_useCameraRelativeRendering)
                        {
                            pos = new Vector3Double(pos.X - camPosDouble.X, pos.Y - camPosDouble.Y, pos.Z - camPosDouble.Z);
                        }
                        DrawEntityAxes(pos, 1.0f);
                    }
                }

                // Upload line vertex data
                _lineBatch.UploadBufferData(cmdbuf);
            }
            /////////////////////
            // MSAA PIPELINE STEP BEGIN 
            /////////////////////
            // Ensure MSAA targets match the current swapchain size (handles window resizes, DPI changes, etc.)
            EnsureMsaaTargets(backbuffer);

            var colorTarget = new ColorTargetInfo(_msaaColor ?? backbuffer, new Color(32, 32, 40, 255));
            if (_msaaColor != null)
            {
                colorTarget.ResolveTexture = backbuffer.Handle; // Resolve MSAA to backbuffer
                colorTarget.StoreOp = StoreOp.Resolve; // Perform resolve into swapchain
            }
            var depthTarget = new DepthStencilTargetInfo(_msaaDepth!, clearDepth: 1f);



            ImDrawData* drawData = null;
            if (_imguiEnabled)
            {
                ImGuiImplSDL3.SDLGPU3NewFrame();
                ImGuiImplSDL3.NewFrame();
                ImGui.NewFrame();

                // Render all our ImGui windows (extracted helpers)
                RenderImguiWindows();

                ImGui.Render();
                drawData = ImGui.GetDrawData();
                ImGuiImplSDL3.SDLGPU3PrepareDrawData(drawData, (SDLGPUCommandBuffer*)cmdbuf.Handle);
            }

            /////////////////////
            // RENDER PASS BEGIN
            /////////////////////
            var pass = cmdbuf.BeginRenderPass(depthTarget, colorTarget);

            // Draw skybox first (no depth write/test) so world renders on top
            if (_skyboxEnabled && _skyboxTexture != null && _skyboxMesh.HasValue)
            {
                // Choose pipeline based on texture type
                var isCube = _skyboxTexture.Type == TextureType.Cube;
                pass.BindGraphicsPipeline(isCube ? _skyboxCubePipeline! : _skyboxPipeline!);
                var skyMesh = _skyboxMesh.Value;
                skyMesh.Bind(pass);
                // Use wrap sampling for skyboxes
                var skySampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);
                pass.BindFragmentSamplers(0, new TextureSamplerBinding[] { new(_skyboxTexture, skySampler) });

                // Skybox should follow the camera; when using camera-relative rendering the view translation
                // has been removed so placing the skybox at the origin keeps it centered on the camera.
                var modelSky = Matrix4x4.CreateScale(_skyboxScale);
                var mvpSky = modelSky * viewProj;
                cmdbuf.PushVertexUniformData(mvpSky, slot: 0);
                pass.DrawIndexedPrimitives(skyMesh.IndexCount, 1, 0, 0, 0);
                skySampler.Dispose();
            }

            pass.BindGraphicsPipeline(_pipeline!);

            foreach (var entity in World.Query(typeof(GpuMesh)))
            {
                var gpuMesh = entity.GetMut<GpuMesh>();
                // Gather transform components first (needed for culling, avoids binding for culled)
                Vector3 translation = entity.Has<Position3D>() ? entity.GetMut<Position3D>().Value : Vector3.Zero;
                Quaternion rotation = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : Quaternion.Identity;
                Vector3 size = entity.Has<Size3D>() ? entity.GetMut<Size3D>().Value : Vector3.One;

                // Skip rendering if entity is explicitly marked Invisible via tag component
                if (entity.Has<Invisible>())
                {
                    continue;
                }

                // If this entity has a physics representation, prefer the simulation pose for accuracy
                /*
                if (entity.Has<PhysicsBody>())
                {
                    var body = entity.GetMut<PhysicsBody>();
                    if (TryGetBodyRef(body.Handle, out var bodyRef))
                    {
                        translation = bodyRef.Pose.Position;
                        rotation = bodyRef.Pose.Orientation;
                    }
                }
                else if (entity.Has<PhysicsStatic>())
                {
                    var st = entity.GetMut<PhysicsStatic>();
                    if (TryGetStaticRef(st.Handle, out var sref))
                    {
                        translation = sref.Pose.Position;
                        rotation = sref.Pose.Orientation;
                    }
                }
                */
                // Sphere culling: use a conservative radius based on max scale axis scaled by tuning factor.
                float radius = MathF.Max(size.X, MathF.Max(size.Y, size.Z)) * _cullingRadiusScale;

                // Size-based visibility: compute projected pixel radius and force visible if above threshold
                bool visibleBySize = false;
                if (_enableSizeVisibility)
                {
                    // Use the same translation that will be used for rendering. When using
                    // camera-relative rendering the model matrices subtract the camera position,
                    // so do the same here so clip-space and distance calculations match.
                    var camPos = HighPrecisionConversions.ToVector3(_camera.Position);
                    var renderTranslation = _useCameraRelativeRendering ? (translation - camPos) : translation;

                    // Transform center into clip space (use renderTranslation for consistency)
                    var center4 = new Vector4(renderTranslation, 1f);
                    var clip = Vector4.Transform(center4, viewProj);
                    if (clip.W > 0f)
                    {
                        // approximate projected pixel radius: r / (dist * tan(fov/2)) * (screenHeight/2)
                        var toObj = renderTranslation; // already in camera-relative space when appropriate
                        var dist = toObj.Length();
                        if (dist > 0f)
                        {
                            float tanHalfFov = MathF.Tan((float)_camera.Fov * 0.5f);
                            float screenHeight = (float)MainWindow.Height;
                            float pixelRadius = (radius / (dist * tanHalfFov)) * (screenHeight * 0.5f);
                            if (pixelRadius >= _minScreenPixelSize)
                            {
                                visibleBySize = true;
                            }
                        }
                    }
                }

                // Perform frustum culling unless size-based visibility forces rendering
                if (_enableFrustumCulling && !visibleBySize && !IsSphereVisible(translation, radius, frustum))
                {
                    continue; // culled
                }

                // Bind mesh only for visible entities
                gpuMesh.Bind(pass);

                // Bind entity texture if present, otherwise bind dummy
                var texture = _whiteTexture!;
                if (entity.Has<Texture2DRef>())
                {
                    var texRef = entity.GetMut<Texture2DRef>();
                    if (texRef.Texture != null) { texture = texRef.Texture; }
                }
                pass.BindFragmentSamplers(new TextureSamplerBinding(texture, GraphicsDevice.LinearSampler));

                // Build MVP and push to vertex uniforms at slot 0 (cbuffer b0, space1)
                // If using camera-relative rendering, subtract camera position from the translation so
                // model matrices are built in view-local coordinates and avoid large world coordinates.
                var modelTrans = translation;
                if (_useCameraRelativeRendering)
                {
                    modelTrans = translation - HighPrecisionConversions.ToVector3(_camera.Position);
                }
                var model = Matrix4x4.CreateScale(size) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(modelTrans);
                var mvp = model * viewProj;

                cmdbuf.PushVertexUniformData(mvp, slot: 0);
                pass.DrawIndexedPrimitives(gpuMesh.IndexCount, 1, 0, 0, 0);
            }

            // Draw lines last so they overlay geometry
            if (_lineBatch != null)
            {
                pass.BindGraphicsPipeline(_linePipeline!);
                _lineBatch.Render(pass, viewProj);
            }


            /*

            var dirDir = Vector3.Normalize(new Vector3(-0.4f, -1.0f, -0.3f));
            var dirIntensity = 1.0f;
            var dirColor = new Vector3(1, 1, 1);

            var ptPos = new Vector3(0, 0, 2);
            var ptRange = 8.0f;
            var ptColor = new Vector3(1.0f, 0.85f, 0.7f);
            var ptIntensity = 1.0f;
            var attLin = 0.0f;
            var attQuad = 0.2f;

            var ambient = 0.2f;

            var lightUbo = new LightUniforms
            {
                Dir_Dir_Intensity = new Vector4(dirDir, dirIntensity),
                Dir_Color = new Vector4(dirColor, 0f),
                Pt_Pos_Range = new Vector4(ptPos, ptRange),
                Pt_Color_Intensity = new Vector4(ptColor, ptIntensity),
                Pt_Attenuation = new Vector2(attLin, attQuad),
                Ambient = ambient
            };

            // cbuffer LightParams : register(b0, space3), slot 0 but space(set) 3
            cmdbuf.PushFragmentUniformData(lightUbo, slot: 0);

            */

            if (_imguiEnabled && drawData != null)
            {
                ImGuiImplSDL3.SDLGPU3RenderDrawData(drawData, (SDLGPUCommandBuffer*)cmdbuf.Handle, (SDLGPURenderPass*)pass.Handle, null);
            }

            cmdbuf.EndRenderPass(pass);


            /////////////////////
            // RENDER PASS END
            /////////////////////

            if (_imguiEnabled && (io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            GraphicsDevice.Submit(cmdbuf);
        }

        // Simple plane struct for frustum culling
        private struct PlaneF
        {
            public Vector3 Normal;
            public float D;
        }

        // Extracts six frustum planes from a view-projection matrix (row-major, v * M convention)
        private static PlaneF[] ExtractFrustumPlanes(Matrix4x4 m)
        {
            var planes = new PlaneF[6];

            // Left: row4 + row1
            planes[0] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31),
                D = m.M44 + m.M41
            });
            // Right: row4 - row1
            planes[1] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31),
                D = m.M44 - m.M41
            });
            // Bottom: row4 + row2
            planes[2] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32),
                D = m.M44 + m.M42
            });
            // Top: row4 - row2
            planes[3] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32),
                D = m.M44 - m.M42
            });
            // Near: row4 + row3 (for DX/Vulkan depth 0..1)
            planes[4] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33),
                D = m.M44 + m.M43
            });
            // Far: row4 - row3
            planes[5] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33),
                D = m.M44 - m.M43
            });

            return planes;
        }

        private static PlaneF NormalizePlane(PlaneF p)
        {
            float invLen = 1f / p.Normal.Length();
            p.Normal *= invLen;
            p.D *= invLen;
            return p;
        }

        private static bool IsSphereVisible(Vector3 center, float radius, PlaneF[] frustum)
        {
            // Outside if outside any plane
            for (int i = 0; i < 6; i++)
            {
                float dist = Vector3.Dot(frustum[i].Normal, center) + frustum[i].D;
                if (dist < -radius)
                {
                    return false;
                }
            }
            return true;
        }

        // ==========================
        // Physics debug draw helpers
        // ==========================
        private void AddWireSphere(Vector3 center, float radius, Color color, int segments = 24)
        {
            if (_lineBatch == null || radius <= 0f) return;
            // Three great circles around X, Y, Z axes
            void Circle(Vector3 axis)
            {
                Vector3 prev = default;
                bool hasPrev = false;
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float ang = t * MathF.Tau;
                    // Build an orthonormal basis for the plane perpendicular to axis
                    Vector3 n = Vector3.Normalize(axis);
                    Vector3 u = Vector3.Normalize(Vector3.Cross(n, Math.Abs(n.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX));
                    Vector3 v = Vector3.Cross(n, u);
                    Vector3 p = center + (((u * MathF.Cos(ang)) + (v * MathF.Sin(ang))) * radius);
                    if (hasPrev)
                    {
                        _lineBatch.AddLine(prev, p, color);
                    }
                    prev = p; hasPrev = true;
                }
            }
            Circle(Vector3.UnitX);
            Circle(Vector3.UnitY);
            Circle(Vector3.UnitZ);
        }

        private void AddWireBox(Vector3 center, Quaternion orientation, Vector3 halfExtents, Color color)
        {
            if (_lineBatch == null) return;
            // 8 corners in local space
            Vector3[] c = new Vector3[8];
            int idx = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                        c[idx++] = new Vector3(x * halfExtents.X, y * halfExtents.Y, z * halfExtents.Z);
                }
            }

            // Transform to world
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            for (int i = 0; i < 8; i++)
            {
                c[i] = Vector3.Transform(c[i], rot) + center;
            }
            // box edges pairs
            int[,] e = new int[,]
            {
                {0,1},{0,2},{0,4},
                {3,1},{3,2},{3,7},
                {5,1},{5,4},{5,7},
                {6,2},{6,4},{6,7}
            };
            for (int i = 0; i < e.GetLength(0); i++)
            {
                _lineBatch!.AddLine(c[e[i, 0]], c[e[i, 1]], color);
            }
        }

        private void AddWireCapsule(Vector3 center, Quaternion orientation, float halfLength, float radius, Color color, int segments = 16)
        {
            if (_lineBatch == null) return;
            // Capsule aligned with local Y axis, then rotate by orientation
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
            Vector3 forward = Vector3.TransformNormal(Vector3.UnitZ, rot);
            Vector3 a = center + (up * halfLength);
            Vector3 b = center - (up * halfLength);
            // cylinder rings
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = (right * MathF.Cos(t0)) + (forward * MathF.Sin(t0));
                Vector3 r1 = (right * MathF.Cos(t1)) + (forward * MathF.Sin(t1));
                _lineBatch.AddLine(a + (r0 * radius), a + (r1 * radius), color);
                _lineBatch.AddLine(b + (r0 * radius), b + (r1 * radius), color);
                _lineBatch.AddLine(a + (r0 * radius), b + (r0 * radius), color);
            }
            // hemispheres
            void Hemisphere(Vector3 centerHem, int sign)
            {
                for (int iy = 0; iy <= segments; iy++)
                {
                    float v = (float)iy / segments * MathF.PI * 0.5f;
                    float y = MathF.Sin(v) * sign;
                    float r = MathF.Cos(v);
                    Vector3 ringUp = up * y;
                    Vector3 prev = default; bool hasPrev = false;
                    for (int ix = 0; ix <= segments; ix++)
                    {
                        float u = (float)ix / segments * MathF.Tau;
                        Vector3 dir = (right * (MathF.Cos(u) * r)) + (forward * (MathF.Sin(u) * r)) + ringUp;
                        Vector3 p = centerHem + (dir * radius);
                        if (hasPrev) _lineBatch.AddLine(prev, p, color);
                        prev = p; hasPrev = true;
                    }
                }
            }
            Hemisphere(a, +1);
            Hemisphere(b, -1);
        }

        private void AddWireCylinder(Vector3 center, Quaternion orientation, float halfLength, float radius, Color color, int segments = 16)
        {
            if (_lineBatch == null) return;
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
            Vector3 forward = Vector3.TransformNormal(Vector3.UnitZ, rot);
            Vector3 a = center + (up * halfLength);
            Vector3 b = center - (up * halfLength);
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = (right * MathF.Cos(t0)) + (forward * MathF.Sin(t0));
                Vector3 r1 = (right * MathF.Cos(t1)) + (forward * MathF.Sin(t1));
                _lineBatch.AddLine(a + (r0 * radius), a + (r1 * radius), color);
                _lineBatch.AddLine(b + (r0 * radius), b + (r1 * radius), color);
                _lineBatch.AddLine(a + (r0 * radius), b + (r0 * radius), color);
            }
        }

        private void DebugDrawColliders()
        {
            if (_lineBatch == null) return;

            // 1) Optionally draw ECS-declared shapes at ECS transform (green) to compare against physics
            if (_debugDrawEcsColliderPoses)
            {
                foreach (var e in World.Query(typeof(CollisionShape)))
                {
                    var cs = e.GetMut<CollisionShape>();
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3Double.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                    DrawShapeFromEcs(cs.Shape, pos, rot, new Color(64, 255, 64, 160));
                }
            }

            // 2) If a physics body exists, also draw at physics pose (red) — where physics thinks it is
            foreach (var e in World.Query(typeof(PhysicsBody), typeof(CollisionShape)))
            {
                var body = e.GetMut<PhysicsBody>();
                var cs = e.GetMut<CollisionShape>();
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    DrawShapeFromEcs(cs.Shape, bodyRef.Pose.Position, bodyRef.Pose.Orientation, new Color(255, 64, 64, 200));
                }
            }

            // 3) For statics, draw at static pose (cyan)
            foreach (var e in World.Query(typeof(PhysicsStatic), typeof(CollisionShape)))
            {
                var st = e.GetMut<PhysicsStatic>();
                var cs = e.GetMut<CollisionShape>();
                var sref = _physicsManager.Simulation.Statics[st.Handle];
                DrawShapeFromEcs(cs.Shape, sref.Pose.Position, sref.Pose.Orientation, new Color(64, 200, 255, 200));
            }
        }

        private void DrawShapeFromEcs(object? shape, Vector3 position, Quaternion orientation, Color color)
        {
            // If rendering is camera-relative, convert the camera position to single precision
            // and subtract it so collider wireframes are placed in the same local/render space
            // as the rest of the scene. This fixes collider debug visuals appearing offset
            // after a floating-origin rebase when camera-relative rendering is enabled.
            if (_useCameraRelativeRendering && _camera != null)
            {
                var camPos = HighPrecisionConversions.ToVector3(_camera.Position);
                position -= camPos;
            }

            // Cull shapes that are too far from the camera. Note: position here is in single-precision
            // camera-relative space when _useCameraRelativeRendering is true; otherwise it's world-space.
            // Compare squared distances for efficiency.
            if (_camera != null)
            {
                var camPosDouble = _camera.Position;
                var posDouble = new AyanamisTower.StellaEcs.HighPrecisionMath.Vector3Double(position.X, position.Y, position.Z);
                if (Vector3Double.DistanceSquared(posDouble, camPosDouble) > (_maxDebugLineDistance * _maxDebugLineDistance))
                {
                    return;
                }
            }

            switch (shape)
            {
                case Sphere s:
                    AddWireSphere(position, s.Radius, color);
                    break;
                case Box b:
                    AddWireBox(position, orientation, new Vector3(b.HalfWidth, b.HalfHeight, b.HalfLength), color);
                    break;
                case Capsule c:
                    AddWireCapsule(position, orientation, c.HalfLength, c.Radius, color);
                    break;
                case Cylinder cy:
                    AddWireCylinder(position, orientation, cy.HalfLength, cy.Radius, color);
                    break;
            }
        }


        /// <summary>
        /// Ensures that the MSAA targets are created and match the size of the backbuffer.
        /// We must do this when resizing the window or changing the DPI. Otherwise there will
        /// be a mismatch and most likely artifacts or a crash.
        /// </summary>
        private void EnsureMsaaTargets(Texture backbuffer)
        {
            // Only recreate when using MSAA
            if (_msaaSamples == SampleCount.One)
            {
                // If MSAA is disabled, dispose any MSAA resources
                _msaaColor?.Dispose();
                _msaaColor = null;
                _msaaDepth?.Dispose();
                _msaaDepth = null; return;
            }

            bool needsColor = _msaaColor == null || _msaaColor.Width != backbuffer.Width || _msaaColor.Height != backbuffer.Height;
            bool needsDepth = _msaaDepth == null || _msaaDepth.Width != backbuffer.Width || _msaaDepth.Height != backbuffer.Height;

            if (needsColor)
            {
                _msaaColor?.Dispose();
                _msaaColor = Texture.Create2D(
                    GraphicsDevice,
                    backbuffer.Width,
                    backbuffer.Height,
                    MainWindow.SwapchainFormat,
                    TextureUsageFlags.ColorTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
            }

            if (needsDepth)
            {
                _msaaDepth?.Dispose();
                _msaaDepth = Texture.Create2D(
                    GraphicsDevice,
                    backbuffer.Width,
                    backbuffer.Height,
                    GraphicsDevice.SupportedDepthStencilFormat,
                    TextureUsageFlags.DepthStencilTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
            }
        }

        private void DestroyImgui()
        {
            // Delegate to the centralized disable method which is idempotent.
            DisableImgui();
        }

        protected override void Destroy()
        {
            DestroyImgui();
            _msaaColor?.Dispose();
            _msaaDepth?.Dispose();
            _pipeline?.Dispose();
            _skyboxPipeline?.Dispose();
            _skyboxCubePipeline?.Dispose();
            _linePipeline?.Dispose();
            _lineBatch?.Dispose();
            _whiteTexture?.Dispose();
            _checkerTexture?.Dispose();
            _skyboxTexture?.Dispose();

            // Cleanup floating origin manager if needed
            // _floatingOriginManager doesn't implement IDisposable, so no cleanup needed
        }
    }
}
