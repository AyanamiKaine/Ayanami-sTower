using System.Drawing;
using System.Numerics;
using AyanamisTower.NihilEx.ECS;
using AyanamisTower.NihilEx.SDLWrapper;
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

        World.System<Orientation, RotationSpeed2D, DeltaTime>("RotateSystem")
                    .Kind(Ecs.OnUpdate) // Use Ecs.OnUpdate directly if Phases["OnUpdate"] is not defined or needed
                    .TermAt(2).Singleton()
                    .Each((ref Orientation orientation, ref RotationSpeed2D speed, ref DeltaTime dt) => // Use Field<T> for component access
                    {
                        float deltaAngleRadians = speed.Speed * dt.DeltaSeconds;
                        Quaternion deltaRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, deltaAngleRadians);

                        // Multiply the current orientation by the delta rotation
                        // Quaternion multiplication is non-commutative: newOrientation = oldOrientation * delta
                        orientation.Value = Quaternion.Normalize(orientation.Value * deltaRotation); // Normalize to prevent drift
                    });

        World.System<Position2D, Size2D, Orientation, RgbaColor, Renderer>("Render2DBoxSystem") // Query Orientation instead of Angle
            .Kind(Phases["OnRender"])
            .TermAt(4).Singleton()
            .Each((ref Position2D pos, ref Size2D size, ref Orientation orientation, ref RgbaColor color, ref Renderer renderer) => // Use Orientation
            {
                if (renderer == null) return;
                Vector2 center = pos.Value;
                Vector2 dimensions = size.Value;
                Quaternion currentOrientation = orientation.Value; // Get the quaternion
                renderer.DrawColor = color;

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


                // Draw lines (same as before)
                renderer.DrawLine(transformedCorners[0].X, transformedCorners[0].Y, transformedCorners[1].X, transformedCorners[1].Y);
                renderer.DrawLine(transformedCorners[1].X, transformedCorners[1].Y, transformedCorners[2].X, transformedCorners[2].Y);
                renderer.DrawLine(transformedCorners[2].X, transformedCorners[2].Y, transformedCorners[3].X, transformedCorners[3].Y);
                renderer.DrawLine(transformedCorners[3].X, transformedCorners[3].Y, transformedCorners[0].X, transformedCorners[0].Y);

            });

        World.System<Orientation, RotationSpeed3D, DeltaTime>("RotateCubeSystem")
            .Kind(Ecs.OnUpdate)
            .TermAt(2).Singleton() // DeltaTime is a singleton
            .Each((ref Orientation orientation, ref RotationSpeed3D speed, ref DeltaTime dt) =>
            {
                // Create rotations for each axis based on speed and delta time
                Quaternion deltaX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, speed.SpeedRadPerSec.X * dt.DeltaSeconds);
                Quaternion deltaY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, speed.SpeedRadPerSec.Y * dt.DeltaSeconds);
                Quaternion deltaZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, speed.SpeedRadPerSec.Z * dt.DeltaSeconds);

                // Combine the deltas and apply to the current orientation
                // Order matters: ZYX is a common convention
                orientation.Value = Quaternion.Normalize(orientation.Value * deltaZ * deltaY * deltaX);
            });

        World.System<ProjectedMesh, MeshGeometry, Orientation, Position3D, Camera, Window>("ProjectCustomMeshSystem")
            .Kind(Phases["PreRender"])
            .TermAt(4).Singleton() // Camera is singleton
            .TermAt(5).Singleton() // Window is singleton
            .Each((ref ProjectedMesh projected, ref MeshGeometry geometry, ref Orientation orientation, ref Position3D position, ref Camera camera, ref Window windowSize) =>
            {
                // Ensure projected vertex array is allocated and sized correctly
                if (projected.ProjectedVertices == null || projected.ProjectedVertices.Length != geometry.BaseVertices.Length)
                {
                    projected.ProjectedVertices = new Vector2[geometry.BaseVertices.Length];
                }

                // 1. Calculate Model Matrix (World Transform)
                Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(orientation.Value);
                Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position.Value);
                Matrix4x4 modelMatrix = rotationMatrix * translationMatrix; // Scale could be added here too

                // 2. Get View-Projection Matrix from Camera
                // Ensure camera's aspect ratio matches window size
                // NOTE: This check should ideally happen when the window resizes, not every frame.
                // We'll add logic in App.OnEvent later.
                // For now, we assume the Camera singleton is updated elsewhere or is correct.
                camera.AspectRatio = (float)windowSize.Size.X / windowSize.Size.Y;
                camera.EnsureMatricesUpdated(); // Make sure ViewProjection is current

                Matrix4x4 viewMatrix = camera.ViewMatrix;             // Get for logging
                Matrix4x4 projectionMatrix = camera.ProjectionMatrix; // Get for logging

                // 3. Calculate Model-View-Projection (MVP) Matrix *directly*
                //    Perform the multiplication step-by-step
                Matrix4x4 modelViewMatrix = Matrix4x4.Multiply(modelMatrix, viewMatrix);
                Matrix4x4 mvpMatrix = Matrix4x4.Multiply(modelViewMatrix, projectionMatrix); // MVP = (Model * View) * Projection


                // 4. Transform and Project Vertices
                float screenCenterX = windowSize.Size.X / 2.0f;
                float screenCenterY = windowSize.Size.Y / 2.0f;

                for (int i = 0; i < geometry.BaseVertices.Length; i++)
                {
                    // Transform vertex by MVP matrix
                    Vector4 clipSpaceVertex = Vector4.Transform(geometry.BaseVertices[i], mvpMatrix);

                    // Perform perspective divide (Normalize Device Coordinates - NDC)
                    // Check for W <= 0 to avoid division by zero/negative (points behind camera)
                    if (clipSpaceVertex.W <= 0.0001f) // Small epsilon
                    {
                        // Handle points behind the camera (e.g., clamp, discard, project differently)
                        // For simplicity, we can just place them far off-screen.
                        projected.ProjectedVertices[i] = new Vector2(float.MinValue, float.MinValue);
                        continue;
                    }

                    Vector3 ndcVertex = new Vector3(clipSpaceVertex.X, clipSpaceVertex.Y, clipSpaceVertex.Z) / clipSpaceVertex.W;

                    // 5. Viewport Transform (NDC to Screen Coordinates)
                    // NDC range is typically [-1, 1] for X and Y (OpenGL convention, adjust if different)
                    float screenX = (ndcVertex.X + 1.0f) / 2.0f * windowSize.Size.X;
                    // Y is often inverted (NDC +1 is top, Screen +Y is bottom)
                    float screenY = (1.0f - ndcVertex.Y) / 2.0f * windowSize.Size.Y;

                    projected.ProjectedVertices[i] = new Vector2(screenX, screenY);
                }
            });

        World.System<Position3D, Size3D, Orientation, RgbaColor, Camera, Window, Renderer>("RenderBoxWireframeSystem")
            .With<Box>() // <--- Query for entities with the Box tag!
            .Kind(Phases["OnRender"])
            .TermAt(4).Singleton() // Camera
            .TermAt(5).Singleton() // WindowSize
            .TermAt(6).Singleton() // Renderer
            .Each((ref Position3D pos, ref Size3D size, ref Orientation orientation, ref RgbaColor color, ref Camera camera, ref Window window, ref Renderer renderer) =>
            {
                if (renderer == null) return;

                // --- Define Local Box Vertices (derived from Size3D) ---
                float hw = size.Value.X / 2.0f; // Half-width
                float hh = size.Value.Y / 2.0f; // Half-height
                float hd = size.Value.Z / 2.0f; // Half-depth
                Vector3[] localVertices =
                [
                    new(-hw, -hh, -hd), new( hw, -hh, -hd), new( hw,  hh, -hd), new(-hw,  hh, -hd), // Back face indices 0-3
            new(-hw, -hh,  hd), new( hw, -hh,  hd), new( hw,  hh,  hd), new(-hw,  hh,  hd)  // Front face indices 4-7
                ];

                // --- Calculate Model, View, Projection, MVP ---
                Matrix4x4 modelMatrix = Matrix4x4.CreateFromQuaternion(orientation.Value) * Matrix4x4.CreateTranslation(pos.Value);
                camera.EnsureMatricesUpdated();
                Matrix4x4 viewMatrix = camera.ViewMatrix;
                Matrix4x4 projectionMatrix = camera.ProjectionMatrix;
                Matrix4x4 mvpMatrix = modelMatrix * viewMatrix * projectionMatrix;

                // --- Transform, Clip Check, Project, Viewport Transform (8 vertices) ---
                Vector2[] screenVertices = new Vector2[8];
                bool[] vertexValid = new bool[8]; // Track visibility

                for (int i = 0; i < 8; i++)
                {
                    Vector4 clipSpaceVertex = Vector4.Transform(localVertices[i], mvpMatrix);
                    if (clipSpaceVertex.W > 0.0001f) // Check W for clipping
                    {
                        vertexValid[i] = true;
                        Vector3 ndcVertex = new Vector3(clipSpaceVertex.X, clipSpaceVertex.Y, clipSpaceVertex.Z) / clipSpaceVertex.W;
                        screenVertices[i] = new Vector2(
                            (ndcVertex.X + 1.0f) / 2.0f * window.Size.X,
                            (1.0f - ndcVertex.Y) / 2.0f * window.Size.Y // Flip Y
                        );
                    }
                    else
                    {
                        vertexValid[i] = false;
                    }
                }

                // --- Draw Box Edges (Implicitly known for a box) ---
                renderer.DrawColor = color;
                int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };

                for (int i = 0; i < 12; i++)
                {
                    int i1 = edges[i, 0];
                    int i2 = edges[i, 1];
                    // Draw line only if both endpoints are valid (in front of near plane)
                    if (vertexValid[i1] && vertexValid[i2])
                    {
                        renderer.DrawLine(screenVertices[i1].X, screenVertices[i1].Y, screenVertices[i2].X, screenVertices[i2].Y);
                    }
                }
            });

        World.System<ProjectedMesh, MeshGeometry, RgbaColor, Renderer>("RenderCustomMeshWireframeSystem")
            .Kind(Phases["OnRender"])
            .TermAt(3).Singleton() // Renderer is singleton
            .Each((ref ProjectedMesh projected, ref MeshGeometry geometry, ref RgbaColor color, ref Renderer renderer) =>
            {
                // Check if projection results are valid
                if (projected.ProjectedVertices == null || projected.ProjectedVertices.Length == 0) return;

                renderer.DrawColor = color; // Set line color

                foreach (var edge in geometry.Edges)
                {
                    // Basic bounds check
                    if (edge.Index1 >= 0 && edge.Index1 < projected.ProjectedVertices.Length &&
                        edge.Index2 >= 0 && edge.Index2 < projected.ProjectedVertices.Length)
                    {
                        Vector2 p1 = projected.ProjectedVertices[edge.Index1];
                        Vector2 p2 = projected.ProjectedVertices[edge.Index2];

                        // Avoid drawing lines with off-screen points if we marked them earlier
                        if (p1.X > float.MinValue && p1.Y > float.MinValue && p2.X > float.MinValue && p2.Y > float.MinValue)
                        {
                            renderer.DrawLine(p1.X, p1.Y, p2.X, p2.Y);
                        }
                    }
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

        World.Component<Size2D>("Size2D")
            .Member<Vector2>("Value");

        World.Component<Size3D>("Size3D")
            .Member<Vector3>("Value");

        World.Component<Orientation>("Orientation")
            .Member<Quaternion>("Value");

        World.Component<Rotation3D>("Rotation3D")
            .Member<Vector3>("Value");

        World.Component<Position2D>("Position2D")
            .Member<Vector2>("Value");

        World.Component<Position3D>("Position3D")
            .Member<Vector3>("Value");

        World.Component<RotationSpeed2D>("RotationSpeed2D")
            .Member<float>("Speed");

        World.Component<RotationSpeed3D>("RotationSpeed3D")
            .Member<Vector3>("SpeedRadPerSec");

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
