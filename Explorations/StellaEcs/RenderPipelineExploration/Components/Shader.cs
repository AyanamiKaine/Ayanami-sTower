using System;
using AyanamisTower.StellaEcs.StellaInvicta.Assets;
using AyanamisTower.StellaEcs.StellaInvicta.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using StellaInvicta.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;


/// <summary>
/// Represents a shader configuration for an entity.
/// This component allows entities to specify which shaders to use for rendering.
/// When you define a shader component for an entity it creates an appropriate render
/// pipeline for it. 
/// 
/// The default is         
/// Blend = BlendMode.NoBlend;
/// Cull = CullMode.CounterClockwiseBack;
/// Depth = DepthMode.DrawAlways;
/// Wireframe = WireframeMode.None;
/// </summary>
public class Shader
{
    /// <summary>
    /// Gets the game instance associated with this shader.
    /// </summary>
    public Game Game { get; }
    /// <summary>
    /// Gets the name of the shader.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets or sets the shader format. By default, this is set to HLSL.
    /// </summary>
    public ShaderCross.ShaderFormat Format { get; set; } = ShaderCross.ShaderFormat.HLSL;
    /// <summary>
    /// Gets or sets the rendering mode for the shader.
    /// </summary>
    public RenderMode RenderMode { get; set; } = new RenderMode();
    /// <summary>
    /// Called for every vertex the material is visible on.
    /// </summary>
    public MoonWorks.Graphics.Shader? VertexShader { get; set; }
    /// <summary>
    /// Called for every pixel the material is visible on. Often also called a pixel shader.
    /// </summary>
    public MoonWorks.Graphics.Shader? FragmentShader { get; set; }

    /// <summary>
    /// Cached graphics pipeline created for this shader.
    /// Call <see cref="BuildOrUpdatePipeline"/> to create or refresh this pipeline.
    /// </summary>
    public MoonWorks.Graphics.GraphicsPipeline? Pipeline { get; private set; }

    /// <summary>
    /// Create a GraphicsPipeline for this shader using the provided <see cref="PipelineFactory"/> and vertex input layout.
    /// This keeps pipeline creation close to the shader component so entities that own this component can easily build
    /// a matching pipeline.
    /// </summary>
    /// <param name="factory">PipelineFactory configured for the current GraphicsDevice and swap/depth formats.</param>
    /// <param name="vertexInput">Vertex input state describing vertex attributes for the mesh to render.</param>
    /// <param name="pipelineName">Optional pipeline name override. Defaults to the shader name.</param>
    /// <returns>A created <see cref="GraphicsPipeline"/> instance.</returns>
    public GraphicsPipeline CreatePipeline(PipelineFactory factory, VertexInputState vertexInput, string? pipelineName = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Ensure shaders are built
        if (VertexShader == null || FragmentShader == null)
        {
            BuildShader();
        }

        if (VertexShader == null || FragmentShader == null)
        {
            throw new InvalidOperationException("Vertex and fragment shaders must be available to create a pipeline.");
        }

        var name = pipelineName ?? Name;

        var builder = factory.CreatePipeline(name + "Pipeline")
            .WithShaders(VertexShader, FragmentShader)
            .WithVertexInput(vertexInput);

        // Blend
        builder = RenderMode.Blend switch
        {
            BlendMode.NoBlend or BlendMode.Opaque => builder.WithBlendState(ColorTargetBlendState.NoBlend),
            BlendMode.NonPremultipliedAlpha => builder.WithBlendState(ColorTargetBlendState.NonPremultipliedAlphaBlend),
            BlendMode.PremultipliedAlpha => builder.WithBlendState(ColorTargetBlendState.PremultipliedAlphaBlend),
            BlendMode.Additive => builder.WithBlendState(ColorTargetBlendState.Additive),
            BlendMode.NoWrite => builder.WithBlendState(ColorTargetBlendState.NoWrite),// Map to the engine's NoWrite preset which disables color writes.
            _ => builder.WithBlendState(ColorTargetBlendState.NoBlend),
        };

        // Rasterizer: culling and wireframe
        RasterizerState raster = RasterizerState.CCW_CullBack;
        switch (RenderMode.Cull)
        {
            case Graphics.CullMode.None:
                raster = RasterizerState.CCW_CullNone;
                break;
            case Graphics.CullMode.CounterClockwiseBack:
                raster = RasterizerState.CCW_CullBack;
                break;
            case Graphics.CullMode.ClockwiseBack:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Back, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.CounterClockwiseFront:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Front, FrontFace = FrontFace.CounterClockwise };
                break;
            case Graphics.CullMode.ClockwiseFront:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Front, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.ClockwiseDisabled:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.None, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.CounterClockwiseDisabled:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.None, FrontFace = FrontFace.CounterClockwise };
                break;
        }

        // Wireframe fill mode
        if (RenderMode.Wireframe != WireframeMode.None)
        {
            raster = new RasterizerState
            {
                CullMode = raster.CullMode,
                FrontFace = raster.FrontFace,
                FillMode = FillMode.Line
            };
        }

        builder = builder.WithRasterizer(raster);

        // Depth mode
        builder = RenderMode.Depth switch
        {
            DepthMode.DrawOpaque => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
            DepthMode.DrawAlways => builder.WithDepthTesting(true, true, CompareOp.Always),// Always pass; still write depth so this object effectively forces its depth into the buffer.
            DepthMode.DrawNever => builder.WithDepthTesting(true, false, CompareOp.Never),
            DepthMode.PrepassAlpha => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
            DepthMode.TestDisabled => builder.WithDepthTesting(false, false),
            _ => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
        };
        return builder.Build();
    }

    /// <summary>
    /// Build or update the cached <see cref="Pipeline"/> using the provided factory and vertex input.
    /// Disposes any previously-created pipeline before assigning the new one.
    /// </summary>
    public MoonWorks.Graphics.GraphicsPipeline BuildOrUpdatePipeline(PipelineFactory factory, VertexInputState vertexInput, string? pipelineName = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var newPipeline = CreatePipeline(factory, vertexInput, pipelineName);

        // Dispose previous pipeline if present to avoid leaking GPU resources
        try
        {
            Pipeline?.Dispose();
        }
        catch
        {
            // swallow - disposal failures are non-fatal here; caller can log if desired
        }

        Pipeline = newPipeline;
        return Pipeline;
    }

    /// <summary>
    /// Represents a shader configuration for an entity.
    /// This component allows entities to specify which shaders to use for rendering.
    /// When you define a shader component for an entity it creates an appropriate render
    /// pipeline for it. 
    /// 
    /// The default is         
    /// Blend = BlendMode.NoBlend;
    /// Cull = CullMode.CounterClockwiseBack;
    /// Depth = DepthMode.DrawAlways;
    /// Wireframe = WireframeMode.None;
    /// </summary>
    public Shader(Game game, string name)
    {
        Game = game;
        Name = name;
        RenderMode = new RenderMode();
        BuildShader();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class with a custom render mode.
    /// </summary>
    /// <param name="renderMode"></param>
    /// <param name="game"></param>
    /// <param name="name"></param>
    public Shader(Game game, string name, RenderMode renderMode)
    {
        Game = game;
        Name = name;
        RenderMode = renderMode;
        BuildShader();
    }

    /// <summary>
    /// Builds the shader for the entity.
    /// </summary>
    private void BuildShader()
    {
        VertexShader = ShaderCross.Create(
                Game.GraphicsDevice,
                Game.RootTitleStorage,
                AssetManager.AssetFolderName + $"/{Name}.hlsl",
                "VSMain",
                Format,
                ShaderStage.Vertex,
                false,
                Name + "VS"
            );

        FragmentShader = ShaderCross.Create(
            Game.GraphicsDevice,
            Game.RootTitleStorage,
            AssetManager.AssetFolderName + $"/{Name}.hlsl",
            "PSMain",
            Format,
            ShaderStage.Fragment,
            false,
            Name + "FS"
        );
    }
}
