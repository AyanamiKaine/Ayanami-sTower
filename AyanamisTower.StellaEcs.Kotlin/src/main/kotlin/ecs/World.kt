package ayanamisTower.stellaEcs.ecs

import kotlin.reflect.KClass

/**
 * The main class that manages all entities, components, and systems.
 * It acts as the central hub for all ECS operations.
 */
class World(private val maxEntities: Int = 100_000) {
    private var nextEntityId = 0
    private val storages = mutableMapOf<KClass<out Any>, ComponentStorage<*>>()
    private val freeEntities = ArrayDeque<Entity>()

    fun createEntity(): Entity {
        return if (freeEntities.isNotEmpty()) {
            freeEntities.removeFirst()
        } else {
            if (nextEntityId >= maxEntities) {
                throw IllegalStateException("Maximum number of entities ($maxEntities) reached.")
            }
            val entity = Entity(nextEntityId)
            nextEntityId++
            entity
        }
    }

    fun destroyEntity(entity: Entity) {
        storages.values.forEach { storage ->
            if (storage.has(entity)) {
                storage.remove(entity)
            }
        }
        freeEntities.addLast(entity)
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

    /**
     * Starts a new fluent query.
     * @return A QueryBuilder instance linked to this world.
     */
    fun query(): QueryBuilder {
        return QueryBuilder(this)
    }

    /**
     * The internal query execution engine called by the QueryBuilder.
     * This logic finds the most optimal way to iterate and filter entities.
     */
    internal fun executeQuery(
        withTypes: Set<KClass<out Any>>,
        withoutTypes: Set<KClass<out Any>>,
        anyTypes: Set<KClass<out Any>>
    ): Sequence<Entity> {
        // A query must have at least one positive condition (with or any) to be valid.
        if (withTypes.isEmpty() && anyTypes.isEmpty()) {
            return emptySequence()
        }

        // Combine 'with' and 'any' to find our base iteration set.
        // We want to iterate over the smallest possible group of entities.
        val baseCandidateTypes = withTypes + anyTypes

        // Get all storages for the base types. If any of them are empty, the query can't succeed.
        val candidateStorages = baseCandidateTypes.map { storages[it] ?: return emptySequence() }

        // The most important optimization: find the smallest storage to iterate over.
        val smallestStorage = candidateStorages.minByOrNull { it.size }!!

        // Get the storages for the other filter conditions.
        val withStorages = withTypes.mapNotNull { storages[it] }
        val withoutStorages = withoutTypes.mapNotNull { storages[it] }
        val anyStorages = anyTypes.mapNotNull { storages[it] }

        // Start with the entities from the smallest storage and apply filters.
        return smallestStorage.getEntities().asSequence().filter { entity ->
            // 1. 'WITH' check: The entity must have ALL components from the 'with' list.
            val hasAllWith = withStorages.all { it.has(entity) }
            if (!hasAllWith) return@filter false

            // 2. 'WITHOUT' check: The entity must have NONE of the components from the 'without' list.
            val hasAnyWithout = withoutStorages.any { it.has(entity) }
            if (hasAnyWithout) return@filter false

            // 3. 'ANY' check: If the 'any' list is not empty, the entity must have AT LEAST ONE component from it.
            if (anyStorages.isNotEmpty()) {
                val hasAtLeastOneAny = anyStorages.any { it.has(entity) }
                if (!hasAtLeastOneAny) return@filter false
            }

            // If all checks pass, the entity is a match!
            true
        }
    }

    @Suppress("UNCHECKED_CAST")
    private fun <T : Any> getOrCreateStorage(type: KClass<T>): ComponentStorage<T> {
        return storages.computeIfAbsent(type) {
            ComponentStorage(type, maxEntities = this.maxEntities)
        } as ComponentStorage<T>
    }
}
