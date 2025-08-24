using System;
using System.Numerics;
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
