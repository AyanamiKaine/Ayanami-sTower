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
using StellaInvicta;
using BepuPhysics;
using BepuUtilities.Memory;
using BepuUtilities;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;

namespace AyanamisTower.StellaEcs.StellaInvicta;

/// <summary>
/// Represents a celestial body in the game world.
/// </summary>
public struct CelestialBody { };

/// <summary>
/// Tag component to mark an entity as a kinematic body in the physics world.
/// </summary>
public struct Kinematic { };

/// <summary>
/// Stores a Bepu body handle for entities that have a physics body.
/// </summary>
public struct PhysicsBody
{
    /// <summary>
    /// Handle to the kinematic/dynamic body in the Bepu simulation.
    /// </summary>
    public BodyHandle Handle;
}

/// <summary>
/// Stores a Bepu static handle for entities that use static colliders.
/// </summary>
public struct PhysicsStatic
{
    /// <summary>
    /// Handle to the static object in the Bepu simulation.
    /// </summary>
    public StaticHandle Handle;
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        material = new PairMaterialProperties { FrictionCoefficient = 1f, MaximumRecoveryVelocity = 2f, SpringSettings = new SpringSettings(30, 1) };
        return true;
    }
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;
    public void Dispose() { }
}

public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public Vector3 Gravity;
    public PoseIntegratorCallbacks(Vector3 gravity) : this() { Gravity = gravity; }
    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public bool AllowSubstepsForUnconstrainedBodies => false;
    public bool IntegrateVelocityForKinematics => false;
    public void Initialize(Simulation simulation) { }
    public void PrepareForIntegration(float dt) { }
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        // Broadcast scalar gravity to a wide vector and scale by the per-lane timestep.
        Vector3Wide.Broadcast(Gravity, out var gravityWide);
        Vector3Wide.Scale(gravityWide, dt, out var gravityDt);
        velocity.Linear += gravityDt;
    }
}

internal static class Program
{
    public static void Main()
    {
        var game = new StellaInvicta();
        game.Run();
    }

    private sealed class StellaInvicta : Game
    {
        // BepuPhysics v2 Simulation
        private Simulation _simulation = null!;
        private BufferPool _bufferPool = null!;
        private ThreadDispatcher _threadDispatcher = null!;
        private MousePicker _mousePicker = null!;

        /// <summary>
        /// Represents the current game world.
        /// </summary>
        public readonly World World = new(100000);
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
        // Lines
        private GraphicsPipeline? _linePipeline;
        private LineBatch3D? _lineBatch;
        // Debug: visualize physics colliders with wireframes
        private bool _debugDrawColliders = true;
        // If true, also draw ECS-declared collider poses (helps spot divergence). Off by default.
        private bool _debugDrawEcsColliderPoses = false;
        public StellaInvicta() : base(
            new AppInfo("Ayanami", "Stella Invicta Demo"),
            new WindowCreateInfo("Stella Invicta", 1280, 720, ScreenMode.Windowed, true, false, false),
            FramePacingSettings.CreateCapped(165, 165),
            ShaderFormat.SPIRV | ShaderFormat.DXIL | ShaderFormat.DXBC,
            debugMode: true)
        {
            InitializeScene();
        }

        // Safely get a BodyReference; returns false if the handle is invalid (e.g., removed or not yet added)
        private bool TryGetBodyRef(BodyHandle handle, out BodyReference bodyRef)
        {
            try
            {
                bodyRef = _simulation.Bodies.GetBodyReference(handle);
                return true;
            }
            catch
            {
                bodyRef = default;
                return false;
            }
        }

        private bool TryGetStaticRef(StaticHandle handle, out StaticReference staticRef)
        {
            try
            {
                staticRef = _simulation.Statics[handle];
                return true;
            }
            catch
            {
                staticRef = default;
                return false;
            }
        }

        public void EnableMSAA(SampleCount sampleCount)
        {


        }

        public void DisableMSAA()
        {

        }

        private void InitializePhysics()
        {
            // BepuPhysics requires a BufferPool for memory management and a ThreadDispatcher for multi-threading.
            _bufferPool = new BufferPool();
            // Using '0' for thread count will default to Environment.ProcessorCount.
            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

            // Create the simulation. The constructor takes callbacks for handling collisions.
            // For this example, we can use the default narrow-phase callbacks.
            _simulation = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, 0, 0)), new SolveDescription(8, 1));
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
            InitializePhysics();
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

            _mousePicker = new MousePicker(_camera, _simulation);

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

            // Line pipeline setup (position+color, line list)
            var vsLine = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                "Assets/LineColor.hlsl",
                "VSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Vertex,
                false,
                "LineVS"
            );
            var fsLine = ShaderCross.Create(
                GraphicsDevice,
                RootTitleStorage,
                "Assets/LineColor.hlsl",
                "PSMain",
                ShaderCross.ShaderFormat.HLSL,
                ShaderStage.Fragment,
                false,
                "LinePS"
            );
            var lineVertexInput = VertexInputState.CreateSingleBinding<LineBatch3D.LineVertex>(0);
            _linePipeline = GraphicsPipeline.Create(GraphicsDevice, new GraphicsPipelineCreateInfo
            {
                VertexShader = vsLine,
                FragmentShader = fsLine,
                VertexInputState = lineVertexInput,
                PrimitiveType = PrimitiveType.LineList,
                RasterizerState = RasterizerState.CCW_CullNone,
                MultisampleState = new MultisampleState { SampleCount = _msaaSamples },
                DepthStencilState = new DepthStencilState
                {
                    EnableDepthTest = true,
                    EnableDepthWrite = false, // lines typically don't write depth
                    CompareOp = CompareOp.LessOrEqual,
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
                            // Use standard non-premultiplied alpha blending for line transparency
                            BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                        }
                    ],
                    HasDepthStencilTarget = true,
                    DepthStencilFormat = GraphicsDevice.SupportedDepthStencilFormat
                },
                Name = "LineColorRenderer"
            });

            _lineBatch = new LineBatch3D(GraphicsDevice, 4096);

            World.OnSetPost((Entity entity, in Mesh _, in Mesh mesh, bool _) =>
            {
                entity.Set(GpuMesh.Upload(GraphicsDevice, mesh.Vertices.AsSpan(), mesh.Indices.AsSpan(), "Cube"));
            });

            World.OnSetPost<CollisionShape>((Entity entity, in CollisionShape _, in CollisionShape collisionShape, bool _) =>
            {
                TypedIndex? shapeIndex = null; // Default to invalid index
                // Add concrete shape type; Shapes.Add<T>() requires an unmanaged struct, not the IShape interface.
                switch (collisionShape.Shape)
                {
                    case Sphere sphere:
                        shapeIndex = _simulation.Shapes.Add(sphere);
                        break;
                    case Box box:
                        shapeIndex = _simulation.Shapes.Add(box);
                        break;
                    case Capsule capsule:
                        shapeIndex = _simulation.Shapes.Add(capsule);
                        break;
                    case Cylinder cylinder:
                        shapeIndex = _simulation.Shapes.Add(cylinder);
                        break;
                    default:
                        Console.WriteLine($"[Physics] Unsupported collision shape type: {collisionShape.Shape?.GetType().Name ?? "null"}");
                        return;
                }

                // Pull initial transform
                var pos = entity.Has<Position3D>() ? entity.GetMut<Position3D>().Value : Vector3.Zero;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : Quaternion.Identity;

                if (entity.Has<Kinematic>())
                {
                    // Create a kinematic body
                    var pose = new RigidPose(pos, rot);
                    var collidable = new CollidableDescription((TypedIndex)shapeIndex!, 0.1f);
                    var activity = new BodyActivityDescription(0.01f);
                    var bodyDesc = BodyDescription.CreateKinematic(pose, collidable, activity);
                    var bodyHandle = _simulation.Bodies.Add(bodyDesc);
                    entity.Set(new PhysicsBody { Handle = bodyHandle });
                }
                else
                {
                    // Create a static collider
                    var staticDescription = new StaticDescription(pos, (TypedIndex)shapeIndex!);
                    var staticHandle = _simulation.Statics.Add(staticDescription);
                    entity.Set(new PhysicsStatic { Handle = staticHandle });
                }
            });

            // The Sun: Center of the solar system.
            var sun = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(0, 0, 0))
                .Set(new Size3D(7.0f))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0f, 0f))
                .Set(new CollisionShape(new Sphere(7.0f * 0.5f)))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Sun.jpg") ?? _checkerTexture! });

            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(7.7f, 0, 5))
                .Set(new Size3D(0.1f))
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.1f)))
                .Set(new AngularVelocity3D(0f, 0.05f, 0f))
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = _checkerTexture! });

            // Mercury: The closest planet to the Sun.
            var mercury = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(7.7f * 4, 0, 5))
                .Set(new Size3D(0.38f))
                .Set(new CollisionShape(new Sphere(0.38f)))
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.002f, 0f))
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Mercury.jpg") ?? _checkerTexture! });

            // Venus: The second planet from the Sun.
            var venus = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(14.5f * 4, 0, 0))
                .Set(new Size3D(0.95f))
                .Set(new CollisionShape(new Sphere(0.95f)))
                .Set(Rotation3D.Identity)
                .Set(new Parent(sun))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Venus.jpg") ?? _checkerTexture! });

            var earth = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(20.0f * 4, 0, 8))
                .Set(new Size3D(1.0f))
                .Set(Rotation3D.Identity)
                .Set(new Parent(sun))
                .Set(new CollisionShape(new Sphere(1f)))
                .Set(new AngularVelocity3D(0f, 0.012f, 0f))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Earth.jpg") ?? _checkerTexture! });

            // The Moon: Positioned relative to Earth.
            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(21.5f * 4, 0, 6))
                .Set(new Size3D(0.27f))
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(1f)))
                .Set(new AngularVelocity3D(0f, 0.05f, 0f))
                .Set(new Parent(earth))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Moon.jpg") ?? _checkerTexture! });

            SpawnGridXZ(
                halfLines: 100,   // creates lines from -100..100 in both axes
                step: 1f,         // 1 unit spacing
                y: 0f,
                color: new Color(255, 255, 255, 32) // more transparent grid to avoid white plane in distance
            );

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(2000, 0, 0)))
                .Set(new Color(255, 64, 64, 255));

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(-2000, 0, 0)))
                .Set(new Color(255, 64, 64, 255));

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(0, 0, 2000)))
                .Set(Color.Blue);

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(0, 0, -2000)))
                .Set(Color.Blue);

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(0, 20000, 0)))
                .Set(Color.Green);

            World.CreateEntity()
                .Set(new Line3D(new Vector3(0, 0, 0), new Vector3(0, -20000, 0)))
                .Set(Color.Green);


            // Example usage:
            // SetSkybox("Assets/skybox.jpg", 50f);
            // Or, for cubemap:
            SetSkyboxCube([
                "Assets/Sky/px.png","Assets/Sky/nx.png",
                "Assets/Sky/py.png","Assets/Sky/ny.png",
                "Assets/Sky/pz.png","Assets/Sky/nz.png"
            ]);
        }

        // Creates an XZ-plane grid centered at origin by spawning Line3D + Color entities.
        // halfLines: number of lines to each side of origin (total = halfLines*2 + 1 per axis)
        // step: spacing between neighboring lines
        // y: elevation of the grid
        // color: line color
        private void SpawnGridXZ(int halfLines, float step, float y, Color color)
        {
            if (halfLines <= 0 || step <= 0f) { return; }

            float extent = halfLines * step;
            for (int i = -halfLines; i <= halfLines; i++)
            {
                float x = i * step;
                // Lines parallel to Z (varying X)
                World.CreateEntity()
                    .Set(new Line3D(new Vector3(x, y, -extent), new Vector3(x, y, extent)))
                    .Set(color);

                float z = i * step;
                // Lines parallel to X (varying Z)
                World.CreateEntity()
                    .Set(new Line3D(new Vector3(-extent, y, z), new Vector3(extent, y, z)))
                    .Set(color);
            }
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
        /// Spawns a large number of planet-like entities for stress testing.
        /// Entities are distributed on random circular orbits on the XZ plane with slight Y jitter.
        /// Optionally attaches static sphere colliders so they can be ray-picked.
        /// </summary>
        /// <param name="count">How many entities to spawn.</param>
        /// <param name="minOrbitRadius">Minimum orbit radius from origin.</param>
        /// <param name="maxOrbitRadius">Maximum orbit radius from origin.</param>
        /// <param name="minScale">Minimum uniform scale (planet radius equivalent).</param>
        /// <param name="maxScale">Maximum uniform scale.</param>
        /// <param name="seed">Random seed.</param>
        /// <param name="addStaticCollider">If true, creates Bepu static sphere colliders for each spawned entity.</param>
        private void SpawnPlanetStressTest(
            int count,
            float minOrbitRadius = 10f,
            float maxOrbitRadius = 300f,
            float minScale = 0.1f,
            float maxScale = 1.5f,
            int seed = 1337,
            bool addStaticCollider = false)
        {
            if (count <= 0) { return; }
            if (minOrbitRadius < 0f) minOrbitRadius = 0f;
            if (maxOrbitRadius < minOrbitRadius) maxOrbitRadius = minOrbitRadius + 1f;
            if (minScale <= 0f) minScale = 0.05f;
            if (maxScale < minScale) maxScale = minScale;

            var rng = new Random(seed);

            // Ensure we have at least a fallback texture
            var tex = _checkerTexture ?? _whiteTexture ?? CreateSolidTexture(255, 255, 255, 255);

            // Pre-create a GPU mesh for spheres to avoid duplicating per-entity CPU meshes
            var sharedSphere = Mesh.CreateSphere3D();

            for (int i = 0; i < count; i++)
            {
                // Random orbit radius and angle
                float r = minOrbitRadius + (float)rng.NextDouble() * (maxOrbitRadius - minOrbitRadius);
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float yJitter = ((float)rng.NextDouble() - 0.5f) * 2f; // [-1,1]

                var pos = new Vector3(MathF.Cos(angle) * r, yJitter, MathF.Sin(angle) * r);
                float s = minScale + (float)rng.NextDouble() * (maxScale - minScale);

                // Small random angular velocity
                var angVel = new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * 0.02f,
                    ((float)rng.NextDouble() - 0.5f) * 0.02f,
                    ((float)rng.NextDouble() - 0.5f) * 0.02f
                );

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere) // use the same CPU-side mesh data
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(angVel))
                    .Set(new Texture2DRef { Texture = tex });

                // Optional: physics collider so raycast picks them.
                if (addStaticCollider)
                {
                    var sphereShape = new Sphere(s * 0.5f);
                    var shapeIndex = _simulation.Shapes.Add(sphereShape);
                    var staticDesc = new StaticDescription(pos, shapeIndex);
                    _simulation.Statics.Add(staticDesc);
                }
            }
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

            // Ensure any entities with CollisionShape have corresponding physics objects even if OnSetPost timing missed
            foreach (var e in World.Query(typeof(CollisionShape)))
            {
                bool isKinematic = e.Has<Kinematic>();
                bool hasBody = e.Has<PhysicsBody>();
                bool hasStatic = e.Has<PhysicsStatic>();
                if (isKinematic && !hasBody)
                {
                    var cs = e.GetMut<CollisionShape>();
                    // Build shape index from ECS shape
                    TypedIndex? sidx = null;
                    switch (cs.Shape)
                    {
                        case Sphere s: sidx = _simulation.Shapes.Add(s); break;
                        case Box b: sidx = _simulation.Shapes.Add(b); break;
                        case Capsule c: sidx = _simulation.Shapes.Add(c); break;
                        case Cylinder cy: sidx = _simulation.Shapes.Add(cy); break;
                        default: break;
                    }
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : Quaternion.Identity;
                    if (sidx.HasValue)
                    {
                        var pose = new RigidPose(pos, rot);
                        var collidable = new CollidableDescription(sidx.Value, 0.1f);
                        var activity = new BodyActivityDescription(0.01f);
                        var bodyDesc = BodyDescription.CreateKinematic(pose, collidable, activity);
                        var handle = _simulation.Bodies.Add(bodyDesc);
                        e.Set(new PhysicsBody { Handle = handle });
                    }
                }
                else if (!isKinematic && !hasStatic)
                {
                    var cs = e.GetMut<CollisionShape>();
                    TypedIndex? sidx = null;
                    switch (cs.Shape)
                    {
                        case Sphere s: sidx = _simulation.Shapes.Add(s); break;
                        case Box b: sidx = _simulation.Shapes.Add(b); break;
                        case Capsule c: sidx = _simulation.Shapes.Add(c); break;
                        case Cylinder cy: sidx = _simulation.Shapes.Add(cy); break;
                        default: break;
                    }
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : Quaternion.Identity;
                    if (sidx.HasValue)
                    {
                        var sdesc = new StaticDescription(pos, sidx.Value) { }; // orientation not in this overload; use position-only static
                        var sh = _simulation.Statics.Add(sdesc);
                        e.Set(new PhysicsStatic { Handle = sh });
                    }
                }
            }

            // Ensure kinematic bodies follow ECS transforms BEFORE stepping the simulation
            foreach (var entity in World.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(Position3D)))
            {
                var body = entity.GetMut<PhysicsBody>();
                var pos = entity.GetMut<Position3D>().Value;
                var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : Quaternion.Identity;
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    bodyRef.Pose = new RigidPose(pos, rot);
                    // Ensure the body is awake and its broadphase bounds reflect the new pose for accurate raycasts
                    // Awake must be set to true otherwise it simply wont work
                    bodyRef.Awake = true;
                    _simulation.Bodies.UpdateBounds(body.Handle);
                }
                else
                {
                    // Optional: log once per entity if needed; avoided here to prevent spam.
                }
            }

            _simulation.Timestep((float)delta.TotalSeconds, _threadDispatcher);

            // Check for mouse click to perform picking
            if (Inputs.Mouse.LeftButton.IsPressed)
            {
                if (_mousePicker.Pick(Inputs.Mouse, (int)MainWindow.Width, (int)MainWindow.Height, out var result))
                {
                    // For now, we just print the result.
                    // The CollidableReference can be either a BodyHandle or a StaticHandle.
                    if (result.Collidable.Mobility == CollidableMobility.Static)
                    {
                        if (TryGetStaticRef(result.Collidable.StaticHandle, out var staticBody))
                        {
                            // You could use a dictionary to map the handle back to your ECS entity here.
                            Console.WriteLine($"SUCCESS: Hit a STATIC object at distance {result.Distance}. Position: {staticBody.Pose.Position}");
                            _camera.Position = staticBody.Pose.Position + new Vector3(0, 10, 6); // Move camera to hit position
                            _camera.LookAt(staticBody.Pose.Position); // Look at the hit position
                        }
                        else
                        {
                            Console.WriteLine($"SUCCESS: Hit a STATIC object at distance {result.Distance}, but static handle is invalid now.");
                        }
                    }
                    else // It's a dynamic/kinematic body
                    {
                        var bh = result.Collidable.BodyHandle;
                        if (TryGetBodyRef(bh, out var bref))
                        {
                            var p = bref.Pose.Position;
                            Console.WriteLine($"SUCCESS: Hit a DYNAMIC object at distance {result.Distance}. Position: {p}");
                            _camera.Position = p + new Vector3(0, 10, 6);
                            _camera.LookAt(p);
                        }
                        else
                        {
                            Console.WriteLine($"SUCCESS: Hit a DYNAMIC object at distance {result.Distance}, but body handle is invalid now (moved/removed).");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("PICK: Missed. No object was hit.");
                }
            }

            // Debug: print current positions of all dynamic/kinematic bodies
            if (Inputs.Mouse.RightButton.IsPressed)
            {
                LogAllDynamicPositions();
            }


            World.Update((float)delta.TotalSeconds);

        }

        // Logs positions of all dynamic/kinematic bodies (entities with PhysicsBody) using simulation poses.
        private void LogAllDynamicPositions()
        {
            int count = 0;
            foreach (var entity in World.Query(typeof(PhysicsBody)))
            {
                var body = entity.GetMut<PhysicsBody>();
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    var p = bodyRef.Pose.Position;
                    var o = bodyRef.Pose.Orientation;
                    Console.WriteLine($"Dynamic Body {body.Handle.Value}: Pos=<{p.X:F3}, {p.Y:F3}, {p.Z:F3}> Ori=<{o.X:F3}, {o.Y:F3}, {o.Z:F3}, {o.W:F3}>");
                    count++;
                }
            }
            if (count == 0)
            {
                Console.WriteLine("No dynamic bodies found.");
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
            // Compute view-projection and extract frustum once per frame.
            var view = _camera.GetViewMatrix();
            var proj = _camera.GetProjectionMatrix();
            var viewProj = view * proj;
            var frustum = ExtractFrustumPlanes(viewProj);
            // Prepare line batch for this frame and upload any lines
            _lineBatch?.Begin();
            // Example lines: axes at origin
            if (_lineBatch != null)
            {

                foreach (var entity in World.Query(typeof(Line3D), typeof(Color)))
                {
                    var line = entity.GetMut<Line3D>();
                    var color = entity.GetMut<Color>();
                    _lineBatch.AddLine(line.Start, line.End, color);
                }

                // Add physics collider wireframes before uploading
                if (_debugDrawColliders)
                {
                    DebugDrawColliders();
                }

                // Upload line vertex data
                _lineBatch.UploadBufferData(cmdbuf);
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
                // Gather transform components first (needed for culling, avoids binding for culled)
                Vector3 translation = entity.Has<Position3D>() ? entity.GetMut<Position3D>().Value : Vector3.Zero;
                Quaternion rotation = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : Quaternion.Identity;
                Vector3 size = entity.Has<Size3D>() ? entity.GetMut<Size3D>().Value : Vector3.One;

                // If this entity has a physics representation, prefer the simulation pose for accuracy
                if (entity.Has<PhysicsBody>())
                {
                    var body = entity.GetMut<PhysicsBody>();
                    if (TryGetBodyRef(body.Handle, out var bodyRef))
                    {
                        translation = bodyRef.Pose.Position;
                        rotation = bodyRef.Pose.Orientation;
                    }
                }
                else if (entity.Has<PhysicsStatic>())
                {
                    var st = entity.GetMut<PhysicsStatic>();
                    if (TryGetStaticRef(st.Handle, out var sref))
                    {
                        translation = sref.Pose.Position;
                        rotation = sref.Pose.Orientation;
                    }
                }

                // Sphere culling: use a conservative radius based on max scale axis.
                float radius = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
                if (!IsSphereVisible(translation, radius, frustum))
                {
                    continue; // culled
                }

                // Bind mesh only for visible entities
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
                var model = Matrix4x4.CreateScale(size) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
                var mvp = model * viewProj;

                cmdbuf.PushVertexUniformData(mvp, slot: 0);
                pass.DrawIndexedPrimitives(gpuMesh.IndexCount, 1, 0, 0, 0);
            }

            // Draw lines last so they overlay geometry
            if (_lineBatch != null)
            {
                pass.BindGraphicsPipeline(_linePipeline!);
                var vp = _camera.GetViewMatrix() * _camera.GetProjectionMatrix();
                _lineBatch.Render(pass, vp);
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

        // Simple plane struct for frustum culling
        private struct PlaneF
        {
            public Vector3 Normal;
            public float D;
        }

        // Extracts six frustum planes from a view-projection matrix (row-major, v * M convention)
        private static PlaneF[] ExtractFrustumPlanes(Matrix4x4 m)
        {
            var planes = new PlaneF[6];

            // Left: row4 + row1
            planes[0] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31),
                D = m.M44 + m.M41
            });
            // Right: row4 - row1
            planes[1] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31),
                D = m.M44 - m.M41
            });
            // Bottom: row4 + row2
            planes[2] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32),
                D = m.M44 + m.M42
            });
            // Top: row4 - row2
            planes[3] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32),
                D = m.M44 - m.M42
            });
            // Near: row4 + row3 (for DX/Vulkan depth 0..1)
            planes[4] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33),
                D = m.M44 + m.M43
            });
            // Far: row4 - row3
            planes[5] = NormalizePlane(new PlaneF
            {
                Normal = new Vector3(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33),
                D = m.M44 - m.M43
            });

            return planes;
        }

        private static PlaneF NormalizePlane(PlaneF p)
        {
            float invLen = 1f / p.Normal.Length();
            p.Normal *= invLen;
            p.D *= invLen;
            return p;
        }

        private static bool IsSphereVisible(Vector3 center, float radius, PlaneF[] frustum)
        {
            // Outside if outside any plane
            for (int i = 0; i < 6; i++)
            {
                float dist = Vector3.Dot(frustum[i].Normal, center) + frustum[i].D;
                if (dist < -radius)
                {
                    return false;
                }
            }
            return true;
        }

        // ==========================
        // Physics debug draw helpers
        // ==========================
        private void AddWireSphere(Vector3 center, float radius, Color color, int segments = 24)
        {
            if (_lineBatch == null || radius <= 0f) return;
            // Three great circles around X, Y, Z axes
            void Circle(Vector3 axis)
            {
                Vector3 prev = default;
                bool hasPrev = false;
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    float ang = t * MathF.Tau;
                    // Build an orthonormal basis for the plane perpendicular to axis
                    Vector3 n = Vector3.Normalize(axis);
                    Vector3 u = Vector3.Normalize(Vector3.Cross(n, Math.Abs(n.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX));
                    Vector3 v = Vector3.Cross(n, u);
                    Vector3 p = center + (u * MathF.Cos(ang) + v * MathF.Sin(ang)) * radius;
                    if (hasPrev)
                    {
                        _lineBatch.AddLine(prev, p, color);
                    }
                    prev = p; hasPrev = true;
                }
            }
            Circle(Vector3.UnitX);
            Circle(Vector3.UnitY);
            Circle(Vector3.UnitZ);
        }

        private void AddWireBox(Vector3 center, Quaternion orientation, Vector3 halfExtents, Color color)
        {
            if (_lineBatch == null) return;
            // 8 corners in local space
            Vector3[] c = new Vector3[8];
            int idx = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        c[idx++] = new Vector3(x * halfExtents.X, y * halfExtents.Y, z * halfExtents.Z);

            // Transform to world
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            for (int i = 0; i < 8; i++)
            {
                c[i] = Vector3.Transform(c[i], rot) + center;
            }
            // box edges pairs
            int[,] e = new int[,]
            {
                {0,1},{0,2},{0,4},
                {3,1},{3,2},{3,7},
                {5,1},{5,4},{5,7},
                {6,2},{6,4},{6,7}
            };
            for (int i = 0; i < e.GetLength(0); i++)
            {
                _lineBatch!.AddLine(c[e[i, 0]], c[e[i, 1]], color);
            }
        }

        private void AddWireCapsule(Vector3 center, Quaternion orientation, float halfLength, float radius, Color color, int segments = 16)
        {
            if (_lineBatch == null) return;
            // Capsule aligned with local Y axis, then rotate by orientation
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
            Vector3 forward = Vector3.TransformNormal(Vector3.UnitZ, rot);
            Vector3 a = center + up * halfLength;
            Vector3 b = center - up * halfLength;
            // cylinder rings
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = right * MathF.Cos(t0) + forward * MathF.Sin(t0);
                Vector3 r1 = right * MathF.Cos(t1) + forward * MathF.Sin(t1);
                _lineBatch.AddLine(a + r0 * radius, a + r1 * radius, color);
                _lineBatch.AddLine(b + r0 * radius, b + r1 * radius, color);
                _lineBatch.AddLine(a + r0 * radius, b + r0 * radius, color);
            }
            // hemispheres
            void Hemisphere(Vector3 centerHem, int sign)
            {
                for (int iy = 0; iy <= segments; iy++)
                {
                    float v = (float)iy / segments * MathF.PI * 0.5f;
                    float y = MathF.Sin(v) * sign;
                    float r = MathF.Cos(v);
                    Vector3 ringUp = up * y;
                    Vector3 prev = default; bool hasPrev = false;
                    for (int ix = 0; ix <= segments; ix++)
                    {
                        float u = (float)ix / segments * MathF.Tau;
                        Vector3 dir = right * (MathF.Cos(u) * r) + forward * (MathF.Sin(u) * r) + ringUp;
                        Vector3 p = centerHem + dir * radius;
                        if (hasPrev) _lineBatch.AddLine(prev, p, color);
                        prev = p; hasPrev = true;
                    }
                }
            }
            Hemisphere(a, +1);
            Hemisphere(b, -1);
        }

        private void AddWireCylinder(Vector3 center, Quaternion orientation, float halfLength, float radius, Color color, int segments = 16)
        {
            if (_lineBatch == null) return;
            Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);
            Vector3 up = Vector3.TransformNormal(Vector3.UnitY, rot);
            Vector3 right = Vector3.TransformNormal(Vector3.UnitX, rot);
            Vector3 forward = Vector3.TransformNormal(Vector3.UnitZ, rot);
            Vector3 a = center + up * halfLength;
            Vector3 b = center - up * halfLength;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = right * MathF.Cos(t0) + forward * MathF.Sin(t0);
                Vector3 r1 = right * MathF.Cos(t1) + forward * MathF.Sin(t1);
                _lineBatch.AddLine(a + r0 * radius, a + r1 * radius, color);
                _lineBatch.AddLine(b + r0 * radius, b + r1 * radius, color);
                _lineBatch.AddLine(a + r0 * radius, b + r0 * radius, color);
            }
        }

        private void DebugDrawColliders()
        {
            if (_lineBatch == null) return;

            // 1) Optionally draw ECS-declared shapes at ECS transform (green) to compare against physics
            if (_debugDrawEcsColliderPoses)
            {
                foreach (var e in World.Query(typeof(CollisionShape)))
                {
                    var cs = e.GetMut<CollisionShape>();
                    var pos = e.Has<Position3D>() ? e.GetMut<Position3D>().Value : Vector3.Zero;
                    var rot = e.Has<Rotation3D>() ? e.GetMut<Rotation3D>().Value : Quaternion.Identity;
                    DrawShapeFromEcs(cs.Shape, pos, rot, new Color(64, 255, 64, 160));
                }
            }

            // 2) If a physics body exists, also draw at physics pose (red) — where physics thinks it is
            foreach (var e in World.Query(typeof(PhysicsBody), typeof(CollisionShape)))
            {
                var body = e.GetMut<PhysicsBody>();
                var cs = e.GetMut<CollisionShape>();
                if (TryGetBodyRef(body.Handle, out var bodyRef))
                {
                    DrawShapeFromEcs(cs.Shape, bodyRef.Pose.Position, bodyRef.Pose.Orientation, new Color(255, 64, 64, 200));
                }
            }

            // 3) For statics, draw at static pose (cyan)
            foreach (var e in World.Query(typeof(PhysicsStatic), typeof(CollisionShape)))
            {
                var st = e.GetMut<PhysicsStatic>();
                var cs = e.GetMut<CollisionShape>();
                var sref = _simulation.Statics[st.Handle];
                DrawShapeFromEcs(cs.Shape, sref.Pose.Position, sref.Pose.Orientation, new Color(64, 200, 255, 200));
            }
        }

        private void DrawShapeFromEcs(object? shape, Vector3 position, Quaternion orientation, Color color)
        {
            switch (shape)
            {
                case Sphere s:
                    AddWireSphere(position, s.Radius, color);
                    break;
                case Box b:
                    AddWireBox(position, orientation, new Vector3(b.HalfWidth, b.HalfHeight, b.HalfLength), color);
                    break;
                case Capsule c:
                    AddWireCapsule(position, orientation, c.HalfLength, c.Radius, color);
                    break;
                case Cylinder cy:
                    AddWireCylinder(position, orientation, cy.HalfLength, cy.Radius, color);
                    break;
                default:
                    break;
            }
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
            _linePipeline?.Dispose();
            _lineBatch?.Dispose();
            _whiteTexture?.Dispose();
            _checkerTexture?.Dispose();
            _skyboxTexture?.Dispose();
        }
    }
}
