package ecs
import ayanamisTower.stellaEcs.ecs.*
import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test

// Re-using the same test components


class EntityExtensionsTest {

    private lateinit var world: World
    private var entity: Entity = Entity(-1)

    @BeforeEach
    fun setUp() {
        world = World()
        entity = world.createEntity()
    }

    @Test
    fun `set extension should add a component to the entity`() {
        val position = Position2D(10, 20)

        // Use the extension function
        entity.set(world, position)

        assertThat(world.getComponent<Position2D>(entity)).isEqualTo(position)
    }

    @Test
    fun `get extension should retrieve a component from the entity`() {
        val position = Position2D(10, 20)
        world.setComponent(entity, position)

        // Use the extension function
        val retrieved = entity.get<Position2D>(world)

        assertThat(retrieved).isNotNull
        assertThat(retrieved).isEqualTo(position)
    }

    @Test
    fun `get extension should return null when component is not present`() {
        // No component is set
        val retrieved = entity.get<Position2D>(world)
        assertThat(retrieved).isNull()
    }

    @Test
    fun `has extension should return true when component is present`() {
        entity.set(world, PlayerTag)

        // Use the extension function
        assertThat(entity.has<PlayerTag>(world)).isTrue()
    }

    @Test
    fun `has extension should return false when component is not present`() {
        // No component is set
        assertThat(entity.has<PlayerTag>(world)).isFalse()
    }

    @Test
    fun `remove extension should remove a component from the entity`() {
        val position = Position2D(5, 5)
        entity.set(world, position)
        assertThat(entity.has<Position2D>(world)).isTrue()

        // Use the extension function
        entity.remove<Position2D>(world)

        assertThat(entity.has<Position2D>(world)).isFalse()
    }
}
