using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using RenderPipelineExploration;

namespace AyanamisTower.StellaEcs.Example;

internal static class Program
{
    public static void Main()
    {
        var game = new CubeGame();
        game.Run();
    }

    private sealed class CubeGame : Game
    {
        // Camera
        private Camera _camera = null!;

        // GPU resources
        private GraphicsPipeline? _pipeline;
        private MoonWorks.Graphics.Buffer? _vb;
        private MoonWorks.Graphics.Buffer? _ib;
        private uint _indexCount;

        // rotation
        private float _angle;

        private SampleCount _msaaSamples = SampleCount.Four;
        private Texture? _msaaColor; // The offscreen texture for MSAA rendering
        private Texture? _msaaDepth; // The offscreen depth buffer (MSAA)
        private uint _lastWindowWidth;
        private uint _lastWindowHeight;

        public CubeGame() : base(
            new AppInfo("Ayanami", "Cube Demo"),
            new WindowCreateInfo("MoonWorks Cube", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(165, 165),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            InitializeScene();
        }

        private void InitializeScene()
        {
            // Camera setup
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _camera = new Camera(new Vector3(0, 2, 6), Vector3.Zero, Vector3.UnitY)
            {
                Aspect = aspect,
                Near = 0.1f,
                Far = 100f,
                Fov = MathF.PI / 3f
            };

            _msaaColor = Texture.Create2D(
                GraphicsDevice,
                MainWindow.Width,
                MainWindow.Height,
                MainWindow.SwapchainFormat,
                TextureUsageFlags.ColorTarget,
                levelCount: 1,
                sampleCount: _msaaSamples
            );
            _msaaDepth = Texture.Create2D(
                GraphicsDevice,
                MainWindow.Width,
                MainWindow.Height,
                GraphicsDevice.SupportedDepthStencilFormat,
                TextureUsageFlags.DepthStencilTarget,
                levelCount: 1,
                sampleCount: _msaaSamples
            );
            // Store initial size for resize detection
            _lastWindowWidth = MainWindow.Width;
            _lastWindowHeight = MainWindow.Height;

            // Compile shaders from HLSL source via ShaderCross
            ShaderCross.Initialize();

            var vs = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                "Assets/Basic3DObjectRenderer.hlsl",
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
                "Assets/Basic3DObjectRenderer.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "Basic3DObjectRendererPS"
            );



            var vertexInput = VertexInputState.CreateSingleBinding<Vertex>(0);

            // Pipeline (no depth for minimal sample -> use depth if needed)
            _pipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vs,
                FragmentShader = fs,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullBack,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = true,
                    EnableDepthWrite = true,
                    CompareOp = CompareOp.LessOrEqual,
                    CompareMask = 0xFF,
                    WriteMask = 0xFF,
                    EnableStencilTest = false
                },
                TargetInfo = new GraphicsPipelineTargetInfo
                {
                    ColorTargetDescriptions =
                    [
                        new ColorTargetDescription
                        {
                            // Must match the swapchain format for the active window
                            Format = MainWindow.SwapchainFormat,
                            BlendState = ColorTargetBlendState.NoBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "Basic3DObjectRenderer"
            });

            CreateCubeBuffers();
        }

        private void CreateCubeBuffers()
        {
            // Simple unit box centered at origin with per-vertex colors
            var box3DMesh = Mesh.CreateBox3D().Scale(2.5f);

            _indexCount = (uint)box3DMesh.Indices.Length;

            _vb = MoonWorks.Graphics.Buffer.Create<Vertex>(GraphicsDevice, "CubeVB", BufferUsageFlags.Vertex, (uint)box3DMesh.Vertices.Length);
            _ib = MoonWorks.Graphics.Buffer.Create<uint>(GraphicsDevice, "CubeIB", BufferUsageFlags.Index, (uint)box3DMesh.Indices.Length);

            var vUpload = TransferBuffer.Create<Vertex>(GraphicsDevice, "CubeVBUpload", TransferBufferUsage.Upload, (uint)box3DMesh.Vertices.Length);
            var iUpload = TransferBuffer.Create<uint>(GraphicsDevice, "CubeIBUpload", TransferBufferUsage.Upload, (uint)box3DMesh.Indices.Length);

            var vspan = vUpload.Map<Vertex>(cycle: false);
            box3DMesh.Vertices.AsSpan().CopyTo(vspan);
            vUpload.Unmap();

            var ispan = iUpload.Map<uint>(cycle: false);
            box3DMesh.Indices.AsSpan().CopyTo(ispan);
            iUpload.Unmap();

            var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            var copy = cmdbuf.BeginCopyPass();
            copy.UploadToBuffer(vUpload, _vb, false);
            copy.UploadToBuffer(iUpload, _ib, false);
            cmdbuf.EndCopyPass(copy);
            GraphicsDevice.Submit(cmdbuf);

            vUpload.Dispose();
            iUpload.Dispose();
        }

        protected override void Update(TimeSpan delta)
        {
            _angle += (float)delta.TotalSeconds * 0.7f;
            // Keep camera aspect up-to-date on resize
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _camera.Aspect = aspect;

            if (MainWindow.Width != _lastWindowWidth || MainWindow.Height != _lastWindowHeight)
            {
                _msaaColor?.Dispose();
                _msaaDepth?.Dispose();
                _msaaColor = Texture.Create2D(
                    GraphicsDevice,
                    MainWindow.Width,
                    MainWindow.Height,
                    MainWindow.SwapchainFormat,
                    TextureUsageFlags.ColorTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
                _msaaDepth = Texture.Create2D(
                    GraphicsDevice,
                    MainWindow.Width,
                    MainWindow.Height,
                    GraphicsDevice.SupportedDepthStencilFormat,
                    TextureUsageFlags.DepthStencilTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
                _lastWindowWidth = MainWindow.Width;
                _lastWindowHeight = MainWindow.Height;
            }
        }

        protected override void Draw(double alpha)
        {
            var cmdbuf = GraphicsDevice.AcquireCommandBuffer();
            var backbuffer = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (backbuffer == null)
            {
                GraphicsDevice.Submit(cmdbuf);
                return;
            }

            var colorTarget = new ColorTargetInfo(_msaaColor ?? backbuffer, new Color(32, 32, 40, 255));
            if (_msaaColor != null)
            {
                colorTarget.ResolveTexture = backbuffer.Handle; // Resolve MSAA to backbuffer
                colorTarget.StoreOp = StoreOp.Resolve; // Perform resolve into swapchain
            }
            var depthTarget = new DepthStencilTargetInfo(_msaaDepth!, clearDepth: 1f);
            var pass = cmdbuf.BeginRenderPass(depthTarget, colorTarget);

            pass.BindGraphicsPipeline(_pipeline!);
            pass.BindVertexBuffers(_vb!);
            pass.BindIndexBuffer(_ib!, IndexElementSize.ThirtyTwo);

            // Build MVP and push to vertex uniforms at slot 0 (cbuffer b0, space1)
            var model = Matrix4x4.CreateFromYawPitchRoll(_angle, _angle * 0.5f, 0) * Matrix4x4.CreateScale(0.8f);
            var mvp = model * _camera.GetViewMatrix() * _camera.GetProjectionMatrix();
            cmdbuf.PushVertexUniformData(mvp, slot: 0);

            /*

            var dirDir = Vector3.Normalize(new Vector3(-0.4f, -1.0f, -0.3f));
            var dirIntensity = 1.0f;
            var dirColor = new Vector3(1, 1, 1);

            var ptPos = new Vector3(0, 0, 2);
            var ptRange = 8.0f;
            var ptColor = new Vector3(1.0f, 0.85f, 0.7f);
            var ptIntensity = 1.0f;
            var attLin = 0.0f;
            var attQuad = 0.2f;

            var ambient = 0.2f;

            var lightUbo = new LightUniforms
            {
                Dir_Dir_Intensity = new Vector4(dirDir, dirIntensity),
                Dir_Color = new Vector4(dirColor, 0f),
                Pt_Pos_Range = new Vector4(ptPos, ptRange),
                Pt_Color_Intensity = new Vector4(ptColor, ptIntensity),
                Pt_Attenuation = new Vector2(attLin, attQuad),
                Ambient = ambient
            };

            // cbuffer LightParams : register(b0, space3), slot 0 but space(set) 3
            cmdbuf.PushFragmentUniformData(lightUbo, slot: 0);

            */


            pass.DrawIndexedPrimitives(_indexCount, 1, 0, 0, 0);

            cmdbuf.EndRenderPass(pass);
            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Destroy()
        {
            _msaaColor?.Dispose();
            _msaaDepth?.Dispose();
            _vb?.Dispose();
            _ib?.Dispose();
            _pipeline?.Dispose();
        }
    }
}
