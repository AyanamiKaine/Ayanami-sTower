using System;
using System.Collections.Concurrent;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace AyanamisTower.StellaEcs.StellaInvicta.Physics
{
    /// <summary>
    /// Thread-safe registry mapping collidable handles to collision layer data (category/mask).
    /// Physics code should update this registry when creating/removing bodies/statics and when an entity's CollisionLayer changes.
    /// </summary>
    public sealed class CollisionLayerRegistry
    {
        private readonly ConcurrentDictionary<(CollidableMobility mobility, int handle), (uint category, uint mask)> _map = new();

        /// <summary>
        /// Register a dynamic/body handle with its collision category and mask.
        /// </summary>
        /// <param name="handle">Body handle value.</param>
        /// <param name="category">Category bitmask this body belongs to.</param>
        /// <param name="mask">Mask of categories this body collides with.</param>
        public void RegisterBodyHandle(int handle, uint category, uint mask) => _map[(CollidableMobility.Dynamic, handle)] = (category, mask);

        /// <summary>
        /// Register a static handle with its collision category and mask.
        /// </summary>
        /// <param name="handle">Static handle value.</param>
        /// <param name="category">Category bitmask this static belongs to.</param>
        /// <param name="mask">Mask of categories this static collides with.</param>
        public void RegisterStaticHandle(int handle, uint category, uint mask) => _map[(CollidableMobility.Static, handle)] = (category, mask);

        /// <summary>
        /// Unregister a dynamic/body handle when it is removed from the simulation.
        /// </summary>
        /// <param name="handle">Body handle value.</param>
        public void UnregisterBodyHandle(int handle) => _map.TryRemove((CollidableMobility.Dynamic, handle), out _);

        /// <summary>
        /// Unregister a static handle when it is removed from the simulation.
        /// </summary>
        /// <param name="handle">Static handle value.</param>
        public void UnregisterStaticHandle(int handle) => _map.TryRemove((CollidableMobility.Static, handle), out _);

        /// <summary>
        /// Attempts to lookup the collision category and mask for the provided collidable reference.
        /// </summary>
        /// <param name="cref">Collidable reference provided by Bepu.</param>
        /// <param name="category">Out category bitmask if found.</param>
        /// <param name="mask">Out mask bitmask if found.</param>
        /// <returns>True if a layer mapping exists for the collidable reference.</returns>
        public bool TryGetLayer(CollidableReference cref, out uint category, out uint mask)
        {
            category = 0; mask = 0;
            int handle = cref.Mobility == CollidableMobility.Static ? cref.StaticHandle.Value : cref.BodyHandle.Value;
            if (handle < 0) return false;
            var key = (cref.Mobility, handle);
            if (_map.TryGetValue(key, out var v))
            {
                category = v.category; mask = v.mask; return true;
            }
            return false;
        }

        /// <summary>
        /// Register a handle with an explicit mobility. Use this when you know the mobility (Kinematic/Dynamic/Static).
        /// </summary>
        public void RegisterHandle(CollidableMobility mobility, int handle, uint category, uint mask) => _map[(mobility, handle)] = (category, mask);

        /// <summary>
        /// Unregister a handle with an explicit mobility.
        /// </summary>
        public void UnregisterHandle(CollidableMobility mobility, int handle) => _map.TryRemove((mobility, handle), out _);
    }
}
