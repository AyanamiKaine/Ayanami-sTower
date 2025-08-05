package ecs
import ayanamisTower.stellaEcs.ecs.*

import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Nested
import org.junit.jupiter.api.Test

/**
 * This test suite demonstrates how to model database-style relationships
 * using the existing ECS framework without any modifications to its core.
 *
 * The fundamental idea is to use components to represent the links between entities.
 */
class RelationshipModelingTest {

    private lateinit var world: World

    @BeforeEach
    fun setUp() {
        world = World()
    }

    //region Component Definitions for Relationships
    // --- Components for One-to-One relationship (Player <-> Soul) ---
    data class Soul(val essence: String)
    data class HasSoul(val soulEntity: Entity) // Attached to Player
    data class BelongsTo(val ownerEntity: Entity) // Attached to Soul

    // --- Components for One-to-Many relationship (Backpack -> Items) ---
    data class Item(val name: String)
    data class IsInBackpack(val backpackEntity: Entity) // Attached to Item

    // --- Components for Many-to-Many relationship (Students <-> Courses) ---
    // Tag components to identify entity types
    data class Student(val name: String)
    data class Course(val title: String)

    // "Relationship" or "Join" components, attached to a dedicated "Enrollment" entity
    data class RelationshipStudent(val student: Entity)
    data class RelationshipCourse(val course: Entity)
    //endregion

    @Nested
    inner class OneToOneRelationshipTest {

        @Test
        fun `should model a one-to-one relationship between a player and their soul`() {
            // Setup: Create a player and a soul entity
            val player = world.createEntity()
            val soul = world.createEntity().apply {
                set(world, Soul("Brave"))
            }

            // Action: Form the relationship by adding components that point to each other.
            player.set(world, HasSoul(soul))
            soul.set(world, BelongsTo(player))

            // Assertion 1: Find the soul from the player
            val playerHasSoul = player.get<HasSoul>(world)
            assertThat(playerHasSoul).isNotNull
            val soulFromPlayer = world.getComponent<Soul>(playerHasSoul!!.soulEntity)
            assertThat(soulFromPlayer?.essence).isEqualTo("Brave")

            // Assertion 2: Find the player from the soul
            val soulBelongsTo = soul.get<BelongsTo>(world)
            assertThat(soulBelongsTo).isNotNull
            assertThat(soulBelongsTo?.ownerEntity).isEqualTo(player)
        }

        @Test
        fun `should break a one-to-one relationship`() {
            // Setup: Create a linked player and soul
            val player = world.createEntity()
            val soul = world.createEntity()
            player.set(world, HasSoul(soul))
            soul.set(world, BelongsTo(player))

            // Action: Break the relationship by removing the components
            player.remove<HasSoul>(world)
            soul.remove<BelongsTo>(world)

            // Assertion: The links are gone
            assertThat(player.has<HasSoul>(world)).isFalse()
            assertThat(soul.has<BelongsTo>(world)).isFalse()
        }
    }

    @Nested
    inner class OneToManyRelationshipTest {

        @Test
        fun `should model a one-to-many relationship for a backpack containing items`() {
            // Setup: Create one backpack and three items
            val backpack = world.createEntity()
            val sword = world.createEntity().apply { set(world, Item("Sword")) }
            val shield = world.createEntity().apply { set(world, Item("Shield")) }
            val potion = world.createEntity().apply { set(world, Item("Potion")) }

            // Another backpack and its item, to ensure we don't mix them up
            val otherBackpack = world.createEntity()
            val scroll = world.createEntity().apply { set(world, Item("Scroll")) }

            // Action: Link items to their respective backpacks.
            // The component is on the "many" side, pointing to the "one".
            sword.set(world, IsInBackpack(backpack))
            shield.set(world, IsInBackpack(backpack))
            potion.set(world, IsInBackpack(backpack))
            scroll.set(world, IsInBackpack(otherBackpack))

            // Assertion: Query for all entities that are items AND are in the first backpack.
            val itemsInBackpack = world.query(Item::class, IsInBackpack::class)
                .filter { entity ->
                    entity.get<IsInBackpack>(world)?.backpackEntity == backpack
                }
                .toList()

            // Verify the correct items were found
            assertThat(itemsInBackpack).hasSize(3)
            assertThat(itemsInBackpack).containsExactlyInAnyOrder(sword, shield, potion)
            assertThat(itemsInBackpack).doesNotContain(scroll)

            // We can also get their data
            val itemNames = itemsInBackpack.map { it.get<Item>(world)?.name }
            assertThat(itemNames).containsExactlyInAnyOrder("Sword", "Shield", "Potion")
        }
    }

    @Nested
    inner class ManyToManyRelationshipTest {

        @Test
        fun `should model a many-to-many relationship for students and courses`() {
            // Setup: Create students and courses
            val alice = world.createEntity().apply { set(world, Student("Alice")) }
            val bob = world.createEntity().apply { set(world, Student("Bob")) }

            val math101 = world.createEntity().apply { set(world, Course("Math 101")) }
            val cs202 = world.createEntity().apply { set(world, Course("CS 202")) }

            // Action: Create "relationship entities" (enrollments) to link students and courses.
            // This is the ECS equivalent of a join table in a relational database.

            // Alice enrolls in Math 101
            world.createEntity().apply {
                set(world, RelationshipStudent(alice))
                set(world, RelationshipCourse(math101))
            }
            // Alice enrolls in CS 202
            world.createEntity().apply {
                set(world, RelationshipStudent(alice))
                set(world, RelationshipCourse(cs202))
            }
            // Bob enrolls in Math 101
            world.createEntity().apply {
                set(world, RelationshipStudent(bob))
                set(world, RelationshipCourse(math101))
            }

            // --- Assertions ---

            // 1. Find all courses for Alice
            val alicesCourses = world.query(RelationshipStudent::class, RelationshipCourse::class)
                .filter { enrollment -> enrollment.get<RelationshipStudent>(world)?.student == alice }
                .mapNotNull { enrollment -> enrollment.get<RelationshipCourse>(world)?.course }
                .toList()

            assertThat(alicesCourses).hasSize(2)
            assertThat(alicesCourses).containsExactlyInAnyOrder(math101, cs202)

            // 2. Find all students in Math 101
            val math101Students = world.query(RelationshipStudent::class, RelationshipCourse::class)
                .filter { enrollment -> enrollment.get<RelationshipCourse>(world)?.course == math101 }
                .mapNotNull { enrollment -> enrollment.get<RelationshipStudent>(world)?.student }
                .toList()

            assertThat(math101Students).hasSize(2)
            assertThat(math101Students).containsExactlyInAnyOrder(alice, bob)

            // 3. Find all students in CS 202
            val cs202Students = world.query(RelationshipStudent::class, RelationshipCourse::class)
                .filter { enrollment -> enrollment.get<RelationshipCourse>(world)?.course == cs202 }
                .mapNotNull { enrollment -> enrollment.get<RelationshipStudent>(world)?.student }
                .toList()

            assertThat(cs202Students).hasSize(1)
            assertThat(cs202Students).containsExactly(alice)
        }
    }
}
