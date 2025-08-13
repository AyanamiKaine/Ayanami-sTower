using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Engine.Components;
using AyanamisTower.StellaEcs.Engine.Graphics;
using AyanamisTower.StellaEcs.Components;
using DefaultRendererClass = AyanamisTower.StellaEcs.Engine.DefaultRenderer.DefaultRenderer;

namespace AyanamisTower.StellaEcs.Engine.Ecs;

/// <summary>
/// Bridges the ECS world to the engine’s lit 3D renderer by discovering entities
/// with Position3D + Mesh3D + RenderLit3D and submitting instances each frame.
/// </summary>
/// <summary>
/// ECS System that discovers Position3D + Mesh3D + RenderLit3D and submits lit instances to the renderer.
/// </summary>
public sealed class RenderSyncSystem3DLit : ISystem
{
    private readonly DefaultRendererClass _renderer;

    /// <inheritdoc />
    public string Name { get; set; } = nameof(RenderSyncSystem3DLit);
    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Creates the render sync system.
    /// </summary>
    public RenderSyncSystem3DLit(DefaultRendererClass renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Clears previous lit submissions and rebuilds them from ECS components.
    /// </summary>
    public void Update(World world, float deltaTime)
    {
        if (!Enabled) return;

        // Clear previous frame’s lit instances, then rebuild from ECS state.
        _renderer.ClearLitInstances();

        // New path: Mesh3D + RenderLit3D
        foreach (var entity in world.Query(typeof(Position3D), typeof(Mesh3D), typeof(RenderLit3D)))
        {
            var pos = world.GetComponent<Position3D>(entity).Value;
            var mesh = world.GetComponent<Mesh3D>(entity).Mesh;
            // Compose model as S * R * T (scale, rotation, translation)
            Matrix4x4 model = Matrix4x4.CreateTranslation(pos);
            if (world.HasComponent<Rotation3D>(entity))
            {
                var rot = world.GetComponent<Rotation3D>(entity).Value;
                model = Matrix4x4.CreateFromQuaternion(rot) * model;
            }
            if (world.HasComponent<Size3D>(entity))
            {
                var scl = world.GetComponent<Size3D>(entity).Value;
                model = Matrix4x4.CreateScale(scl) * model;
            }
            _renderer.AddMesh3DLit(() => mesh, () => model, castsShadows: !world.HasComponent<NoShadowCasting>(entity));
        }

        // Obsolete compatibility path removed. Use Mesh3D + RenderLit3D.
    }
}
