using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.CorePlugin;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/// <summary>
/// Synchronizes child world positions so Position3D = Parent.Position3D + LocalPosition3D.
/// If LocalPosition3D is missing, it is initialized from the current offset: child.Position3D - parent.Position3D.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem3D))]
[UpdateAfter(typeof(OrbitSystem3D))]
public sealed class ParentChildPositionSyncSystem : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "ParentChildPositionSyncSystem";
    public int Priority { get; set; } = 0;

    public void Update(World world, float deltaTime)
    {
        if (!Enabled) { return; }

        // Do a few passes to propagate through deeper hierarchies in the same frame.
        // This avoids needing an explicit depth sort.
        const int maxPasses = 4;
        for (int pass = 0; pass < maxPasses; pass++)
        {
            foreach (var entity in world.Query(typeof(Position3D), typeof(Parent)).ToList())
            {
                var parentRef = entity.GetCopy<Parent>();
                var childWorldPos = entity.GetCopy<Position3D>();

                var parent = parentRef.Entity;
                if (!parent.IsValid() || !parent.Has<Position3D>())
                {
                    continue;
                }

                var parentWorldPos = parent.GetCopy<Position3D>();

                // Ensure there is a LocalPosition3D. If missing, initialize from current world offset.
                LocalPosition3D local;
                if (entity.Has<LocalPosition3D>())
                {
                    local = entity.GetCopy<LocalPosition3D>();
                }
                else
                {
                    local = new LocalPosition3D(
                        childWorldPos.Value.X - parentWorldPos.Value.X,
                        childWorldPos.Value.Y - parentWorldPos.Value.Y,
                        childWorldPos.Value.Z - parentWorldPos.Value.Z
                    );
                    entity.Set(local);
                }

                // Compute world = parent world + local
                var newWorld = parentWorldPos.Value + local.Value;
                if (newWorld != childWorldPos.Value)
                {
                    childWorldPos.Value = newWorld;
                    entity.Set(childWorldPos);
                }
            }
        }
    }
}
#pragma warning restore CS1591
