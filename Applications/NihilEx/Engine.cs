using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using Flecs.NET.Core;

namespace AyanamisTower.NihilEx;

/// <summary>
/// Represents the main engine class responsible for managing the ECS world and its systems.
/// </summary>
public class Engine
{
    /// <summary>
    /// Stores the defined phases for the engine's execution pipeline.
    /// </summary>
    public Dictionary<string, Entity> Phases = [];
    /// <summary>
    /// Gets the Flecs world associated with this engine instance.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class with the specified Flecs world.
    /// </summary>
    /// <param name="world">The Flecs world to associate with this engine.</param>
    public Engine(World world)
    {
        World = world;
        InitComponents();
        InitDefaultPhases();
        InitDefaultSystems();
    }

    private void InitDefaultPhases()
    {
        var preRender = World
            .Entity("PreRender")
            .Add(Ecs.Phase)
            .DependsOn(Ecs.OnUpdate);
        Phases.Add("PreRender", preRender);

        var onRender = World
            .Entity("OnRender")
            .Add(Ecs.Phase)
            .DependsOn(preRender);

        Phases.Add("OnRender", onRender);

        var postRender = World
            .Entity("PostRender")
            .Add(Ecs.Phase)
            .DependsOn(onRender);

        Phases.Add("PostRender", postRender);
    }

    private void InitDefaultSystems()
    {
        InitRenderSystems();
    }

    private void InitComponents()
    {
        World.Component<Vector3>("Vector3")
                    .Member<float>("X")
                    .Member<float>("Y")
                    .Member<float>("Z");

        World.Component<Rotation3D>("Rotation3D")
            .Member<Vector3>("Value");
        World.Component<Position3D>("Position3D")
            .Member<Vector3>("Value");
        World.Component<RotationSpeed>("RotationSpeed")
            .Member<float>("Speed");

        World.Component<Color>("Color")
            .Member<byte>("R")
            .Member<byte>("G")
            .Member<byte>("B")
            .Member<byte>("A");
    }

    private void InitRenderSystems()
    {
        World.System<Renderer>()
            .Kind(Phases["PreRender"])
            .Each((Entity _, ref Renderer renderer) =>
            {
                renderer.Clear();
            });

        World.System<Renderer>()
            .Kind(Phases["PostRender"])
            .Each((Entity _, ref Renderer renderer) =>
            {
                renderer.Present();
            });
    }
}
