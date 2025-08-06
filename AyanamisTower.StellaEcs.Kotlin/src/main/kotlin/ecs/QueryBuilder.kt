package ayanamisTower.stellaEcs.ecs

import kotlin.reflect.KClass

/**
 * A fluent builder for creating complex ECS queries.
 * Supports AND, NOT, and ANY operations on component types.
 */
class QueryBuilder internal constructor(private val world: World) {
    // These need to be public for inline reified functions to access them
    val withTypes = mutableSetOf<KClass<out Any>>()
    val withoutTypes = mutableSetOf<KClass<out Any>>()
    val anyTypes = mutableSetOf<KClass<out Any>>()

    /**
     * Entities must have ALL of these component types (AND operation).
     */
    fun with(vararg types: KClass<out Any>): QueryBuilder {
        withTypes.addAll(types)
        return this
    }

    /**
     * Entities must have ALL of these component types (AND operation).
     */
    inline fun <reified T : Any> with(): QueryBuilder {
        withTypes.add(T::class)
        return this
    }

    /**
     * Entities must NOT have any of these component types (NOT operation).
     */
    fun without(vararg types: KClass<out Any>): QueryBuilder {
        withoutTypes.addAll(types)
        return this
    }

    /**
     * Entities must NOT have this component type (NOT operation).
     */
    inline fun <reified T : Any> without(): QueryBuilder {
        withoutTypes.add(T::class)
        return this
    }

    /**
     * Entities must have AT LEAST ONE of these component types (OR operation).
     * Can be combined with() for more complex queries.
     */
    fun any(vararg types: KClass<out Any>): QueryBuilder {
        anyTypes.addAll(types)
        return this
    }

    /**
     * Entities must have this component type as one of the ANY options.
     */
    inline fun <reified T : Any> any(): QueryBuilder {
        anyTypes.add(T::class)
        return this
    }

    /**
     * Execute the query and return matching entities.
     */
    fun execute(): Sequence<Entity> {
        return world.executeQuery(withTypes, withoutTypes, anyTypes)
    }

    /**
     * Execute the query and iterate over results with a lambda.
     */
    inline fun forEach(action: (Entity) -> Unit) {
        execute().forEach(action)
    }

    /**
     * Execute the query and return results as a list.
     */
    fun toList(): List<Entity> {
        return execute().toList()
    }

    /**
     * Execute the query and return the first matching entity, or null if none found.
     */
    fun firstOrNull(): Entity? {
        return execute().firstOrNull()
    }

    /**
     * Execute the query and return the count of matching entities.
     */
    fun count(): Int {
        return execute().count()
    }

    /**
     * Execute the query and check if any entities match.
     */
    fun any(): Boolean {
        return execute().any()
    }
}
