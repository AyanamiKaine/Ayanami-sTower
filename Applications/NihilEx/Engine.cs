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

        World.Component<Vector2>("Vector2")
                    .Member<float>("X")
                    .Member<float>("Y");

        World.Component<Vector3>("Vector3")
                    .Member<float>("X")
                    .Member<float>("Y")
                    .Member<float>("Z");


        World.Component<Vector4>("Vector4")
                    .Member<float>("X")
                    .Member<float>("Y")
                    .Member<float>("Z")
                    .Member<float>("W");

        World.Component<Plane>("Plane")
                    .Member<Vector3>("Normal")
                    .Member<float>("D");

        World.Component<Matrix3x2>("Matrix3x2")
                    .Member<float>("M11")
                    .Member<float>("M12")
                    .Member<float>("M21")
                    .Member<float>("M22")
                    .Member<float>("M31")
                    .Member<float>("M32");

        World.Component<Matrix4x4>("Matrix4x4")
            .Member<float>("M11")
            .Member<float>("M12")
            .Member<float>("M13")
            .Member<float>("M14")
            .Member<float>("M21")
            .Member<float>("M22")
            .Member<float>("M23")
            .Member<float>("M24")
            .Member<float>("M31")
            .Member<float>("M32")
            .Member<float>("M33")
            .Member<float>("M34")
            .Member<float>("M41")
            .Member<float>("M42")
            .Member<float>("M43")
            .Member<float>("M44");

        World.Component<Quaternion>("Quaternion")
            .Member<float>("X")
            .Member<float>("Y")
            .Member<float>("Z")
            .Member<float>("W");

        World.Component<Orientation>("Orientation")
            .Member<Quaternion>("Value");

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
