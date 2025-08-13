using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Example;

/// <summary>
/// A reusable render step that can record commands into a pass.
/// </summary>
public interface IRenderStep : IDisposable
{
    /// <summary>
    /// Called once after construction, when GPU objects can be created.
    /// </summary>
    void Initialize(GraphicsDevice device);

    /// <summary>
    /// Optional per-frame CPU/GPU work that must happen before any render pass begins.
    /// Use this for uploads or copy passes that cannot occur during a render pass.
    /// </summary>
    void Prepare(CommandBuffer cmdbuf, in ViewContext view);

    /// <summary>
    /// Record this step into the render pass.
    /// </summary>
    void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view);

    /// <summary>
    /// Optional per-frame update hook for CPU-side state.
    /// </summary>
    void Update(TimeSpan delta);
}

/// <summary>
/// A view/projection bundle shared with steps.
/// </summary>
/// <remarks>
/// Creates a new view context.
/// </remarks>
/// <param name="view"></param>
/// <param name="projection"></param>
/// <param name="orthoPixels"></param>
/// <param name="width"></param>
/// <param name="height"></param>
public readonly struct ViewContext(Matrix4x4 view, Matrix4x4 projection, Matrix4x4 orthoPixels, int width, int height)
{
    /// <summary>
    /// The view matrix for the current frame.
    /// </summary>
    public readonly Matrix4x4 View = view;
    /// <summary>
    /// The projection matrix for the current frame.
    /// </summary>
    public readonly Matrix4x4 Projection = projection;
    /// <summary>
    /// The orthographic pixel matrix for the current frame.
    /// </summary>
    public readonly Matrix4x4 OrthoPixels = orthoPixels;
    /// <summary>
    /// The width of the viewport.
    /// </summary>
    public readonly int Width = width;
    /// <summary>
    /// The height of the viewport.
    /// </summary>
    public readonly int Height = height;
}

