using System;
using System.Numerics;
using System.IO;
using System.Runtime.InteropServices;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using MoonWorks.Storage;

namespace AyanamisTower.StellaEcs.StellaInvicta;

internal static class Program
{
    public static void Main()
    {
        var game = new StellaInvicta();
        game.Run();
    }

    private sealed class StellaInvicta : Game
    {
        /// <summary>
        /// Represents the current game world.
        /// </summary>
        public readonly World World = new();
        // Camera
        private Camera _camera = null!;
        private CameraController _cameraController = null!;

        // GPU resources
        private GraphicsPipeline? _pipeline;
        // rotation
        private float _angle;
        // textures
        private Texture? _whiteTexture;
        private Texture? _checkerTexture;

        private SampleCount _msaaSamples = SampleCount.Four;
        private Texture? _msaaColor; // The offscreen texture for MSAA rendering
        private Texture? _msaaDepth; // The offscreen depth buffer (MSAA)
                                     // Skybox resources
        private GraphicsPipeline? _skyboxPipeline;
        private GraphicsPipeline? _skyboxCubePipeline;
        private Texture? _skyboxTexture;
        private GpuMesh? _skyboxMesh;
        private float _skyboxScale = 50f;
        private bool _skyboxEnabled;
        public StellaInvicta() : base(
            new AppInfo("Ayanami", "Stella Invicta Demo"),
            new WindowCreateInfo("Stella Invicta", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(165, 165),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            InitializeScene();
        }

        public void EnableMSAA(SampleCount sampleCount)
        {


        }

        public void DisableMSAA()
        {

        }

        public void EnableVSync()
        {
            GraphicsDevice.SetSwapchainParameters(MainWindow, MainWindow.SwapchainComposition, PresentMode.VSync);

            // Can improve the frame latency
            // GraphicsDevice.SetAllowedFramesInFlight(1);
        }

        public void DisableVSync()
        {
            GraphicsDevice.SetSwapchainParameters(MainWindow, MainWindow.SwapchainComposition, PresentMode.Immediate);
        }

        private void InitializeScene()
        {
            var pluginLoader = new HotReloadablePluginLoader(World, "Plugins");

            // 3. Load all plugins that already exist in the folder at startup.
            pluginLoader.LoadAllExistingPlugins();

            // 4. Start watching for any new plugins or changes.
            pluginLoader.StartWatching();

            World.EnableRestApi();

            EnableVSync();

            // Camera setup
            var aspect = (float)MainWindow.Width / MainWindow.Height;
            _camera = new Camera(new Vector3(0, 2, 6), Vector3.Zero, Vector3.UnitY)
            {
                Aspect = aspect,
                Near = 0.1f,
                Far = 100f,
                Fov = MathF.PI / 3f
            };
            _cameraController = new CameraController(_camera);

            // MSAA targets are (re)created in Draw to match the actual swapchain size.
            _msaaColor = null;
            _msaaDepth = null;

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
            var vsSkyCube = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                "Assets/SkyboxCube.hlsl",
                "VSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Vertex,
                false,
                "SkyboxCubeVS"
            );
            var fsSkyCube = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                "Assets/SkyboxCube.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "SkyboxCubePS"
            );
            // Create simple textures
            _whiteTexture = CreateSolidTexture(255, 255, 255, 255);
            _checkerTexture = CreateCheckerboard(256, 256, 8,
                (230, 230, 230, 255), (40, 40, 40, 255));



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

            // Dedicated skybox pipeline: no depth write/test, no culling (render inside of sphere)
            _skyboxPipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vs,
                FragmentShader = fs,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    CompareOp = CompareOp.Always,
                    CompareMask = 0xFF,
                    WriteMask = 0x00,
                    EnableStencilTest = false
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
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "SkyboxRenderer"
            });

            _skyboxCubePipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vsSkyCube,
                FragmentShader = fsSkyCube,
                VertexInputState = vertexInput,
                PrimitiveType = PrimitiveType.TriangleList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = false,
                    EnableDepthWrite = false,
                    CompareOp = CompareOp.Always,
                    CompareMask = 0xFF,
                    WriteMask = 0x00,
                    EnableStencilTest = false
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
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "SkyboxCubeRenderer"
            });

            World.OnSetPost((Entity entity, in Mesh _, in Mesh mesh, bool _) =>
            {
                entity.Set(GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "Cube"));
            });


            // The Sun: Center of the solar system.
            World.CreateEntity()
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(0, 0, 0)) // Positioned at the origin
                .Set(new Size3D(15.0f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.15f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Sun.jpg") ?? _checkerTexture! });

            // Mercury: The closest planet to the Sun.
            World.CreateEntity()
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(7.7f, 0, 0)) // Position is ~0.38 AU from the Sun
                .Set(new Size3D(0.38f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.15f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Mercury.jpg") ?? _checkerTexture! });

            // Venus: The second planet from the Sun.
            World.CreateEntity()
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(14.5f, 0, 0)) // Position is ~0.72 AU from the Sun
                .Set(new Size3D(0.95f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.15f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Venus.jpg") ?? _checkerTexture! });

            // Earth: Our baseline for distance (1 Astronomical Unit).
            World.CreateEntity()
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(20.0f, 0, 0)) // We define this distance as 1 AU
                .Set(new Size3D(1.0f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.15f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Earth.jpg") ?? _checkerTexture! });

            // The Moon: Positioned relative to Earth.
            World.CreateEntity()
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(21.5f, 0, 0)) // Exaggerated distance from Earth for visibility
                .Set(new Size3D(0.27f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.15f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Moon.jpg") ?? _checkerTexture! });

            // Example usage:
            // SetSkybox("Assets/skybox.jpg", 50f);
            // Or, for cubemap:
            SetSkyboxCube([
                "Assets/Sky/px.png","Assets/Sky/nx.png",
                "Assets/Sky/py.png","Assets/Sky/ny.png",
                "Assets/Sky/pz.png","Assets/Sky/nz.png"
            ]);
        }

        /// <summary>
        /// Creates a large textured sphere centered on the camera and draws it as a skybox.
        /// Pass a 2D panoramic/equirectangular image (JPG/PNG) or DDS. Call once to set/replace.
        /// </summary>
        private void SetSkybox(string path, float scale = 50f)
        {
            var tex = LoadTextureFromFile(path);
            if (tex == null)
            {
                Console.WriteLine($"[SetSkybox] Failed to load skybox texture: {path}");
                _skyboxEnabled = false;
                return;
            }

            _skyboxTexture?.Dispose();
            _skyboxTexture = tex;
            _skyboxScale = scale;

            if (_skyboxMesh == null)
            {
                var mesh = Mesh.CreateSphere3D();
                _skyboxMesh = GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "SkyboxSphere");
            }

            _skyboxEnabled = true;
        }

        // Normalize to TitleStorage-friendly relative POSIX path
        private static string NormalizeTitlePath(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
            {
                var baseDir = AppContext.BaseDirectory.Replace('\\', '/');
                if (normalized.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(baseDir.Length);
                }
                else
                {
                    // Leave as-is; TitleStorage calls will fail and log
                }
            }
            return normalized.TrimStart('/');
        }

        /// <summary>
        /// Creates a cubemap texture from six images. Expected order: +X, -X, +Y, -Y, +Z, -Z.
        /// All images must be square and same size. Returns null on failure.
        /// </summary>
        private Texture? CreateCubemapFromSixFiles(string posX, string negX, string posY, string negY, string posZ, string negZ)
        {
            string[] input =
            [
                NormalizeTitlePath(posX),
                NormalizeTitlePath(negX),
                NormalizeTitlePath(posY),
                NormalizeTitlePath(negY),
                NormalizeTitlePath(posZ),
                NormalizeTitlePath(negZ)
            ];

            // Validate existence and gather dimensions
            uint size = 0;
            for (int i = 0; i < 6; i++)
            {
                if (!RootTitleStorage.GetFileSize(input[i], out _))
                {
                    Console.WriteLine($"[CreateCubemap] Missing file: {input[i]}");
                    return null;
                }
                if (!MoonWorks.Graphics.ImageUtils.ImageInfoFromFile(RootTitleStorage, input[i], out var w, out var h, out _))
                {
                    Console.WriteLine($"[CreateCubemap] Unsupported or corrupt image: {input[i]}");
                    return null;
                }
                if (w != h)
                {
                    Console.WriteLine($"[CreateCubemap] Image is not square: {input[i]} ({w}x{h})");
                    return null;
                }
                if (i == 0) { size = w; }
                else if (w != size || h != size)
                {
                    Console.WriteLine($"[CreateCubemap] Image size mismatch: {input[i]} ({w}x{h}) expected {size}x{size}");
                    return null;
                }
            }

            using var uploader = new ResourceUploader(GraphicsDevice);
            var cube = Texture.CreateCube(GraphicsDevice, "Cubemap", size, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler, levelCount: 1);
            if (cube == null)
            {
                Console.WriteLine("[CreateCubemap] Failed to create cube texture");
                return null;
            }

            for (int face = 0; face < 6; face++)
            {
                var region = new TextureRegion
                {
                    Texture = cube.Handle,
                    Layer = (uint)face,
                    MipLevel = 0,
                    X = 0,
                    Y = 0,
                    Z = 0,
                    W = size,
                    H = size,
                    D = 1
                };
                uploader.SetTextureDataFromCompressed(RootTitleStorage, input[face], region);
            }

            uploader.UploadAndWait();
            return cube;
        }

        /// <summary>
        /// Sets a cubemap skybox using 6 face file paths in the order:
        /// +X, -X, +Y, -Y, +Z, -Z. Builds a cube mesh if needed and binds a cubemap pipeline.
        /// </summary>
        private void SetSkyboxCube(string[] facePaths, float scale = 50f)
        {
            if (facePaths == null || facePaths.Length != 6)
            {
                Console.WriteLine("[SetSkyboxCube] Provide exactly 6 file paths: +X,-X,+Y,-Y,+Z,-Z");
                _skyboxEnabled = false;
                return;
            }

            var cubeTex = CreateCubemapFromSixFiles(facePaths[0], facePaths[1], facePaths[2], facePaths[3], facePaths[4], facePaths[5]);
            if (cubeTex == null)
            {
                Console.WriteLine("[SetSkyboxCube] Failed to create cubemap");
                _skyboxEnabled = false;
                return;
            }

            _skyboxTexture?.Dispose();
            _skyboxTexture = cubeTex;
            _skyboxScale = scale;

            // Use a cube mesh so positions map to cube directions cleanly
            if (_skyboxMesh == null)
            {
                var cube = Mesh.CreateBox3D(1, 1, 1);
                _skyboxMesh = GpuMesh.Upload(GraphicsDevice, cube.Vertices.AsSpan(), cube.Indices.AsSpan(), "SkyboxCubeMesh");
            }

            _skyboxEnabled = true;
        }

        private Texture CreateSolidTexture(byte r, byte g, byte b, byte a)
        {
            using var uploader = new ResourceUploader(GraphicsDevice, 4);
            var tex = uploader.CreateTexture2D(
                "Solid",
                [r, g, b, a],
                TextureFormat.R8G8B8A8Unorm,
                TextureUsageFlags.Sampler,
                1, 1);
            uploader.UploadAndWait();
            return tex;
        }

        private Texture CreateCheckerboard(uint width, uint height, int cells, (byte r, byte g, byte b, byte a) c0, (byte r, byte g, byte b, byte a) c1)
        {
            int w = (int)width, h = (int)height;
            var data = new byte[w * h * 4];
            int cellW = Math.Max(1, w / cells);
            int cellH = Math.Max(1, h / cells);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool useC0 = ((x / cellW) + (y / cellH)) % 2 == 0;
                    var (r, g, b, a) = useC0 ? c0 : c1;
                    int i = ((y * w) + x) * 4;
                    data[i + 0] = r;
                    data[i + 1] = g;
                    data[i + 2] = b;
                    data[i + 3] = a;
                }
            }
            using var uploader = new ResourceUploader(GraphicsDevice, (uint)data.Length);
            var tex = uploader.CreateTexture2D<byte>(
                "Checker",
                data,
                TextureFormat.R8G8B8A8Unorm,
                TextureUsageFlags.Sampler,
                width, height);
            uploader.UploadAndWait();
            return tex;
        }

        /// <summary>
        /// Loads an image file (PNG, JPG, BMP, etc.) or a DDS file and uploads it as a GPU texture.
        /// For standard image formats, data is decoded to RGBA8 and uploaded as a 2D texture.
        /// For DDS, existing mip levels and cube faces are preserved.
        /// Returns null if the file could not be read or decoded.
        /// </summary>
        private Texture? LoadTextureFromFile(string path)
        {
            // TitleStorage requires POSIX-style separators and relative paths.
            // Normalize separators and, if given an absolute path, try to make it relative to the app base.
            var normalized = path.Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
            {
                var baseDir = AppContext.BaseDirectory.Replace('\\', '/');
                if (normalized.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(baseDir.Length);
                }
                else
                {
                    Console.WriteLine($"[LoadTextureFromFile] Absolute path outside TitleStorage root not supported: {normalized}");
                    return null;
                }
            }

            // Choose loader by file extension
            var ext = Path.GetExtension(normalized).ToLowerInvariant();

            using var uploader = new ResourceUploader(GraphicsDevice);
            Texture? texture;
            var name = Path.GetFileNameWithoutExtension(normalized);
            if (ext == ".dds")
            {
                // Quick existence check
                if (!RootTitleStorage.GetFileSize(normalized, out _))
                {
                    Console.WriteLine($"[LoadTextureFromFile] File not found in TitleStorage: {normalized}");
                    return null;
                }
                texture = uploader.CreateTextureFromDDS(name, RootTitleStorage, normalized);
            }
            else
            {
                // Quick existence check
                if (!RootTitleStorage.GetFileSize(normalized, out _))
                {
                    Console.WriteLine($"[LoadTextureFromFile] File not found in TitleStorage: {normalized}");
                    return null;
                }
                texture = uploader.CreateTexture2DFromCompressed(
                    name,
                    RootTitleStorage,
                    normalized,
                    TextureFormat.R8G8B8A8Unorm,
                    TextureUsageFlags.Sampler
                );
                if (texture == null)
                {
                    // Fallback: try StbImageSharp to decode common formats (JPEG/PNG/etc.)
                    try
                    {
                        if (!RootTitleStorage.GetFileSize(normalized, out var fileSize) || fileSize == 0)
                        {
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: file not found or empty: {normalized}");
                            return null;
                        }

                        var bytes = new byte[fileSize];
                        if (!RootTitleStorage.ReadFile(normalized, bytes))
                        {
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: failed to read file: {normalized}");
                            return null;
                        }

                        // Decode to RGBA using StbImageSharp
                        var img = StbImageSharp.ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
                        if (img == null || img.Data == null || img.Width <= 0 || img.Height <= 0)
                        {
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: decode failed for: {normalized}");
                            return null;
                        }

                        texture = uploader.CreateTexture2D<byte>(
                            name,
                            img.Data,
                            TextureFormat.R8G8B8A8Unorm,
                            TextureUsageFlags.Sampler,
                            (uint)img.Width,
                            (uint)img.Height
                        );
                        if (texture == null)
                        {
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: texture creation failed for: {normalized}");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LoadTextureFromFile] Fallback exception for {normalized}: {ex.Message}");
                        return null;
                    }
                }
            }

            uploader.UploadAndWait();
            return texture;
        }


        protected override void Update(TimeSpan delta)
        {
            _angle += (float)delta.TotalSeconds * 0.7f;
            // Keep camera aspect up-to-date on resize
            _camera.Aspect = (float)((float)MainWindow.Width / MainWindow.Height);
            // Update camera via controller abstraction
            _cameraController.Update(Inputs, MainWindow, delta);
            World.Update((float)delta.TotalSeconds);
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
            /////////////////////
            // MSAA PIPELINE STEP BEGIN 
            /////////////////////
            // Ensure MSAA targets match the current swapchain size (handles window resizes, DPI changes, etc.)
            EnsureMsaaTargets(backbuffer);

            var colorTarget = new ColorTargetInfo(_msaaColor ?? backbuffer, new Color(32, 32, 40, 255));
            if (_msaaColor != null)
            {
                colorTarget.ResolveTexture = backbuffer.Handle; // Resolve MSAA to backbuffer
                colorTarget.StoreOp = StoreOp.Resolve; // Perform resolve into swapchain
            }
            var depthTarget = new DepthStencilTargetInfo(_msaaDepth!, clearDepth: 1f);
            /////////////////////
            // RENDER PASS BEGIN
            /////////////////////
            var pass = cmdbuf.BeginRenderPass(depthTarget, colorTarget);

            // Draw skybox first (no depth write/test) so world renders on top
            if (_skyboxEnabled && _skyboxTexture != null && _skyboxMesh.HasValue)
            {
                // Choose pipeline based on texture type
                var isCube = _skyboxTexture.Type == TextureType.Cube;
                pass.BindGraphicsPipeline(isCube ? _skyboxCubePipeline! : _skyboxPipeline!);
                var skyMesh = _skyboxMesh.Value;
                skyMesh.Bind(pass);
                // Use wrap sampling for skyboxes
                var skySampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearWrap);
                pass.BindFragmentSamplers(0, new TextureSamplerBinding[] { new(_skyboxTexture, skySampler) });

                var modelSky = Matrix4x4.CreateScale(_skyboxScale) * Matrix4x4.CreateTranslation(_camera.Position);
                var mvpSky = modelSky * _camera.GetViewMatrix() * _camera.GetProjectionMatrix();
                cmdbuf.PushVertexUniformData(mvpSky, slot: 0);
                pass.DrawIndexedPrimitives(skyMesh.IndexCount, 1, 0, 0, 0);
                skySampler.Dispose();
            }

            pass.BindGraphicsPipeline(_pipeline!);


            foreach (var entity in World.Query(typeof(GpuMesh)))
            {
                var gpuMesh = entity.GetMut<GpuMesh>();
                gpuMesh.Bind(pass);

                // Bind entity texture if present, otherwise bind dummy
                var texture = _whiteTexture!;
                if (entity.Has<Texture2DRef>())
                {
                    var texRef = entity.GetMut<Texture2DRef>();
                    if (texRef.Texture != null) { texture = texRef.Texture; }
                }
                pass.BindFragmentSamplers(new TextureSamplerBinding(texture, GraphicsDevice.LinearSampler));

                // Build MVP and push to vertex uniforms at slot 0 (cbuffer b0, space1)
                // Translation from ECS Position3D (if present)
                Vector3 translation = Vector3.Zero;
                if (entity.Has<Position3D>())
                {
                    translation = entity.GetMut<Position3D>().Value;
                }

                Quaternion rotation = Quaternion.Identity;
                if (entity.Has<Rotation3D>())
                {
                    rotation = entity.GetMut<Rotation3D>().Value;
                }

                Vector3 size = Vector3.One;
                if (entity.Has<Size3D>())
                {
                    size = entity.GetMut<Size3D>().Value;
                }

                // Apply transforms in the correct order so translation is not affected by scale:
                // Scale -> Rotate -> Translate
                var model = Matrix4x4.CreateScale(size) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
                var mvp = model * _camera.GetViewMatrix() * _camera.GetProjectionMatrix();

                cmdbuf.PushVertexUniformData(mvp, slot: 0);
                pass.DrawIndexedPrimitives(gpuMesh.IndexCount, 1, 0, 0, 0);
            }


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


            cmdbuf.EndRenderPass(pass);

            /////////////////////
            // RENDER PASS END
            /////////////////////

            GraphicsDevice.Submit(cmdbuf);
        }


        /// <summary>
        /// Ensures that the MSAA targets are created and match the size of the backbuffer.
        /// We must do this when resizing the window or changing the DPI. Otherwise there will
        /// be a mismatch and most likely artifacts or a crash.
        /// </summary>
        private void EnsureMsaaTargets(Texture backbuffer)
        {
            // Only recreate when using MSAA
            if (_msaaSamples == SampleCount.One)
            {
                // If MSAA is disabled, dispose any MSAA resources
                _msaaColor?.Dispose();
                _msaaColor = null;
                _msaaDepth?.Dispose();
                _msaaDepth = null; return;
            }

            bool needsColor = _msaaColor == null || _msaaColor.Width != backbuffer.Width || _msaaColor.Height != backbuffer.Height;
            bool needsDepth = _msaaDepth == null || _msaaDepth.Width != backbuffer.Width || _msaaDepth.Height != backbuffer.Height;

            if (needsColor)
            {
                _msaaColor?.Dispose();
                _msaaColor = Texture.Create2D(
                    GraphicsDevice,
                    backbuffer.Width,
                    backbuffer.Height,
                    MainWindow.SwapchainFormat,
                    TextureUsageFlags.ColorTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
            }

            if (needsDepth)
            {
                _msaaDepth?.Dispose();
                _msaaDepth = Texture.Create2D(
                    GraphicsDevice,
                    backbuffer.Width,
                    backbuffer.Height,
                    GraphicsDevice.SupportedDepthStencilFormat,
                    TextureUsageFlags.DepthStencilTarget,
                    levelCount: 1,
                    sampleCount: _msaaSamples
                );
            }
        }

        protected override void Destroy()
        {
            _msaaColor?.Dispose();
            _msaaDepth?.Dispose();
            _pipeline?.Dispose();
            _skyboxPipeline?.Dispose();
            _skyboxCubePipeline?.Dispose();
            _whiteTexture?.Dispose();
            _checkerTexture?.Dispose();
            _skyboxTexture?.Dispose();
        }
    }
}
