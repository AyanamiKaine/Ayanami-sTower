using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;

namespace AyanamisTower.StellaEcs.Example;

/// <summary>
/// Renders the on-screen text overlay via TextBatch.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextOverlayRenderStep"/> class.
/// </remarks>
/// <param name="pipeline"></param>
/// <param name="batch"></param>
/// <param name="builder"></param>
public sealed class TextOverlayRenderStep(GraphicsPipeline pipeline, TextBatch batch, Func<(bool show, Action<TextBatch> build)> builder) : IRenderStep
{
    private readonly GraphicsPipeline _pipeline = pipeline;
    private readonly TextBatch _batch = batch;
    private readonly Func<(bool show, Action<TextBatch> build)> _builder = builder;

    /// <summary>
    /// Initializes the text overlay render step.
    /// </summary>
    /// <param name="device"></param>
    public void Initialize(GraphicsDevice device) { }
    /// <summary>
    /// Updates the text overlay render step.
    /// </summary>
    /// <param name="delta"></param>
    public void Update(TimeSpan delta) { }

    /// <summary>
    /// Prepares the text overlay for rendering.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="view"></param>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view)
    {
        var (show, build) = _builder();
        if (!show || build == null) { return; }

        _batch.Start();
        build(_batch);
        _batch.UploadBufferData(cmdbuf);
    }
    /// <summary>
    /// Records the text overlay rendering commands.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="pass"></param>
    /// <param name="view"></param>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        var (show, _) = _builder();
        if (!show) { return; }

        pass.BindGraphicsPipeline(_pipeline);
        pass.SetViewport(new Viewport((uint)view.Width, (uint)view.Height));
        _batch.Render(pass, view.OrthoPixels);
    }

    /// <inheritdoc/>
    public void Dispose() { }
}

