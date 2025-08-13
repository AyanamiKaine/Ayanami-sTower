using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Input;
using MoonWorks.Math.Fixed;
using System.Numerics;
using System.Timers;
using Vector3 = System.Numerics.Vector3;

namespace AyanamisTower.StellaEcs.Example;


internal static class Program
{
    public static void Main()
    {
        var game = new HelloGame();
        game.Run();
    }
    private sealed class HelloGame : Game
    {
        private World world = new();
        // Pipeline and buffers
        private GraphicsPipeline cubePipeline;
        private GraphicsPipeline rectPipeline;
        private Mesh cubeMesh;
        private Mesh rectMesh;
        private Vector3 rectColor = new(1f, 0f, 0f); // Start as red
        private float time;
        private Camera camera;
        // Add: FPS counter & window title
        private readonly string baseTitle = "Hello MoonWorks - Shaders";
        private float fpsTimer;
        private int fpsFrames;
        private double fps;
        // Add: rectangle hover state and currently displayed color (may be highlighted)
        private bool rectHovered;
        private Vector3 displayedRectColor;
        // Add: text overlay
        private GraphicsPipeline textPipeline;
        private TextBatch textBatch;
        private Font? uiFont; // MSDF font loaded from Shaders/
        private bool showOverlay = true;
        // New: simple render pipeline orchestrator
        private RenderPipeline renderPipeline;

        public HelloGame() : base(
            appInfo: new AppInfo("MyOrg", "HelloMoonWorks"),
            windowCreateInfo: new WindowCreateInfo(
                windowTitle: "Hello MoonWorks - Shaders",
                windowWidth: 800,
                windowHeight: 480,
                screenMode: ScreenMode.Windowed,
                systemResizable: true,
                startMaximized: false,
                highDPI: false
            ),
            framePacingSettings: FramePacingSettings.CreateCapped(timestepFPS: 240, framerateCapFPS: 240),
            availableShaderFormats: ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC | ShaderFormat.MSL,
            debugMode: true
        )
        {
            var pluginLoader = new HotReloadablePluginLoader(world, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();

            world.CreateEntity()
                .Set(new Position2D(0, 0))
                .Set(new Velocity2D(1, 1));

            world.EnableRestApi();

            ShaderCross.Initialize();

            // Load/compile cube shaders
            var vs = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                filepath: "Assets/Cube.hlsl",
                entrypoint: "VSMain",
                shaderFormat: ShaderCross.ShaderFormat.HLSL,
                shaderStage: ShaderStage.Vertex,
                enableDebug: false,
                name: "CubeVS"
            );
            var ps = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                filepath: "Assets/Cube.hlsl",
                entrypoint: "PSMain",
                shaderFormat: ShaderCross.ShaderFormat.HLSL,
                shaderStage: ShaderStage.Fragment,
                enableDebug: false,
                name: "CubePS"
            );


            // Cube pipeline: float3 position, float3 color
            var cubeVertexInput = new VertexInputState
            {
                VertexBufferDescriptions =
                [
                    VertexBufferDescription.Create<Mesh.Vertex3D>(slot: 0)
                ],
                VertexAttributes =
                [
                    new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = 0 },
                    new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector3>() }
                ]
            };

            cubePipeline = GraphicsPipeline.Create(
                GraphicsDevice,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = vs,
                    FragmentShader = ps,
                    VertexInputState = cubeVertexInput,
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
                        ColorTargetDescriptions =
                        [
                            new ColorTargetDescription
                            {
                                Format = MainWindow.SwapchainFormat,
                                BlendState = ColorTargetBlendState.NoBlend
                            }
                        ]
                    },
                    Name = "CubePipeline"
                }
            );

            // Rectangle pipeline: float2 position, float3 color
            var rectVertexInput = new VertexInputState
            {
                VertexBufferDescriptions =
                [
                    VertexBufferDescription.Create<Mesh.Vertex>(slot: 0)
                ],
                VertexAttributes =
                [
                    new VertexAttribute { Location = 0, BufferSlot = 0, Format = VertexElementFormat.Float2, Offset = 0 },
                    new VertexAttribute { Location = 1, BufferSlot = 0, Format = VertexElementFormat.Float3, Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<System.Numerics.Vector2>() }
                ]
            };

            rectPipeline = GraphicsPipeline.Create(
                GraphicsDevice,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = vs,
                    FragmentShader = ps,
                    VertexInputState = rectVertexInput,
                    PrimitiveType = PrimitiveType.TriangleList,
                    RasterizerState = RasterizerState.CCW_CullBack,
                    MultisampleState = MultisampleState.None,
                    DepthStencilState = new DepthStencilState
                    {
                        EnableDepthTest = false,
                        EnableDepthWrite = false,
                        EnableStencilTest = false,
                        CompareOp = CompareOp.Always
                    },
                    TargetInfo = new GraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions =
                        [
                            new ColorTargetDescription
                            {
                                Format = MainWindow.SwapchainFormat,
                                BlendState = ColorTargetBlendState.NoBlend
                            }
                        ]
                    },
                    Name = "RectPipeline"
                }
            );

            // Text pipeline (MSDF): uses built-in text shaders
            textPipeline = GraphicsPipeline.Create(
                GraphicsDevice,
                new GraphicsPipelineCreateInfo
                {
                    VertexShader = GraphicsDevice.TextVertexShader,
                    FragmentShader = GraphicsDevice.TextFragmentShader,
                    VertexInputState = GraphicsDevice.TextVertexInputState,
                    PrimitiveType = PrimitiveType.TriangleList,
                    RasterizerState = RasterizerState.CCW_CullNone,
                    MultisampleState = MultisampleState.None,
                    DepthStencilState = DepthStencilState.Disable,
                    TargetInfo = new GraphicsPipelineTargetInfo
                    {
                        ColorTargetDescriptions =
                        [
                            new ColorTargetDescription
                            {
                                Format = MainWindow.SwapchainFormat,
                                // MSDF shaders output straight alpha
                                BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                            }
                        ]
                    },
                    Name = "TextPipeline"
                }
            );

            // Create cube mesh
            cubeMesh = Mesh.CreateBox3D(GraphicsDevice, 0.7f);

            // Create camera
            camera = new Camera();

            // Create 2D rectangle mesh (centered at origin, size 1x1)
            rectMesh = Mesh.CreateQuad(GraphicsDevice, 1f, 1f, rectColor);
            displayedRectColor = rectColor;

            // Initialize text batch and try to load an MSDF font from Assets/
            textBatch = new TextBatch(GraphicsDevice);
            // Provide the path to your MSDF font (.ttf/.otf with matching .json and .png atlas next to it)
            // Example expected files: Assets/Roboto-Regular.ttf, Assets/Roboto-Regular.json, Assets/Roboto-Regular.png
            uiFont = Font.Load(GraphicsDevice, RootTitleStorage, "Assets/Roboto-Regular.ttf");
            if (uiFont == null)
            {
                Logger.LogWarn("MSDF font not found in Assets/. Place .ttf/.otf with matching .json and .png next to it (msdf-atlas-gen output). Overlay will be disabled.");
                showOverlay = false;
            }

            // Build render pipeline steps
            renderPipeline = new RenderPipeline()
                .Add(new CubeRenderStep(
                    cubePipeline,
                    () => cubeMesh,
                    () => Matrix4x4.CreateFromYawPitchRoll(time, time * 0.7f, 0)
                ))
                .Add(new RectRenderStep(
                    rectPipeline,
                    () => rectMesh,
                    () =>
                    {
                        var orthoProj = Matrix4x4.CreateOrthographicOffCenter(-2, 2, -1.5f, 1.5f, -1, 1);
                        var rectModel = Matrix4x4.CreateTranslation(new Vector3(0.5f, 0.5f, 0));
                        return rectModel * orthoProj;
                    }
                ))
                .Add(new TextOverlayRenderStep(
                    textPipeline,
                    textBatch,
                    () =>
                    {
                        // Builder returns whether to show and how to populate the batch for this frame
                        return (showOverlay && uiFont != null, batch =>
                        {
                            const int size = 18;
                            const float x = 12f;
                            float y = 22f;
                            var white = new Color(230, 235, 245);
                            var accent = new Color(170, 200, 255);

                            batch.Add(uiFont!, "MoonWorks Example", size + 2, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), accent);
                            y += 26f;
                            batch.Add(uiFont!, "Esc: Quit", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                            y += 20f;
                            batch.Add(uiFont!, "F11: Toggle Fullscreen", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                            y += 20f;
                            batch.Add(uiFont!, "Mouse Wheel: Zoom Camera", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                            y += 20f;
                            batch.Add(uiFont!, "Click square to toggle color", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                            y += 20f;
                            batch.Add(uiFont!, $"FPS: {(int)fps}", size, Matrix4x4.CreateTranslation(new Vector3(x, y, 0)), white);
                        }
                        );
                    }
                ));

            renderPipeline.Initialize(GraphicsDevice);
        }

        private readonly struct Vertex(System.Numerics.Vector2 pos, float r, float g, float b)
        {
            public readonly System.Numerics.Vector2 Pos = pos;
            public readonly float R = r;
            public readonly float G = g;
            public readonly float B = b;
        }

        protected override void Update(TimeSpan delta)
        {
            if (Inputs.Keyboard.IsPressed(KeyCode.Escape))
            {
                Quit();
            }

            // Toggle fullscreen with F11
            if (Inputs.Keyboard.IsPressed(KeyCode.F11))
            {
                MainWindow.SetScreenMode(
                    MainWindow.ScreenMode == ScreenMode.Windowed ? ScreenMode.Fullscreen : ScreenMode.Windowed
                );
            }

            // Toggle overlay with F1
            if (Inputs.Keyboard.IsPressed(KeyCode.F1))
            {
                showOverlay = !showOverlay;
            }

            // Mouse wheel zoom: adjust camera FOV
            if (Inputs.Mouse.Wheel != 0)
            {
                var newFov = camera.Fov - (Inputs.Mouse.Wheel * 0.05f);
                if (newFov < 0.3f) newFov = 0.3f;
                else if (newFov > 1.6f) newFov = 1.6f;
                camera.Fov = newFov;
            }

            time += (float)delta.TotalSeconds;

            // Simple WASD camera movement
            float moveSpeed = 2.5f * (float)delta.TotalSeconds;
            var move = Vector3.Zero;
            if (Inputs.Keyboard.IsDown(KeyCode.W)) move += new Vector3(0, 0, -moveSpeed);
            if (Inputs.Keyboard.IsDown(KeyCode.S)) move += new Vector3(0, 0, moveSpeed);
            if (Inputs.Keyboard.IsDown(KeyCode.A)) move += new Vector3(-moveSpeed, 0, 0);
            if (Inputs.Keyboard.IsDown(KeyCode.D)) move += new Vector3(moveSpeed, 0, 0);
            camera.Move(move);

            // Rectangle hover + click detection (orthographic space)
            {
                int mouseX = Inputs.Mouse.X;
                int mouseY = Inputs.Mouse.Y;
                float winW = MainWindow.Width;
                float winH = MainWindow.Height;

                float ndcX = (2f * (mouseX / winW)) - 1f;
                float ndcY = 1f - (2f * (mouseY / winH));

                float worldX = ndcX * 2f;   // ortho x: [-2, 2]
                float worldY = ndcY * 1.5f; // ortho y: [-1.5, 1.5]

                const float rectLeft = 0.0f;
                const float rectRight = 1.0f;
                const float rectTop = 1.0f;
                const float rectBottom = 0.0f;

                bool inside = worldX >= rectLeft && worldX <= rectRight && worldY >= rectBottom && worldY <= rectTop;

                // Hover highlight: only rebuild mesh when visible color changes
                if (inside != rectHovered)
                {
                    rectHovered = inside;
                    var targetColor = rectHovered ? Saturate(rectColor * 1.35f) : rectColor;
                    if (!ApproximatelyEqual(displayedRectColor, targetColor))
                    {
                        displayedRectColor = targetColor;
                        rectMesh = Mesh.CreateQuad(GraphicsDevice, 1f, 1f, displayedRectColor);
                    }
                }

                if (inside && Inputs.Mouse.LeftButton.IsPressed)
                {
                    // Toggle color between red and green, keep hover highlight
                    rectColor = (rectColor.X == 1f && rectColor.Y == 0f)
                        ? new Vector3(0f, 1f, 0f)
                        : new Vector3(1f, 0f, 0f);

                    var targetColor = rectHovered ? Saturate(rectColor * 1.35f) : rectColor;
                    if (!ApproximatelyEqual(displayedRectColor, targetColor))
                    {
                        displayedRectColor = targetColor;
                        rectMesh = Mesh.CreateQuad(GraphicsDevice, 1f, 1f, displayedRectColor);
                    }
                }
            }

            // Update FPS in window title about twice a second
            fpsFrames++;
            fpsTimer += (float)delta.TotalSeconds;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsFrames / fpsTimer;
                var ms = 1000.0 / global::System.Math.Max(fps, 0.0001);
                MainWindow.SetTitle($"{baseTitle} | FPS {fps:0} ({ms:0.0} ms) | F11 Fullscreen | Wheel Zoom | Esc Quit | Click square | F1 Help");
                fpsFrames = 0;
                fpsTimer = 0f;
            }
            world.Update(1f / 60f); // Update with delta time

            renderPipeline.Update(delta);
        }

        private static Vector3 Saturate(Vector3 v)
        {
            return new Vector3(MathF.Min(v.X, 1f), MathF.Min(v.Y, 1f), MathF.Min(v.Z, 1f));
        }

        private static bool ApproximatelyEqual(Vector3 a, Vector3 b)
        {
            const float eps = 1e-3f;
            return MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps && MathF.Abs(a.Z - b.Z) < eps;
        }

        protected override void Draw(double alpha)
        {
            // Calculate view/projection matrices for this frame
            var cmdbuf = GraphicsDevice.AcquireCommandBuffer();

            // Update camera aspect and matrices
            var width = MainWindow.Width;
            var height = MainWindow.Height;
            camera.Aspect = (float)width / height;
            var proj = camera.GetProjectionMatrix();
            var view = camera.GetViewMatrix();
            Matrix4x4 orthoPx = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

            var ctx = new ViewContext(view, proj, orthoPx, (int)width, (int)height);

            // Run any pre-pass work (uploads/copies, etc.)
            renderPipeline.Prepare(cmdbuf, ctx);

            var swapchain = cmdbuf.AcquireSwapchainTexture(MainWindow);
            if (swapchain == null)
            {
                GraphicsDevice.Cancel(cmdbuf);
                return;
            }

            var colorTarget = new ColorTargetInfo(swapchain, new Color(10, 20, 40));
            var renderPass = cmdbuf.BeginRenderPass([colorTarget]);
            renderPass.SetViewport(new Viewport(swapchain.Width, swapchain.Height));

            // Record all steps
            renderPipeline.Record(cmdbuf, renderPass, ctx);

            cmdbuf.EndRenderPass(renderPass);
            GraphicsDevice.Submit(cmdbuf);
        }

        protected override void Destroy()
        {
            // Let GraphicsDevice dispose managed GPU resources on shutdown.
            // No explicit cleanup needed here for the demo.
            renderPipeline?.Dispose();
        }
    }
}
