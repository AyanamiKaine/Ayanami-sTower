using System;
using System.Numerics;
using System.Threading.Tasks;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.CorePlugin;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Orbits any entity that has a Parent and Position3D around its parent's Position3D.
/// Uses AngularVelocity3D.Y as the angular speed in radians/sec around the global Y axis.
/// The initial relative offset (child.Position3D - parent.Position3D) defines the orbit radius.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public sealed class OrbitSystem3D : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "OrbitSystem3D";
    public int Priority { get; set; } = 0;

    public void Update(World world, float deltaTime)
    {
        if (!Enabled) { return; }
        // Snapshot to avoid mutation issues while iterating
        var entities = world.Query(typeof(Position3D), typeof(Parent), typeof(AngularVelocity3D)).ToList();
        if (entities.Count == 0) return;

        int n = entities.Count;
        var newLocals = new LocalPosition3D[n];
        var needSet = new bool[n];

        // Compute rotations in parallel. Writes are stored and applied sequentially below.
        Parallel.For(0, n, i =>
        {
            var entity = entities[i];
            var parentRef = entity.GetCopy<Parent>();
            var childWorldPos = entity.GetCopy<Position3D>();
            var angVel = entity.GetCopy<AngularVelocity3D>();

            // Ensure parent has a position
            var parent = parentRef.Entity;
            if (!parent.Has<Position3D>())
            {
                needSet[i] = false;
                return;
            }

            var parentWorldPos = parent.GetCopy<Position3D>();

            // Work in local space: use LocalPosition3D as the radius vector.
            Vector3Double r;
            if (entity.Has<LocalPosition3D>())
            {
                r = entity.GetCopy<LocalPosition3D>().Value;
            }
            else
            {
                // Initialize local from current world offset if missing
                r = childWorldPos.Value - parentWorldPos.Value;
            }
            double radiusSq = r.LengthSquared();
            if (radiusSq <= 1e-9f)
            {
                // If colocated, give it a tiny nudge on X to establish an orbit plane
                r = new Vector3Double(1f, 0f, 0f);
            }

            // AngularVelocity3D.Y drives orbit around global Y axis
            double omega = angVel.Value.Y; // radians per second
            if (Math.Abs(omega) <= 1e-6f)
            {
                needSet[i] = false;
                return; // no orbit this frame
            }

            double angle = omega * deltaTime;
            var q = QuaternionDouble.CreateFromAxisAngle(Vector3Double.UnitY, angle);
            Vector3Double rRot = Vector3Double.Transform(r, q);

            newLocals[i] = new LocalPosition3D(rRot);
            needSet[i] = true;
        });

        // Apply writes sequentially to avoid concurrent mutation of the world.
        for (int i = 0; i < n; ++i)
        {
            if (!needSet[i]) continue;
            var entity = entities[i];
            // If the entity lacks a LocalPosition3D set it; otherwise overwrite.
            if (!entity.Has<LocalPosition3D>())
            {
                entity.Set(newLocals[i]);
            }
            else
            {
                entity.Set(newLocals[i]);
            }
        }
    }
}
#pragma warning restore CS1591
