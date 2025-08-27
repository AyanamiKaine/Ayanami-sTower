using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Thread-safe storage for narrow-phase contacts. This is a reference type that can be shared
/// by many small struct wrappers used by Bepu's Simulation.Create generic requirement.
/// </summary>
public sealed class NarrowPhaseContactStorage : IDisposable
{
    private readonly object _lock = new();
    private readonly List<(CollidableReference A, CollidableReference B)> _contacts = new();

    internal void Add((CollidableReference A, CollidableReference B) pair)
    {
        lock (_lock) _contacts.Add(pair);
    }

    public List<(CollidableReference A, CollidableReference B)> SnapshotAndClear()
    {
        lock (_lock)
        {
            var copy = new List<(CollidableReference, CollidableReference)>(_contacts);
            _contacts.Clear();
            return copy;
        }
    }

    public void Dispose() { }
}

/// <summary>
/// Struct wrapper implementing <see cref="INarrowPhaseCallbacks"/> required by Bepu.
/// Delegates actual recording to a shared <see cref="NarrowPhaseContactStorage"/> instance.
/// </summary>
public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public NarrowPhaseContactStorage Storage;

    public NarrowPhaseCallbacks(NarrowPhaseContactStorage storage) { Storage = storage; }

    public void Initialize(Simulation simulation) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        material = new PairMaterialProperties { FrictionCoefficient = 1f, MaximumRecoveryVelocity = 2f, SpringSettings = new SpringSettings(30, 1) };
        const float penetrationEpsilon = -1e-3f;
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= penetrationEpsilon)
            {
                Storage?.Add((pair.A, pair.B));
                break;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        const float penetrationEpsilon = -1e-3f;
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= penetrationEpsilon)
            {
                Storage?.Add((pair.A, pair.B));
                break;
            }
        }
        return true;
    }

    public void Dispose() { }
}


