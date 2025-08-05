package ayanamisTower.stellaEcs.ecs

import kotlin.reflect.KClass

/**
 * Manages the storage for a single type of component using a sparse set.
 *
 * A sparse set allows for O(1) access, insertion, and removal of components for any entity.
 * It uses two arrays:
 * - `sparse`: Indexed by the entity's ID. It stores the index into the `dense` array.
 * This can be large and sparsely populated.
 * - `dense`: A tightly packed array that stores the actual component data.
 * - `entities`: A parallel array to `dense` that stores the ecs.Entity ID for each component.
 * This allows us to know which entity owns the component at a given dense index.
 */
class ComponentStorage<T : Any>(
    private val componentType: KClass<T>,
    initialCapacity: Int = 256,
    private val maxEntities: Int = 100_000
) {
    private var dense: Array<Any?> = arrayOfNulls(initialCapacity)
    private var sparse: IntArray = IntArray(maxEntities) { -1 }
    private var entities: Array<Entity?> = arrayOfNulls(initialCapacity)

    var size: Int = 0
        private set

    fun has(entity: Entity): Boolean {
        val sparseIndex = entity.id
        if (sparseIndex !in 0..<maxEntities) return false
        val denseIndex = sparse[sparseIndex]
        return denseIndex != -1 && denseIndex < size && entities[denseIndex] == entity
    }

    /**
     * Sets or replaces a component for a given entity. This is the core of the "copy-and-replace" pattern.
     */
    fun set(entity: Entity, component: T) {
        if (has(entity)) {
            // If the entity already has this component, just replace it in the dense array.
            val denseIndex = sparse[entity.id]
            dense[denseIndex] = component
            return
        }

        if (size == dense.size) {
            val newCapacity = size * 2
            dense = dense.copyOf(newCapacity)
            entities = entities.copyOf(newCapacity)
        }

        val denseIndex = size
        val sparseIndex = entity.id

        dense[denseIndex] = component
        entities[denseIndex] = entity
        sparse[sparseIndex] = denseIndex

        size++
    }

    @Suppress("UNCHECKED_CAST")
    fun get(entity: Entity): T? {
        if (!has(entity)) return null
        val denseIndex = sparse[entity.id]
        return dense[denseIndex] as? T
    }

    fun remove(entity: Entity) {
        if (!has(entity)) return

        val sparseIndexToRemove = entity.id
        val denseIndexToRemove = sparse[sparseIndexToRemove]
        val lastDenseIndex = size - 1
        val lastComponent = dense[lastDenseIndex]
        val lastEntity = entities[lastDenseIndex]!!

        dense[denseIndexToRemove] = lastComponent
        entities[denseIndexToRemove] = lastEntity
        sparse[lastEntity.id] = denseIndexToRemove
        sparse[sparseIndexToRemove] = -1
        dense[lastDenseIndex] = null
        entities[lastDenseIndex] = null

        size--
    }

    fun getEntities(): Iterable<Entity> {
        return entities.take(size).map { it!! }
    }
}
