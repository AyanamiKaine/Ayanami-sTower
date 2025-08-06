package ecs

import ayanamisTower.stellaEcs.ecs.Entity
import ayanamisTower.stellaEcs.ecs.World
import ayanamisTower.stellaEcs.ecs.set
import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.assertThrows

import kotlin.reflect.KClass

/**
 * An annotation for an Entity extension function that specifies
 * the function should only execute if the entity has all the
 * specified components.
 */
@Target(AnnotationTarget.FUNCTION)
@Retention(AnnotationRetention.SOURCE)
annotation class With(vararg val components: KClass<*>)

class ComponentAnnotationTest {

    @With(Position2D::class)
    internal fun Entity.move(world: World, pos2D: Position2D) {
        this.set(world, pos2D)
    }

    @Test
    fun `Automatic has component test when defining extension methods using the with annotation`() {
        val world = World()
        val e = world.createEntity()
        e.set(world, Position2D(0,0))
        e.move(world,Position2D(5,5))
    }
}
