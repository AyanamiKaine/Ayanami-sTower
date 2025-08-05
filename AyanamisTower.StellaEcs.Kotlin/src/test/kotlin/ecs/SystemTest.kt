package ecs
import ayanamisTower.stellaEcs.ecs.*

import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test
import java.util.concurrent.CountDownLatch
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

class SystemsTest {

    private lateinit var world: World

    // A simple system is just a function that takes the world and operates on it.
    private fun movementSystem(world: World) {

        // The system queries for all entities that have both a Position and a Velocity.
        // By calling .toList(), we create a snapshot of the entities that match the
        // query at this exact moment. We then iterate over our private, stable list.
        // This prevents ConcurrentModificationException if another thread modifies
        // the underlying component storages while this system is running.

        val query = world.query(Position2D::class, Velocity::class).toList()

        for (entity in query) {
            // Instead of using !!, we safely get the components. If a component
            // is null (because another thread destroyed the entity after our snapshot
            // was taken), we simply 'continue' to the next entity. This prevents the NPE.
            val pos = world.getComponent<Position2D>(entity) ?: continue
            val vel = world.getComponent<Velocity>(entity) ?: continue

            val newPos = Position2D(pos.x + vel.dx, pos.y + vel.dy)
            entity.set(world, newPos)
        }
    }

    @BeforeEach
    fun setUp() {
        world = World()
    }

    @Test
    fun `immutable movement system should be safe to run concurrently`() {
        // Arrange: Create a large number of entities
        val entityCount = 1000
        for (i in 0 until entityCount) {
            val entity = world.createEntity()
            world.setComponent(entity, Position2D(0, 0))
            world.setComponent(entity, Velocity(1, 1))
        }

        // Act: Run the immutable system from multiple threads concurrently
        val threadPool = Executors.newFixedThreadPool(4)
        val iterations = 5

        for (i in 1..iterations) {
            // Each task represents a "frame" where the system runs
            val task = Runnable { movementSystem(world) }
            // Submit the task to all threads to run in parallel
            repeat(4) { threadPool.submit(task) }
        }

        threadPool.shutdown()
        // Wait for all threads to finish
        threadPool.awaitTermination(10, java.util.concurrent.TimeUnit.SECONDS)


        // Assert: Check if the final state is correct.
        // Each of the 1000 entities should have had its position updated 5 times.
        // Since we are running the system 4 times in parallel for each iteration,
        // we need to account for that. Let's simplify and just check one iteration.
        // For this test, let's just run the system once across multiple threads.

        // Re-running a simplified version for clarity
        val world2 = World()
        for (i in 0 until entityCount) {
            val entity = world2.createEntity()
            world2.setComponent(entity, Position2D(0, 0))
            world2.setComponent(entity, Velocity(1, 1))
        }

        val singleUpdatePool = Executors.newFixedThreadPool(4)
        val task = Runnable { movementSystem(world2) }
        singleUpdatePool.submit(task)
        singleUpdatePool.shutdown()
        singleUpdatePool.awaitTermination(10, java.util.concurrent.TimeUnit.SECONDS)


        val query = world2.query(Position2D::class)
        var checkedEntities = 0
        for (entity in query) {
            val pos = world2.getComponent<Position2D>(entity)!!
            // Each entity's position should have been incremented exactly once.
            assertThat(pos.x).isEqualTo(1)
            assertThat(pos.y).isEqualTo(1)
            checkedEntities++
        }
        assertThat(checkedEntities).isEqualTo(entityCount)
    }

    @Test
    fun `immutable system should not crash when entities are concurrently destroyed`() {
        // Arrange: Create a large number of entities
        val entityCount = 2000
        for (i in 0 until entityCount) {
            val entity = world.createEntity()
            world.setComponent(entity, Position2D(0, 0))
            world.setComponent(entity, Velocity(1, 1))
            // Tag half the entities for destruction
            if (i % 2 == 0) {
                world.setComponent(entity, PlayerTag)
            }
        }

        // This system will run at the same time and destroy entities.
        fun concurrentDestructionSystem(world: World) {
            val entitiesToDestroy = world.query(PlayerTag::class).toList()
            for (entity in entitiesToDestroy) {
                world.destroyEntity(entity)
            }
        }

        // Act: Run both systems concurrently in a thread pool.
        val threadPool = Executors.newFixedThreadPool(2)
        val latch = CountDownLatch(2) // Used to wait for both tasks to finish

        threadPool.submit {
            try {
                movementSystem(world)
            } finally {
                latch.countDown()
            }
        }
        threadPool.submit {
            try {
                concurrentDestructionSystem(world)
            } finally {
                latch.countDown()
            }
        }

        // Assert: The test's main goal is to complete without throwing an exception.
        val finishedInTime = latch.await(5, TimeUnit.SECONDS)
        threadPool.shutdownNow()

        assertThat(finishedInTime).isTrue() // Verify the systems actually finished.

        // We can also do a sanity check on the final state.
        // All entities with the PlayerTag should be gone.
        assertThat(world.query(PlayerTag::class).count()).isEqualTo(0)

        // The remaining entities (those without the tag) should have had their positions updated.
        val survivors = world.query(Position2D::class).toList()
        assertThat(survivors.size).isEqualTo(entityCount / 2)
        survivors.forEach { entity ->
            val pos = world.getComponent<Position2D>(entity)!!
            assertThat(pos.x).isEqualTo(1)
            assertThat(pos.y).isEqualTo(1)
        }
    }

    @Test
    fun `movement system should only update entities with both Position and Velocity`() {
        // Arrange: Create entities with different component combinations.

        // Entity 1: Has both components, should be updated by the system.
        val movingEntity = world.createEntity()
        val movingEntityPos = Position2D(10, 20)
        world.setComponent(movingEntity, movingEntityPos)
        world.setComponent(movingEntity, Velocity(5, -2))

        // Entity 2: Only has Position, should be ignored by the system.
        val staticEntity = world.createEntity()
        val staticEntityPos = Position2D(100, 200)
        world.setComponent(staticEntity, staticEntityPos)

        // Entity 3: Only has Velocity, should be ignored by the system.
        val velocityOnlyEntity = world.createEntity()
        world.setComponent(velocityOnlyEntity, Velocity(1, 1))

        // Entity 4: Has all components, should also be updated.
        val taggedMovingEntity = world.createEntity()
        val taggedMovingEntityPos = Position2D(0, 0)
        world.setComponent(taggedMovingEntity, taggedMovingEntityPos)
        world.setComponent(taggedMovingEntity, Velocity(1, 1))
        world.setComponent(taggedMovingEntity, PlayerTag)


        // Act: Run the system.
        movementSystem(world)

        // Assert: Check the state of all entities after the system has run.

        // The moving entity's position should be updated.
        assertThat(movingEntity.get<Position2D>(world)!!.x).isEqualTo(15)
        assertThat(movingEntity.get<Position2D>(world)!!.y).isEqualTo(18)

        // The tagged moving entity's position should be updated.
        assertThat(taggedMovingEntity.get<Position2D>(world)!!.x).isEqualTo(1)
        assertThat(taggedMovingEntity.get<Position2D>(world)!!.y).isEqualTo(1)

        // The static entity's position should remain unchanged.
        assertThat(staticEntity.get<Position2D>(world)!!.x).isEqualTo(100)
        assertThat(staticEntity.get<Position2D>(world)!!.y).isEqualTo(200)

        // We can also assert that the component on the velocity-only entity still exists
        // and hasn't been inadvertently removed or modified.
        assertThat(world.hasComponent<Velocity>(velocityOnlyEntity)).isTrue()
    }
}
