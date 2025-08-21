using System;
using System.Numerics;
using System.Collections.Generic;
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
        // Build a list of parented entities and compute their ancestor depth so we can
        // process parents before children (top-down). This avoids race conditions when
        // parents and children are both mutated by other systems in the same frame.
        var entities = world.Query(typeof(Position3D), typeof(Parent)).ToList();
        if (entities.Count == 0) { return; }

        var list = new List<(Entity Entity, int Depth)>(entities.Count);
        foreach (var e in entities)
        {
            int depth = 0;
            var seen = new HashSet<Entity>();
            var cur = e;
            // Walk up the parent chain to count ancestors. Protect against cycles.
            while (cur.Has<Parent>())
            {
                var pref = cur.GetCopy<Parent>();
                var p = pref.Entity;
                if (!p.IsValid() || !seen.Add(p)) break;
                depth++;
                cur = p;
                // safety cap
                if (depth > 1024) break;
            }
            list.Add((e, depth));
        }

        // Sort by depth ascending so root-most parents (depth=0) are processed first.
        list.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        foreach (var pair in list)
        {
            var entity = pair.Entity;
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
#pragma warning restore CS1591
