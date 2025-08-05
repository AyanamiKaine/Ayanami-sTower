package ecs
import ayanamisTower.stellaEcs.ecs.*

import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.assertThrows

class WorldTest {

    private lateinit var world: World

    @BeforeEach
    fun setUp() {
        // A new world is created for each test to ensure isolation
        world = World(maxEntities = 100)
    }

    @Test
    fun `createEntity should return a new, unique entity`() {
        val entity1 = world.createEntity()
        val entity2 = world.createEntity()

        assertThat(entity1).isNotNull
        assertThat(entity2).isNotNull
        assertThat(entity1.id).isNotEqualTo(entity2.id)
    }

    @Test
    fun `createEntity should throw IllegalStateException when max entities is reached`() {
        val smallWorld = World(maxEntities = 2)
        smallWorld.createEntity()
        smallWorld.createEntity()

        val exception = assertThrows<IllegalStateException> {
            smallWorld.createEntity()
        }
        assertThat(exception.message).isEqualTo("Maximum number of entities (2) reached.")
    }

    @Test
    fun `setComponent and getComponent should add and retrieve a component`() {
        val entity = world.createEntity()
        val position = Position2D(10, 20)

        world.setComponent(entity, position)
        val retrieved = world.getComponent<Position2D>(entity)

        assertThat(retrieved).isNotNull
        assertThat(retrieved).isEqualTo(position)
    }

    @Test
    fun `getComponent should return null for an entity without the component`() {
        val entity = world.createEntity()
        // No component is set on the entity
        val retrieved = world.getComponent<Position2D>(entity)
        assertThat(retrieved).isNull()
    }

    @Test
    fun `getComponent should return null for a component type that has no storage`() {
        val entity = world.createEntity()
        // The world has no storage for Velocity components yet
        val retrieved = world.getComponent<Velocity>(entity)
        assertThat(retrieved).isNull()
    }

    @Test
    fun `hasComponent should return true if entity has the component`() {
        val entity = world.createEntity()
        world.setComponent(entity, Position2D(1, 1))

        assertThat(world.hasComponent<Position2D>(entity)).isTrue()
    }

    @Test
    fun `hasComponent should return false if entity does not have the component`() {
        val entityWith = world.createEntity()
        val entityWithout = world.createEntity()
        world.setComponent(entityWith, Position2D(1, 1))

        assertThat(world.hasComponent<Position2D>(entityWithout)).isFalse()
    }

    @Test
    fun `removeComponent should remove a component from an entity`() {
        val entity = world.createEntity()
        world.setComponent(entity, Position2D(5, 5))
        assertThat(world.hasComponent<Position2D>(entity)).isTrue()

        world.removeComponent<Position2D>(entity)
        assertThat(world.hasComponent<Position2D>(entity)).isFalse()
        assertThat(world.getComponent<Position2D>(entity)).isNull()
    }

    @Test
    fun `destroyEntity should remove all components associated with the entity`() {
        val entity = world.createEntity()
        world.setComponent(entity, Position2D(1, 2))
        world.setComponent(entity, Velocity(3, 4))
        world.setComponent(entity, PlayerTag)

        assertThat(world.hasComponent<Position2D>(entity)).isTrue()
        assertThat(world.hasComponent<Velocity>(entity)).isTrue()
        assertThat(world.hasComponent<PlayerTag>(entity)).isTrue()

        world.destroyEntity(entity)

        assertThat(world.hasComponent<Position2D>(entity)).isFalse()
        assertThat(world.hasComponent<Velocity>(entity)).isFalse()
        assertThat(world.hasComponent<PlayerTag>(entity)).isFalse()
    }

    @Test
    fun `query should return entities with a single component type`() {
        val entity1 = world.createEntity().apply { world.setComponent(this, Position2D(1, 1)) }
        world.createEntity().apply { world.setComponent(this, Velocity(1, 1)) } // Should not be in result
        val entity3 = world.createEntity().apply { world.setComponent(this, Position2D(2, 2)) }

        val result = world.query(Position2D::class).toList()

        assertThat(result).containsExactlyInAnyOrder(entity1, entity3)
    }

    @Test
    fun `query should return entities with multiple component types`() {
        val entity1 = world.createEntity().apply {
            world.setComponent(this, Position2D(1, 1))
            world.setComponent(this, Velocity(10, 10))
            world.setComponent(this, PlayerTag)
        }
        val entity2 = world.createEntity().apply { // Missing Velocity
            world.setComponent(this, Position2D(2, 2))
            world.setComponent(this, PlayerTag)
        }
        val entity3 = world.createEntity().apply {
            world.setComponent(this, Position2D(3, 3))
            world.setComponent(this, Velocity(30, 30))
            world.setComponent(this, PlayerTag)
        }
        val entity4 = world.createEntity().apply { // Missing PlayerTag
            world.setComponent(this, Position2D(4, 4))
            world.setComponent(this, Velocity(40, 40))
        }

        val result = world.query(Position2D::class, Velocity::class, PlayerTag::class).toList()

        assertThat(result).containsExactlyInAnyOrder(entity1, entity3)
    }

    @Test
    fun `query should return an empty sequence if one component type has no entities`() {
        world.createEntity().apply { world.setComponent(this, Position2D(1, 1)) }
        // No entities have a Velocity component

        val result = world.query(Position2D::class, Velocity::class).toList()

        assertThat(result).isEmpty()
    }

    @Test
    fun `query should return an empty sequence if no types are provided`() {
        val result = world.query().toList()
        assertThat(result).isEmpty()
    }

    @Test
    fun `query should reflect changes after component removal`() {
        val entity1 = world.createEntity().apply {
            world.setComponent(this, Position2D(1, 1))
            world.setComponent(this, Velocity(1, 1))
        }
        val entity2 = world.createEntity().apply {
            world.setComponent(this, Position2D(2, 2))
            world.setComponent(this, Velocity(2, 2))
        }

        assertThat(world.query(Position2D::class, Velocity::class).toList()).hasSize(2)

        // Remove a component from one entity
        world.removeComponent<Velocity>(entity1)

        val result = world.query(Position2D::class, Velocity::class).toList()
        assertThat(result).containsExactly(entity2)
    }
}
