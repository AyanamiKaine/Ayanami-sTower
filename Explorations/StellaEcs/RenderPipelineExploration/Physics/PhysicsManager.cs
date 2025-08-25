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
        Simulation = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, 0, 0)), new SolveDescription(8, 1));
    }
    /// <summary>
    /// Advances the physics simulation by a single timestep.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void Step(double deltaTime)
    {
        Simulation.Timestep((float)deltaTime, _threadDispatcher);

        // After the physics step, detect overlaps between ECS-declared colliders and
        // raise enter/stay/exit events. We use a conservative bounding-sphere test
        // derived from known primitive shapes for a lightweight implementation.
        DetectCollisions();
    }

    /// <summary>
    /// Detect overlapping pairs using a simple bounding-sphere approximation and
    /// raise enter/stay/exit events.
    /// </summary>
    private void DetectCollisions()
    {
        _currentCollisions.Clear();

        // Gather entities that have a collision shape and a position
        foreach (var a in _world.Query(typeof(CollisionShape), typeof(Position3D)))
        {
            var shapeA = a.GetCopy<CollisionShape>().Shape;
            var posA = a.GetCopy<Position3D>().Value;
            var rA = GetBoundingSphereRadius(shapeA);

            foreach (var b in _world.Query(typeof(CollisionShape), typeof(Position3D)))
            {
                if (a == b) continue;

                // Order pairs so (A,B) and (B,A) map to the same tuple
                var first = a;
                var second = b;
                if (first.GetHashCode() > second.GetHashCode()) (first, second) = (second, first);

                // avoid double-testing the same ordered pair in this nested loop
                if (_currentCollisions.Contains((first, second))) continue;

                var shapeB = b.GetCopy<CollisionShape>().Shape;
                var posB = b.GetCopy<Position3D>().Value;
                var rB = GetBoundingSphereRadius(shapeB);

                var dx = posA.X - posB.X;
                var dy = posA.Y - posB.Y;
                var dz = posA.Z - posB.Z;
                var distSq = dx * dx + dy * dy + dz * dz;
                var radiusSum = rA + rB;

                if (distSq <= (radiusSum * radiusSum))
                {
                    _currentCollisions.Add((first, second));
                }
            }
        }

        // Compare previous vs current to emit events
        // Enter: in current but not in previous
        foreach (var pair in _currentCollisions)
        {
            if (!_previousCollisions.Contains(pair))
            {
                OnCollisionEnter?.Invoke(pair.Item1, pair.Item2);
            }
            else
            {
                OnCollisionStay?.Invoke(pair.Item1, pair.Item2);
            }
        }

        // Exit: in previous but not in current
        foreach (var pair in _previousCollisions)
        {
            if (!_currentCollisions.Contains(pair))
            {
                OnCollisionExit?.Invoke(pair.Item1, pair.Item2);
            }
        }

        // Swap sets for next frame
        _previousCollisions.Clear();
        foreach (var p in _currentCollisions) _previousCollisions.Add(p);
    }

    private static double GetBoundingSphereRadius(object? shape)
    {
        if (shape == null) return 0.0;

        switch (shape)
        {
            case Sphere s:
                return s.Radius;
            case Box b:
                // approximate by the diagonal of the half-extents (HalfWidth/Height/Length)
                var hx = b.HalfWidth;
                var hy = b.HalfHeight;
                var hz = b.HalfLength;
                return Math.Sqrt(hx * hx + hy * hy + hz * hz);
            case Capsule c:
                return c.Radius + c.HalfLength;
            case Cylinder cy:
                return cy.Radius + cy.HalfLength;
            default:
                // unknown shapes: return a small default to avoid false positives
                return 0.0;
        }
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
    /// <summary>
    /// Creates a physics object for the specified entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="collisionShape"></param>
    public void CreatePhysicsObjectForEntity(Entity entity, CollisionShape collisionShape)
    {
        TypedIndex? shapeIndex = null;
        switch (collisionShape.Shape)
        {
            case Sphere sphere: shapeIndex = Simulation.Shapes.Add(sphere); break;
            case Box box: shapeIndex = Simulation.Shapes.Add(box); break;
                // Add other shapes...
        }

        if (!shapeIndex.HasValue) return;

        var pos = entity.Has<Position3D>() ? entity.GetCopy<Position3D>().Value : Vector3Double.Zero;
        var rot = entity.Has<Rotation3D>() ? entity.GetCopy<Rotation3D>().Value : QuaternionDouble.Identity;

        if (entity.Has<Kinematic>())
        {
            var bodyDesc = BodyDescription.CreateKinematic(new RigidPose(pos, rot), new CollidableDescription(shapeIndex.Value, 0.1f), new BodyActivityDescription(0.01f));
            var bodyHandle = Simulation.Bodies.Add(bodyDesc);
            entity.Set(new PhysicsBody { Handle = bodyHandle });
        }
        else // Static
        {
            var staticDesc = new StaticDescription(HighPrecisionConversions.ToVector3(pos), shapeIndex.Value);
            var staticHandle = Simulation.Statics.Add(staticDesc);
            entity.Set(new PhysicsStatic { Handle = staticHandle });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _bufferPool.Clear();
        _threadDispatcher.Dispose();
    }
}
