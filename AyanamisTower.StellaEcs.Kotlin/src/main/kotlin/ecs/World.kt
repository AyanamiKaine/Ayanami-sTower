package ayanamisTower.stellaEcs.ecs

import kotlin.reflect.KClass

/**
 * The main class that manages all entities, components, and systems.
 * It acts as the central hub for all ECS operations.
 */
class World(private val maxEntities: Int = 100_000) {
    private var nextEntityId = 0
    private val storages = mutableMapOf<KClass<out Any>, ComponentStorage<*>>()

    fun createEntity(): Entity {
        if (nextEntityId >= maxEntities) {
            throw IllegalStateException("Maximum number of entities ($maxEntities) reached.")
        }
        val entity = Entity(nextEntityId)
        nextEntityId++
        return entity
    }

    fun destroyEntity(entity: Entity) {
        storages.values.forEach { storage ->
            if (storage.has(entity)) {
                storage.remove(entity)
            }
        }
    }

    /**
     * Sets or replaces a component for an entity.
     * This is now the primary method for all state changes.
     */
    inline fun <reified T : Any> setComponent(entity: Entity, component: T) {
        setComponent(entity, component, T::class)
    }

    fun <T : Any> setComponent(entity: Entity, component: T, type: KClass<T>) {
        val storage = getOrCreateStorage(type)
        storage.set(entity, component)
    }

    inline fun <reified T : Any> getComponent(entity: Entity): T? {
        return getComponent(entity, T::class)
    }

    @Suppress("UNCHECKED_CAST")
    fun <T : Any> getComponent(entity: Entity, type: KClass<T>): T? {
        val storage = storages[type] ?: return null
        return (storage as? ComponentStorage<T>)?.get(entity)
    }

    inline fun <reified T : Any> hasComponent(entity: Entity): Boolean {
        return hasComponent(entity, T::class)
    }

    fun <T: Any> hasComponent(entity: Entity, type: KClass<T>): Boolean {
        val storage = storages[type] ?: return false
        return storage.has(entity)
    }

    inline fun <reified T : Any> removeComponent(entity: Entity) {
        removeComponent(entity, T::class)
    }

    fun <T : Any> removeComponent(entity: Entity, type: KClass<T>) {
        storages[type]?.remove(entity)
    }

    fun query(vararg types: KClass<out Any>): Sequence<Entity> {
        if (types.isEmpty()) {
            return emptySequence()
        }

        val relevantStorages = types.map { storages[it] ?: return emptySequence() }
        val smallestStorage = relevantStorages.minByOrNull { it.size }!!
        val otherStorages = relevantStorages - smallestStorage

        return smallestStorage.getEntities().asSequence().filter { entity ->
            otherStorages.all { it.has(entity) }
        }
    }

    @Suppress("UNCHECKED_CAST")
    private fun <T : Any> getOrCreateStorage(type: KClass<T>): ComponentStorage<T> {
        return storages.computeIfAbsent(type) {
            ComponentStorage(type, maxEntities = this.maxEntities)
        } as ComponentStorage<T>
    }
}
