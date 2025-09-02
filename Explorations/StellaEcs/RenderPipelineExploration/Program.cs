using System;
using System.Numerics;
using System.Collections.Generic;
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
using StellaInvicta.Physics;
using AyanamisTower.StellaEcs.StellaInvicta.Systems;
using System.Runtime.InteropServices;
using StellaInvicta.Graphics;
using StellaInvicta.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta;


internal static class Program
{
    public static void Main()
    {
        var game = new StellaInvictaGame();
        game.Run();
    }

    private sealed class StellaInvictaGame : Game
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
        private float _tickRate = 30.0f; // Runtime-controlled tick rate (Hz). Adjust in ImGui to change fixed timestep.
        private double _fixedSimulationStepSeconds => 1.0 / Math.Max(0.0001, _tickRate); // Computed fixed timestep seconds from tick rate. Use this everywhere instead of a const.
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
        private PipelineFactory? _pipelineFactory;
        private GraphicsPipeline? _pipeline;
        private VertexInputState _defaultVertexInput;
        // rotation
        private float _angle;
        // textures
        private Texture? _whiteTexture;
        private Texture? _blackTexture;
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
        private GraphicsPipeline? _imguiPipeline;

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
                                                  // Minimum on-screen radius for selection indicators so tiny objects remain visible
        private float _minSelectionPixelSize = 6.0f;
        // Selection indicator customization
        private enum SelectionIndicatorShape { Circle = 0, Ring = 1, Square = 2, Crosshair = 3, Homeworld = 4 }
        private SelectionIndicatorShape _indicatorShape = SelectionIndicatorShape.Homeworld;
        private Vector4 _indicatorBorderColor = new Vector4(0.1f, 0.9f, 0.9f, 0.95f);
        private Vector4 _indicatorFillColor = new Vector4(0.1f, 0.9f, 0.9f, 0.20f);
        private bool _indicatorUseFill = true;
        private float _indicatorThickness = 2.0f;
        private float _crosshairLengthScale = 1.0f; // Crosshair arm length as a multiple of radius
        private float _hwCornerLengthScale = 0.4f; // Homeworld corner length (x radius)
        private float _hwInsetScale = 1.0f; // Homeworld bracket inset/outset (x radius)
                                            // Homeworld indicator animation
        private bool _indicatorAnimate = false;
        private float _indicatorAnimSpeed = 1.5f; // Hz
        private float _indicatorAnimInsetAmp = 0.08f; // +/- fraction of radius for inset pulsation
        private float _indicatorAnimCornerAmp = 0.15f; // +/- fraction for corner length pulsation

        // Interpolation toggle
        private bool _interpolationEnabled = true;

        // Floating origin system
        private FloatingOriginManager? _floatingOriginManager;
        // Input manager to simplify checked inputs
        private InputManager _inputManager = new();
        private readonly MouseInteractionService _mouseInteraction = new();
        // Collision interaction service (initialized in ctor after physics manager)
        private CollisionInteractionService _collisionInteraction = null!;
        // Selection interaction service
        private readonly SelectionInteractionService _selectionInteraction = new();
        // Selection drag rectangle (screen-space via ImGui)
        private bool _isDragSelecting = false;
        private Vector2 _dragStartScreen;
        private Vector2 _dragEndScreen;
        // Track previous selection set for component syncing
        private HashSet<Entity> _prevSelection = new();
        // FPS counter for window title
        private readonly string _baseTitle = "Stella Invicta";
        private float _fpsTimer = 0f;
        private int _fpsFrames = 0;
        private double _fps = 0.0;
        // Track last draw time to measure actual render FPS (not update rate)
        private DateTime _lastDrawTime = DateTime.UtcNow;
        // Currently selected entity in the ImGui entities window
        private Entity _selectedEntity = default;
        // Accumulated time for animated shaders (e.g., Sun)
        private float _shaderTime = 0f;
        public StellaInvictaGame() : base(
            new AppInfo("Ayanami", "Stella Invicta Demo"),
            new WindowCreateInfo("Stella Invicta", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(60, 360),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            _physicsManager = new PhysicsManager(World);
            // Initialize collision interaction service so entities can register handlers
            _collisionInteraction = new CollisionInteractionService(_physicsManager);

            // Initialize pipeline factory
            _pipelineFactory = new PipelineFactory(
                GraphicsDevice,
                _msaaSamples,
                MainWindow.SwapchainFormat,
                GraphicsDevice.SupportedDepthStencilFormat
            );

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
        /// Prefers the interpolated render position (RenderPosition3D), then the simulation position (Position3D).
        /// Returns Vector3Zero if no position is available.
        /// </summary>
        private Vector3Double GetEntityWorldPosition(Entity e)
        {
            if (!World.IsEntityValid(e)) return Vector3Double.Zero;
            if (e.Has<RenderPosition3D>()) return e.GetCopy<RenderPosition3D>().Value;
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
            // Camera tuning controls
            if (_cameraController != null && ImGui.CollapsingHeader("Camera"))
            {
                // Pan speed
                float pan = (float)_cameraController.PanSpeed;
                if (ImGui.SliderFloat("Pan Speed", ref pan, 0f, 200f)) _cameraController.PanSpeed = pan;

                // Edge pan
                bool edge = _cameraController.EdgePanEnabled;
                if (ImGui.Checkbox("Edge Pan", ref edge)) _cameraController.EdgePanEnabled = edge;
                int edgeThreshold = _cameraController.EdgePanThreshold;
                if (ImGui.SliderInt("Edge Threshold", ref edgeThreshold, 0, 200)) _cameraController.EdgePanThreshold = edgeThreshold;
                float edgeSpeed = (float)_cameraController.EdgePanSpeed;
                if (ImGui.SliderFloat("Edge Pan Speed", ref edgeSpeed, 0f, 200f)) _cameraController.EdgePanSpeed = edgeSpeed;

                // Rotation
                float rotate = (float)_cameraController.RotateSensitivity;
                if (ImGui.SliderFloat("Rotate Sensitivity", ref rotate, 0.0001f, 0.1f)) _cameraController.RotateSensitivity = rotate;
                bool smoothRot = _cameraController.SmoothRotation;
                if (ImGui.Checkbox("Smooth Rotation", ref smoothRot)) _cameraController.SmoothRotation = smoothRot;
                float rotSmooth = (float)_cameraController.RotationSmoothing;
                if (ImGui.SliderFloat("Rotation Smoothing", ref rotSmooth, 0f, 120f)) _cameraController.RotationSmoothing = rotSmooth;

                // Zoom
                float zoom = (float)_cameraController.ZoomSpeed;
                if (ImGui.SliderFloat("Zoom Speed", ref zoom, 0f, 1000f)) _cameraController.ZoomSpeed = zoom;
                float scrollMul = (float)_cameraController.ScrollZoomMultiplier;
                if (ImGui.SliderFloat("Scroll Multiplier", ref scrollMul, 0f, 10f)) _cameraController.ScrollZoomMultiplier = scrollMul;
                float maxScroll = (float)_cameraController.MaxScrollZoomStep;
                if (ImGui.SliderFloat("Max Scroll Step", ref maxScroll, 0f, 10000f)) _cameraController.MaxScrollZoomStep = maxScroll;

                // Smoothing
                float smoothing = (float)_cameraController.Smoothing;
                if (ImGui.SliderFloat("Camera Smoothing", ref smoothing, 0f, 60f)) _cameraController.Smoothing = smoothing;

                // Distance limits
                float minDist = (float)_cameraController.MinDistance;
                float maxDist = (float)_cameraController.MaxDistance;
                if (ImGui.InputFloat("Min Distance", ref minDist, 0f, 0f, "F2")) _cameraController.MinDistance = Math.Max(0.001, minDist);
                if (ImGui.InputFloat("Max Distance", ref maxDist, 0f, 0f, "F2")) _cameraController.MaxDistance = Math.Max(_cameraController.MinDistance, maxDist);

                // Pan distance exponent
                float panExp = (float)_cameraController.PanDistanceScaleExponent;
                if (ImGui.SliderFloat("Pan Distance Exponent", ref panExp, 0f, 2f)) _cameraController.PanDistanceScaleExponent = panExp;

                // Follow smoothing params
                float followInit = _cameraController.InitialFollowSmoothingSeconds;
                if (ImGui.SliderFloat("Follow Initial Smoothing", ref followInit, 0f, 3f)) _cameraController.InitialFollowSmoothingSeconds = followInit;
                float followTrack = _cameraController.FollowTrackingSmoothingRate;
                if (ImGui.SliderFloat("Follow Tracking Rate", ref followTrack, 0f, 240f)) _cameraController.FollowTrackingSmoothingRate = followTrack;

                if (ImGui.Button("Snap Rotation To Target")) _cameraController.SnapRotationToTarget(); ImGui.SameLine();
                if (ImGui.Button("Snap Rotation To Camera")) _cameraController.SnapRotationToCamera();
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
            ImGui.Checkbox("Interpolation Enabled", ref _interpolationEnabled);
            // Tweak the fixed simulation tick rate at runtime. Changing this will immediately affect
            // how many fixed steps are processed each frame.
            ImGui.SliderFloat("Tick Rate (Hz)", ref _tickRate, 1f, 240f);
            ImGui.Text($"Fixed dt: {_fixedSimulationStepSeconds:F4} s");
            ImGui.Checkbox("Frustum Culling", ref _enableFrustumCulling);
            ImGui.SliderFloat("Culling Radius Scale", ref _cullingRadiusScale, 0.1f, 10f);

            // Selection debug info
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Selection"))
            {
                var sel = _selectionInteraction.CurrentSelection;
                ImGui.Text($"Selected Count: {sel.Count}");
                ImGui.SliderFloat("Min Indicator Radius (px)", ref _minSelectionPixelSize, 0.0f, 32.0f);
                // Indicator customization controls
                int shapeIdx = (int)_indicatorShape;
                string[] shapes = new[] { "Circle", "Ring", "Square", "Crosshair", "Homeworld" };
                if (ImGui.Combo("Indicator Shape", ref shapeIdx, shapes, shapes.Length))
                {
                    _indicatorShape = (SelectionIndicatorShape)shapeIdx;
                }
                ImGui.ColorEdit4("Border Color", ref _indicatorBorderColor);
                ImGui.Checkbox("Use Fill", ref _indicatorUseFill);
                if (_indicatorUseFill && _indicatorShape != SelectionIndicatorShape.Ring && _indicatorShape != SelectionIndicatorShape.Crosshair && _indicatorShape != SelectionIndicatorShape.Homeworld)
                {
                    ImGui.ColorEdit4("Fill Color", ref _indicatorFillColor);
                }
                ImGui.SliderFloat("Border Thickness", ref _indicatorThickness, 0.5f, 8.0f);
                if (_indicatorShape == SelectionIndicatorShape.Crosshair)
                {
                    ImGui.SliderFloat("Crosshair Length (x radius)", ref _crosshairLengthScale, 0.25f, 2.0f);
                }
                if (_indicatorShape == SelectionIndicatorShape.Homeworld)
                {
                    ImGui.SliderFloat("HW Corner Length (x radius)", ref _hwCornerLengthScale, 0.15f, 0.8f);
                    ImGui.SliderFloat("HW Inset (x radius)", ref _hwInsetScale, 0.7f, 1.3f);
                    ImGui.Checkbox("Animate HW Brackets", ref _indicatorAnimate);
                    if (_indicatorAnimate)
                    {
                        ImGui.SliderFloat("HW Anim Speed (Hz)", ref _indicatorAnimSpeed, 0.1f, 5.0f);
                        ImGui.SliderFloat("HW Inset Amplitude", ref _indicatorAnimInsetAmp, 0.0f, 0.3f);
                        ImGui.SliderFloat("HW Corner Amp", ref _indicatorAnimCornerAmp, 0.0f, 0.5f);
                    }
                }
                if (sel.Count == 0)
                {
                    ImGui.Text("None");
                }
                else
                {
                    if (ImGui.Button("Clear Selection"))
                    {
                        _selectionInteraction.ClearSelection();
                    }
                    ImGui.BeginChild("sel_list", new Vector2(0, 150), ImGuiChildFlags.None);
                    foreach (var e in sel)
                    {
                        string label = $"E{e.Id}";
                        try
                        {
                            if (e.Has<Position3D>())
                            {
                                var p = e.GetCopy<Position3D>().Value;
                                label += $"  pos=({p.X:F1},{p.Y:F1},{p.Z:F1})";
                            }
                        }
                        catch { /* entity might be invalidated between frames */ }

                        bool isFocused = _selectedEntity != default && e.Equals(_selectedEntity);
                        if (ImGui.Selectable(label, isFocused))
                        {
                            _selectedEntity = e;
                        }
                    }
                    ImGui.EndChild();
                }
            }
            ImGui.End();
        }

        // Grouped call for all ImGui windows we want to show
        private void RenderImguiWindows()
        {
            RenderImguiDebugWindow();
            RenderImguiEntitiesWindow();
            // Overlay selection indicators (drawn in foreground draw list)
            RenderSelectionIndicatorsImGui();
        }

        // Sync the Selected tag component on entities to match the SelectionInteractionService set
        private void SyncSelectedComponents()
        {
            // Snapshot current selection
            var current = _selectionInteraction.CurrentSelection;

            // Remove tag from entities no longer selected
            foreach (var e in _prevSelection)
            {
                if (!current.Contains(e))
                {
                    try
                    {
                        if (World.IsEntityValid(e) && e.Has<Selected>())
                        {
                            e.Remove<Selected>();
                        }
                    }
                    catch { /* entity may be destroyed */ }
                }
            }

            // Add tag to newly selected entities
            foreach (var e in current)
            {
                if (!_prevSelection.Contains(e))
                {
                    try
                    {
                        if (World.IsEntityValid(e) && !e.Has<Selected>())
                        {
                            e.Set(new Selected());
                        }
                    }
                    catch { /* entity may be destroyed */ }
                }
            }

            // Update previous snapshot
            _prevSelection = new HashSet<Entity>(current);
        }

        // Draw a screen-space overlay ring for each selected entity using ImGui
        private void RenderSelectionIndicatorsImGui()
        {
            if (!_imguiEnabled) return;

            var sel = _selectionInteraction.CurrentSelection;
            if (sel.Count == 0) return;

            float screenW = (float)MainWindow.Width;
            float screenH = (float)MainWindow.Height;

            var view = HighPrecisionConversions.ToMatrix(_camera.GetViewMatrix());
            var proj = HighPrecisionConversions.ToMatrix(_camera.GetProjectionMatrix());
            if (_useCameraRelativeRendering) { view.Translation = Vector3.Zero; }
            var viewProj = view * proj;
            var camPos = HighPrecisionConversions.ToVector3(_camera.Position);
            float tanHalfFov = MathF.Tan((float)_camera.Fov * 0.5f);

            var drawList = ImGui.GetForegroundDrawList();
            uint borderCol = ImGui.ColorConvertFloat4ToU32(_indicatorBorderColor);
            uint fillCol = ImGui.ColorConvertFloat4ToU32(_indicatorFillColor);

            // Optional: Alt + Left Click to zoom to a selected entity under the cursor (no physics pick needed)
            // Only when mouse isn't captured by UI windows.
            var ioLocal = ImGui.GetIO();
            bool altLeftClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ioLocal.KeyAlt && !ioLocal.WantCaptureMouse;
            Vector2 mousePos = altLeftClick ? ImGui.GetMousePos() : default;
            Entity? zoomTarget = null;
            Vector3Double zoomTargetWorld = default;
            float zoomTargetRadius = 0f; // world-space radius estimate
            float bestDistSq = float.MaxValue;

            foreach (var e in sel)
            {
                try
                {
                    if (!World.IsEntityValid(e)) continue;

                    // Position
                    Vector3Double posD;
                    if (e.Has<RenderPosition3D>()) posD = e.GetCopy<RenderPosition3D>().Value;
                    else if (e.Has<Position3D>()) posD = e.GetCopy<Position3D>().Value;
                    else if (e.Has<PhysicsBody>() && TryGetBodyRef(e.GetCopy<PhysicsBody>().Handle, out var bodyRef)) posD = bodyRef.Pose.Position;
                    else continue;

                    // Radius estimate
                    float radius = 0.6f;
                    if (e.Has<Size3D>())
                    {
                        var s = e.GetCopy<Size3D>().Value;
                        radius = MathF.Max((float)s.X, MathF.Max((float)s.Y, (float)s.Z)) * 0.6f;
                    }
                    else if (e.Has<CollisionShape>())
                    {
                        var cs = e.GetCopy<CollisionShape>().Shape;
                        radius = cs switch
                        {
                            Sphere s => s.Radius,
                            Box b => MathF.Sqrt((b.HalfWidth * b.HalfWidth) + (b.HalfHeight * b.HalfHeight) + (b.HalfLength * b.HalfLength)),
                            Capsule c => c.HalfLength + c.Radius,
                            Cylinder cy => MathF.Sqrt((cy.HalfLength * cy.HalfLength) + (cy.Radius * cy.Radius)),
                            _ => radius
                        };
                    }

                    // Project to screen
                    var pos = HighPrecisionConversions.ToVector3(posD);
                    var renderPos = _useCameraRelativeRendering ? (pos - camPos) : pos;
                    var clip = Vector4.Transform(new Vector4(renderPos, 1f), viewProj);
                    if (clip.W <= 0f) continue;

                    var toObj = renderPos;
                    float dist = MathF.Max(1e-4f, toObj.Length());
                    float pixelRadius = (radius / (dist * MathF.Max(1e-4f, tanHalfFov))) * (screenH * 0.5f);
                    // Enforce a minimum pixel radius for visibility
                    if (_minSelectionPixelSize > 0)
                    {
                        pixelRadius = MathF.Max(pixelRadius, _minSelectionPixelSize);
                    }
                    // small halo padding
                    pixelRadius *= 1.15f;

                    var ndcX = clip.X / clip.W;
                    var ndcY = clip.Y / clip.W;
                    float sx = ((ndcX * 0.5f) + 0.5f) * screenW;
                    float sy = (1f - ((ndcY * 0.5f) + 0.5f)) * screenH;

                    // Cull if far off-screen
                    if (sx < -50 || sx > screenW + 50 || sy < -50 || sy > screenH + 50) continue;

                    var center = new Vector2(sx, sy);

                    // Accumulate best zoom target if Alt+Left was clicked this frame
                    if (altLeftClick)
                    {
                        float dx = mousePos.X - center.X;
                        float dy = mousePos.Y - center.Y;
                        float d2 = (dx * dx) + (dy * dy);
                        // Consider a hit when inside indicator radius (simple, works for all shapes)
                        if (d2 <= (pixelRadius * pixelRadius) && d2 < bestDistSq)
                        {
                            zoomTarget = e;
                            zoomTargetWorld = posD;
                            zoomTargetRadius = MathF.Max(0.01f, radius);
                            bestDistSq = d2;
                        }
                    }

                    switch (_indicatorShape)
                    {
                        case SelectionIndicatorShape.Circle:
                            if (_indicatorUseFill)
                            {
                                drawList.AddCircleFilled(center, pixelRadius, fillCol);
                            }
                            drawList.AddCircle(center, pixelRadius, borderCol, 0, _indicatorThickness);
                            break;
                        case SelectionIndicatorShape.Ring:
                            drawList.AddCircle(center, pixelRadius, borderCol, 0, MathF.Max(1.0f, _indicatorThickness));
                            break;
                        case SelectionIndicatorShape.Square:
                            {
                                float half = pixelRadius;
                                var pMin = new Vector2(center.X - half, center.Y - half);
                                var pMax = new Vector2(center.X + half, center.Y + half);
                                if (_indicatorUseFill)
                                {
                                    drawList.AddRectFilled(pMin, pMax, fillCol, 2f, ImDrawFlags.None);
                                }
                                drawList.AddRect(pMin, pMax, borderCol, 2f, ImDrawFlags.None, _indicatorThickness);
                            }
                            break;
                        case SelectionIndicatorShape.Crosshair:
                            {
                                float len = pixelRadius * MathF.Max(0.1f, _crosshairLengthScale);
                                float t = MathF.Max(1.0f, _indicatorThickness);
                                // Horizontal
                                drawList.AddLine(new Vector2(center.X - len, center.Y), new Vector2(center.X + len, center.Y), borderCol, t);
                                // Vertical
                                drawList.AddLine(new Vector2(center.X, center.Y - len), new Vector2(center.X, center.Y + len), borderCol, t);
                            }
                            break;
                        case SelectionIndicatorShape.Homeworld:
                            {
                                // Base inset and corner length
                                float insetScale = MathF.Max(0.1f, _hwInsetScale);
                                float cornerScale = MathF.Max(0.1f, _hwCornerLengthScale);
                                if (_indicatorAnimate)
                                {
                                    float phase = _shaderTime * (MathF.PI * 2f) * MathF.Max(0.0f, _indicatorAnimSpeed);
                                    float sin = MathF.Sin(phase);
                                    insetScale *= (1f + _indicatorAnimInsetAmp * sin);
                                    cornerScale *= (1f + _indicatorAnimCornerAmp * sin);
                                }
                                float r = pixelRadius * insetScale;
                                // Ensure length is not larger than r for aesthetics
                                float len = Math.Max(1.0f, Math.Min(r, r * cornerScale));
                                float t = MathF.Max(1.0f, _indicatorThickness);
                                float left = center.X - r;
                                float right = center.X + r;
                                float top = center.Y - r;
                                float bottom = center.Y + r;

                                // Top-left corner
                                drawList.AddLine(new Vector2(left, top), new Vector2(left + len, top), borderCol, t);
                                drawList.AddLine(new Vector2(left, top), new Vector2(left, top + len), borderCol, t);
                                // Top-right corner
                                drawList.AddLine(new Vector2(right, top), new Vector2(right - len, top), borderCol, t);
                                drawList.AddLine(new Vector2(right, top), new Vector2(right, top + len), borderCol, t);
                                // Bottom-left corner
                                drawList.AddLine(new Vector2(left, bottom), new Vector2(left + len, bottom), borderCol, t);
                                drawList.AddLine(new Vector2(left, bottom), new Vector2(left, bottom - len), borderCol, t);
                                // Bottom-right corner
                                drawList.AddLine(new Vector2(right, bottom), new Vector2(right - len, bottom), borderCol, t);
                                drawList.AddLine(new Vector2(right, bottom), new Vector2(right, bottom - len), borderCol, t);
                            }
                            break;
                    }
                }
                catch { /* continue with next */ }
            }

            // If user Alt+Left-clicked within any selection indicator, zoom/focus to that entity.
            if (altLeftClick && zoomTarget.HasValue && zoomTarget.Value != default)
            {
                // Compute a pleasant camera distance based on size and FOV (similar to double-click code).
                const float targetScreenFraction = 0.35f;
                double distance = _cameraController.MinDistance;
                try
                {
                    if (zoomTarget.Value.Has<Size3D>())
                    {
                        var s = zoomTarget.Value.GetMut<Size3D>().Value;
                        double maxAxis = Math.Max(s.X, Math.Max(s.Y, s.Z));
                        double boundingRadius = Math.Max(0.01f, maxAxis * 0.6);
                        double fovHalfTan = Math.Tan(_camera.Fov * 0.5f);
                        if (fovHalfTan > 1e-6)
                        {
                            double fromSize = boundingRadius / (targetScreenFraction * fovHalfTan);
                            distance = Math.Max(fromSize, _cameraController.MinDistance);
                        }
                    }
                    else
                    {
                        // Fallback: derive from estimated collider/size radius if available
                        double fovHalfTan = Math.Tan(_camera.Fov * 0.5f);
                        if (fovHalfTan > 1e-6)
                        {
                            double fromSize = zoomTargetRadius / (targetScreenFraction * fovHalfTan);
                            distance = Math.Max(fromSize, _cameraController.MinDistance);
                        }
                    }
                }
                catch { }
                distance = Math.Clamp(distance, _cameraController.MinDistance, _cameraController.MaxDistance);

                // If the entity is dynamic, follow its world position provider; else focus on fixed position.
                try
                {
                    var ent = zoomTarget.Value;
                    if (ent.Has<PhysicsBody>())
                    {
                        var followEntity = ent; // capture
                        _cameraController.SetFocusProvider(() => GetEntityWorldPosition(followEntity), distance);
                    }
                    else
                    {
                        _cameraController.SetFocus(zoomTargetWorld, distance);
                    }
                }
                catch
                {
                    _cameraController.SetFocus(zoomTargetWorld, distance);
                }
            }
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

                // Components list removed per user request.
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

            World.RegisterSystem(new LightingSystem());

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
                    // Map pick result to an Entity? so the interaction service can notify handlers.
                    Entity? clickedEntity = null;
                    if (result.Collidable.Mobility == CollidableMobility.Static)
                    {
                        if (TryGetStaticRef(result.Collidable.StaticHandle, out var staticBody))
                        {
                            foreach (var e in World.Query(typeof(PhysicsStatic)))
                            {
                                if (e.GetMut<PhysicsStatic>().Handle.Equals(result.Collidable.StaticHandle))
                                {
                                    clickedEntity = e;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var bh = result.Collidable.BodyHandle;
                        foreach (var e in World.Query(typeof(PhysicsBody)))
                        {
                            if (e.GetMut<PhysicsBody>().Handle.Equals(bh))
                            {
                                clickedEntity = e;
                                break;
                            }
                        }
                    }

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

                    // Notify interaction service about this click (if any entity was associated)
                    _mouseInteraction.NotifyClick(clickedEntity);
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

            // Toggle interpolation (I)
            _inputManager.RegisterKeyPressed(KeyCode.I, () =>
            {
                _interpolationEnabled = !_interpolationEnabled;
                Console.WriteLine($"[Interpolation] Enabled: {_interpolationEnabled}");
            }, "ToggleInterpolation");

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
            _blackTexture = AssetManager.CreateSolidTexture(this, 0, 0, 0, 255);

            _checkerTexture = AssetManager.CreateCheckerboardTexture(this, 256, 256, 8,
                (230, 230, 230, 255), (40, 40, 40, 255));



            _defaultVertexInput = VertexInputState.CreateSingleBinding<Vertex>(0);

            // Create pipelines using the factory
            _pipeline = _pipelineFactory!.CreateStandard3DPipeline(vs, fs, _defaultVertexInput, "Basic3DObjectRenderer");

            // Dedicated skybox pipeline: no depth write/test, no culling (render inside of sphere)
            _skyboxPipeline = _pipelineFactory!.CreateSkyboxPipeline(vs, fs, _defaultVertexInput, "SkyboxRenderer");

            _imguiPipeline = _pipelineFactory!.CreateImGuiPipeline(vs, fs, _defaultVertexInput, "ImGuiRenderer");

            _skyboxCubePipeline = _pipelineFactory!.CreatePipeline("SkyboxCubeRenderer")
                .WithShaders(vsSkyCube, fsSkyCube)
                .WithVertexInput(_defaultVertexInput)
                .WithRasterizer(RasterizerState.CCW_CullNone)
                .WithDepthTesting(false, false)
                .WithBlendState(ColorTargetBlendState.NoBlend)
                .Build();

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
            _linePipeline = _pipelineFactory!.CreateLinePipeline(vsLine, fsLine, lineVertexInput, "LineColorRenderer");

            _lineBatch = new LineBatch3D(GraphicsDevice, 409600);


            World.OnPreDestroy((Entity entity) =>
            {
                if (entity.Has<GpuMesh>())
                {
                    entity.GetMut<GpuMesh>().Dispose();
                }
            });

            World.OnSetPost((Entity entity, in Mesh _, in Mesh mesh, bool _) =>
            {
                entity.Set(GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "Cube"));
            });


            // Weak outer star lights: primary above/below plus several subtle fills
            var outerStarLightAbove = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(-0.3f, -1.0f, -0.2f)),
                    new Color(220, 215, 200, 255),
                    0.0001f
                ));

            var outerStarLightBelow = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(0.3f, 1.0f, 0.2f)),
                    new Color(220, 215, 200, 255),
                    0.0001f
                ));

            // Additional faint directional fills to simulate distant starlight / ambient glow
            var outerStarLightNE = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(-0.6f, -0.7f, -0.1f)),
                    new Color(200, 210, 230, 255),
                    0.00005f
                ));

            var outerStarLightNW = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(0.6f, -0.7f, -0.1f)),
                    new Color(230, 225, 210, 255),
                    0.00005f
                ));

            var outerStarLightSE = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(-0.4f, 0.8f, 0.3f)),
                    new Color(200, 215, 200, 255),
                    0.00003f
                ));

            var outerStarLightSW = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(0.4f, 0.8f, 0.3f)),
                    new Color(210, 200, 215, 255),
                    0.00003f
                ));

            // Optional cool-tinted moonlike fill
            var outerStarLightCool = World.CreateEntity()
                .Set(new DirectionalLight(
                    Vector3.Normalize(new Vector3(0.0f, -1.0f, 0.5f)),
                    new Color(200, 220, 255, 255),
                    0.00002f
                ));

            // Create the default star system at world origin (extracted to a helper to allow multiple spawns)
            CreateStarSystem(new Vector3(0f, 0f, 0f), 80.0f);

            SpawnGalaxies(5);

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
            var tex = AssetManager.LoadTextureFromFile(this, path, sRGB: true);
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
                                                            // Put sun on a different collision layer than asteroids so they won't collide.
                .Set(new CollisionShape(new Sphere(10.0f * 0.5f)))
                .Set(PredefinedMaterials.Silver)
                .Set(new Components.Shader(this, "Sun", true) { Plan = Components.Shader.BindingPlan.UnlitEmissiveSun })
                .Set(new PointLight(Color.LightYellow, 0.01f, 3250f, 1.0f, 0.0014f, 0.000007f))
                /*
                Category = what the object is. Here the entity is a Sun (its category bit = Sun).
                
                Mask = which categories this entity wants the engine to notify it about. CollisionCategory.None means the Sun does not want to receive collision callbacks for any category.
                
                The engine treats contacts as something either side may request, but only notifies the side(s) that actually requested the contact (directed events).
                */

                .Set(CollisionCategory.Sun.ToLayer(CollisionCategory.None))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Sun.jpg", sRGB: true) ?? _checkerTexture! });

            sun.OnSelection(_selectionInteraction, (Entity e) =>
            {
                Console.WriteLine("[Selection] Selected entity Sun, playing audio");
                AudioManager.PlayOneShot(this, AssetManager.AssetFolderName + "/Sun.ogg");
            });

            sun.OnDeselection(_selectionInteraction, (Entity e) =>
            {
                Console.WriteLine("[Selection] Deselected entity Sun");
            });

            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(0.2f, 0, 0).X, AuToWorld(0.2f, 0, 0).Y, AuToWorld(0.2f, 0, 0).Z)) // distance = 0.39 AU
                .Set(new Size3D(0.38f)) // size = 0.38x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.38f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.1f, 0f)) // speed relative to Earth (fastest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! })
                .Set(new Components.Shader(this, "Light", true));

            World.CreateEntity()
                .Set(new Position3D(origin.X + 2, origin.Y + 8, origin.Z + 2))
                .Set(Mesh.CreateBox3D())
                .Set(new Size3D(2f))
                // Try loading a per-entity diffuse map from the Asset folder. If it fails, fall back to the checker texture.
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/diffuseMapExample.png", sRGB: true) ?? _checkerTexture! })
                // Attach a specular map for per-pixel specular control (falls back to white texture meaning "full specular")
                .Set(new SpecularMapRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/specularMapExample.png") ?? _whiteTexture! });

            World.CreateEntity()
                .Set(new Position3D(10 + origin.X, origin.Y, 5 + origin.Z))
                .Set(Mesh.CreateBox3D())
                .Set(new Size3D(2f))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            World.CreateEntity()
                .Set(new Position3D(origin.X, origin.Y, origin.Z + 30))
                .Set(Mesh.CreateBox3D())
                .Set(new Size3D(3f))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            World.CreateEntity()
                .Set(new Position3D(origin.X, origin.Y, origin.Z + 20))
                .Set(Mesh.CreateBox3D())
                .Set(new Size3D(1f))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(origin.X + 5, origin.Y, origin.Z + 5))
                .Set(new Size3D(1.0f)) // Artistically scaled size
                .Set(Rotation3D.Identity)
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            sun.OnCollisionEnter(_collisionInteraction, (Entity self, Entity other) =>
            {
                Console.WriteLine($"[Collision] Self: {self.Id}, Other: {other.Id}, This should not trigger because the sun does not want to collide with anything and should ignore any collision!");
            });

            var asteroidA = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateAsteroid3D())
                .Set(new Position3D(origin.X + 10, origin.Y, origin.Z))
                .Set(new Size3D(1.0f)) // Artistically scaled size
                .Set(Rotation3D.Identity)
                .Set(new Velocity3D(-0.5, 0, 0))
                .Set(new AngularVelocity3D(0f, 0.001f, 0f)) // Slow rotation for effect
                .Set(new CollisionShape(new Sphere(1.0f * 0.5f)))
                .Set(CollisionCategory.Asteroid.ToLayer(CollisionCategory.Sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Moon.jpg", sRGB: true) ?? _checkerTexture! });

            var asteroidBDifferentPhysicsLayer = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateAsteroid3D())
                .Set(new Position3D(origin.X - 10, origin.Y, origin.Z))
                .Set(new Size3D(1.0f)) // Artistically scaled size
                .Set(Rotation3D.Identity)
                .Set(new Velocity3D(0.5, 0, 0))
                .Set(new AngularVelocity3D(0f, 0.001f, 0f)) // Slow rotation for effect
                .Set(new CollisionShape(new Sphere(1.0f * 0.5f)))
                .Set(CollisionCategory.Asteroid.ToLayer(CollisionCategory.None))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Moon.jpg", sRGB: true) ?? _checkerTexture! });


            /*

            When AsteroidA overlaps Sun:
            Physics contact may be generated and resolved (because AsteroidA wanted it).
            AsteroidA’s OnCollisionEnter handler will be invoked with (self=AsteroidA, other=Sun).
            Sun’s OnCollisionEnter will NOT be invoked.
            
            When AsteroidB overlaps Sun:
            No contact generated (neither side requested), nothing happens. 
            */
            // Should not trigger!
            asteroidBDifferentPhysicsLayer.OnCollisionEnter(_collisionInteraction, (Entity self, Entity other) =>
            {
                self.Destroy();
                Console.WriteLine($"[Collision] Self: {self.Id}, Other: {other.Id}, destroying asteroid! THIS SHOULD NOT HAPPEN!");
            });

            asteroidA.OnCollisionEnter(_collisionInteraction, (Entity self, Entity other) =>
            {
                self.Destroy();
                Console.WriteLine($"[Collision] Self: {self.Id}, Other: {other.Id}, destroying asteroid!");
            });

            // Example collision handler: spawn a similar sphere at the same position when the sun collides with anything
            /*

            asteroid.OnCollisionExit(_collisionInteraction, (Entity self, Entity other) =>
            {
                Console.WriteLine($"[Collision] Self: {self.Id}, Other: {other.Id}, exiting asteroid!");
            });


            asteroid.OnCollisionStay(_collisionInteraction, (Entity self, Entity other) =>
            {
                Console.WriteLine($"[Collision] Self: {self.Id}, Other: {other.Id}, staying asteroid!");
            });


            sun.OnMouseEnter(_mouseInteraction, (Entity e) =>
            {
                Console.WriteLine($"[Mouse] Entered Sun entity {e.Id}");
            });

            sun.OnMouseExit(_mouseInteraction, (Entity e) =>
              {
                  Console.WriteLine($"[Mouse] Exited Sun entity {e.Id}");
              });

            sun.OnClick(_mouseInteraction, (Entity e) =>
              {
                  Console.WriteLine($"[Mouse] Clicked Sun entity {e.Id}");
              });
            */

            // Mercury: 0.39 AU from the Sun.
            var mercury = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(AuToWorld(0.39f, 0, 0).X, AuToWorld(0.39f, 0, 0).Y, AuToWorld(0.39f, 0, 0).Z)) // distance = 0.39 AU
                .Set(new Size3D(0.38f)) // size = 0.38x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.38f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.0048f, 0f)) // speed relative to Earth (fastest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Mercury.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.0016f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Venus.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.001f, 0f)) // speed = baseline
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Earth.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.0012f, 0f))
                .Set(new Parent(earth))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Moon.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.0018f, 0f))
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
                .Set(new AngularVelocity3D(0f, 0.0011f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Mars.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.009f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Jupiter.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.0016f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Saturn.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.0025f, 0f)) // speed relative to Earth
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
                .Set(new AngularVelocity3D(0f, 0.008f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Neptune.jpg", sRGB: true) ?? _checkerTexture! });

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
                .Set(new AngularVelocity3D(0f, 0.005f, 0f)) // speed relative to Earth (slowest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            pluto.Set(new OrbitCircle(sun, 39.48f * auScale, new Color(200, 180, 200, 96), segments: 160));

            // Classic main-belt style asteroid belt between Mars and Jupiter (~2.2 - 3.2 AU)
            SpawnAsteroidBelt(origin,
                parent: sun,
                count: 20,
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
        /// Creates a randomized asteroid mesh with varying parameters for visual diversity.
        /// </summary>
        /// <param name="rng">Random number generator to use for consistency</param>
        /// <param name="baseSize">Base size of the asteroid for scaling parameters</param>
        /// <returns>A randomly generated asteroid mesh</returns>
        private Mesh CreateRandomizedAsteroid(Random rng, float baseSize)
        {
            // Define different asteroid types with different characteristics
            var asteroidType = rng.Next(5); // 5 different asteroid types

            float radius = 0.5f; // Base radius before size scaling
            int radialSegments = 32 + rng.Next(33); // 32-64 segments
            int rings = 16 + rng.Next(17); // 16-32 rings
            int seedValue = rng.Next(int.MaxValue);

            switch (asteroidType)
            {
                case 0: // Smooth, lightly cratered asteroid
                    return Mesh.CreateAsteroid3D(
                        radius: radius,
                        radialSegments: radialSegments,
                        rings: rings,
                        seed: seedValue,
                        frequency: 0.2f + ((float)rng.NextDouble() * 0.3f), // 0.2-0.5
                        perturbationStrength: 0.15f + ((float)rng.NextDouble() * 0.15f), // 0.15-0.3
                        roughness: 0.3f + ((float)rng.NextDouble() * 0.4f), // 0.3-0.7
                        craterDensity: 0.1f + ((float)rng.NextDouble() * 0.2f) // 0.1-0.3
                    );

                case 1: // Heavily cratered, rough asteroid
                    return Mesh.CreateAsteroid3D(
                        radius: radius,
                        radialSegments: radialSegments,
                        rings: rings,
                        seed: seedValue,
                        frequency: 0.4f + ((float)rng.NextDouble() * 0.4f), // 0.4-0.8
                        perturbationStrength: 0.3f + ((float)rng.NextDouble() * 0.3f), // 0.3-0.6
                        roughness: 0.7f + ((float)rng.NextDouble() * 0.3f), // 0.7-1.0
                        craterDensity: 0.4f + ((float)rng.NextDouble() * 0.4f) // 0.4-0.8
                    );

                case 2: // Very irregular, jagged asteroid
                    return Mesh.CreateAsteroid3D(
                        radius: radius,
                        radialSegments: radialSegments,
                        rings: rings,
                        seed: seedValue,
                        frequency: 0.6f + ((float)rng.NextDouble() * 0.6f), // 0.6-1.2
                        perturbationStrength: 0.4f + ((float)rng.NextDouble() * 0.4f), // 0.4-0.8
                        roughness: 0.8f + ((float)rng.NextDouble() * 0.2f), // 0.8-1.0
                        craterDensity: 0.2f + ((float)rng.NextDouble() * 0.3f) // 0.2-0.5
                    );

                case 3: // Metallic-looking, less cratered asteroid
                    return Mesh.CreateAsteroid3D(
                        radius: radius,
                        radialSegments: radialSegments,
                        rings: rings,
                        seed: seedValue,
                        frequency: 0.3f + ((float)rng.NextDouble() * 0.3f), // 0.3-0.6
                        perturbationStrength: 0.2f + ((float)rng.NextDouble() * 0.2f), // 0.2-0.4
                        roughness: 0.4f + ((float)rng.NextDouble() * 0.3f), // 0.4-0.7
                        craterDensity: 0.05f + ((float)rng.NextDouble() * 0.15f) // 0.05-0.2
                    );

                case 4: // Medium complexity asteroid
                default:
                    return Mesh.CreateAsteroid3D(
                        radius: radius,
                        radialSegments: radialSegments,
                        rings: rings,
                        seed: seedValue,
                        frequency: 0.25f + ((float)rng.NextDouble() * 0.5f), // 0.25-0.75
                        perturbationStrength: 0.25f + ((float)rng.NextDouble() * 0.25f), // 0.25-0.5
                        roughness: 0.5f + ((float)rng.NextDouble() * 0.3f), // 0.5-0.8
                        craterDensity: 0.2f + ((float)rng.NextDouble() * 0.3f) // 0.2-0.5
                    );
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
            var tex = AssetManager.LoadTextureFromFile(this, AssetManager.AssetFolderName + "/Moon.jpg", sRGB: true) ?? _checkerTexture!;

            // We'll create different asteroid types per entity instead of using a shared mesh

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

                // Create randomized asteroid mesh with varying parameters
                var asteroidMesh = CreateRandomizedAsteroid(rng, s);

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(asteroidMesh)
                    .Set(new Kinematic())
                    .Set(new Parent(parent))
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(new CollisionShape(new Sphere(s * 0.5f)))
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

            // Per-frame hover: pick under cursor and notify mouse interaction service so
            // OnMouseEnter / OnMouseExit callbacks are triggered automatically.
            try
            {
                Entity? hoveredEntity = null;
                if (_mousePicker.Pick(Inputs.Mouse, (int)MainWindow.Width, (int)MainWindow.Height, out var hoverResult))
                {
                    if (hoverResult.Collidable.Mobility == CollidableMobility.Static)
                    {
                        if (TryGetStaticRef(hoverResult.Collidable.StaticHandle, out var staticBody))
                        {
                            foreach (var e in World.Query(typeof(PhysicsStatic)))
                            {
                                if (e.GetMut<PhysicsStatic>().Handle.Equals(hoverResult.Collidable.StaticHandle))
                                {
                                    hoveredEntity = e;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var bh = hoverResult.Collidable.BodyHandle;
                        foreach (var e in World.Query(typeof(PhysicsBody)))
                        {
                            if (e.GetMut<PhysicsBody>().Handle.Equals(bh))
                            {
                                hoveredEntity = e;
                                break;
                            }
                        }
                    }
                }

                _mouseInteraction.NotifyHover(hoveredEntity);
            }
            catch { }

            _angle += (float)delta.TotalSeconds * 0.7f;
            // Keep camera aspect up-to-date on resize
            _camera.Aspect = (float)((float)MainWindow.Width / MainWindow.Height);
            // Camera update moved to AFTER the physics timestep so any live-follow providers
            // sample the physics bodies' post-step poses. This prevents a one-frame lag
            // when the simulation time scale is increased which caused the camera to
            // trail the physics objects and accumulate an offset.

            // While our camera updates in drawing our rebase is done in the update loop
            if (_floatingOriginManager != null && _floatingOriginManager.Update(_camera.Position, out var rebaseOffset))
            {
                // Keep the camera near origin too so it doesn't immediately trigger another rebase
                _camera.Position -= rebaseOffset;
                _cameraController.ApplyOriginShift(rebaseOffset);
            }

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
            foreach (var entity in World.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(RenderPosition3D)))
            {
                var body = entity.GetMut<PhysicsBody>();
                var pos = entity.GetMut<RenderPosition3D>().Value;
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
                // Snapshot entity positions BEFORE running physics/world update for this tick
                AyanamisTower.StellaEcs.StellaInvicta.Systems.InterpolationSystems.SnapshotPositions(World);

                // Step physics with a fixed, deterministic timestep
                _physicsManager.Step((float)_fixedSimulationStepSeconds);

                // Advance the ECS world by the same fixed timestep
                World.Update((float)_fixedSimulationStepSeconds);

                _simulationAccumulator -= _fixedSimulationStepSeconds;
            }


            // NOTE: World.Update was already called inside the fixed-step loop above.
            // Input checks (F1,F2, mouse pick, culling toggles) are registered with _inputManager in InitializeScene
            // Camera updates and floating-origin rebasing are performed per-render in Draw() so the camera
            // receives the render-frame delta (not the fixed simulation delta) and remains smooth even when
            // the simulation runs at a lower fixed-step rate.

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
            _shaderTime += (float)dt;
            if (_fpsTimer >= 0.5f)
            {
                var avgFps = _fpsFrames / Math.Max(_fpsTimer, 1e-6f);
                var ms = 1000.0 / Math.Max(avgFps, 0.0001);
                MainWindow.SetTitle($"{_baseTitle} | FPS {avgFps:0} ({ms:0.0} ms) | F11 Fullscreen | Esc Quit");
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }
            // Interpolate render positions using leftover accumulator fraction so visuals are smooth
            try
            {
                double alphaInterp = Math.Clamp(_simulationAccumulator / _fixedSimulationStepSeconds, 0.0, 1.0);
                if (_interpolationEnabled)
                {
                    AyanamisTower.StellaEcs.StellaInvicta.Systems.InterpolationSystems.InterpolateRenderPositions(World, alphaInterp);
                }
                else
                {
                    // If interpolation is disabled, copy current simulation positions to render positions
                    AyanamisTower.StellaEcs.StellaInvicta.Systems.InterpolationSystems.SetRenderPositionsToCurrent(World);
                }
            }
            catch { }


            foreach (var entity in World.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(RenderPosition3D)))
            {
                var body = entity.GetMut<PhysicsBody>();
                var pos = entity.GetMut<RenderPosition3D>().Value;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    bodyRef.Pose = new RigidPose(pos, rot);
                    bodyRef.Awake = true;
                    _physicsManager.Simulation.Bodies.UpdateBounds(body.Handle);
                }
            }

            // Update camera using render-frame delta so camera movement is smooth regardless of simulation step rate
            try
            {
                // Convert dt to a TimeSpan-like delta used by the controller Update signature
                var renderDelta = TimeSpan.FromSeconds(dt);
                _cameraController.Update(Inputs, MainWindow, renderDelta);

                // Update floating origin system (check if rebase is needed) using camera's post-update position

            }
            catch { }
            // Compute single-precision view/projection and extract frustum once per frame.
            var viewDouble = _camera.GetViewMatrix();
            var projDouble = _camera.GetProjectionMatrix();
            // (interpolation already performed earlier so rendering can use RenderPosition3D)
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
                    foreach (var e in World.Query(typeof(RenderPosition3D)))
                    {
                        var ent = e; // capture
                        var pos = ent.Has<RenderPosition3D>() ? ent.GetMut<RenderPosition3D>().Value : Vector3Double.Zero;

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
                    foreach (var e in World.Query(typeof(DebugAxes), typeof(RenderPosition3D)))
                    {
                        var ent = e; // capture
                        var pos = ent.GetMut<RenderPosition3D>().Value;

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
                colorTarget.StoreOp = StoreOp.Store; // Store for first pass
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

                // Draw selection rectangle overlay on top of any windows
                RenderSelectionRectangleImGui();

                ImGui.Render();
                drawData = ImGui.GetDrawData();
                ImGuiImplSDL3.SDLGPU3PrepareDrawData(drawData, (SDLGPUCommandBuffer*)cmdbuf.Handle);
            }

            var lightingSystem = World.GetSystemsWithOrder().FirstOrDefault(s => s.system is LightingSystem).system as LightingSystem;

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
                pass.BindFragmentSamplers(0, [new(_skyboxTexture, skySampler)]);

                // Skybox should follow the camera; when using camera-relative rendering the view translation
                // has been removed so placing the skybox at the origin keeps it centered on the camera.
                var modelSky = Matrix4x4.CreateScale(_skyboxScale);
                var mvpSky = modelSky * viewProj;
                cmdbuf.PushVertexUniformData(mvpSky, slot: 0);
                pass.DrawIndexedPrimitives(skyMesh.IndexCount, 1, 0, 0, 0);
                skySampler.Dispose();
            }

            pass.BindGraphicsPipeline(_pipeline!);

            // We'll bind global uniforms (like lights) lazily per-pipeline only if the active shader needs them.
            // Track which pipelines have received their global bindings for this frame.
            var globalsBoundForPipeline = new System.Collections.Generic.HashSet<GraphicsPipeline>();

            foreach (var entity in World.Query(typeof(GpuMesh)))
            {
                var gpuMesh = entity.GetMut<GpuMesh>();
                // Gather transform components first (needed for culling, avoids binding for culled)
                Vector3 translation = entity.Has<RenderPosition3D>() ? entity.GetMut<RenderPosition3D>().Value : Vector3.Zero;
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

                // If entity defines a custom shader, prefer its pipeline. Build it on-demand.
                if (entity.Has<Components.Shader>())
                {
                    var s = entity.GetMut<Components.Shader>();
                    if (s.Pipeline == null)
                    {
                        // Build pipeline with defaults for this shader using our factory and the default vertex layout
                        if (_pipelineFactory != null)
                        {
                            s.BuildOrUpdatePipeline(_pipelineFactory, _defaultVertexInput, s.Name + "_Pipeline");
                        }
                    }

                    if (s.Pipeline != null)
                    {
                        pass.BindGraphicsPipeline(s.Pipeline);
                    }
                    else
                    {
                        pass.BindGraphicsPipeline(_pipeline!);
                    }
                }
                else
                {
                    pass.BindGraphicsPipeline(_pipeline!);
                }

                // Determine active pipeline and optional binding plan from the shader component.
                GraphicsPipeline activePipeline = _pipeline!;
                AyanamisTower.StellaEcs.StellaInvicta.Components.Shader.BindingPlan? plan = null;
                if (entity.Has<Components.Shader>())
                {
                    var s = entity.GetMut<Components.Shader>();
                    if (s.Pipeline != null) { activePipeline = s.Pipeline; }
                    plan = s.Plan; // may be null if not configured; we'll fallback below
                    if (plan == null && (s.Name?.IndexOf("sun", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        plan = Components.Shader.BindingPlan.UnlitEmissiveSun;
                        s.Plan = plan;
                    }
                }
                // Fallback plan assumes legacy behavior (needs lights, material, vertex uniforms, diffuse+specular)
                plan ??= AyanamisTower.StellaEcs.StellaInvicta.Components.Shader.BindingPlan.LegacyDefault;

                // Bind global lighting uniforms only if needed and not yet bound for this pipeline
                if (!globalsBoundForPipeline.Contains(activePipeline) && lightingSystem != null && lightingSystem.HasLights)
                {
                    // Directional lights
                    if (plan.NeedsDirectionalLights)
                    {
                        var gpuDir = new GpuDirectionalLight[lightingSystem.DirectionalLightCount];
                        int usedDir = 0;
                        for (int i = 0; i < lightingSystem.DirectionalLightCount && usedDir < lightingSystem.DirectionalLightCount; i++)
                        {
                            var sdir = lightingSystem.DirectionalLights[i];
                            if (sdir.Intensity <= 0f) continue;
                            var gd = new GpuDirectionalLight
                            {
                                Direction = Vector3.Normalize(sdir.Direction),
                                Color = sdir.Color,
                                Intensity = sdir.Intensity
                            };
                            gpuDir[usedDir++] = gd;
                        }
                        for (int i = usedDir; i < gpuDir.Length; i++) gpuDir[i] = new GpuDirectionalLight();
                        unsafe
                        {
                            fixed (GpuDirectionalLight* p = gpuDir)
                            {
                                cmdbuf.PushFragmentUniformData(p, (uint)(Marshal.SizeOf<GpuDirectionalLight>() * gpuDir.Length), slot: (uint)plan.FragmentSlotOrDefault("DirectionalLights", 0));
                            }
                        }
                    }

                    // Point lights
                    if (plan.NeedsPointLights)
                    {
                        var gpuPoint = new GpuPointLight[60];
                        int usedPoint = 0;
                        for (int i = 0; i < lightingSystem.PointLightCount && usedPoint < gpuPoint.Length; i++)
                        {
                            var spl = lightingSystem.PointLights[i];
                            if (spl.Intensity <= 0f) continue;
                            var gp = new GpuPointLight
                            {
                                Position = lightingSystem.PointLightPositions[i],
                                Color = spl.Color,
                                Intensity = spl.Intensity,
                                Range = spl.Range,
                                Kc = spl.Constant,
                                Kl = spl.Linear,
                                Kq = spl.Quadratic
                            };
                            gpuPoint[usedPoint++] = gp;
                        }
                        for (int i = usedPoint; i < gpuPoint.Length; i++) gpuPoint[i] = new GpuPointLight();
                        unsafe
                        {
                            fixed (GpuPointLight* p = gpuPoint)
                            {
                                cmdbuf.PushFragmentUniformData(p, (uint)(Marshal.SizeOf<GpuPointLight>() * gpuPoint.Length), slot: (uint)plan.FragmentSlotOrDefault("PointLights", 1));
                            }
                        }
                    }

                    // Spot lights
                    if (plan.NeedsSpotLights)
                    {
                        var gpuSpot = new GpuSpotLight[16];
                        int usedSpot = 0;
                        for (int i = 0; i < lightingSystem.SpotLightCount && usedSpot < gpuSpot.Length; i++)
                        {
                            var sspot = lightingSystem.SpotLights[i];
                            if (sspot.Intensity <= 0f) continue;
                            var gs = new GpuSpotLight
                            {
                                Position = lightingSystem.SpotLightPositions[i],
                                Direction = Vector3.Normalize(sspot.Direction),
                                Color = sspot.Color,
                                Range = sspot.Range,
                                InnerAngle = sspot.InnerAngle,
                                OuterAngle = sspot.OuterAngle,
                                Kc = sspot.Constant,
                                Kl = sspot.Linear,
                                Kq = sspot.Quadratic
                            };
                            gpuSpot[usedSpot++] = gs;
                        }
                        for (int i = usedSpot; i < gpuSpot.Length; i++) gpuSpot[i] = new GpuSpotLight();
                        unsafe
                        {
                            fixed (GpuSpotLight* p = gpuSpot)
                            {
                                cmdbuf.PushFragmentUniformData(p, (uint)(Marshal.SizeOf<GpuSpotLight>() * gpuSpot.Length), slot: (uint)plan.FragmentSlotOrDefault("SpotLights", 2));
                            }
                        }
                    }

                    // Light counts
                    if (plan.NeedsLightCounts)
                    {
                        var counts = new LightCountsUniform
                        {
                            directionalLightCount = (uint)lightingSystem.DirectionalLightCount,
                            pointLightCount = (uint)lightingSystem.PointLightCount,
                            spotLightCount = (uint)lightingSystem.SpotLightCount
                        };
                        cmdbuf.PushFragmentUniformData(in counts, slot: (uint)plan.FragmentSlotOrDefault("LightCounts", 3));
                    }

                    globalsBoundForPipeline.Add(activePipeline);
                }

                // Bind entity textures only if the shader expects them
                if (plan.NeedsDiffuseTexture)
                {
                    var diffuseTex = _whiteTexture!;
                    if (entity.Has<Texture2DRef>())
                    {
                        var texRef = entity.GetMut<Texture2DRef>();
                        if (texRef.Texture != null) { diffuseTex = texRef.Texture; }
                    }
                    pass.BindFragmentSamplers(0, [new TextureSamplerBinding(diffuseTex, GraphicsDevice.LinearSampler)]);
                }

                if (plan.NeedsSpecularTexture)
                {
                    var specularTex = _blackTexture!; // black -> no specular by default
                    if (entity.Has<SpecularMapRef>())
                    {
                        var specRef = entity.GetMut<SpecularMapRef>();
                        if (specRef.Texture != null) { specularTex = specRef.Texture; }
                    }
                    pass.BindFragmentSamplers(1, [new TextureSamplerBinding(specularTex, GraphicsDevice.LinearSampler)]);
                }

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

                // Create struct for vertex uniforms
                // Model = camera-relative model (used for rendering to avoid floating-origin issues)
                // ModelWorld = full world-space model (used for shadow sampling so shadow coords match depth pass)
                var vertexUniforms = new VertexUniforms
                {
                    MVP = mvp,
                    Model = model,
                    ModelWorld = Matrix4x4.CreateScale(size) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation),
                    // Camera position in world space (used by shader for correct viewDir)
                    CameraPosition = HighPrecisionConversions.ToVector3(_camera.Position)
                };

                if (plan.NeedsVertexUniforms)
                {
                    cmdbuf.PushVertexUniformData(in vertexUniforms, slot: (uint)plan.VertexSlotOrDefault("VertexUniforms", 0));
                }

                // Push per-entity material into fragment slot 4 (MaterialProperties cbuffer b4, space3)
                // Use the entity's Material component if present, otherwise fall back to a predefined default.
                MaterialPropertiesUniform matUniform;
                if (entity.Has<Components.Material>())
                {
                    var matComp = entity.GetMut<Components.Material>();
                    matUniform = new MaterialPropertiesUniform { material = matComp };
                }
                else
                {
                    matUniform = new MaterialPropertiesUniform { material = PredefinedMaterials.Default };
                }
                if (plan.NeedsMaterial)
                {
                    cmdbuf.PushFragmentUniformData(in matUniform, slot: (uint)plan.FragmentSlotOrDefault("Material", 4));
                }

                // Custom PS params for unlit shaders like the Sun
                if (plan.NeedsCustomPSParams)
                {
                    {

                        //cmdbuf.PushFragmentUniformData(in sun, slot: (uint)plan.FragmentSlotOrDefault("PSParams", 0));

                    }
                }

                pass.DrawIndexedPrimitives(gpuMesh.IndexCount, 1, 0, 0, 0);
            }

            // Draw lines last so they overlay geometry
            if (_lineBatch != null)
            {
                pass.BindGraphicsPipeline(_linePipeline!);
                _lineBatch.Render(pass, viewProj);
            }

            cmdbuf.EndRenderPass(pass);

            // Second render pass for ImGui (no depth)
            colorTarget.LoadOp = LoadOp.Load; // Load the contents from first pass
            if (_msaaColor != null)
            {
                colorTarget.StoreOp = StoreOp.ResolveAndStore; // Resolve in second pass
            }
            var imguiPass = cmdbuf.BeginRenderPass(colorTarget); // No depth target

            imguiPass.BindGraphicsPipeline(_imguiPipeline!);

            if (_imguiEnabled && drawData != null)
            {
                ImGuiImplSDL3.SDLGPU3RenderDrawData(drawData, (SDLGPUCommandBuffer*)cmdbuf.Handle, (SDLGPURenderPass*)imguiPass.Handle, null);
            }

            cmdbuf.EndRenderPass(imguiPass);


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

        /// <summary>
        /// Renders a semi-transparent blue selection rectangle using ImGui's foreground draw list.
        /// Captures mouse drag in screen space and only draws while the left button is held.
        /// </summary>
        private void RenderSelectionRectangleImGui()
        {
            // Ensure ImGui is active
            if (!_imguiEnabled) return;

            var ioLocal = ImGui.GetIO();

            // If UI wants the mouse, don't start or continue a world selection drag
            // (still allow drawing if already dragging and not over UI, tweak as desired)
            bool wantCapture = ioLocal.WantCaptureMouse;

            bool leftClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
            bool leftDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool leftReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            // Begin drag
            if (!_isDragSelecting && leftClicked && !wantCapture)
            {
                _isDragSelecting = true;
                _dragStartScreen = ImGui.GetMousePos();
                _dragEndScreen = _dragStartScreen;
            }

            // Update drag
            if (_isDragSelecting)
            {
                _dragEndScreen = ImGui.GetMousePos();

                // End drag
                if (!leftDown || leftReleased)
                {
                    _isDragSelecting = false;
                    // Evaluate selection when drag ends
                    TryEvaluateSelectionRectangle(_dragStartScreen, _dragEndScreen);
                }
            }

            // Draw rectangle only while dragging and when it has a visible area
            if (_isDragSelecting)
            {
                float x0 = MathF.Min(_dragStartScreen.X, _dragEndScreen.X);
                float y0 = MathF.Min(_dragStartScreen.Y, _dragEndScreen.Y);
                float x1 = MathF.Max(_dragStartScreen.X, _dragEndScreen.X);
                float y1 = MathF.Max(_dragStartScreen.Y, _dragEndScreen.Y);

                // Clamp to window bounds
                float w = MainWindow != null ? MainWindow.Width : 0;
                float h = MainWindow != null ? MainWindow.Height : 0;
                x0 = MathF.Max(0, MathF.Min(x0, w));
                y0 = MathF.Max(0, MathF.Min(y0, h));
                x1 = MathF.Max(0, MathF.Min(x1, w));
                y1 = MathF.Max(0, MathF.Min(y1, h));

                // Skip if tiny
                if (MathF.Abs(x1 - x0) > 1f && MathF.Abs(y1 - y0) > 1f)
                {
                    var pMin = new Vector2(x0, y0);
                    var pMax = new Vector2(x1, y1);

                    var drawList = ImGui.GetForegroundDrawList();

                    // Colors: translucent fill and solid-ish border
                    uint fillCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.55f, 1.0f, 0.20f));
                    uint borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.55f, 1.0f, 0.85f));

                    drawList.AddRectFilled(pMin, pMax, fillCol, 2f, ImDrawFlags.None);
                    drawList.AddRect(pMin, pMax, borderCol, 2f, ImDrawFlags.None, 2.0f);
                }
            }
        }

        /// <summary>
        /// On selection release, compute which entities intersect the screen rectangle and print them.
        /// </summary>
        private void TryEvaluateSelectionRectangle(Vector2 start, Vector2 end)
        {
            float x0 = MathF.Min(start.X, end.X);
            float y0 = MathF.Min(start.Y, end.Y);
            float x1 = MathF.Max(start.X, end.X);
            float y1 = MathF.Max(start.Y, end.Y);

            // Ignore tiny drags (treat as click)
            if (MathF.Abs(x1 - x0) < 3f || MathF.Abs(y1 - y0) < 3f)
            {
                return;
            }

            float screenW = (float)MainWindow.Width;
            float screenH = (float)MainWindow.Height;
            x0 = MathF.Max(0, MathF.Min(x0, screenW));
            y0 = MathF.Max(0, MathF.Min(y0, screenH));
            x1 = MathF.Max(0, MathF.Min(x1, screenW));
            y1 = MathF.Max(0, MathF.Min(y1, screenH));

            // Rebuild view-projection consistent with camera-relative rendering mode
            var viewD = _camera.GetViewMatrix();
            var projD = _camera.GetProjectionMatrix();
            var view = HighPrecisionConversions.ToMatrix(viewD);
            var proj = HighPrecisionConversions.ToMatrix(projD);
            if (_useCameraRelativeRendering) { view.Translation = Vector3.Zero; }
            var viewProj = view * proj;
            var camPos = HighPrecisionConversions.ToVector3(_camera.Position);
            float tanHalfFov = MathF.Tan((float)_camera.Fov * 0.5f);

            // Helper local functions
            static bool CircleIntersectsRect(float cx, float cy, float r, float rx0, float ry0, float rx1, float ry1)
            {
                float clampedX = MathF.Max(rx0, MathF.Min(cx, rx1));
                float clampedY = MathF.Max(ry0, MathF.Min(cy, ry1));
                float dx = cx - clampedX;
                float dy = cy - clampedY;
                return ((dx * dx) + (dy * dy)) <= (r * r);
            }

            var selected = new System.Collections.Generic.List<Entity>();

            foreach (var e in World.GetAllEntities())
            {
                // Find a position
                Vector3Double posD;
                if (e.Has<RenderPosition3D>()) posD = e.GetMut<RenderPosition3D>().Value;
                else if (e.Has<Position3D>()) posD = e.GetMut<Position3D>().Value;
                else if (e.Has<PhysicsBody>() && TryGetBodyRef(e.GetMut<PhysicsBody>().Handle, out var bodyRef)) posD = bodyRef.Pose.Position;
                else continue;

                // Estimate a radius
                float radius = 0.6f; // default small radius
                if (e.Has<Size3D>())
                {
                    var s = e.GetMut<Size3D>().Value;
                    radius = MathF.Max((float)s.X, MathF.Max((float)s.Y, (float)s.Z)) * 0.6f;
                }
                else if (e.Has<CollisionShape>())
                {
                    var cs = e.GetMut<CollisionShape>().Shape;
                    radius = cs switch
                    {
                        Sphere s => s.Radius,
                        Box b => MathF.Sqrt((b.HalfWidth * b.HalfWidth) + (b.HalfHeight * b.HalfHeight) + (b.HalfLength * b.HalfLength)),
                        Capsule c => c.HalfLength + c.Radius,// conservative
                        Cylinder cy => MathF.Sqrt((cy.HalfLength * cy.HalfLength) + (cy.Radius * cy.Radius)),
                        _ => 0.6f,
                    };
                }

                // Convert to render space and project to screen
                var pos = HighPrecisionConversions.ToVector3(posD);
                var renderPos = _useCameraRelativeRendering ? (pos - camPos) : pos;
                var center4 = new Vector4(renderPos, 1f);
                var clip = Vector4.Transform(center4, viewProj);
                if (clip.W <= 0f) continue; // behind camera

                var toObj = renderPos;
                float dist = toObj.Length();
                if (dist <= 1e-4f) dist = 1e-4f;
                float pixelRadius = (radius / (dist * MathF.Max(1e-4f, tanHalfFov))) * (screenH * 0.5f);

                var ndcX = clip.X / clip.W;
                var ndcY = clip.Y / clip.W;
                float sx = ((ndcX * 0.5f) + 0.5f) * screenW;
                float sy = (1f - ((ndcY * 0.5f) + 0.5f)) * screenH;

                if (CircleIntersectsRect(sx, sy, pixelRadius, x0, y0, x1, y1))
                {
                    selected.Add(e);
                }
            }

            if (selected.Count > 0)
            {
                var ids = new System.Text.StringBuilder();
                for (int i = 0; i < selected.Count; i++)
                {
                    if (i > 0) ids.Append(", ");
                    ids.Append('E'); ids.Append(selected[i].Id);
                }
                Console.WriteLine($"Selection rectangle hit {selected.Count} entities: {ids}");

                // Determine selection mode based on modifiers: Shift=Add, Ctrl=Subtract, none=Replace
                var ioLocal = ImGui.GetIO();
                var mode = SelectionInteractionService.SelectionMode.Replace;
                if (ioLocal.KeyShift) mode = SelectionInteractionService.SelectionMode.Add;
                else if (ioLocal.KeyCtrl) mode = SelectionInteractionService.SelectionMode.Subtract;

                _selectionInteraction.ApplySelection(selected, mode);
                // Keep Selected tag components in sync
                SyncSelectedComponents();
            }
            else
            {
                Console.WriteLine("Selection rectangle hit 0 entities.");
                // If no modifiers are held (Replace mode), clear the current selection set.
                var ioLocal = ImGui.GetIO();
                bool replaceMode = !ioLocal.KeyShift && !ioLocal.KeyCtrl;
                if (replaceMode)
                {
                    _selectionInteraction.ClearSelection();
                    SyncSelectedComponents();
                }
            }
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
