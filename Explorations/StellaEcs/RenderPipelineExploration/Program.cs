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

    [StructLayout(LayoutKind.Sequential)]
    private struct LightUniforms
    {
        public Vector4 Dir_Dir_Intensity;   // xyz = dir, w = intensity
        public Vector4 Dir_Color;           // rgb = color
        public Vector4 Pt_Pos_Range;        // xyz = pos, w = range
        public Vector4 Pt_Color_Intensity;  // rgb = color, w = intensity
        public Vector2 Pt_Attenuation;      // x = linear, y = quadratic
        private Vector2 _pad;               // padding to 16-byte alignment
        public float Ambient;
        private Vector3 _pad2;              // padding to 16-byte alignment
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VSUniforms
    {
        public Matrix4x4 Model;
        public Matrix4x4 MVP;
    }

    private sealed class CubeGame : Game
    {
        // Camera state
        private Vector3 _camPos = new(0, 2, 6);
        private Vector3 _camTarget = Vector3.Zero;
        private Matrix4x4 _view;
        private Matrix4x4 _proj;

        // GPU resources
        private GraphicsPipeline? _pipeline;
        private MoonWorks.Graphics.Buffer? _vb;
        private MoonWorks.Graphics.Buffer? _ib;
        private uint _indexCount;

        // rotation
        private float _angle;

        public CubeGame() : base(
            new AppInfo("Ayanami", "Cube Demo"),
            new WindowCreateInfo("MoonWorks Cube", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(60, 60),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            InitializeScene();
        }

        private void InitializeScene()
        {
            // Camera matrices
            _view = Matrix4x4.CreateLookAt(_camPos, _camTarget, Vector3.UnitY);
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, aspect, 0.1f, 100f);

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
                MultisampleState = MultisampleState.None,
                DepthStencilState = DepthStencilState.Disable,
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
                    HasDepthStencilTarget = false
                },
                Name = "CubePipeline"
            });

            CreateCubeBuffers();
        }

        private void CreateCubeBuffers()
        {
            // Simple unit box centered at origin with per-vertex colors
            var box3DMesh = Mesh.CreateBox3D();

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
            // Keep view proj up-to-date on resize
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, aspect, 0.1f, 100f);
            _view = Matrix4x4.CreateLookAt(_camPos, _camTarget, Vector3.UnitY);
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

            var colorTarget = new ColorTargetInfo(backbuffer, new Color(32, 32, 40, 255));
            var pass = cmdbuf.BeginRenderPass(colorTarget);

            pass.BindGraphicsPipeline(_pipeline!);
            pass.BindVertexBuffers(_vb!);
            pass.BindIndexBuffer(_ib!, IndexElementSize.ThirtyTwo);

            // Build model and MVP and push to vertex uniforms at slot 0 (VSParams: model, mvp)
            var model = Matrix4x4.CreateFromYawPitchRoll(_angle, _angle * 0.5f, 0) * Matrix4x4.CreateScale(0.8f);
            var mvp = model * _view * _proj;
            var vsUniforms = new VSUniforms { Model = model, MVP = mvp };
            cmdbuf.PushVertexUniformData(vsUniforms, slot: 0);

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
            cmdbuf.PushFragmentUniformData(lightUbo, slot: 0);


            pass.DrawIndexedPrimitives(_indexCount, 1, 0, 0, 0);

            cmdbuf.EndRenderPass(pass);
            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Destroy()
        {
            _vb?.Dispose();
            _ib?.Dispose();
            _pipeline?.Dispose();
        }
    }
}
