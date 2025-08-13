using System;
using System.Numerics;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine.Components;
using AyanamisTower.StellaEcs.Engine.Graphics;
using DefaultRendererClass = AyanamisTower.StellaEcs.Engine.DefaultRenderer.DefaultRenderer;

namespace AyanamisTower.StellaEcs.Engine.Ecs;

/// <summary>
/// Bridges ECS to the textured-lit renderer: submits Position3D + Mesh3D + Texture2DRef + RenderTexturedLit3D.
/// </summary>
public sealed class RenderSyncSystem3DTexturedLit : ISystem
{
    private readonly DefaultRendererClass _renderer;

    /// <summary>
    /// System name for debugging/profiling.
    /// </summary>
    public string Name { get; set; } = nameof(RenderSyncSystem3DTexturedLit);
    /// <summary>
    /// Whether the system should update this frame.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Creates a new sync system that submits ECS entities to the textured-lit renderer.
    /// </summary>
    /// <param name="renderer">The default renderer to submit instances to.</param>
    public RenderSyncSystem3DTexturedLit(DefaultRendererClass renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Queries for entities with Position3D, Mesh3D, Texture2DRef, and RenderTexturedLit3D and submits them to the renderer.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">Delta time in seconds.</param>
    public void Update(World world, float deltaTime)
    {
        if (!Enabled) return;

        // Clear any previous frame textured-lit instances
        _renderer.ClearTexturedLitInstances();

        int entityCount = 0;
        int shadowCasterCount = 0;

        foreach (var entity in world.Query(typeof(Position3D), typeof(Mesh3D), typeof(Texture2DRef), typeof(RenderTexturedLit3D)))
        {
            entityCount++;
            var pos = world.GetComponent<Position3D>(entity).Value;
            var mesh = world.GetComponent<Mesh3D>(entity).Mesh;
            var texture = world.GetComponent<Texture2DRef>(entity).Texture;

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

            bool castsShadows = !world.HasComponent<NoShadowCasting>(entity);
            if (castsShadows) shadowCasterCount++;

            _renderer.AddTextured3DLit(() => mesh, () => model, texture, castsShadows);
        }

        // Debug output every 60 frames (~1 second at 60fps)
        if (System.Environment.TickCount % 1000 < 16) // approximately once per second
        {
            Console.WriteLine($"TexturedLit: {entityCount} entities, {shadowCasterCount} shadow casters");
        }
    }
}
