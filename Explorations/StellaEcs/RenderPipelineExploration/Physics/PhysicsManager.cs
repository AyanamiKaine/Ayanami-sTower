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
    // handle -> entity mappings
    private readonly Dictionary<int, Entity> _bodyHandleToEntity = new();
    private readonly Dictionary<int, Entity> _staticHandleToEntity = new();
    // Collision tracking sets used to detect enter/exit/stay
    private readonly HashSet<(Entity, Entity)> _previousCollisions = new();
    private readonly HashSet<(Entity, Entity)> _currentCollisions = new();

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
        _narrowPhase = new NarrowPhaseCallbacks(_narrowStorage);
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
            }
            else
            {
                // Create a static collider
                var staticDescription = new StaticDescription(HighPrecisionConversions.ToVector3(pos), (TypedIndex)shapeIndex!);
                var staticHandle = Simulation.Statics.Add(staticDescription);
                entity.Set(new PhysicsStatic { Handle = staticHandle });
                _staticHandleToEntity[staticHandle.Value] = entity;

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
        // Snapshot via the shared storage instance. _narrowPhase contains the struct wrapper; extract the storage.
        var contacts = _narrowStorage.SnapshotAndClear();

        _currentCollisions.Clear();
        foreach ((CollidableReference aRef, CollidableReference bRef) in contacts)
        {
            if (TryResolveEntity(aRef, out var ea) && TryResolveEntity(bRef, out var eb))
            {
                // canonicalize ordering to avoid duplicate pairs with swapped order
                var pair = ea.GetHashCode() <= eb.GetHashCode() ? (ea, eb) : (eb, ea);
                _currentCollisions.Add(pair);
            }
        }

        // Enter / Stay
        foreach (var p in _currentCollisions)
        {
            if (!_previousCollisions.Contains(p))
                OnCollisionEnter?.Invoke(p.Item1, p.Item2);
            else
                OnCollisionStay?.Invoke(p.Item1, p.Item2);
        }
        // Exit
        foreach (var p in _previousCollisions)
        {
            if (!_currentCollisions.Contains(p))
                OnCollisionExit?.Invoke(p.Item1, p.Item2);
        }

        // prepare for next frame
        _previousCollisions.Clear();
        foreach (var p in _currentCollisions) _previousCollisions.Add(p);
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
}
