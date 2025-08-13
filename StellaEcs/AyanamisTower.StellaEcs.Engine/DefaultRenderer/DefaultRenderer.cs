using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using AyanamisTower.StellaEcs.Engine.Rendering;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Storage;

namespace AyanamisTower.StellaEcs.Engine.DefaultRenderer;

/// <summary>
/// High-level renderer that hides pipeline setup and exposes simple object-creation APIs.
/// </summary>
public sealed class DefaultRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly RenderPipeline _pipeline = new();

    private readonly MeshInstancesRenderStep _mesh3DStep;
    private readonly MeshInstancesRenderStep _quad2DStep;
    private readonly TextOverlayRenderStep _textStep;
    private readonly LitMeshInstancesRenderStep _mesh3DLitStep;

    private readonly TextBatch _textBatch;
    private readonly GraphicsPipeline _textPipeline;
    private Func<TextBatch, bool>? _overlayBuilder;

    private readonly List<Mesh> _ownedMeshes = new();

    /// <summary>
    /// Creates a new instance of the DefaultRenderer.
    /// </summary>
    /// <param name="device">Graphics device to create pipelines and resources with.</param>
    /// <param name="window">Window used for swapchain format.</param>
    /// <param name="rootTitleStorage">Title storage for loading built-in shaders/assets.</param>
    public DefaultRenderer(GraphicsDevice device, Window window, TitleStorage rootTitleStorage)
    {
        _device = device;

        // Shared HLSL for simple colored meshes (Cube.hlsl already in Assets)
        ShaderCross.Initialize();
        var vs = ShaderCross.Create(
                _device,
                rootTitleStorage,
                filepath: "Assets/Cube.hlsl",
                entrypoint: "VSMain",
                shaderFormat: ShaderCross.ShaderFormat.HLSL,
                shaderStage: ShaderStage.Vertex,
                enableDebug: false,
                name: "DefaultVS"
            );
        var ps = ShaderCross.Create(
                _device,
                rootTitleStorage,
                filepath: "Assets/Cube.hlsl",
                entrypoint: "PSMain",
                shaderFormat: ShaderCross.ShaderFormat.HLSL,
                shaderStage: ShaderStage.Fragment,
                enableDebug: false,
                name: "DefaultPS"
            );

        // 3D pipeline: Vertex3D (pos/col), depth on
        var vertex3DInput = new VertexInputState
        {
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex3D>(0)],
            VertexAttributes =
            [
                new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 },
                new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() }
            ]
        };

        var pipeline3D = GraphicsPipeline.Create(
                _device,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = vs,
                    FragmentShader = ps,
                    VertexInputState = vertex3DInput,
                    PrimitiveType = PrimitiveType.TriangleList,
                    RasterizerState = RasterizerState.CCW_CullBack,
                    MultisampleState = MultisampleState.None,
                    DepthStencilState = new DepthStencilState
                    {
                        EnableDepthTest = true,
                        EnableDepthWrite = true,
                        EnableStencilTest = false,
                        CompareOp = CompareOp.Less
                    },
                    TargetInfo = new GraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }]
                    },
                    Name = "Default3DPipeline"
                }
            );

        _mesh3DStep = new MeshInstancesRenderStep(pipeline3D, MultiplyMode.WorldViewProj);

        // 2D pipeline: Vertex (pos2/col3), depth off
        var vertex2DInput = new VertexInputState
        {
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex>(0)],
            VertexAttributes =
            [
                new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float2, Offset = 0 },
                new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector2>() }
            ]
        };

        var pipeline2D = GraphicsPipeline.Create(
                _device,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = vs,
                    FragmentShader = ps,
                    VertexInputState = vertex2DInput,
                    PrimitiveType = PrimitiveType.TriangleList,
                    RasterizerState = RasterizerState.CCW_CullBack,
                    MultisampleState = MultisampleState.None,
                    DepthStencilState = new DepthStencilState { EnableDepthTest = false, EnableDepthWrite = false, EnableStencilTest = false, CompareOp = CompareOp.Always },
                    TargetInfo = new GraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }]
                    },
                    Name = "Default2DPipeline"
                }
            );

        _quad2DStep = new MeshInstancesRenderStep(pipeline2D, MultiplyMode.OrthoPixels);

        // Text pipeline and batch
        _textPipeline = GraphicsPipeline.Create(
            _device,
            new GraphicsPipelineCreateInfo
            {
                VertexShader = _device.TextVertexShader,
                FragmentShader = _device.TextFragmentShader,
                VertexInputState = _device.TextVertexInputState,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = MultisampleState.None,
                DepthStencilState = DepthStencilState.Disable,
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend }]
                },
                Name = "DefaultTextPipeline"
            }
        );
        _textBatch = new TextBatch(_device);

        _textStep = new TextOverlayRenderStep(
            _textPipeline,
            _textBatch,
            () =>
            {
                if (_overlayBuilder == null) return (false, _ => { });
                return (true, b => _overlayBuilder?.Invoke(b));
            }
        );

        // Compile lit shader for normal-based lighting
        var (litVS, litPS) = CompileHlsl(rootTitleStorage, "Assets/LitMesh.hlsl", namePrefix: "Lit");
        var litVertexInput = new VertexInputState
        {
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex3DLit>(0)],
            VertexAttributes =
            [
                new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 },
                new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() },
                new VertexAttribute { Location = 2, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)(System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() * 2) }
            ]
        };
        var lit3D = CreatePipeline(
            window,
            litVertexInput,
            litVS,
            litPS,
            depth: new DepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, EnableStencilTest = false, CompareOp = CompareOp.Less },
            blend: ColorTargetBlendState.NoBlend,
            primitiveType: PrimitiveType.TriangleList,
            rasterizer: RasterizerState.CCW_CullBack,
            name: "Lit3D"
        );
        _mesh3DLitStep = new LitMeshInstancesRenderStep(lit3D);

        _pipeline
            .Add(_mesh3DStep)
            .Add(_quad2DStep)
            .Add(_textStep)
            .Add(_mesh3DLitStep);

        _pipeline.Initialize(_device);
    }

    /// <summary>
    /// The underlying render pipeline that executes the steps.
    /// </summary>
    public RenderPipeline Pipeline => _pipeline;

    /// <summary>
    /// Adds a rotating cube or any 3D mesh instance.
    /// </summary>
    /// <param name="mesh">Provider for the mesh to draw.</param>
    /// <param name="model">Provider for the model matrix.</param>
    public void AddMesh3D(Func<Mesh> mesh, Func<Matrix4x4> model) => _mesh3DStep.AddInstance(mesh, model);

    /// <summary>
    /// Adds a 3D mesh with a custom pipeline override (e.g., user-specified shaders or states).
    /// </summary>
    public void AddMesh3D(Func<Mesh> mesh, Func<Matrix4x4> model, GraphicsPipeline customPipeline)
        => _mesh3DStep.AddInstance(mesh, model, customPipeline);

    /// <summary>
    /// Adds a colored quad in 2D (orthographic space) with a model matrix provider.
    /// </summary>
    /// <param name="mesh">Provider for the quad mesh.</param>
    /// <param name="model">Provider for the model matrix in your chosen ortho coordinates.</param>
    public void AddQuad2D(Func<Mesh> mesh, Func<Matrix4x4> model) => _quad2DStep.AddInstance(mesh, model);

    /// <summary>
    /// Adds a 2D quad or mesh with a custom pipeline override.
    /// </summary>
    public void AddQuad2D(Func<Mesh> mesh, Func<Matrix4x4> model, GraphicsPipeline customPipeline)
        => _quad2DStep.AddInstance(mesh, model, customPipeline);

    /// <summary>
    /// Adds a simple box mesh (cube) with size using a model provider.
    /// </summary>
    public void AddCube(Func<Matrix4x4> model, float size = 0.7f)
    {
        var mesh = Mesh.CreateBox3D(_device, size);
        _ownedMeshes.Add(mesh);
        AddMesh3D(() => mesh, model);
    }

    /// <summary>
    /// Adds a simple box mesh (cube) with flat color using a model provider.
    /// </summary>
    public void AddCube(Func<Matrix4x4> model, Vector3 color, float size = 0.7f)
    {
        var mesh = Mesh.CreateBox3D(_device, size, color);
        _ownedMeshes.Add(mesh);
        AddMesh3D(() => mesh, model);
    }

    /// <summary>
    /// Adds a simple quad with size and color using a model provider in 2D space.
    /// </summary>
    public void AddQuad(Func<Matrix4x4> model, float w = 1f, float h = 1f, Vector3? color = null)
    {
        var mesh = Mesh.CreateQuad(_device, w, h, color ?? new Vector3(1, 1, 1));
        _ownedMeshes.Add(mesh);
        AddQuad2D(() => mesh, model);
    }

    /// <summary>
    /// Creates a graphics pipeline from custom shaders and a vertex layout. Keeps format/state sensible by default.
    /// </summary>
    /// <param name="window">Used for color target format.</param>
    /// <param name="vertexInput">Vertex input layout.</param>
    /// <param name="vertexShader">Compiled vertex shader.</param>
    /// <param name="fragmentShader">Compiled fragment shader.</param>
    /// <param name="depth">Depth/stencil state or null for no depth.</param>
    /// <param name="blend">Blend state for color target.</param>
    /// <param name="primitiveType">Primitive topology.</param>
    /// <param name="rasterizer">Rasterizer state.</param>
    /// <param name="name">Optional pipeline name for debugging.</param>
    public GraphicsPipeline CreatePipeline(
        Window window,
        VertexInputState vertexInput,
        Shader vertexShader,
        Shader fragmentShader,
        DepthStencilState? depth = null,
        ColorTargetBlendState? blend = null,
        PrimitiveType primitiveType = PrimitiveType.TriangleList,
        RasterizerState? rasterizer = null,
        string? name = null
    )
    {
        return GraphicsPipeline.Create(
            _device,
            new GraphicsPipelineCreateInfo
            {
                VertexShader = vertexShader,
                FragmentShader = fragmentShader,
                VertexInputState = vertexInput,
                PrimitiveType = primitiveType,
                RasterizerState = rasterizer ?? RasterizerState.CCW_CullBack,
                MultisampleState = MultisampleState.None,
                DepthStencilState = depth ?? DepthStencilState.Disable,
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = blend ?? ColorTargetBlendState.NoBlend }]
                },
                Name = name ?? "CustomPipeline"
            }
        );
    }

    /// <summary>
    /// Helper to compile HLSL shaders using ShaderCross.
    /// </summary>
    public (Shader vs, Shader ps) CompileHlsl(TitleStorage rootTitleStorage, string filepath, string vsEntry = "VSMain", string psEntry = "PSMain", bool debug = false, string? namePrefix = null)
    {
        ShaderCross.Initialize();
        var vs = ShaderCross.Create(_device, rootTitleStorage, filepath, vsEntry, ShaderCross.ShaderFormat.HLSL, ShaderStage.Vertex, debug, (namePrefix ?? "") + "VS");
        var ps = ShaderCross.Create(_device, rootTitleStorage, filepath, psEntry, ShaderCross.ShaderFormat.HLSL, ShaderStage.Fragment, debug, (namePrefix ?? "") + "PS");
        return (vs, ps);
    }

    /// <summary>
    /// Sets the overlay builder. Return true to indicate drawing occurred; pass null to hide overlay.
    /// </summary>
    /// <param name="builder">Text batch builder or null.</param>
    public void SetOverlayBuilder(Func<TextBatch, bool>? builder) => _overlayBuilder = builder;

    /// <summary>
    /// Disposes managed state associated with the renderer.
    /// </summary>
    public void Dispose()
    {
        _pipeline.Dispose();
        _textBatch.Dispose();
        foreach (var m in _ownedMeshes)
        {
            try { m.Dispose(); } catch { }
        }
        _ownedMeshes.Clear();
        // Pipelines are owned by GraphicsDevice and freed on device destroy; safe to skip explicit dispose.
    }

    /// <summary>
    /// Adds a lit 3D mesh instance (requires Vertex3DLit layout) using the point light.
    /// </summary>
    public void AddMesh3DLit(Func<Mesh> mesh, Func<Matrix4x4> model) => _mesh3DLitStep.AddInstance(mesh, model);

    /// <summary>
    /// Adds a lit cube with flat albedo color.
    /// </summary>
    public void AddCubeLit(Func<Matrix4x4> model, Vector3 color, float size = 0.7f)
    {
        var mesh = Mesh.CreateBox3DLit(_device, size, color);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Sets the point light (sun-like) parameters used by the lit 3D pipeline.
    /// </summary>
    public void SetPointLight(Vector3 position, Vector3 color, float ambient = 0.2f) => _mesh3DLitStep.SetLight(position, color, ambient);
}
