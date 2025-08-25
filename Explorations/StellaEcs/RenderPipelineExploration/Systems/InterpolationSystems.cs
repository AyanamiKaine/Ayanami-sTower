using System;
using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.HighPrecisionMath;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.StellaEcs.StellaInvicta.Systems
{
    public static class InterpolationSystems
    {
        // Snapshot positions before a physics tick. Minimal work: only entities that have Position3D.
        public static void SnapshotPositions(World world)
        {
            foreach (var e in world.Query(typeof(Position3D)))
            {
                var pos = e.GetMut<Position3D>().Value;
                e.Set(new PreviousPosition3D(pos));
            }
        }

        // Interpolate positions for rendering. alpha in [0,1].
        public static void InterpolateRenderPositions(World world, double alpha)
        {
            float t = (float)Math.Clamp(alpha, 0.0, 1.0);
            foreach (var e in world.Query(typeof(Position3D)))
            {
                var cur = e.GetMut<Position3D>().Value;
                var prev = e.Has<PreviousPosition3D>() ? e.GetCopy<PreviousPosition3D>().Value : cur;
                var interp = Lerp(prev, cur, t);
                e.Set(new RenderPosition3D(interp));
            }
        }

        private static Vector3Double Lerp(Vector3Double a, Vector3Double b, float t)
        {
            return new Vector3Double(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        // Helper used by render code when reading an entity's draw position.
        public static Vector3Double GetRenderPosition(Entity e)
        {
            if (e.Has<RenderPosition3D>()) return e.GetCopy<RenderPosition3D>().Value;
            if (e.Has<Position3D>()) return e.GetCopy<Position3D>().Value;
            return Vector3Double.Zero;
        }
    }
}
