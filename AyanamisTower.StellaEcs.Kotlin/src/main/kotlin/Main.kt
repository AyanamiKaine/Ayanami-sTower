package ayanamisTower.stellaEcs
import ayanamisTower.stellaEcs.ecs.Entity
import ayanamisTower.stellaEcs.ecs.World
import ayanamisTower.stellaEcs.ecs.set
import java.util.ArrayList
import kotlin.random.Random
import kotlin.system.measureTimeMillis

//TIP To <b>Run</b> code, press <shortcut actionId="Run"/> or
// click the <icon src="AllIcons.Actions.Execute"/> icon in the gutter.

// --- Components for the benchmark ---
data class Position(val x: Float, val y: Float)
data class Velocity(val dx: Float, val dy: Float)
data class Renderable(val spriteId: Int)
data class AIControlled(val state: Int)

fun main() {
    // --- Benchmark Parameters ---
    val numEntities = 100_000
    val updateCycles = 100 // Increased cycles for a more stable average

    // Give some buffer for maxEntities
    val world = World(maxEntities = numEntities + 100)
    val entities = ArrayList<Entity>(numEntities)

    println("--- Kotlin Sparse Set ECS Benchmark ---")
    println("Target entities: $numEntities | System update cycles: $updateCycles")
    println("----------------------------------------")

    // 1. Entity Creation Benchmark
    val creationTime = measureTimeMillis {
        for (i in 0 until numEntities) {
            entities.add(world.createEntity())
        }
    }
    println("1. Entity Creation:       $creationTime ms")

    // 2. Component Addition Benchmark (creating archetypes)
    val addComponentTime = measureTimeMillis {
        for (i in 0 until numEntities) {
            val entity = entities[i]
            // Create different archetypes based on entity index
            when (i % 4) {
                0 -> { // Movable, renderable object
                    world.setComponent(entity, Position(Random.nextFloat(), Random.nextFloat()))
                    world.setComponent(entity, Velocity(Random.nextFloat(), Random.nextFloat()))
                    world.setComponent(entity, Renderable(i))
                }
                1 -> { // Static renderable object
                    world.setComponent(entity, Position(Random.nextFloat(), Random.nextFloat()))
                    world.setComponent(entity, Renderable(i))
                }
                2 -> { // AI-controlled movable object
                    world.setComponent(entity, Position(Random.nextFloat(), Random.nextFloat()))
                    world.setComponent(entity, Velocity(Random.nextFloat(), Random.nextFloat()))
                    world.setComponent(entity, AIControlled(0))
                }
                3 -> { // An object with just a position
                    world.setComponent(entity, Position(Random.nextFloat(), Random.nextFloat()))
                }
            }
        }
    }
    println("2. Component Addition:      $addComponentTime ms")

    // 3. System Simulation (Query + Update) Benchmark
    println("\n3. System Simulation (average time per cycle over $updateCycles cycles):")

    val physicsSystemTime = measureTimeMillis {
        repeat(updateCycles) {
            // This system finds all entities with Position and Velocity and updates them.
            world.query(Position::class, Velocity::class).forEach { entity ->
                val pos = world.getComponent<Position>(entity)!!
                val vel = world.getComponent<Velocity>(entity)!!
                entity.set(world, Position(pos.x + vel.dx * 0.016f, pos.y + vel.dy * 0.016f))
            }
        }
    }
    val avgPhysicsTime = physicsSystemTime / updateCycles.toDouble()
    println("   - Physics System (Pos, Vel):   ${"%.3f".format(avgPhysicsTime)} ms/cycle")

    val renderSystemTime = measureTimeMillis {
        repeat(updateCycles) {
            var count = 0
            // This system finds all entities with Position and Renderable.
            world.query(Position::class, Renderable::class).forEach { _ ->
                // In a real system, you'd use the components to draw something.
                // Here we just iterate to measure query and access speed.
                count++
            }
        }
    }
    val avgRenderTime = renderSystemTime / updateCycles.toDouble()
    println("   - Render System (Pos, Render): ${"%.3f".format(avgRenderTime)} ms/cycle")

    val aiSystemTime = measureTimeMillis {
        repeat(updateCycles) {
            // This system finds all entities with AI and Velocity.
            world.query(AIControlled::class, Velocity::class).forEach { entity ->
                val ai = world.getComponent<AIControlled>(entity)!!
                val vel = world.getComponent<Velocity>(entity)!!
                // Simple AI: change direction
                if (ai.state == 1) {
                   entity.set(world, Velocity(dy = vel.dy, dx = vel.dx * -1))
                }
            }
        }
    }
    val avgAiTime = aiSystemTime / updateCycles.toDouble()
    println("   - AI System (AI, Vel):         ${"%.3f".format(avgAiTime)} ms/cycle")

    // 4. Component Removal Benchmark
    // We will remove the Velocity component from all entities that have it.
    // .toList() is important to not modify the collection while iterating.
    val entitiesWithVelocity = world.query(Velocity::class).toList()
    val removalTime = measureTimeMillis {
        entitiesWithVelocity.forEach { entity ->
            world.removeComponent<Velocity>(entity)
        }
    }
    println("\n4. Component Removal:       $removalTime ms (removed from ${entitiesWithVelocity.size} entities)")
    val remainingVelocities = world.query(Velocity::class).count()
    println("   - Verification: Velocities remaining = $remainingVelocities (should be 0)")

    // 5. Entity Destruction Benchmark
    val destructionTime = measureTimeMillis {
        // .toList() prevents issues with modifying the list while iterating over it.
        entities.toList().forEach { entity ->
            world.destroyEntity(entity)
        }
    }
    println("\n5. Entity Destruction:      $destructionTime ms")

    // Verify all components are gone by checking the largest component storage.
    val remainingEntities = world.query(Position::class).count()
    println("   - Verification: Entities with Position = $remainingEntities (should be 0)")
}

