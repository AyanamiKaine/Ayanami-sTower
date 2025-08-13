using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using AyanamisTower.StellaEcs.Engine.Rendering;
using AyanamisTower.StellaEcs.Engine.DefaultRenderer;

namespace AyanamisTower.StellaEcs.Engine;

/// <summary>
/// A lightweight base application that wraps MoonWorks.Game and orchestrates a RenderPipeline.
/// Subclass this to focus on your game logic; call UsePipeline after constructing your steps.
/// </summary>
public class App : Game
{
    /// <summary>
    /// The camera used to compute the default view/projection matrices.
    /// </summary>
    protected readonly Camera Camera = new();

    private RenderPipeline? _pipeline;
    private Texture? _depthTexture;

    /// <summary>
    /// Background clear color used for the default render pass.
    /// </summary>
    public Color ClearColor { get; set; } = new Color(10, 20, 40);

    /// <summary>
    /// When true (default), the base class handles a couple of helpful shortcuts:
    /// Esc to quit and F11 to toggle fullscreen.
    /// </summary>
    protected bool HandleCommonShortcuts { get; set; } = true;

    /// <summary>
    /// Instance of the high-level default renderer if attached via UseDefaultRenderer().
    /// </summary>
    protected DefaultRenderer.DefaultRenderer? DefaultRendererInstance { get; private set; }

    /// <summary>
    /// When true, the mouse cursor is captured and hidden for FPS-style camera controls.
    /// </summary>
    protected bool MouseCaptured { get; private set; } = false;

    /// <summary>
    /// Center position of the window for mouse wrapping.
    /// </summary>
    private Vector2 _windowCenter;

    // Suppress the first mouse delta after enabling relative mode to avoid an initial jump.
    private bool _suppressNextMouseDelta = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="appInfo">App info metadata.</param>
    /// <param name="windowCreateInfo">Window creation info.</param>
    /// <param name="framePacingSettings">Frame pacing settings.</param>
    /// <param name="availableShaderFormats">Available shader formats.</param>
    /// <param name="debugMode">Enable MoonWorks debug mode.</param>
    public App(
        AppInfo appInfo,
        WindowCreateInfo windowCreateInfo,
        FramePacingSettings framePacingSettings,
        ShaderFormat availableShaderFormats,
        bool debugMode
    ) : base(appInfo, windowCreateInfo, framePacingSettings, availableShaderFormats, debugMode)
    {
    }

    /// <summary>
    /// Convenience constructor with sensible defaults.
    /// </summary>
    /// <param name="windowTitle">The window title to display.</param>
    /// <param name="width">Initial window width.</param>
    /// <param name="height">Initial window height.</param>
    /// <param name="debugMode">Enable MoonWorks debug mode.</param>
    public App(
        string windowTitle = "StellaEcs App",
        int width = 1280,
        int height = 720,
        bool debugMode = true
    ) : base(
        new AppInfo("AyanamisTower", windowTitle),
        new WindowCreateInfo(
            windowTitle,
            (uint)width,
            (uint)height,
            ScreenMode.Windowed,
            true,
            false,
            false
        ),
        FramePacingSettings.CreateCapped(360, 360),
        ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC | ShaderFormat.MSL,
        debugMode
    )
    { }

    /// <summary>
    /// Supply a render pipeline to the app. This will Initialize the pipeline immediately.
    /// Call this after you create your steps and pipelines.
    /// </summary>
    /// <param name="pipeline"></param>
    protected void UsePipeline(RenderPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _pipeline.Initialize(GraphicsDevice);
    }

    /// <summary>
    /// Creates and attaches the DefaultRenderer, returning it for object creation.
    /// </summary>
    protected DefaultRenderer.DefaultRenderer UseDefaultRenderer()
    {
        DefaultRendererInstance = new DefaultRenderer.DefaultRenderer(GraphicsDevice, MainWindow, RootTitleStorage);
        UsePipeline(DefaultRendererInstance.Pipeline);
        return DefaultRendererInstance;
    }

    /// <summary>
    /// Override to update your application state. RenderPipeline.Update is called afterwards automatically.
    /// </summary>
    /// <param name="delta">Time step since last frame.</param>
    protected virtual void OnUpdate(TimeSpan delta) { }

    /// <summary>
    /// Computes the ViewContext used for rendering. Override to customize camera math.
    /// Defaults to perspective view/projection and an orthographic pixel matrix for overlays.
    /// </summary>
    protected virtual ViewContext ComputeViewContext()
    {
        var width = MainWindow.Width;
        var height = MainWindow.Height;
        Camera.Aspect = (float)width / Math.Max(1f, (float)height);

        var view = Camera.GetViewMatrix();
        var proj = Camera.GetProjectionMatrix();
        var orthoPx = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        return new ViewContext(view, proj, orthoPx, (int)width, (int)height);
    }

    /// <summary>
    /// Represents a 3D ray with an origin and direction.
    /// </summary>
    protected readonly struct Ray3(Vector3 origin, Vector3 direction)
    {
        /// <summary>Ray origin in world space.</summary>
        public Vector3 Origin { get; } = origin;
        /// <summary>Normalized direction vector in world space.</summary>
        public Vector3 Direction { get; } = Vector3.Normalize(direction);
    }

    /// <summary>
    /// Converts a screen pixel coordinate into a world-space ray using the current camera and viewport.
    /// </summary>
    protected Ray3 ScreenPointToRay(int x, int y)
    {
        var ctx = ComputeViewContext();
        float ndcX = (2f * (x / (float)Math.Max(1, ctx.Width))) - 1f;
        float ndcY = 1f - (2f * (y / (float)Math.Max(1, ctx.Height)));

        var forward = Vector3.Normalize(Camera.Target - Camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, Camera.Up));
        var up = Vector3.Normalize(Vector3.Cross(right, forward));

        float tanFov = MathF.Tan(Camera.Fov / 2f);
        var dir = Vector3.Normalize(
            forward + (ndcX * tanFov * Camera.Aspect) * right + (ndcY * tanFov) * up
        );
        return new Ray3(Camera.Position, dir);
    }

    /// <summary>
    /// Intersects a ray with a plane defined by a point and a normal.
    /// </summary>
    protected static bool RaycastPlane(Ray3 ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hitPoint)
    {
        const float Eps = 1e-6f;
        var denom = Vector3.Dot(planeNormal, ray.Direction);
        if (MathF.Abs(denom) < Eps)
        {
            hitPoint = default;
            return false; // Parallel
        }
        float t = Vector3.Dot(planePoint - ray.Origin, planeNormal) / denom;
        if (t < 0)
        {
            hitPoint = default;
            return false; // Behind origin
        }
        hitPoint = ray.Origin + (t * ray.Direction);
        return true;
    }

    /// <summary>
    /// Convenience: convert current mouse position to a world-space point on the horizontal ground plane (Y = groundY).
    /// </summary>
    protected bool MouseToGround(out Vector3 worldPoint, float groundY = 0f)
    {
        var ray = ScreenPointToRay(Inputs.Mouse.X, Inputs.Mouse.Y);
        var planePt = new Vector3(0, groundY, 0);
        var planeN = Vector3.UnitY;
        return RaycastPlane(ray, planePt, planeN, out worldPoint);
    }

    /// <summary>
    /// Convenience: convert current mouse position to a world-space point on the Z-plane (Z = planeZ).
    /// Useful with the default camera at Z>0 looking toward the origin.
    /// </summary>
    protected bool MouseToPlaneZ(out Vector3 worldPoint, float planeZ = 0f)
    {
        var ray = ScreenPointToRay(Inputs.Mouse.X, Inputs.Mouse.Y);
        var planePt = new Vector3(0, 0, planeZ);
        var planeN = Vector3.UnitZ;
        return RaycastPlane(ray, planePt, planeN, out worldPoint);
    }

    /// <summary>
    /// Converts a screen pixel coordinate to orthographic pixel space used by DefaultRenderer's 2D step.
    /// </summary>
    protected Vector2 ScreenToOrthoPixel(int x, int y) => new Vector2(x, y);

    /// <inheritdoc />
    protected override void Update(TimeSpan delta)
    {
        // Optional built-in shortcuts
        if (HandleCommonShortcuts)
        {
            HandleShortcuts();
        }

        OnUpdate(delta);
        _pipeline?.Update(delta);
    }

    /// <summary>
    /// Handles common keyboard shortcuts when <see cref="HandleCommonShortcuts"/> is true.
    /// </summary>
    protected virtual void HandleShortcuts()
    {
        if (Inputs.Keyboard.IsPressed(KeyCode.Escape))
        {
            if (MouseCaptured)
            {
                ReleaseMouse();
            }
            else
            {
                Quit();
            }
        }

        if (Inputs.Keyboard.IsPressed(KeyCode.F11))
        {
            MainWindow.SetScreenMode(
                MainWindow.ScreenMode == ScreenMode.Windowed ? ScreenMode.Fullscreen : ScreenMode.Windowed
            );
        }
    }

    /// <summary>
    /// Captures the mouse cursor for FPS-style camera controls.
    /// </summary>
    protected void CaptureMouse()
    {
        if (!MouseCaptured)
        {
            MouseCaptured = true;
            MainWindow.SetRelativeMouseMode(true);
            _windowCenter = new Vector2(MainWindow.Width / 2f, MainWindow.Height / 2f);
            _suppressNextMouseDelta = true;
        }
    }

    /// <summary>
    /// Releases the mouse cursor from capture.
    /// </summary>
    protected void ReleaseMouse()
    {
        if (MouseCaptured)
        {
            MouseCaptured = false;
            MainWindow.SetRelativeMouseMode(false);
        }
    }

    /// <summary>
    /// Gets the mouse movement delta when the mouse is captured.
    /// In relative mouse mode, mouse X/Y represent the delta directly.
    /// </summary>
    protected Vector2 GetMouseDelta()
    {
        if (!MouseCaptured) return Vector2.Zero;

        // On the first frame after enabling capture, suppress the initial delta.
        if (_suppressNextMouseDelta)
        {
            _suppressNextMouseDelta = false;
            return Vector2.Zero;
        }

        // In relative mouse mode, SDL provides relative motion via DeltaX/DeltaY.
        return new Vector2(Inputs.Mouse.DeltaX, Inputs.Mouse.DeltaY);
    }    /// <inheritdoc />
    protected override void Draw(double alpha)
    {
        if (_pipeline == null)
        {
            // Nothing to render yet
            return;
        }

        // Compute per-frame view context
        var ctx = ComputeViewContext();

        var cmdbuf = GraphicsDevice.AcquireCommandBuffer();

        // Allow steps to upload or do pre-pass work
        _pipeline.Prepare(cmdbuf, ctx);

        var swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
        if (swapchain == null)
        {
            GraphicsDevice.Cancel(cmdbuf);
            return;
        }

        // Ensure a depth texture exists and matches the swapchain dimensions
        if (_depthTexture == null ||
            _depthTexture.Width != swapchain.Width ||
            _depthTexture.Height != swapchain.Height ||
            _depthTexture.Format != GraphicsDevice.SupportedDepthFormat)
        {
            try { _depthTexture?.Dispose(); } catch { }
            _depthTexture = Texture.Create2D(
                GraphicsDevice,
                name: "Main Depth",
                width: swapchain.Width,
                height: swapchain.Height,
                format: GraphicsDevice.SupportedDepthFormat,
                usageFlags: TextureUsageFlags.DepthStencilTarget
            );
        }

        var colorTarget = new ColorTargetInfo(swapchain, ClearColor);
        var depthTarget = new DepthStencilTargetInfo(_depthTexture, clearDepth: 1.0f);
        var renderPass = cmdbuf.BeginRenderPass(in depthTarget, [colorTarget]);
        renderPass.SetViewport(new Viewport(swapchain.Width, swapchain.Height));

        _pipeline.Record(cmdbuf, renderPass, ctx);

        cmdbuf.EndRenderPass(renderPass);
        GraphicsDevice.Submit(cmdbuf);
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        // Prefer disposing the DefaultRenderer which owns meshes and the pipeline
        if (DefaultRendererInstance != null)
        {
            try { DefaultRendererInstance.Dispose(); } catch { }
            DefaultRendererInstance = null;
            _pipeline = null;
            try { _depthTexture?.Dispose(); } catch { }
            _depthTexture = null;
            return;
        }

        try { _pipeline?.Dispose(); } catch { }
        _pipeline = null;
        try { _depthTexture?.Dispose(); } catch { }
        _depthTexture = null;
    }
}


