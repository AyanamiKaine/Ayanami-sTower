using System;
using System.Collections.Generic;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;

namespace AyanamisTower.StellaEcs.Example;

/// <summary>
/// Very small render pipeline orchestrator. Steps are executed in order.
/// </summary>
public sealed class RenderPipeline : IDisposable
{
    private readonly List<IRenderStep> _steps = [];
    private GraphicsDevice _device = null!;
    /// <summary>
    /// Adds a render step to the pipeline.
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    public RenderPipeline Add(IRenderStep step)
    {
        _steps.Add(step);
        return this;
    }
    /// <summary>
    /// Initializes the render pipeline.
    /// </summary>
    /// <param name="device"></param>
    public void Initialize(GraphicsDevice device)
    {
        _device = device;
        foreach (var s in _steps)
        {
            s.Initialize(device);
        }
    }
    /// <summary>
    /// Updates the render pipeline.
    /// </summary>
    /// <param name="delta"></param>
    public void Update(TimeSpan delta)
    {
        foreach (var s in _steps)
        {
            s.Update(delta);
        }
    }

    /// <summary>
    /// Execute all steps. First runs Prepare on each step (outside of a pass),
    /// then the caller should create a pass and call Record.
    /// </summary>
    public void Prepare(CommandBuffer cmdbuf, in ViewContext view)
    {
        foreach (var s in _steps)
        {
            s.Prepare(cmdbuf, view);
        }
    }
    /// <summary>
    /// Records the rendering commands for each step.
    /// </summary>
    /// <param name="cmdbuf"></param>
    /// <param name="pass"></param>
    /// <param name="view"></param>
    public void Record(CommandBuffer cmdbuf, RenderPass pass, in ViewContext view)
    {
        foreach (var s in _steps)
        {
            s.Record(cmdbuf, pass, view);
        }
    }
    /// <summary>
    /// Disposes of the render pipeline and its steps.
    /// </summary>
    public void Dispose()
    {
        foreach (var s in _steps)
        {
            try { s.Dispose(); } catch { }
        }
        _steps.Clear();
    }
}

