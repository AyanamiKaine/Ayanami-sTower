using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using MoonWorks.Input;
using AyanamisTower.StellaEcs.HighPrecisionMath;
using AyanamisTower.StellaEcs.StellaInvicta.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics;

/// <summary>
/// A helper class to perform raycasting from the mouse cursor into the BepuPhysics simulation.
/// </summary>
public class MousePicker
{
    private readonly Camera _camera;
    private readonly Simulation _simulation;
    /// <summary>
    /// Optional floating origin manager. If set, ray origins will be converted
    /// from absolute/world coordinates into the simulation's relative coordinates
    /// before calling into the physics <see cref="Simulation"/>.
    /// </summary>
    public FloatingOriginManager? FloatingOriginManager { get; set; }
    /// <summary>
    /// When true, MousePicker will remove the camera translation from the view
    /// matrix used for unprojection so picking matches camera-relative rendering.
    /// </summary>
    public bool UseCameraRelativeRendering { get; set; } = true;
    /// <summary>
    /// Initializes a new instance of the <see cref="MousePicker"/> class.
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="simulation"></param>
    public MousePicker(Camera camera, Simulation simulation)
    {
        _camera = camera;
        _simulation = simulation;
    }

    /// <summary>
    /// Performs a raycast from the camera through the mouse cursor to find the closest physics object.
    /// </summary>
    /// <param name="mouse">The current mouse input state.</param>
    /// <param name="windowWidth">The width of the game window.</param>
    /// <param name="windowHeight">The height of the game window.</param>
    /// <param name="hitResult">The result of the hit, if any.</param>
    /// <returns>True if an object was hit, otherwise false.</returns>
    public bool Pick(Mouse mouse, int windowWidth, int windowHeight, out PickResult hitResult)
    {
        // 1) Mouse position to Normalized Device Coordinates [-1,1] for X/Y.
        float ndcX = ((mouse.X / (float)windowWidth) * 2f) - 1f;
        float ndcY = 1f - ((mouse.Y / (float)windowHeight) * 2f);

        // 2) Near/Far in NDC. Use 0 for near, 1 for far in DX/Vulkan conventions.
        var nearNDC = new Vector4(ndcX, ndcY, 0f, 1f);
        var farNDC = new Vector4(ndcX, ndcY, 1f, 1f);

        // 3) Invert combined (Projection * View) to unproject from NDC to world.
        var proj = _camera.GetProjectionMatrix();
        var view = _camera.GetViewMatrix();
        var viewMat = HighPrecisionConversions.ToMatrix(view);
        var projMat = HighPrecisionConversions.ToMatrix(proj);
        if (UseCameraRelativeRendering)
        {
            viewMat.Translation = Vector3.Zero;
        }
        var viewProj = viewMat * projMat;
        if (!Matrix4x4.Invert(viewProj, out var invViewProj))
        {
            hitResult = default;
            return false;
        }

        // 4) Unproject and perform perspective divide (divide by W).
        Vector4 nearWorld4 = Vector4.Transform(nearNDC, invViewProj);
        Vector4 farWorld4 = Vector4.Transform(farNDC, invViewProj);
        if (nearWorld4.W == 0 || farWorld4.W == 0)
        {
            hitResult = default;
            return false;
        }
        // Because we are using camera-relative rendering, these unprojected points are in
        // CAMERA-RELATIVE space. They are small, single-precision vectors.
        Vector3 nearCamRelative = new Vector3(nearWorld4.X, nearWorld4.Y, nearWorld4.Z) / nearWorld4.W;
        Vector3 farCamRelative = new Vector3(farWorld4.X, farWorld4.Y, farWorld4.Z) / farWorld4.W;

        // 5) Convert the camera-relative points to SIMULATION-RELATIVE points for the raycast.
        // The camera's position is already stored relative to the simulation origin (as a Vector3Double).
        var camPosSim = _camera.Position;

        // By adding the camera-relative offset to the camera's simulation-relative position,
        // we get the final simulation-relative position of the ray's start and end points.
        // We do this with double precision to maintain accuracy.
        var nearSimD = camPosSim + new Vector3Double(nearCamRelative.X, nearCamRelative.Y, nearCamRelative.Z);
        var farSimD = camPosSim + new Vector3Double(farCamRelative.X, farCamRelative.Y, farCamRelative.Z);

        // The physics simulation works with single-precision vectors.
        // We can now safely cast our simulation-relative doubles to floats.
        Vector3 simNear = new Vector3((float)nearSimD.X, (float)nearSimD.Y, (float)nearSimD.Z);
        Vector3 simFar = new Vector3((float)farSimD.X, (float)farSimD.Y, (float)farSimD.Z);

        // 6) Construct and perform the raycast in simulation space.
        var simDir = simFar - simNear;
        if (simDir.LengthSquared() <= 0f)
        {
            hitResult = default;
            return false;
        }
        var simRayDirection = Vector3.Normalize(simDir);
        const float EPS = 1e-3f;
        var simRayOrigin = simNear + (simRayDirection * EPS);

        var hitHandler = new ClosestHitHandler();
        _simulation.RayCast(simRayOrigin, simRayDirection, 50000f, ref hitHandler);

        // 7) If we got a hit, convert the result back to absolute world coordinates for the caller.
        if (hitHandler.HasHit)
        {
            // The hit point is in simulation-relative coordinates.
            Vector3 hitSimPoint = simRayOrigin + (simRayDirection * hitHandler.T);

            // Convert to absolute/world coordinates using the FloatingOriginManager.
            Vector3 hitWorldPoint;
            if (FloatingOriginManager != null)
            {
                var abs = FloatingOriginManager.ToAbsolutePosition(hitSimPoint);
                hitWorldPoint = new Vector3((float)abs.X, (float)abs.Y, (float)abs.Z);
            }
            else
            {
                // If no floating origin, simulation coordinates are world coordinates.
                hitWorldPoint = hitSimPoint;
            }

            // Distance from camera (expressed in world units). Use camera absolute position.
            var camPosAbs = _camera.Position;
            if (FloatingOriginManager != null)
            {
                camPosAbs = FloatingOriginManager.ToAbsolutePosition((Vector3)_camera.Position);
            }
            float distanceFromCamera = Vector3.Distance((Vector3)camPosAbs, hitWorldPoint);

            hitResult = new PickResult
            {
                Collidable = hitHandler.HitCollidable,
                Distance = distanceFromCamera,
                Normal = hitHandler.Normal,
                HitLocation = hitWorldPoint
            };
            return true;
        }

        hitResult = default;
        return false;
    }

    /// <summary>
    /// A struct that implements IRayHitHandler to find the closest hit object.
    /// BepuPhysics uses this callback pattern to avoid memory allocations during queries.
    /// </summary>
    private struct ClosestHitHandler : IRayHitHandler
    {
        public CollidableReference HitCollidable { get; private set; }
        public float T { get; private set; }
        public Vector3 Normal { get; private set; }
        public bool HasHit { get; private set; }

        public ClosestHitHandler()
        {
            T = float.MaxValue;
            HasHit = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            // We can filter hits here. For now, we allow testing against all collidables.
            return true;
        }

#pragma warning disable RCS1242 // Do not pass non-read-only struct by read-only reference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData _, ref float maximumT, float t, Vector3 normal, CollidableReference collidable)
        {
            // This is called for every object the ray intersects.
            // If this hit is closer than the one we've stored, we update our result.
            if (t < T)
            {
                T = t;
                Normal = normal;
                HitCollidable = collidable;
                HasHit = true;
                // By updating maximumT, we tell the simulation to ignore any hits farther than this one.
                // This is a powerful optimization!
                maximumT = t;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

#pragma warning disable RCS1242 // Do not pass non-read-only struct by read-only reference
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRayHit(in RayData _, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            // This is called for every object the ray intersects.
            // If this hit is closer than the one we've stored, we update our result.
            if (t < T)
            {
                T = t;
                Normal = normal;
                HitCollidable = collidable;
                HasHit = true;
                // By updating maximumT, we tell the simulation to ignore any hits farther than this one.
                // This is a powerful optimization!
                maximumT = t;
            }
        }
    }
}

/// <summary>
/// A simple struct to hold the results of a successful pick operation.
/// </summary>
public struct PickResult
{
    /// <summary>
    /// The collidable object that was hit by the ray.
    /// </summary>
    public CollidableReference Collidable;
    /// <summary>
    /// The distance from the camera to the hit location.
    /// </summary>
    public float Distance;
    /// <summary>
    /// The normal vector at the hit location.
    /// </summary>
    public Vector3 Normal;
    /// <summary>
    /// The location of the hit in world space.
    /// </summary>
    public Vector3 HitLocation;
}

