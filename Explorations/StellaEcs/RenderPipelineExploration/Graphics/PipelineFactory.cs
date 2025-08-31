using MoonWorks.Graphics;
using System;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// Factory for creating graphics pipelines with common configurations.
/// Uses a fluent builder pattern for easy customization.
/// </summary>
public class PipelineFactory
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SampleCount _msaaSamples;
    private readonly TextureFormat _swapchainFormat;
    private readonly TextureFormat _depthStencilFormat;
    /// <summary>
    /// Initializes a new instance of the PipelineFactory class.
    /// </summary>
    public PipelineFactory(GraphicsDevice graphicsDevice, SampleCount msaaSamples,
                          TextureFormat swapchainFormat, TextureFormat depthStencilFormat)
    {
        _graphicsDevice = graphicsDevice;
        _msaaSamples = msaaSamples;
        _swapchainFormat = swapchainFormat;
        _depthStencilFormat = depthStencilFormat;
    }

    /// <summary>
    /// Creates a builder for a new graphics pipeline.
    /// </summary>
    public PipelineBuilder CreatePipeline(string name = "GraphicsPipeline")
    {
        return new PipelineBuilder(_graphicsDevice, _msaaSamples, _swapchainFormat, _depthStencilFormat, name);
    }

    /// <summary>
    /// Creates a standard 3D object pipeline with depth testing enabled.
    /// </summary>
    public GraphicsPipeline CreateStandard3DPipeline(Shader vertexShader, Shader fragmentShader,
                                                   VertexInputState vertexInput, string name = "Standard3D")
    {
        return CreatePipeline(name)
            .WithShaders(vertexShader, fragmentShader)
            .WithVertexInput(vertexInput)
            .WithDepthTesting(true, true)
            .WithBlendState(ColorTargetBlendState.NoBlend)
            .Build();
    }

    /// <summary>
    /// Creates a skybox pipeline with no depth testing/writing and no culling.
    /// </summary>
    public GraphicsPipeline CreateSkyboxPipeline(Shader vertexShader, Shader fragmentShader,
                                               VertexInputState vertexInput, string name = "Skybox")
    {
        return CreatePipeline(name)
            .WithShaders(vertexShader, fragmentShader)
            .WithVertexInput(vertexInput)
            .WithRasterizer(RasterizerState.CCW_CullNone)
            .WithDepthTesting(false, false)
            .WithBlendState(ColorTargetBlendState.NoBlend)
            .Build();
    }

    /// <summary>
    /// Creates a line rendering pipeline with alpha blending.
    /// </summary>
    public GraphicsPipeline CreateLinePipeline(Shader vertexShader, Shader fragmentShader,
                                            VertexInputState vertexInput, string name = "LineRenderer")
    {
        return CreatePipeline(name)
            .WithShaders(vertexShader, fragmentShader)
            .WithVertexInput(vertexInput)
            .WithPrimitiveType(PrimitiveType.LineList)
            .WithRasterizer(RasterizerState.CCW_CullNone)
            .WithDepthTesting(true, false) // Test but don't write depth
            .WithBlendState(ColorTargetBlendState.NonPremultipliedAlphaBlend)
            .Build();
    }
    /// <summary>
    /// Creates a wireframe rendering pipeline.
    /// </summary>
    /// <param name="vs"></param>
    /// <param name="fs"></param>
    /// <param name="vertexInput"></param>
    /// <returns></returns>
    public GraphicsPipeline CreateWireframePipeline(Shader vs, Shader fs, VertexInputState vertexInput)
    {
        return CreatePipeline("WireframePipeline")
            .WithShaders(vs, fs)
            .WithVertexInput(vertexInput)
            .WithPrimitiveType(PrimitiveType.LineList)
            .WithRasterizer(new RasterizerState
            {
                CullMode = (MoonWorks.Graphics.CullMode)CullMode.None,
                FrontFace = FrontFace.CounterClockwise,
                FillMode = FillMode.Line // Wireframe mode
            })
            .WithDepthTesting(true, false)
            .WithBlendState(ColorTargetBlendState.NoBlend)
            .Build();
    }

    /// <summary>
    /// Create a post-processing pipeline (full screen quad, no depth)
    /// </summary>
    public GraphicsPipeline CreatePostProcessPipeline(Shader vs, Shader fs, VertexInputState vertexInput)
    {
        return CreatePipeline("PostProcessPipeline")
            .WithShaders(vs, fs)
            .WithVertexInput(vertexInput)
            .WithPrimitiveType(PrimitiveType.TriangleList)
            .WithRasterizer(RasterizerState.CCW_CullNone)
            .WithDepthStencil(false) // No depth for post-processing
            .WithBlendState(ColorTargetBlendState.NoBlend)
            .Build();
    }

    /// <summary>
    /// Creates an ImGui pipeline without depth stencil.
    /// </summary>
    public GraphicsPipeline CreateImGuiPipeline(Shader vertexShader, Shader fragmentShader,
                                              VertexInputState vertexInput, string name = "ImGui")
    {
        return CreatePipeline(name)
            .WithShaders(vertexShader, fragmentShader)
            .WithVertexInput(vertexInput)
            .WithDepthStencil(false)
            .WithDepthTesting(false, false)
            .WithMultisample(new MultisampleState { SampleCount = SampleCount.Two })
            .WithBlendState(ColorTargetBlendState.NonPremultipliedAlphaBlend)
            .Build();
    }

    /// <summary>
    /// Creates a transparent/particle pipeline with alpha blending.
    /// </summary>
    public GraphicsPipeline CreateTransparentPipeline(Shader vertexShader, Shader fragmentShader,
                                                    VertexInputState vertexInput, string name = "Transparent")
    {
        return CreatePipeline(name)
            .WithShaders(vertexShader, fragmentShader)
            .WithVertexInput(vertexInput)
            .WithDepthTesting(true, false) // Test but don't write depth
            .WithBlendState(ColorTargetBlendState.NonPremultipliedAlphaBlend)
            .Build();
    }
}

/// <summary>
/// Fluent builder for graphics pipelines.
/// </summary>
public class PipelineBuilder
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SampleCount _msaaSamples;
    private readonly TextureFormat _swapchainFormat;
    private readonly TextureFormat _depthStencilFormat;
    private readonly string _name;

    // Required components
    private Shader? _vertexShader;
    private Shader? _fragmentShader;
    private VertexInputState _vertexInput;

    // Optional components with defaults
    private PrimitiveType _primitiveType = PrimitiveType.TriangleList;
    private RasterizerState _rasterizerState = RasterizerState.CCW_CullBack;
    private MultisampleState _multisampleState;
    private DepthStencilState _depthStencilState;
    private GraphicsPipelineTargetInfo _targetInfo;
    private ColorTargetBlendState _blendState = ColorTargetBlendState.NoBlend;
    private bool _hasDepthStencil = true;

    /// <summary>
    /// Initializes a new instance of the PipelineBuilder class.
    /// </summary>
    public PipelineBuilder(GraphicsDevice graphicsDevice, SampleCount msaaSamples,
                          TextureFormat swapchainFormat, TextureFormat depthStencilFormat, string name)
    {
        _graphicsDevice = graphicsDevice;
        _msaaSamples = msaaSamples;
        _swapchainFormat = swapchainFormat;
        _depthStencilFormat = depthStencilFormat;
        _name = name;

        // Set defaults
        _multisampleState = new MultisampleState { SampleCount = _msaaSamples };
        _depthStencilState = new DepthStencilState
        {
            EnableDepthTest = true,
            EnableDepthWrite = true,
            CompareOp = CompareOp.LessOrEqual,
            CompareMask = 0xFF,
            WriteMask = 0xFF,
            EnableStencilTest = false
        };
        _targetInfo = new GraphicsPipelineTargetInfo
        {
            ColorTargetDescriptions = new[]
            {
                new ColorTargetDescription
                {
                    Format = _swapchainFormat,
                    BlendState = _blendState
                }
            },
            HasDepthStencilTarget = _hasDepthStencil,
            DepthStencilFormat = _depthStencilFormat
        };
    }

    /// <summary>
    /// Sets the vertex and fragment shaders.
    /// </summary>
    public PipelineBuilder WithShaders(Shader vertexShader, Shader fragmentShader)
    {
        _vertexShader = vertexShader;
        _fragmentShader = fragmentShader;
        return this;
    }

    /// <summary>
    /// Sets the vertex input state.
    /// </summary>
    public PipelineBuilder WithVertexInput(VertexInputState vertexInput)
    {
        _vertexInput = vertexInput;
        return this;
    }

    /// <summary>
    /// Sets the primitive type (default: TriangleList).
    /// </summary>
    public PipelineBuilder WithPrimitiveType(PrimitiveType primitiveType)
    {
        _primitiveType = primitiveType;
        return this;
    }

    /// <summary>
    /// Sets the rasterizer state.
    /// </summary>
    public PipelineBuilder WithRasterizer(RasterizerState rasterizerState)
    {
        _rasterizerState = rasterizerState;
        return this;
    }

    /// <summary>
    /// Sets the multisample state.
    /// </summary>
    public PipelineBuilder WithMultisample(MultisampleState multisampleState)
    {
        _multisampleState = multisampleState;
        return this;
    }

    /// <summary>
    /// Configures depth testing and writing.
    /// </summary>
    public PipelineBuilder WithDepthTesting(bool enableDepthTest, bool enableDepthWrite,
                                           CompareOp compareOp = CompareOp.LessOrEqual)
    {
        _depthStencilState = new DepthStencilState
        {
            EnableDepthTest = enableDepthTest,
            EnableDepthWrite = enableDepthWrite,
            CompareOp = compareOp,
            CompareMask = (byte)(enableDepthTest ? (uint)0xFF : 0x00),
            WriteMask = (byte)(enableDepthWrite ? (uint)0xFF : 0x00),
            EnableStencilTest = false
        };
        return this;
    }

    /// <summary>
    /// Sets the blend state for the color target.
    /// </summary>
    public PipelineBuilder WithBlendState(ColorTargetBlendState blendState)
    {
        _blendState = blendState;
        _targetInfo.ColorTargetDescriptions[0].BlendState = blendState;
        return this;
    }

    /// <summary>
    /// Enables or disables depth stencil target.
    /// </summary>
    public PipelineBuilder WithDepthStencil(bool hasDepthStencil)
    {
        _hasDepthStencil = hasDepthStencil;
        _targetInfo.HasDepthStencilTarget = hasDepthStencil;
        return this;
    }

    /// <summary>
    /// Sets custom depth stencil state.
    /// </summary>
    public PipelineBuilder WithDepthStencilState(DepthStencilState depthStencilState)
    {
        _depthStencilState = depthStencilState;
        return this;
    }

    /// <summary>
    /// Sets custom target info.
    /// </summary>
    public PipelineBuilder WithTargetInfo(GraphicsPipelineTargetInfo targetInfo)
    {
        _targetInfo = targetInfo;
        return this;
    }

    /// <summary>
    /// Builds and returns the graphics pipeline.
    /// </summary>
    public GraphicsPipeline Build()
    {
        if (_vertexShader == null || _fragmentShader == null)
        {
            throw new InvalidOperationException("Vertex shader and fragment shader are required.");
        }

        return GraphicsPipeline.Create(_graphicsDevice, new GraphicsPipelineCreateInfo
        {
            VertexShader = _vertexShader,
            FragmentShader = _fragmentShader,
            VertexInputState = _vertexInput,
            PrimitiveType = _primitiveType,
            RasterizerState = _rasterizerState,
            MultisampleState = _multisampleState,
            DepthStencilState = _depthStencilState,
            TargetInfo = _targetInfo,
            Name = _name
        });
    }
}
