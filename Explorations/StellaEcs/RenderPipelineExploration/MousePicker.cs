using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using MoonWorks.Input;

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
            // System.Numerics uses row-vector convention for Transform; world -> clip is v * (view * projection).
            // So unprojection uses inverse(view * projection).
            var viewProj = Matrix4x4.Multiply(view, proj);
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
            // Prefer originating at the camera to avoid precision issues at the near plane
            var rayOrigin = _camera.Position;
            var dir = farWorld - rayOrigin;
            if (dir.LengthSquared() <= 0f)
            {
                hitResult = default;
                return false;
            }
            var rayDirection = Vector3.Normalize(dir);
            const float EPS = 1e-3f;
            rayOrigin += rayDirection * EPS;

            // 6) Raycast.
            var hitHandler = new ClosestHitHandler();
            // Use a long ray to account for large scenes
            _simulation.RayCast(rayOrigin, rayDirection, 100000f, ref hitHandler);

            // 7) Fill result if hit.
            if (hitHandler.HasHit)
            {
                hitResult = new PickResult
                {
                    Collidable = hitHandler.HitCollidable,
                    Distance = hitHandler.T,
                    Normal = hitHandler.Normal,
                    HitLocation = rayOrigin + rayDirection * hitHandler.T
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
