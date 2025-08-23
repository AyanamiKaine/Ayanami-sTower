using System;
using System.Numerics;
using System.Collections.Generic;
using System.Threading.Tasks;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;

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

        // Build a list of parented entities and compute their ancestor depth so we can
        // process parents before children (top-down). We'll group by depth and then
        // compute needed updates in parallel per-depth, applying writes sequentially
        // to avoid unsafe concurrent writes into the world/entity system.
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

        // Process per-depth groups. Entities at the same depth can be computed in parallel
        // because their parents are at lower depths and already processed.
        int idx = 0;
        while (idx < list.Count)
        {
            int curDepth = list[idx].Depth;
            var group = new List<Entity>();
            while (idx < list.Count && list[idx].Depth == curDepth)
            {
                group.Add(list[idx].Entity);
                idx++;
            }

            int n = group.Count;
            // Prepare a results array to store writes required for each entity.
            var results = new (Entity Entity, bool NeedSetLocal, LocalPosition3D NewLocal, bool NeedSetPosition, Vector3Double NewPosition)?[n];

            Parallel.For(0, n, i =>
            {
                var entity = group[i];
                var parentRef = entity.GetCopy<Parent>();
                var childWorldPos = entity.GetCopy<Position3D>();

                var parent = parentRef.Entity;
                if (!parent.IsValid() || !parent.Has<Position3D>())
                {
                    results[i] = null;
                    return;
                }

                var parentWorldPos = parent.GetCopy<Position3D>();

                // Compute or read local offset.
                LocalPosition3D local;
                bool hadLocal = entity.Has<LocalPosition3D>();
                if (hadLocal)
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
                }

                // Compute world = parent world + local
                var newWorld = parentWorldPos.Value + local.Value;
                bool posChanged = newWorld != childWorldPos.Value;

                results[i] = (entity, !hadLocal, local, posChanged, newWorld);
            });

            // Apply writes sequentially to avoid concurrent world mutation.
            for (int i = 0; i < n; ++i)
            {
                var r = results[i];
                if (r == null) continue;
                var (entity, needSetLocal, newLocal, needSetPosition, newPosVec) = r.Value;
                if (needSetLocal && !entity.Has<LocalPosition3D>())
                {
                    entity.Set(newLocal);
                }
                if (needSetPosition)
                {
                    var posComp = entity.GetCopy<Position3D>();
                    posComp.Value = newPosVec;
                    entity.Set(posComp);
                }
            }
        }
    }
}
#pragma warning restore CS1591
