using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using AyanamisTower.StellaEcs.Engine.Rendering;

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

    /// <inheritdoc />
    protected override void Update(TimeSpan delta)
    {
        OnUpdate(delta);
        _pipeline?.Update(delta);
    }

    /// <inheritdoc />
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

        var colorTarget = new ColorTargetInfo(swapchain, new Color(10, 20, 40));
        var renderPass = cmdbuf.BeginRenderPass([colorTarget]);
        renderPass.SetViewport(new Viewport(swapchain.Width, swapchain.Height));

        _pipeline.Record(cmdbuf, renderPass, ctx);

        cmdbuf.EndRenderPass(renderPass);
        GraphicsDevice.Submit(cmdbuf);
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        try { _pipeline?.Dispose(); } catch { /* ignore on shutdown */ }
        _pipeline = null;
    }
}


