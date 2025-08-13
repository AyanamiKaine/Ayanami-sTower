using System;
using System.Collections.Generic;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Graphics;
using AyanamisTower.StellaEcs.Engine.Rendering;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Storage;
using System.IO;

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
    private readonly Dictionary<Texture, TexturedLitMeshInstancesRenderStep> _texturedSteps = new();
    private ShadowCubeRenderStep? _shadowStep;
    private Sampler? _shadowSampler;

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
                            ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }],
                            HasDepthStencilTarget = true,
                            DepthStencilFormat = _device.SupportedDepthFormat
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
                        ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }],
                        HasDepthStencilTarget = true,
                        DepthStencilFormat = _device.SupportedDepthFormat
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
                    ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend }],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = _device.SupportedDepthFormat
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
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex3DMaterial>(0)],
            VertexAttributes =
            [
                new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 }, // pos
                new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() }, // nrm
                new VertexAttribute { Location = 2, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)(System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() * 2 + System.Runtime.InteropServices.Marshal.SizeOf<Vector2>()) } // col
            ]
        };
        var lit3D = GraphicsPipeline.Create(
            _device,
            new GraphicsPipelineCreateInfo
            {
                VertexShader = litVS,
                FragmentShader = litPS,
                VertexInputState = litVertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullBack,
                MultisampleState = MultisampleState.None,
                DepthStencilState = new DepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, EnableStencilTest = false, CompareOp = CompareOp.Less },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = _device.SupportedDepthFormat
                },
                Name = "Lit3D"
            }
        );
        _mesh3DLitStep = new LitMeshInstancesRenderStep(lit3D);

        // Textured-lit pipeline: Vertex3DTexturedLit (pos/nrm/uv) + texture at t0/s0
        var (txVS, txPS) = CompileHlsl(rootTitleStorage, "Assets/LitMeshTextured.hlsl", namePrefix: "LitTex");
        var texturedVertexInput = new VertexInputState
        {
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex3DMaterial>(0)],
            VertexAttributes =
            [
                new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 }, // pos
                new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() }, // nrm
                new VertexAttribute { Location = 2, BufferSlot = 0, Format = VertexElementFormat.Float2, Offset = (uint)(System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() * 2) } // uv
            ]
        };
        _texturedLitPipeline = GraphicsPipeline.Create(
                _device,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = txVS,
                    FragmentShader = txPS,
                    VertexInputState = texturedVertexInput,
                    PrimitiveType = PrimitiveType.TriangleList,
                    RasterizerState = RasterizerState.CCW_CullBack,
                    MultisampleState = MultisampleState.None,
                    DepthStencilState = new DepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, EnableStencilTest = false, CompareOp = CompareOp.Less },
                    TargetInfo = new GraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions = [new ColorTargetDescription { Format = window.SwapchainFormat, BlendState = ColorTargetBlendState.NoBlend }],
                        HasDepthStencilTarget = true,
                        DepthStencilFormat = _device.SupportedDepthFormat
                    },
                    Name = "TexturedLit3D"
                }
            );

        // Shadow depth cubemap pre-pass (rendered to color cube)
        var (shadowVS, shadowPS) = CompileHlsl(rootTitleStorage, "Assets/ShadowDepthCube.hlsl", namePrefix: "Shadow");
        var shadowInput = new VertexInputState
        {
            // Use the same stride as our meshes (Vertex3DMaterial); only POSITION at location 0 is consumed.
            VertexBufferDescriptions = [VertexBufferDescription.Create<Mesh.Vertex3DMaterial>(0)],
            VertexAttributes = [new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 }]
        };
        var shadowPipeline = GraphicsPipeline.Create(
            _device,
            new GraphicsPipelineCreateInfo
            {
                VertexShader = shadowVS,
                FragmentShader = shadowPS,
                VertexInputState = shadowInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullBack,
                MultisampleState = MultisampleState.None,
                DepthStencilState = DepthStencilState.Disable,
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions = [new ColorTargetDescription { Format = TextureFormat.R8Unorm, BlendState = ColorTargetBlendState.NoBlend }]
                },
                Name = "ShadowCube"
            }
        );
        _shadowStep = new ShadowCubeRenderStep(_device, shadowPipeline, size: 512);

        _pipeline
                .Add(_shadowStep)
                .Add(_mesh3DStep)
                .Add(_mesh3DLitStep)
                .Add(_quad2DStep)
                .Add(_textStep);

        _pipeline.Initialize(_device);

        // Create a clamp sampler for the shadow cubemap
        _shadowSampler = Sampler.Create(_device, "ShadowSampler", new SamplerCreateInfo
        {
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxLod = 1000
        });

        if (_shadowStep != null && _shadowSampler != null)
        {
            _mesh3DLitStep.SetShadowMap(_shadowStep.CubeTexture, _shadowSampler);
        }
    }

    /// <summary>
    /// The underlying render pipeline that executes the steps.
    /// </summary>
    public RenderPipeline Pipeline => _pipeline;

    private readonly GraphicsPipeline _texturedLitPipeline;

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
    public void AddMesh3DLit(Func<Mesh> mesh, Func<Matrix4x4> model)
    {
        _mesh3DLitStep.AddInstance(mesh, model);
        _shadowStep?.AddCaster(mesh, model);
    }

    /// <summary>
    /// Adds a lit cube with flat albedo color.
    /// </summary>
    public void AddCubeLit(Func<Matrix4x4> model, Vector3 color, float size = 0.7f)
    {
        var mesh = Mesh.CreateBox3D(_device, size, color);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Adds a lit sphere with flat albedo color for debug visualization.
    /// </summary>
    public void AddSphereLit(Func<Matrix4x4> model, Vector3 color, float radius = 0.1f)
    {
        var mesh = Mesh.CreateSphere3D(_device, radius, color);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Adds a lit plane (XZ) with flat albedo color.
    /// </summary>
    public void AddPlaneLit(Func<Matrix4x4> model, Vector3 color, float width = 5f, float depth = 5f)
    {
        var mesh = Mesh.CreatePlane3DLit(_device, width, depth, color);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Adds a lit cylinder with flat albedo color.
    /// </summary>
    public void AddCylinderLit(Func<Matrix4x4> model, Vector3 color, float radius = 0.5f, float height = 1.5f, int radialSegments = 24, bool capped = true)
    {
        var mesh = Mesh.CreateCylinder3DLit(_device, radius, height, color, radialSegments, 1, capped);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Adds a lit capsule with flat albedo color.
    /// </summary>
    public void AddCapsuleLit(Func<Matrix4x4> model, Vector3 color, float radius = 0.5f, float height = 2.0f, int radialSegments = 24, int hemisphereStacks = 8)
    {
        var mesh = Mesh.CreateCapsule3DLit(_device, radius, height, color, radialSegments, hemisphereStacks);
        _ownedMeshes.Add(mesh);
        AddMesh3DLit(() => mesh, model);
    }

    /// <summary>
    /// Creates a Texture from raw RGBA8 pixel data and uploads it.
    /// </summary>
    public Texture CreateTextureFromRgba8Pixels(uint width, uint height, ReadOnlySpan<byte> pixels, bool srgb = true)
    {
        var tex = Texture.Create2D(_device, width, height, srgb ? TextureFormat.R8G8B8A8UnormSRGB : TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
        if (tex == null) throw new InvalidOperationException("Failed to create texture.");
        var tbuf = TransferBuffer.Create<byte>(_device, "TexUpload", TransferBufferUsage.Upload, (uint)pixels.Length);
        var span = tbuf.Map<byte>(cycle: false);
        pixels.CopyTo(span);
        tbuf.Unmap();
        var cmdbuf = _device.AcquireCommandBuffer();
        var copy = cmdbuf.BeginCopyPass();
        // Use explicit region to ensure depth=1 for 2D textures
        copy.UploadToTexture(
            new TextureTransferInfo { TransferBuffer = tbuf.Handle, Offset = 0 },
            new TextureRegion { Texture = tex.Handle, MipLevel = 0, Layer = 0, X = 0, Y = 0, Z = 0, W = width, H = height, D = 1 },
            false
        );
        cmdbuf.EndCopyPass(copy);
        _device.Submit(cmdbuf);
        tbuf.Dispose();
        return tex;
    }

    /// <summary>
    /// Adds a lit textured 3D mesh instance (requires Vertex3DTexturedLit layout) with the given texture.
    /// Multiple textures create independent steps internally.
    /// </summary>
    public void AddTextured3DLit(Func<Mesh> mesh, Func<Matrix4x4> model, Texture texture)
    {
        if (!_texturedSteps.TryGetValue(texture, out var step))
        {
            step = new TexturedLitMeshInstancesRenderStep(_texturedLitPipeline, texture, _device.LinearSampler);
            _texturedSteps.Add(texture, step);
            _pipeline.Add(step);
            if (_shadowStep != null && _shadowSampler != null)
            {
                step.SetShadowMap(_shadowStep.CubeTexture, _shadowSampler);
            }
        }
        step.AddInstance(mesh, model);
        _shadowStep?.AddCaster(mesh, model);
    }

    /// <summary>
    /// Convenience: creates and adds a textured UV-sphere instance.
    /// </summary>
    public void AddTexturedSphereLit(Func<Matrix4x4> model, Texture texture, float radius = 1f, int slices = 64, int stacks = 32)
    {
        var mesh = Mesh.CreateSphere3DTexturedLit(_device, radius, slices, stacks);
        _ownedMeshes.Add(mesh);
        AddTextured3DLit(() => mesh, model, texture);
    }

    /// <summary>
    /// Clears all lit 3D instances queued so far. Useful for ECS-driven per-frame submissions.
    /// </summary>
    public void ClearLitInstances()
    {
        _mesh3DLitStep.ClearInstances();
        _shadowStep?.ClearCasters();
    }

    /// <summary>
    /// Clears all textured-lit instances queued so far.
    /// </summary>
    public void ClearTexturedLitInstances()
    {
        foreach (var step in _texturedSteps.Values)
        {
            step.ClearInstances();
        }
        _shadowStep?.ClearCasters();
    }

    /// <summary>
    /// Sets the point light (sun-like) parameters used by the lit 3D pipeline.
    /// </summary>
    public void SetPointLight(Vector3 position, Vector3 color, float ambient = 0.2f)
    {
        _mesh3DLitStep.SetLight(position, color, ambient);
        foreach (var step in _texturedSteps.Values)
        {
            step.SetLight(position, color, ambient);
        }
        _shadowStep?.SetLightPosition(position);
    }

    /// <summary>
    /// Sets global shadow parameters (far plane, bias) and updates all steps.
    /// </summary>
    public void SetShadows(float farPlane, float depthBias)
    {
        _mesh3DLitStep.SetShadowParams(farPlane, depthBias);
        foreach (var step in _texturedSteps.Values)
        {
            step.SetShadowParams(farPlane, depthBias);
        }
        _shadowStep?.SetSettings(0.05f, farPlane, depthBias);
    }
}
