package ayanamisTower.stellaEcs.ecs

/**
 * Sets or replaces a component on this entity using the specified world.
 */
inline fun <reified T : Any> Entity.set(world: World, component: T) {
    world.setComponent(this, component)
}

/**
 * Gets a component of a specific type from this entity.
 */
inline fun <reified T : Any> Entity.get(world: World): T? {
    return world.getComponent<T>(this)
}

/**
 * Checks if this entity has a component of a specific type.
 */
inline fun <reified T : Any> Entity.has(world: World): Boolean {
    return world.hasComponent<T>(this)
}

/**
 * Removes a component of a specific type from this entity.
 */
inline fun <reified T : Any> Entity.remove(world: World) {
    world.removeComponent<T>(this)
}
