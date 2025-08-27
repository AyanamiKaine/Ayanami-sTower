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
    // Store per-pair desire flags so the dispatcher can invoke callbacks asymmetrically.
    private readonly List<(CollidableReference A, CollidableReference B, bool AWants, bool BWants)> _contacts = new();

    internal void Add((CollidableReference A, CollidableReference B, bool AWants, bool BWants) item)
    {
        lock (_lock) _contacts.Add(item);
    }

    public List<(CollidableReference A, CollidableReference B, bool AWants, bool BWants)> SnapshotAndClear()
    {
        lock (_lock)
        {
            var copy = new List<(CollidableReference, CollidableReference, bool, bool)>(_contacts);
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
    public CollisionLayerRegistry? LayerRegistry;

    public NarrowPhaseCallbacks(NarrowPhaseContactStorage storage, CollisionLayerRegistry? registry = null) { Storage = storage; LayerRegistry = registry; }

    public void Initialize(Simulation simulation) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        // If a layer registry is present, consult it and skip pair if categories/masks don't match.
        if (LayerRegistry != null)
        {
            if (LayerRegistry.TryGetLayer(a, out var ca, out var ma) && LayerRegistry.TryGetLayer(b, out var cb, out var mb))
            {
                // Two objects should generate contacts if either side wants to interact.
                // Allow contact generation when (A.mask & B.category) != 0 || (B.mask & A.category) != 0
                if (((ma & cb) == 0) && ((mb & ca) == 0)) return false;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        if (LayerRegistry != null)
        {
            if (LayerRegistry.TryGetLayer(pair.A, out var ca, out var ma) && LayerRegistry.TryGetLayer(pair.B, out var cb, out var mb))
            {
                if (((ma & cb) == 0) || ((mb & ca) == 0)) return false;
            }
        }
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        material = new PairMaterialProperties { FrictionCoefficient = 1f, MaximumRecoveryVelocity = 2f, SpringSettings = new SpringSettings(30, 1) };
        const float penetrationEpsilon = -1e-3f;
        // If a layer registry exists and indicates these two collidables should not interact, skip recording.
        if (LayerRegistry != null)
        {
            if (LayerRegistry.TryGetLayer(pair.A, out var ca, out var ma) && LayerRegistry.TryGetLayer(pair.B, out var cb, out var mb))
            {
                // If neither side wants interaction, skip recording.
                if (((ma & cb) == 0) && ((mb & ca) == 0))
                    return true;
            }
        }

        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= penetrationEpsilon)
            {
                if (LayerRegistry != null && LayerRegistry.TryGetLayer(pair.A, out var ca2, out var ma2) && LayerRegistry.TryGetLayer(pair.B, out var cb2, out var mb2))
                {
                    bool aWants = (ma2 & cb2) != 0;
                    bool bWants = (mb2 & ca2) != 0;
                    Storage?.Add((pair.A, pair.B, aWants, bWants));
                }
                else
                {
                    // If no registry, assume both sides want it (backwards compatible)
                    Storage?.Add((pair.A, pair.B, true, true));
                }
                break;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        const float penetrationEpsilon = -1e-3f;
        if (LayerRegistry != null)
        {
            if (LayerRegistry.TryGetLayer(pair.A, out var ca, out var ma) && LayerRegistry.TryGetLayer(pair.B, out var cb, out var mb))
            {
                if (((ma & cb) == 0) && ((mb & ca) == 0))
                    return true;
            }
        }

        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= penetrationEpsilon)
            {
                if (LayerRegistry != null && LayerRegistry.TryGetLayer(pair.A, out var ca2, out var ma2) && LayerRegistry.TryGetLayer(pair.B, out var cb2, out var mb2))
                {
                    bool aWants = (ma2 & cb2) != 0;
                    bool bWants = (mb2 & ca2) != 0;
                    Storage?.Add((pair.A, pair.B, aWants, bWants));
                }
                else
                {
                    Storage?.Add((pair.A, pair.B, true, true));
                }
                break;
            }
        }
        return true;
    }

    public void Dispose() { }
}


