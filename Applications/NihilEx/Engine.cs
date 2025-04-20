using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using Flecs.NET.Bindings;
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

        World.Import<Ecs.Stats>();
        world.Set(default(flecs.EcsRest));
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

        World.System<Orientation, RotationSpeed>("RotateSystem")
                    .Kind(Ecs.OnUpdate) // Use Ecs.OnUpdate directly if Phases["OnUpdate"] is not defined or needed
                    .Iter((Iter it, Field<Orientation> orientation, Field<RotationSpeed> speed) => // Use Field<T> for component access
                    {
                        float dt = it.DeltaTime();
                        for (int i = 0; i < it.Count(); i++)
                        {
                            // Calculate delta rotation around Z-axis
                            float deltaAngleRadians = speed[i].Speed * dt;
                            Quaternion deltaRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, deltaAngleRadians);

                            // Multiply the current orientation by the delta rotation
                            // Quaternion multiplication is non-commutative: newOrientation = oldOrientation * delta
                            orientation[i].Value = Quaternion.Normalize(orientation[i].Value * deltaRotation); // Normalize to prevent drift
                        }
                    });

        World.System<Position2D, ECS.Size, Orientation, RgbaColor>("Render2DBoxSystem") // Query Orientation instead of Angle
            .Kind(Phases["OnRender"])
            .Iter((Iter it, Field<Position2D> pos, Field<ECS.Size> size, Field<Orientation> orientation, Field<RgbaColor> color) => // Use Orientation
            {
                ref var renderer = ref it.World().GetMut<Renderer>();
                if (renderer == null) return;

                for (int i = 0; i < it.Count(); i++)
                {
                    Vector2 center = pos[i].Value;
                    Vector2 dimensions = size[i].Value;
                    Quaternion currentOrientation = orientation[i].Value; // Get the quaternion
                    RgbaColor drawColor = color[i];

                    float hw = dimensions.X / 2.0f;
                    float hh = dimensions.Y / 2.0f;

                    Vector2[] corners =
                    [
                        new(-hw, -hh), new( hw, -hh), new( hw,  hh), new(-hw,  hh)
                    ];

                    Vector2[] transformedCorners = new Vector2[4];
                    for (int j = 0; j < 4; j++)
                    {
                        // Use Vector2.Transform with the quaternion
                        transformedCorners[j] = Vector2.Transform(corners[j], currentOrientation) + center;
                    }

                    renderer.DrawColor = drawColor;

                    // Draw lines (same as before)
                    renderer.RenderLine(transformedCorners[0].X, transformedCorners[0].Y, transformedCorners[1].X, transformedCorners[1].Y);
                    renderer.RenderLine(transformedCorners[1].X, transformedCorners[1].Y, transformedCorners[2].X, transformedCorners[2].Y);
                    renderer.RenderLine(transformedCorners[2].X, transformedCorners[2].Y, transformedCorners[3].X, transformedCorners[3].Y);
                    renderer.RenderLine(transformedCorners[3].X, transformedCorners[3].Y, transformedCorners[0].X, transformedCorners[0].Y);
                }
            });

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

        World.Component<ECS.Size>("Size")
            .Member<Vector2>("Value");

        World.Component<Orientation>("Orientation")
            .Member<Quaternion>("Value");

        World.Component<Rotation3D>("Rotation3D")
            .Member<Vector3>("Value");

        World.Component<Position2D>("Position2D")
            .Member<Vector2>("Value");

        World.Component<Position3D>("Position3D")
            .Member<Vector3>("Value");
        World.Component<RotationSpeed>("RotationSpeed")
            .Member<float>("Speed");

        World.Component<ECS.Point>("Point")
            .Member<Vector2>("Value");

        World.Component<Line>("Line")
            .Member<ECS.Point>("Start")
            .Member<ECS.Point>("End");

        World.Component<RgbaColor>("Color")
            .Member<byte>("R")
            .Member<byte>("G")
            .Member<byte>("B")
            .Member<byte>("A");

        World.Component<ProjectionType>("ProjectionType")
            .Constant("Perspective", ProjectionType.Perspective)
            .Constant("Orthographic", ProjectionType.Orthographic);

        World.Component<Camera>("Camera")
            .Member<Vector3>("Position")
            .Member<Quaternion>("Orientation")
            .Member<Vector3>("WorldUpDirection")
            .Member<ProjectionType>("ProjectionMode")
            .Member<float>("FieldOfViewDegrees")
            .Member<float>("FieldOfViewRadians")
            .Member<float>("AspectRatio")
            .Member<float>("NearPlane")
            .Member<float>("FarPlane");
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
