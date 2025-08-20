using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Components;

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
        foreach (var entity in world.Query(typeof(Position3D), typeof(Parent), typeof(AngularVelocity3D)).ToList())
        {
            var parentRef = entity.GetCopy<Parent>();
            var childWorldPos = entity.GetCopy<Position3D>();
            var angVel = entity.GetCopy<AngularVelocity3D>();

            // Ensure parent has a position
            var parent = parentRef.Entity;
            if (!parent.Has<Position3D>())
            {
                continue;
            }

            var parentWorldPos = parent.GetCopy<Position3D>();

            // Work in local space: use LocalPosition3D as the radius vector.
            Vector3 r;
            if (entity.Has<LocalPosition3D>())
            {
                r = entity.GetCopy<LocalPosition3D>().Value;
            }
            else
            {
                // Initialize local from current world offset if missing
                r = childWorldPos.Value - parentWorldPos.Value;
                entity.Set(new LocalPosition3D(r.X, r.Y, r.Z));
            }
            float radiusSq = r.LengthSquared();
            if (radiusSq <= 1e-9f)
            {
                // If colocated, give it a tiny nudge on X to establish an orbit plane
                r = new Vector3(1f, 0f, 0f);
            }

            // AngularVelocity3D.Y drives orbit around global Y axis
            float omega = angVel.Value.Y; // radians per second
            if (MathF.Abs(omega) <= 1e-6f)
            {
                continue; // no orbit this frame
            }

            float angle = omega * deltaTime;
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
            Vector3 rRot = Vector3.Transform(r, q);

            // Write back rotated local offset; the ParentChildPositionSyncSystem will update world position.
            entity.Set(new LocalPosition3D(rRot.X, rRot.Y, rRot.Z));
        }
    }
}
#pragma warning restore CS1591
