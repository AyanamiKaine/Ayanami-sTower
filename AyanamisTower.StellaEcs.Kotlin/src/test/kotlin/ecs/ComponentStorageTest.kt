package ecs

import ayanamisTower.stellaEcs.ecs.ComponentStorage
import ayanamisTower.stellaEcs.ecs.Entity
import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test

class ComponentStorageTest {

    private lateinit var storage: ComponentStorage<Position2D>
    private val maxEntities = 50

    @BeforeEach
    fun setUp() {
        // Create a fresh storage for each test
        storage = ComponentStorage(Position2D::class, maxEntities = maxEntities)
    }

    @Test
    fun `should add and check for a component`() {
        val entity = Entity(5)
        val component = Position2D(10, 20)

        assertThat(storage.has(entity)).isFalse()
        storage.set(entity, component)
        assertThat(storage.has(entity)).isTrue()
    }

    @Test
    fun `should get a component that was added`() {
        val entity = Entity(10)
        val component = Position2D(100, 200)

        storage.set(entity, component)
        val retrieved = storage.get(entity)

        assertThat(retrieved).isNotNull
        assertThat(retrieved).isEqualTo(component)
        assertThat(retrieved?.x).isEqualTo(100)
    }

    @Test
    fun `get should return null for an entity that does not have the component`() {
        val entityWithComponent = Entity(1)
        val entityWithoutComponent = Entity(2)
        storage.set(entityWithComponent, Position2D(1, 1))

        val retrieved = storage.get(entityWithoutComponent)
        assertThat(retrieved).isNull()
    }

    @Test
    fun `should remove a component`() {
        val entity = Entity(7)
        storage.set(entity, Position2D(0, 0))
        assertThat(storage.has(entity)).isTrue()

        storage.remove(entity)
        assertThat(storage.has(entity)).isFalse()
        assertThat(storage.get(entity)).isNull()
        assertThat(storage.size).isEqualTo(0)
    }

    @Test
    fun `removing a component correctly swaps with the last element`() {
        val entity1 = Entity(1)
        val entity2 = Entity(2)
        val entity3 = Entity(3)
        val component3 = Position2D(3, 3)

        storage.set(entity1, Position2D(1, 1))
        storage.set(entity2, Position2D(2, 2))
        storage.set(entity3, component3)

        assertThat(storage.size).isEqualTo(3)

        // Remove the middle element
        storage.remove(entity2)

        assertThat(storage.size).isEqualTo(2)
        assertThat(storage.has(entity2)).isFalse()
        // The last element (entity3) should have been moved into the removed element's slot
        assertThat(storage.get(entity3)).isEqualTo(component3)
        assertThat(storage.getEntities()).containsExactlyInAnyOrder(entity1, entity3)
    }

    @Test
    fun `should correctly report its size`() {
        assertThat(storage.size).isEqualTo(0)
        storage.set(Entity(1), Position2D(1, 1))
        assertThat(storage.size).isEqualTo(1)
        storage.set(Entity(2), Position2D(2, 2))
        assertThat(storage.size).isEqualTo(2)
        storage.remove(Entity(1))
        assertThat(storage.size).isEqualTo(1)
    }

    @Test
    fun `should return all entities that have the component`() {
        val entity1 = Entity(5)
        val entity2 = Entity(15)
        val entity3 = Entity(25)

        storage.set(entity1, Position2D(1, 1))
        storage.set(entity3, Position2D(3, 3))

        val entities = storage.getEntities()
        assertThat(entities).containsExactlyInAnyOrder(entity1, entity3)
        assertThat(entities).doesNotContain(entity2)
    }

    @Test
    fun `should handle adding a component to an entity that already has one by updating it`() {
        val entity = Entity(8)
        val initialComponent = Position2D(10, 10)
        val updatedComponent = Position2D(99, 99)

        storage.set(entity, initialComponent)
        assertThat(storage.get(entity)?.x).isEqualTo(10)
        assertThat(storage.size).isEqualTo(1)

        storage.set(entity, updatedComponent)
        assertThat(storage.size).isEqualTo(1) // Size should not change
        assertThat(storage.get(entity)?.x).isEqualTo(99)
    }
}
