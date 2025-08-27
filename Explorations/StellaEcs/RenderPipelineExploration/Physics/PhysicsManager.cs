using System;
using System.Numerics;
using System.Collections.Generic;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using AyanamisTower.StellaEcs.StellaInvicta.Components;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using BepuUtilities.Memory;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// Manages the physics simulation.
/// </summary>
public class PhysicsManager : IDisposable
{
    /// <summary>
    /// The physics simulation instance.
    /// </summary>
    public Simulation Simulation { get; }
    private readonly BufferPool _bufferPool;
    private readonly ThreadDispatcher _threadDispatcher;
    private readonly World _world;
    private readonly NarrowPhaseCallbacks _narrowPhase;
    private readonly NarrowPhaseContactStorage _narrowStorage;
    private readonly CollisionLayerRegistry _layerRegistry;
    // handle -> entity mappings
    private readonly Dictionary<int, Entity> _bodyHandleToEntity = new();
    private readonly Dictionary<int, Entity> _staticHandleToEntity = new();
    // Collision tracking sets used to detect enter/exit/stay.
    // We track directed collisions (subject -> other) so callbacks can be dispatched
    // only to entities that "wanted" the interaction.
    private readonly HashSet<(Entity Subject, Entity Other)> _previousDirectedCollisions = new();
    private readonly HashSet<(Entity Subject, Entity Other)> _currentDirectedCollisions = new();

    // Events fired when collisions begin, persist, or end
    /// <summary>
    /// Raised when two entities begin overlapping this frame.
    /// Parameters: (entityA, entityB)
    /// </summary>
    public event Action<Entity, Entity>? OnCollisionEnter;

    /// <summary>
    /// Raised when two entities remain overlapping (was overlapping previous frame).
    /// Parameters: (entityA, entityB)
    /// </summary>
    public event Action<Entity, Entity>? OnCollisionStay;

    /// <summary>
    /// Raised when two entities were overlapping last frame but are no longer overlapping.
    /// Parameters: (entityA, entityB)
    /// </summary>
    public event Action<Entity, Entity>? OnCollisionExit;
    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicsManager"/> class.
    /// </summary>
    /// <param name="world"></param>
    public PhysicsManager(World world)
    {
        _world = world;
        _bufferPool = new BufferPool();
        _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
        _narrowStorage = new NarrowPhaseContactStorage();
        _layerRegistry = new CollisionLayerRegistry();
        _narrowPhase = new NarrowPhaseCallbacks(_narrowStorage, _layerRegistry);
        Simulation = Simulation.Create(_bufferPool, _narrowPhase, new PoseIntegratorCallbacks(new Vector3(0, 0, 0)), new SolveDescription(8, 1));


        world.OnSetPost((Entity entity, in CollisionShape _, in CollisionShape collisionShape, bool _) =>
        {
            TypedIndex? shapeIndex = null; // Default to invalid index
                                           // Add concrete shape type; Shapes.Add<T>() requires an unmanaged struct, not the IShape interface.
            switch (collisionShape.Shape)
            {
                case Sphere sphere:
                    shapeIndex = Simulation.Shapes.Add(sphere);
                    break;
                case Box box:
                    shapeIndex = Simulation.Shapes.Add(box);
                    break;
                case Capsule capsule:
                    shapeIndex = Simulation.Shapes.Add(capsule);
                    break;
                case Cylinder cylinder:
                    shapeIndex = Simulation.Shapes.Add(cylinder);
                    break;
                default:
                    Console.WriteLine($"[Physics] Unsupported collision shape type: {collisionShape.Shape?.GetType().Name ?? "null"}");
                    return;
            }

            // Pull initial transform
            var pos = entity.Has<Position3D>() ? entity.GetMut<Position3D>().Value : Vector3Double.Zero;
            var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;

            if (entity.Has<Kinematic>())
            {
                // Create a kinematic body
                var pose = new RigidPose(pos, rot);
                var collidable = new CollidableDescription((TypedIndex)shapeIndex!, 0.1f);
                var activity = new BodyActivityDescription(0.01f);
                var bodyDesc = BodyDescription.CreateKinematic(pose, collidable, activity);
                var bodyHandle = Simulation.Bodies.Add(bodyDesc);
                entity.Set(new PhysicsBody { Handle = bodyHandle });
                _bodyHandleToEntity[bodyHandle.Value] = entity;
                // Register collision layer for this handle. If entity doesn't have one yet,
                // register a sensible default (category = 1<<0, mask = all) so layer lookups always succeed.
                uint defaultCategory = 1u << 0;
                uint defaultMask = uint.MaxValue;
                // Kinematic bodies are reported as Kinematic mobility by Bepu; register with that mobility.
                if (entity.Has<CollisionLayer>())
                {
                    var layer = entity.GetCopy<CollisionLayer>();
                    // Register under both Kinematic and Dynamic to be robust to mobility reporting.
                    _layerRegistry.RegisterHandle(CollidableMobility.Kinematic, bodyHandle.Value, layer.Category, layer.Mask);
                    _layerRegistry.RegisterHandle(CollidableMobility.Dynamic, bodyHandle.Value, layer.Category, layer.Mask);
                }
                else
                {
                    _layerRegistry.RegisterHandle(CollidableMobility.Kinematic, bodyHandle.Value, defaultCategory, defaultMask);
                    _layerRegistry.RegisterHandle(CollidableMobility.Dynamic, bodyHandle.Value, defaultCategory, defaultMask);
                }
            }
            else
            {
                // Create a static collider
                var staticDescription = new StaticDescription(HighPrecisionConversions.ToVector3(pos), (TypedIndex)shapeIndex!);
                var staticHandle = Simulation.Statics.Add(staticDescription);
                entity.Set(new PhysicsStatic { Handle = staticHandle });
                _staticHandleToEntity[staticHandle.Value] = entity;
                uint defaultCategory = 1u << 0;
                uint defaultMask = uint.MaxValue;
                if (entity.Has<CollisionLayer>())
                {
                    var layer = entity.GetCopy<CollisionLayer>();
                    _layerRegistry.RegisterHandle(CollidableMobility.Static, staticHandle.Value, layer.Category, layer.Mask);
                }
                else
                {
                    _layerRegistry.RegisterHandle(CollidableMobility.Static, staticHandle.Value, defaultCategory, defaultMask);
                }

            }
        });

        // When a CollisionLayer component is set on an entity, register any existing physics handles with the registry.
        world.OnSetPost((Entity entity, in CollisionLayer _, in CollisionLayer layer, bool _) =>
        {
            try
            {
                if (entity.Has<PhysicsBody>())
                {
                    var pb = entity.GetCopy<PhysicsBody>();
                    _layerRegistry.RegisterHandle(CollidableMobility.Kinematic, pb.Handle.Value, layer.Category, layer.Mask);
                    _layerRegistry.RegisterHandle(CollidableMobility.Dynamic, pb.Handle.Value, layer.Category, layer.Mask);
                }
                if (entity.Has<PhysicsStatic>())
                {
                    var ps = entity.GetCopy<PhysicsStatic>();
                    _layerRegistry.RegisterStaticHandle(ps.Handle.Value, layer.Category, layer.Mask);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Physics] Exception registering collision layer for entity {entity.Id}: {ex}");
            }
        });

        // Ensure physics objects are removed from the simulation when an entity is destroyed.
        // Use the world's pre-destroy hook so components are still readable.
        world.OnPreDestroy((Entity entity) =>
        {
            try
            {
                RemovePhysicsForEntity(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Physics] Exception while removing physics for destroyed entity: {ex}");
            }
        });

    }
    /// <summary>
    /// Advances the physics simulation by a single timestep.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Step(double deltaTime)
    {
        Simulation.Timestep((float)deltaTime, _threadDispatcher);

        // Process contacts recorded by the narrow-phase collector.
        // Snapshot via the shared storage instance. Each contact now includes flags indicating
        // whether the A side wanted the contact and whether the B side wanted it.
        var contacts = _narrowStorage.SnapshotAndClear();

        _currentDirectedCollisions.Clear();
        foreach ((CollidableReference aRef, CollidableReference bRef, bool aWants, bool bWants) in contacts)
        {
            if (!TryResolveEntity(aRef, out var ea) || !TryResolveEntity(bRef, out var eb))
                continue;

            // If A wanted the contact, record directed pair (A -> B)
            if (aWants)
            {
                _currentDirectedCollisions.Add((ea, eb));
            }

            // If B wanted the contact, record directed pair (B -> A)
            if (bWants)
            {
                _currentDirectedCollisions.Add((eb, ea));
            }
        }

        // Enter / Stay (directed)
        foreach (var p in _currentDirectedCollisions)
        {
            if (!_previousDirectedCollisions.Contains(p))
                OnCollisionEnter?.Invoke(p.Subject, p.Other);
            else
                OnCollisionStay?.Invoke(p.Subject, p.Other);
        }

        // Exit (directed)
        foreach (var p in _previousDirectedCollisions)
        {
            if (!_currentDirectedCollisions.Contains(p))
                OnCollisionExit?.Invoke(p.Subject, p.Other);
        }

        // prepare for next frame
        _previousDirectedCollisions.Clear();
        foreach (var p in _currentDirectedCollisions) _previousDirectedCollisions.Add(p);
    }

    /// <summary>
    /// Synchronizes the kinematic bodies with their current transforms.
    /// </summary>
    public void SyncKinematicBodies()
    {
        foreach (var entity in _world.Query(typeof(PhysicsBody), typeof(Kinematic), typeof(Position3D)))
        {
            var body = entity.GetMut<PhysicsBody>();
            var pos = entity.GetMut<Position3D>().Value;
            var rot = entity.Has<Rotation3D>() ? entity.GetMut<Rotation3D>().Value : QuaternionDouble.Identity;

            var bodyRef = Simulation.Bodies.GetBodyReference(body.Handle);
            if (bodyRef.Exists)
            {
                bodyRef.Pose = new RigidPose(pos, rot);
                bodyRef.Awake = true;
            }
        }
    }
    /// <inheritdoc/>
    public void Dispose()
    {
        _bufferPool.Clear();
        _threadDispatcher.Dispose();
        _narrowStorage.Dispose();
    }

    private bool TryResolveEntity(CollidableReference cref, out Entity e)
    {
        e = default;
        if (cref.Mobility != CollidableMobility.Static)
        {
            var bh = cref.BodyHandle;
            return _bodyHandleToEntity.TryGetValue(bh.Value, out e);
        }
        else
        {
            var sh = cref.StaticHandle;
            return _staticHandleToEntity.TryGetValue(sh.Value, out e);
        }
    }

    // Remove any physics objects (body or static) associated with an entity from the simulation
    private void RemovePhysicsForEntity(Entity entity)
    {
        // Bodies (dynamic/kinematic)
        if (entity.Has<PhysicsBody>())
        {
            var pb = entity.GetCopy<PhysicsBody>();
            var handle = pb.Handle;
            try
            {
                // Safely check and remove the body if it still exists
                var bodyRef = Simulation.Bodies.GetBodyReference(handle);
                if (bodyRef.Exists)
                {
                    Simulation.Bodies.Remove(handle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Physics] Warning: failed to remove body handle {handle.Value}: {ex.Message}");
            }
            _bodyHandleToEntity.Remove(handle.Value);
            // Body could be Kinematic/Dynamic — try to remove both possible mobility entries.
            _layerRegistry.UnregisterHandle(CollidableMobility.Kinematic, handle.Value);
            _layerRegistry.UnregisterHandle(CollidableMobility.Dynamic, handle.Value);
        }

        // Statics
        if (entity.Has<PhysicsStatic>())
        {
            var ps = entity.GetCopy<PhysicsStatic>();
            var handle = ps.Handle;
            try
            {
                // Statics use the Statics set
                var staticRef = Simulation.Statics.GetStaticReference(handle);
                if (staticRef.Exists)
                {
                    Simulation.Statics.Remove(handle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Physics] Warning: failed to remove static handle {handle.Value}: {ex.Message}");
            }
            _staticHandleToEntity.Remove(handle.Value);
            _layerRegistry.UnregisterHandle(CollidableMobility.Static, handle.Value);
        }
    }
}
