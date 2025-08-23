using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using MoonWorks.Input;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.StellaInvicta
{
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
        public bool UseCameraRelativeRendering { get; set; } = false;
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
            // Note: MoonWorks/HLSL typically uses DX/Vulkan depth range [0,1] for Z.
            float ndcX = (mouse.X / (float)windowWidth) * 2f - 1f;
            float ndcY = 1f - (mouse.Y / (float)windowHeight) * 2f;

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
            // System.Numerics uses row-vector convention for Transform; world -> clip is v * (view * projection).
            // So unprojection uses inverse(view * projection).
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
            Vector3 nearWorld = new Vector3(nearWorld4.X, nearWorld4.Y, nearWorld4.Z) / nearWorld4.W;
            Vector3 farWorld = new Vector3(farWorld4.X, farWorld4.Y, farWorld4.Z) / farWorld4.W;

            // 5) Build ray from near point into the scene.
            // Nudge origin slightly forward to avoid immediate t=0 hits when starting inside/on geometry.
            var rayOrigin = nearWorld;
            var dir = farWorld - nearWorld;
            if (dir.LengthSquared() <= 0f)
            {
                hitResult = default;
                return false;
            }
            var rayDirection = Vector3.Normalize(dir);
            const float EPS = 1e-3f;
            rayOrigin += rayDirection * EPS;

            // 6) Convert ray into the simulation's coordinate space and Raycast.
            // If UseCameraRelativeRendering is enabled we unprojected into camera-relative
            // space, so add the camera absolute position back to get world coordinates.
            Vector3 rayOriginWorld = rayOrigin;
            if (UseCameraRelativeRendering)
            {
                rayOriginWorld = rayOrigin + (Vector3)_camera.Position; // cast Vector3Double -> Vector3
            }

            // If a FloatingOriginManager is present, convert the absolute world origin
            // to the physics-relative origin expected by the simulation.
            Vector3 simRayOrigin = rayOriginWorld;
            if (FloatingOriginManager != null)
            {
                simRayOrigin = FloatingOriginManager.ToRelativePosition(new Vector3Double(rayOriginWorld.X, rayOriginWorld.Y, rayOriginWorld.Z));
            }

            var hitHandler = new ClosestHitHandler();
            _simulation.RayCast(simRayOrigin, rayDirection, 50000f, ref hitHandler);

            // 7) Fill result if hit. Convert hit location/distance back to world coordinates
            // so callers always receive world-space hit data regardless of floating origin.
            if (hitHandler.HasHit)
            {
                // Hit point in simulation-relative coordinates
                Vector3 hitSimPoint = simRayOrigin + rayDirection * hitHandler.T;

                // Convert to absolute/world coordinates if we used a FloatingOriginManager
                Vector3 hitWorldPoint;
                if (FloatingOriginManager != null)
                {
                    var abs = FloatingOriginManager.ToAbsolutePosition(hitSimPoint);
                    hitWorldPoint = new Vector3((float)abs.X, (float)abs.Y, (float)abs.Z);
                }
                else
                {
                    // If no floating origin is used, sim coordinates are world coordinates.
                    hitWorldPoint = hitSimPoint;
                }

                // Distance from camera (expressed in world units). Use camera absolute position.
                var camPos = (Vector3)_camera.Position;
                float distanceFromCamera = Vector3.Distance(camPos, hitWorldPoint);

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
}
