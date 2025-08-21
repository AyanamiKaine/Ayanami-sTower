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

/// <summary>
/// Stores the absolute world position using double precision to avoid floating point issues.
/// This is the "true" position in the universe, while Position3D stores the relative position
/// from the current floating origin.
/// </summary>
public struct AbsolutePosition
{
    /// <summary>
    /// The absolute position in double precision coordinates.
    /// </summary>
    public Vector3d Value;

    /// <summary>
    /// Creates a new AbsolutePosition with the specified position.
    /// </summary>
    public AbsolutePosition(Vector3d position)
    {
        Value = position;
    }

    /// <summary>
    /// Creates a new AbsolutePosition with the specified coordinates.
    /// </summary>
    public AbsolutePosition(double x, double y, double z)
    {
        Value = new Vector3d(x, y, z);
    }
}

/// <summary>
/// A Vector3 using double precision for large coordinate values.
/// </summary>
public struct Vector3d
{
    /// <summary>X coordinate</summary>
    public double X;
    /// <summary>Y coordinate</summary>
    public double Y;
    /// <summary>Z coordinate</summary>
    public double Z;

    /// <summary>
    /// Creates a new Vector3d with the specified coordinates.
    /// </summary>
    public Vector3d(double x, double y, double z)
    {
        X = x; Y = y; Z = z;
    }

    /// <summary>Adds two Vector3d instances.</summary>
    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    /// <summary>Subtracts two Vector3d instances.</summary>
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    /// <summary>Multiplies a Vector3d by a scalar.</summary>
    public static Vector3d operator *(Vector3d a, double scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    /// <summary>Divides a Vector3d by a scalar.</summary>
    public static Vector3d operator /(Vector3d a, double scalar) => new(a.X / scalar, a.Y / scalar, a.Z / scalar);

    /// <summary>Implicitly converts Vector3d to Vector3 (with precision loss).</summary>
    public static implicit operator Vector3(Vector3d v) => new((float)v.X, (float)v.Y, (float)v.Z);
    /// <summary>Implicitly converts Vector3 to Vector3d.</summary>
    public static implicit operator Vector3d(Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>Gets the length of the vector.</summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
    /// <summary>Returns a normalized version of this vector.</summary>
    public Vector3d Normalized() => this / Length();

    /// <summary>Zero vector constant.</summary>
    public static readonly Vector3d Zero = new(0, 0, 0);

    /// <summary>String representation of the vector.</summary>
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

/// <summary>
/// Manages the floating origin system to prevent floating point precision issues.
/// Periodically rebases all world coordinates by subtracting a large offset.
/// </summary>
public class FloatingOriginManager
{
    private Vector3d _currentOrigin = Vector3d.Zero;
    private readonly double _rebaseThreshold;
    private readonly World _world;
    private readonly Simulation _simulation;

    /// <summary>The current floating origin offset in world coordinates.</summary>
    public Vector3d CurrentOrigin => _currentOrigin;
    /// <summary>True if a rebase operation is currently in progress.</summary>
    public bool IsRebasing { get; private set; }

    /// <summary>
    /// Creates a new FloatingOriginManager.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <param name="simulation">The BepuPhysics simulation instance.</param>
    /// <param name="rebaseThreshold">Distance threshold that triggers a rebase.</param>
    public FloatingOriginManager(World world, Simulation simulation, double rebaseThreshold = 10000.0)
    {
        _world = world;
        _simulation = simulation;
        _rebaseThreshold = rebaseThreshold;
    }

    /// <summary>
    /// Forces a rebase by a specific offset in world coordinates.
    /// Callers should also subtract the same offset from the camera position to keep
    /// the camera near the origin.
    /// </summary>
    public void ForceRebase(Vector3 offset)
    {
        var d = new Vector3d(offset.X, offset.Y, offset.Z);
        PerformRebase(d);
    }

    /// <summary>
    /// Checks if a rebase is needed based on the camera position and performs it if necessary.
    /// Returns true if a rebase occurred and outputs the rebase offset that was applied.
    /// The caller should subtract this offset from the camera position (and any other view-space
    /// references) to keep them near the origin as well.
    /// </summary>
    public bool Update(Vector3 cameraPosition, out Vector3 rebaseOffset)
    {
        var cameraDistance = new Vector3d(cameraPosition.X, cameraPosition.Y, cameraPosition.Z).Length();

        if (cameraDistance > _rebaseThreshold)
        {
            rebaseOffset = cameraPosition; // shift world by -camera, move origin by +camera
            var rebaseOffsetD = new Vector3d(rebaseOffset.X, rebaseOffset.Y, rebaseOffset.Z);
            PerformRebase(rebaseOffsetD);
            return true;
        }

        rebaseOffset = default;
        return false;
    }

    /// <summary>
    /// Performs a floating origin rebase by shifting all entities and physics objects.
    /// </summary>
    private void PerformRebase(Vector3d offset)
    {
        IsRebasing = true;

        // Update the current origin
        _currentOrigin += offset;

        // Rebase all entities with AbsolutePosition
        foreach (var entity in _world.Query(typeof(AbsolutePosition)))
        {
            var absolutePos = entity.GetMut<AbsolutePosition>();

            // Update relative position
            var newRelativePos = absolutePos.Value - _currentOrigin;

            if (entity.Has<Position3D>())
            {
                var relativeVector = (Vector3)newRelativePos;
                entity.Set(new Position3D(relativeVector.X, relativeVector.Y, relativeVector.Z));
            }
        }

        // Rebase physics objects
        RebasePhysicsObjects(offset);

        IsRebasing = false;
    }

    /// <summary>
    /// Rebases all physics objects in the simulation.
    /// </summary>
    private void RebasePhysicsObjects(Vector3d offset)
    {
        var offsetVector = (Vector3)offset;

        // Rebase kinematic/dynamic bodies
        foreach (var entity in _world.Query(typeof(PhysicsBody)))
        {
            var physicsBody = entity.GetMut<PhysicsBody>();

            if (TryGetBodyReference(physicsBody.Handle, out var bodyRef))
            {
                var currentPose = bodyRef.Pose;
                var newPosition = currentPose.Position - offsetVector;
                bodyRef.Pose = new RigidPose(newPosition, currentPose.Orientation);
                bodyRef.Awake = true; // Ensure the body is awake after position change
                _simulation.Bodies.UpdateBounds(physicsBody.Handle);
            }
        }

        // Rebase static objects
        foreach (var entity in _world.Query(typeof(PhysicsStatic)))
        {
            var physicsStatic = entity.GetMut<PhysicsStatic>();

            if (TryGetStaticReference(physicsStatic.Handle, out var staticRef))
            {
                var currentPose = staticRef.Pose;
                var newPosition = currentPose.Position - offsetVector;

                // For static objects, we need to remove and re-add them with the new position
                // since static objects in BepuPhysics don't allow direct position modification
                var shapeIndex = staticRef.Shape;
                _simulation.Statics.Remove(physicsStatic.Handle);

                var newDesc = new StaticDescription(newPosition, shapeIndex);
                var newHandle = _simulation.Statics.Add(newDesc);
                entity.Set(new PhysicsStatic { Handle = newHandle });
            }
        }
    }

    /// <summary>
    /// Converts an absolute position to a relative position from the current origin.
    /// </summary>
    public Vector3 ToRelativePosition(Vector3d absolutePosition)
    {
        return (Vector3)(absolutePosition - _currentOrigin);
    }

    /// <summary>
    /// Converts a relative position to an absolute position.
    /// </summary>
    public Vector3d ToAbsolutePosition(Vector3 relativePosition)
    {
        return _currentOrigin + new Vector3d(relativePosition.X, relativePosition.Y, relativePosition.Z);
    }

    private bool TryGetBodyReference(BodyHandle handle, out BodyReference bodyRef)
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

    private bool TryGetStaticReference(StaticHandle handle, out StaticReference staticRef)
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
        private Entity? SunEntity;
        /// <summary>
        /// Represents the current game world.
        /// </summary>
        public readonly World World = new(10000000);
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
        private bool _debugDrawColliders = false;
        // If true, also draw ECS-declared collider poses (helps spot divergence). Off by default.
        private bool _debugDrawEcsColliderPoses = false;

        // Floating origin system
        private FloatingOriginManager? _floatingOriginManager;
        public StellaInvicta() : base(
            new AppInfo("Ayanami", "Stella Invicta Demo"),
            new WindowCreateInfo("Stella Invicta", 1280, 720, ScreenMode.Windowed, true, false, true),
            FramePacingSettings.CreateCapped(60, 60),
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

            // Initialize floating origin system
            _floatingOriginManager = new FloatingOriginManager(World, _simulation, 1000.0); // Rebase when camera is 1000 units from origin

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

            _lineBatch = new LineBatch3D(GraphicsDevice, 409600);

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

            // --- SCENE SETUP CONSTANTS ---
            // We use Astronomical Units (AU) for realistic scaling. 1 AU = Distance from Earth to Sun.
            // This scale factor determines how many units in our 3D world correspond to 1 AU.
            // You can increase/decrease this value to make the solar system larger or smaller.
            const float AU_SCALE_FACTOR = 40.0f;

            // --- CELESTIAL BODY CREATION ---

            // The Sun: Center of the solar system.
            // Note: The Sun's size is drastically scaled down for visibility.
            // In reality, it's ~109 times the diameter of Earth.
            var sun = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(0, 0, 0))
                .Set(new AbsolutePosition(0, 0, 0)) // Store absolute position for floating origin
                .Set(new Size3D(10.0f)) // Artistically scaled size
                .Set(Rotation3D.Identity)
                .Set(new AngularVelocity3D(0f, 0.001f, 0f)) // Slow rotation for effect
                .Set(new CollisionShape(new Sphere(10.0f * 0.6f)))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Sun.jpg") ?? _checkerTexture! });

            SunEntity = sun;

            // Mercury: 0.39 AU from the Sun.
            var mercury = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(0.39f * AU_SCALE_FACTOR, 0, 0)) // distance = 0.39 AU
                .Set(new AbsolutePosition(0.39f * AU_SCALE_FACTOR, 0, 0)) // Store absolute position
                .Set(new Size3D(0.38f)) // size = 0.38x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.38f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.048f, 0f)) // speed relative to Earth (fastest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Mercury.jpg") ?? _checkerTexture! });

            // Venus: 0.72 AU from the Sun.
            var venus = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(0.72f * AU_SCALE_FACTOR, 0, 0)) // distance = 0.72 AU
                .Set(new Size3D(0.95f)) // size = 0.95x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.95f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.016f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Venus.jpg") ?? _checkerTexture! });

            // Earth: 1.0 AU from the Sun (our baseline).
            var earth = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(1.0f * AU_SCALE_FACTOR, 0, 0)) // distance = 1.0 AU
                .Set(new Size3D(1.0f)) // size = 1.0x (baseline)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(1.0f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.01f, 0f)) // speed = baseline
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Earth.jpg") ?? _checkerTexture! });

            // The Moon: Positioned relative to Earth.
            // Compute Earth's world position and place the moon at an offset from Earth.
            // Also explicitly set LocalPosition3D so the OrbitSystem rotates around the Earth.
            var earthPos = earth.GetCopy<Position3D>().Value;
            var moonLocalOffset = new Vector3(2.0f, 0f, 0f); // distance from Earth in world units
            var moonWorldPos = earthPos + moonLocalOffset;

            World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(moonWorldPos.X, moonWorldPos.Y, moonWorldPos.Z))
                .Set(new LocalPosition3D(moonLocalOffset.X, moonLocalOffset.Y, moonLocalOffset.Z))
                .Set(new Size3D(0.27f)) // size = 0.27x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.27f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.12f, 0f)) // Orbits Earth faster
                .Set(new Parent(earth))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Moon.jpg") ?? _checkerTexture! });

            // Mars: 1.52 AU from the Sun.
            var mars = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(1.52f * AU_SCALE_FACTOR, 0, 0)) // distance = 1.52 AU
                .Set(new Size3D(0.53f)) // size = 0.53x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.53f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.0053f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Mars.jpg") ?? _checkerTexture! });

            // Jupiter: 5.20 AU from the Sun.
            var jupiter = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(5.20f * AU_SCALE_FACTOR, 0, 0)) // distance = 5.20 AU
                .Set(new Size3D(4.5f)) // size = 11.2x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(4.5f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.00084f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Jupiter.jpg") ?? _checkerTexture! });

            // Saturn: 9.58 AU from the Sun.
            var saturn = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(9.58f * AU_SCALE_FACTOR, 0, 0)) // distance = 9.58 AU
                .Set(new Size3D(4.0f)) // size = 9.45x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(4.0f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.00034f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Saturn.jpg") ?? _checkerTexture! });

            // Uranus: 19.22 AU from the Sun.
            var uranus = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(19.22f * AU_SCALE_FACTOR, 0, 0)) // distance = 19.22 AU
                .Set(new Size3D(2.5f)) // size = 4.0x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(2.5f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.00012f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Uranus.jpg") ?? _checkerTexture! });

            // Neptune: 30.05 AU from the Sun. (Added for completeness)
            var neptune = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(30.05f * AU_SCALE_FACTOR, 0, 0)) // distance = 30.05 AU
                .Set(new Size3D(2.4f)) // size = 3.88x Earth (scaled down artistically)
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(2.4f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.00006f, 0f)) // speed relative to Earth
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Neptune.jpg") ?? _checkerTexture! });

            // Pluto: 39.48 AU from the Sun (average).
            var pluto = World.CreateEntity()
                .Set(new CelestialBody())
                .Set(new Kinematic())
                .Set(Mesh.CreateSphere3D())
                .Set(new Position3D(39.48f * AU_SCALE_FACTOR, 0, 0)) // distance = 39.48 AU
                .Set(new Size3D(0.18f)) // size = 0.18x Earth
                .Set(Rotation3D.Identity)
                .Set(new CollisionShape(new Sphere(0.18f * 0.6f)))
                .Set(new AngularVelocity3D(0f, 0.00004f, 0f)) // speed relative to Earth (slowest)
                .Set(new Parent(sun))
                .Set(new Texture2DRef { Texture = LoadTextureFromFile("Assets/Pluto.jpg") ?? _checkerTexture! });

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


            SpawnAsteroidField(new(20, 0, 20), 20, 5, 0.002f, 0.05f);
            SpawnAsteroidField(new(-20, 0, -20), 20, 5, 0.002f, 0.05f);
            SpawnAsteroidField(new(-30, 0, -60), 20, 5, 0.002f, 0.05f);
            SpawnAsteroidField(new(-30, 0, 60), 20, 5, 0.002f, 0.05f);
            SpawnAsteroidField(new(-80, 0, -60), 20, 5, 0.002f, 0.05f);
            SpawnAsteroidField(new(-30, 0, -20), 20, 5, 0.002f, 0.05f);

            // Classic main-belt style asteroid belt between Mars and Jupiter (~2.2 - 3.2 AU)
            // Uses the same AU scale factor defined above so radii match planet positions
            SpawnAsteroidBelt(new Vector3(0, 0, 0),
                count: 4000,
                innerRadius: 2.2f * AU_SCALE_FACTOR,
                outerRadius: 3.2f * AU_SCALE_FACTOR,
                minSize: 0.02f,
                maxSize: 0.05f,
                inclinationDegrees: 1.5f,
                seed: 8675309,
                addStaticCollider: false);

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
                float r = minOrbitRadius + ((float)rng.NextDouble() * (maxOrbitRadius - minOrbitRadius));
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float yJitter = ((float)rng.NextDouble() - 0.5f) * 2f; // [-1,1]

                var pos = new Vector3(MathF.Cos(angle) * r, yJitter, MathF.Sin(angle) * r);
                float s = minScale + ((float)rng.NextDouble() * (maxScale - minScale));

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
        /// Spawns an asteroid field centered at `center` using simple sphere meshes and optional static colliders.
        /// </summary>
        /// <param name="center">World-space center of the asteroid field.</param>
        /// <param name="count">Number of asteroids to spawn.</param>
        /// <param name="fieldRadius">Radius of the spherical region to distribute asteroids in.</param>
        /// <param name="minSize">Minimum asteroid uniform scale.</param>
        /// <param name="maxSize">Maximum asteroid uniform scale.</param>
        /// <param name="seed">Random seed for reproducible fields.</param>
        /// <param name="addStaticCollider">If true, adds a static sphere collider for each asteroid.</param>
        /// <param name="planeThickness">Thickness of the asteroid field plane.</param>
        private void SpawnAsteroidField(Vector3 center, int count = 200, float fieldRadius = 100f, float minSize = 0.2f, float maxSize = 2.0f, float planeThickness = 0.5f, int seed = 424242, bool addStaticCollider = false)
        {
            if (count <= 0) return;
            var rng = new Random(seed);

            var tex = _checkerTexture ?? _whiteTexture ?? CreateSolidTexture(255, 255, 255, 255);

            // Shared CPU mesh for all asteroids
            var sharedSphere = Mesh.CreateSphere3D();

            for (int i = 0; i < count; i++)
            {
                // Disk/cylindrical distribution on XZ plane (uniform within disk).
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                // use sqrt(U) to produce uniform distribution over disk area
                float r = (float)Math.Sqrt(rng.NextDouble()) * fieldRadius;
                var x = center.X + (MathF.Cos(angle) * r);
                var z = center.Z + (MathF.Sin(angle) * r);
                // Small Y jitter around center.Y so asteroids lie close to the orbital plane
                float y = center.Y + (((float)rng.NextDouble() - 0.5f) * planeThickness);

                var pos = new Vector3(x, y, z);

                float s = minSize + ((float)rng.NextDouble() * (maxSize - minSize));

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere)
                    .Set(new Kinematic())
                    .Set(new Parent((Entity)SunEntity!))
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(new CollisionShape(new Sphere(s)))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(0, 0.005f, 0))
                    .Set(new Texture2DRef { Texture = tex });

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
        /// Spawns an annular asteroid belt around a given center. Distributes asteroids between innerRadius and outerRadius
        /// on the XZ plane with small random inclinations to give a torus-like appearance.
        /// </summary>
        /// <param name="center">Center of the belt (usually the star position)</param>
        /// <param name="count">Total asteroids to spawn</param>
        /// <param name="innerRadius">Inner radius of belt</param>
        /// <param name="outerRadius">Outer radius of belt</param>
        /// <param name="minSize">Minimum asteroid size</param>
        /// <param name="maxSize">Maximum asteroid size</param>
        /// <param name="inclinationDegrees">Max inclination in degrees applied as small tilt from XZ plane</param>
        /// <param name="seed">Random seed</param>
        /// <param name="addStaticCollider">If true, adds static colliders for picking</param>
        private void SpawnAsteroidBelt(Vector3 center, int count = 1000, float innerRadius = 200f, float outerRadius = 400f, float minSize = 0.2f, float maxSize = 1.5f, float inclinationDegrees = 2.0f, int seed = 424242, bool addStaticCollider = false)
        {
            if (count <= 0) return;
            if (innerRadius < 0f) innerRadius = 0f;
            if (outerRadius < innerRadius) outerRadius = innerRadius + 1f;

            var rng = new Random(seed);
            var tex = _checkerTexture ?? _whiteTexture ?? CreateSolidTexture(255, 255, 255, 255);
            var sharedSphere = Mesh.CreateSphere3D();

            // convert inclination to radians
            float inclRad = MathF.Abs(inclinationDegrees) * (MathF.PI / 180f);

            for (int i = 0; i < count; i++)
            {
                // pick a radius with probability proportional to area (so uniform density across annulus)
                float u = (float)rng.NextDouble();
                float r = MathF.Sqrt(u * (outerRadius * outerRadius - innerRadius * innerRadius) + innerRadius * innerRadius);
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);

                // small inclination: tilt by a small angle around random node line
                float tilt = ((float)rng.NextDouble() * 2f - 1f) * inclRad; // [-inclRad, inclRad]
                // choose random longitude of ascending node
                float node = (float)(rng.NextDouble() * Math.PI * 2.0);

                // base position in XZ
                float x = center.X + MathF.Cos(angle) * r;
                float z = center.Z + MathF.Sin(angle) * r;
                // apply inclination by rotating point about node axis roughly (approx)
                // We'll compute small Y displacement using sin(tilt) * r * sin(some offset)
                float y = center.Y + MathF.Sin(tilt) * r * MathF.Sin(angle - node);

                var pos = new Vector3(x, y, z);

                float s = minSize + ((float)rng.NextDouble() * (maxSize - minSize));

                var e = World.CreateEntity()
                    .Set(new CelestialBody())
                    .Set(sharedSphere)
                    .Set(new Kinematic())
                    .Set(new Parent((Entity)SunEntity!))
                    .Set(new Position3D(pos.X, pos.Y, pos.Z))
                    .Set(new Size3D(s))
                    .Set(new CollisionShape(new Sphere(s)))
                    .Set(Rotation3D.Identity)
                    .Set(new AngularVelocity3D(0, 0.0025f, 0))
                    .Set(new Texture2DRef { Texture = tex });

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
            // Normalize to TitleStorage-friendly relative POSIX path
            var normalized = NormalizeTitlePath(path);

            // Quick existence + file size check
            if (!RootTitleStorage.GetFileSize(normalized, out var fileSize))
            {
                Console.WriteLine($"[LoadTextureFromFile] File not found in TitleStorage: {normalized}");
                return null;
            }

            // Choose loader by file extension
            var ext = Path.GetExtension(normalized).ToLowerInvariant();

            using var uploader = new ResourceUploader(GraphicsDevice);
            Texture? texture = null;
            var name = Path.GetFileNameWithoutExtension(normalized);

            // Try to obtain basic image info (width/height). If this fails, prefer the explicit StbImageSharp fallback
            bool haveImageInfo = false;
            int imgW = 0, imgH = 0;
            if (MoonWorks.Graphics.ImageUtils.ImageInfoFromFile(RootTitleStorage, normalized, out var w, out var h, out _))
            {
                imgW = (int)w;
                imgH = (int)h;
                haveImageInfo = true;
                if (imgW < 1 || imgH < 1)
                {
                    Console.WriteLine($"[LoadTextureFromFile] Image has invalid dimensions ({imgW}x{imgH}): {normalized}");
                    return null;
                }
            }

            if (ext == ".dds")
            {
                texture = uploader.CreateTextureFromDDS(name, RootTitleStorage, normalized);
                if (texture == null)
                {
                    Console.WriteLine($"[LoadTextureFromFile] CreateTextureFromDDS failed for: {normalized}");
                    return null;
                }
            }
            else
            {
                // Only attempt the compressed-path uploader if we successfully read image dimensions.
                // Some backends may create an invalid/zero-sized texture if fed an unknown container.
                if (haveImageInfo)
                {
                    texture = uploader.CreateTexture2DFromCompressed(
                        name,
                        RootTitleStorage,
                        normalized,
                        TextureFormat.R8G8B8A8Unorm,
                        TextureUsageFlags.Sampler
                    );

                    if (texture == null)
                    {
                        Console.WriteLine($"[LoadTextureFromFile] CreateTexture2DFromCompressed returned null for: {normalized}, falling back to software decode");
                    }
                    else
                    {
                        // We trust ImageInfoFromFile here; assume texture dimensions match. If you still see assertions,
                        // we'll fall back below when texture is null.
                    }
                }

                if (texture == null)
                {
                    // Fallback: try StbImageSharp to decode common formats (JPEG/PNG/etc.)
                    try
                    {
                        if (fileSize == 0)
                        {
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: file is empty: {normalized}");
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
                            Console.WriteLine($"[LoadTextureFromFile] Fallback: decode failed or invalid dimensions for: {normalized}");
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

            // Final defensive validation
            if (texture == null)
            {
                Console.WriteLine($"[LoadTextureFromFile] Failed to create texture for: {normalized}");
                return null;
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

            // Update floating origin system (check if rebase is needed)
            if (_floatingOriginManager != null)
            {
                if (_floatingOriginManager.Update(_camera.Position, out var rebaseOffset))
                {
                    // Keep the camera near origin too so it doesn't immediately trigger another rebase
                    _camera.Position -= rebaseOffset;
                }
            }

            // Ensure entities with Position3D also have AbsolutePosition (for newly created entities)
            foreach (var entity in World.Query(typeof(Position3D)))
            {
                if (!entity.Has<AbsolutePosition>())
                {
                    var pos = entity.GetMut<Position3D>().Value;
                    var absolutePos = _floatingOriginManager?.ToAbsolutePosition(pos) ?? new Vector3d(pos.X, pos.Y, pos.Z);
                    entity.Set(new AbsolutePosition(absolutePos));
                }
            }

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
            // Note: Mouse picking works correctly with floating origin since it operates on 
            // the physics simulation's coordinate space which is kept relative to the current origin
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
                            // Rebase the world at the target so it sits at the grid origin, then place camera at desired offset
                            if (_floatingOriginManager != null)
                            {
                                var offset = staticBody.Pose.Position; // shift world by -offset
                                _floatingOriginManager.ForceRebase(offset);
                                _camera.Position = new Vector3(0, 10, 6); // camera relative to new origin
                                _camera.LookAt(Vector3.Zero);
                            }
                            else
                            {
                                // Fallback: no floating origin manager, just move camera in-place
                                _camera.Position = staticBody.Pose.Position + new Vector3(0, 10, 6);
                                _camera.LookAt(staticBody.Pose.Position);
                            }
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
                            if (_floatingOriginManager != null)
                            {
                                _floatingOriginManager.ForceRebase(p);
                                _camera.Position = new Vector3(0, 10, 6);
                                _camera.LookAt(Vector3.Zero);
                            }
                            else
                            {
                                Console.WriteLine($"SUCCESS: Hit a DYNAMIC object at distance {result.Distance}. Position: {p}");
                                _camera.Position = p + new Vector3(0, 10, 6);
                                _camera.LookAt(p);
                            }
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

            World.Update((float)delta.TotalSeconds);

            // Debug: Print floating origin info (uncomment for debugging)
            // if (_floatingOriginManager != null)
            // {
            //     var origin = _floatingOriginManager.CurrentOrigin;
            //     var camDist = new Vector3d(_camera.Position.X, _camera.Position.Y, _camera.Position.Z).Length();
            //     Console.WriteLine($"[FloatingOrigin] Current Origin: {origin}, Camera Distance from Origin: {camDist:F1}");
            // }

            // Debug: Press F5 to manually trigger a floating origin rebase for testing
            if (Inputs.Keyboard.IsPressed(KeyCode.F5))
            {
                TestFloatingOriginRebase();
            }
        }

        /// <summary>
        /// Test method to manually trigger a floating origin rebase for debugging.
        /// </summary>
        private void TestFloatingOriginRebase()
        {
            if (_floatingOriginManager != null)
            {
                Console.WriteLine("[Test] Manually triggering floating origin rebase...");

                // Simulate moving the camera far from origin to trigger rebase
                var farPosition = new Vector3(5000, 0, 5000);
                _camera.Position = farPosition;

                // Force an update to trigger the rebase
                if (_floatingOriginManager.Update(farPosition, out var rebaseOffset))
                {
                    _camera.Position -= rebaseOffset;
                }

                Console.WriteLine($"[Test] Camera moved to {farPosition}, rebase should have occurred.");
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
                    Vector3 p = center + (((u * MathF.Cos(ang)) + (v * MathF.Sin(ang))) * radius);
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
            Vector3 a = center + (up * halfLength);
            Vector3 b = center - (up * halfLength);
            // cylinder rings
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = (right * MathF.Cos(t0)) + (forward * MathF.Sin(t0));
                Vector3 r1 = (right * MathF.Cos(t1)) + (forward * MathF.Sin(t1));
                _lineBatch.AddLine(a + (r0 * radius), a + (r1 * radius), color);
                _lineBatch.AddLine(b + (r0 * radius), b + (r1 * radius), color);
                _lineBatch.AddLine(a + (r0 * radius), b + (r0 * radius), color);
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
                        Vector3 dir = (right * (MathF.Cos(u) * r)) + (forward * (MathF.Sin(u) * r)) + ringUp;
                        Vector3 p = centerHem + (dir * radius);
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
            Vector3 a = center + (up * halfLength);
            Vector3 b = center - (up * halfLength);
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments * MathF.Tau;
                float t1 = (float)(i + 1) / segments * MathF.Tau;
                Vector3 r0 = (right * MathF.Cos(t0)) + (forward * MathF.Sin(t0));
                Vector3 r1 = (right * MathF.Cos(t1)) + (forward * MathF.Sin(t1));
                _lineBatch.AddLine(a + (r0 * radius), a + (r1 * radius), color);
                _lineBatch.AddLine(b + (r0 * radius), b + (r1 * radius), color);
                _lineBatch.AddLine(a + (r0 * radius), b + (r0 * radius), color);
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

            // Cleanup floating origin manager if needed
            // _floatingOriginManager doesn't implement IDisposable, so no cleanup needed
        }
    }
}
