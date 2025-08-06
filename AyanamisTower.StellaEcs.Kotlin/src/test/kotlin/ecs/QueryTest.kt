package ecs

import ayanamisTower.stellaEcs.ecs.*
import org.assertj.core.api.Assertions.assertThat
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.DisplayName
import org.junit.jupiter.api.Nested
import org.junit.jupiter.api.Test

@DisplayName("World QueryBuilder Tests")
class QueryTest {

    private lateinit var world: World

    // --- Test Entities ---
    private var player: Entity = Entity(-1)
    private var basicEnemy: Entity = Entity(-1)
    private var hiddenEnemy: Entity = Entity(-1)
    private var invisibleWall: Entity = Entity(-1)
    private var projectile: Entity = Entity(-1)
    private var staticScenery: Entity = Entity(-1)

    @BeforeEach
    fun setUp() {
        world = World()

        // Create a diverse set of entities to test against.
        player = world.createEntity() // Has: Position, Velocity, PlayerTag, Renderable
        player.set(world, Position2D(0, 0))
        player.set(world, Velocity(1, 1))
        player.set(world, PlayerTag)
        player.set(world, Renderable)

        basicEnemy = world.createEntity() // Has: Position, EnemyTag, Renderable
        basicEnemy.set(world, Position2D(10, 10))
        basicEnemy.set(world, EnemyTag)
        basicEnemy.set(world, Renderable)

        hiddenEnemy = world.createEntity() // Has: Position, EnemyTag, Renderable, Hidden
        hiddenEnemy.set(world, Position2D(20, 20))
        hiddenEnemy.set(world, EnemyTag)
        hiddenEnemy.set(world, Renderable)
        hiddenEnemy.set(world, Hidden)

        invisibleWall = world.createEntity() // Has: Position, Hidden
        invisibleWall.set(world, Position2D(30, 30))
        invisibleWall.set(world, Hidden)

        projectile = world.createEntity() // Has: Velocity only
        projectile.set(world, Velocity(100, 100))

        staticScenery = world.createEntity() // Has: Position, Renderable
        staticScenery.set(world, Position2D(40, 40))
        staticScenery.set(world, Renderable)
    }

    @Nested
    @DisplayName("'with' clause (AND logic)")
    inner class WithClause {
        @Test
        fun `with a single component should return all entities having it`() {
            val result = world.query().with<Position2D>().toList()

            assertThat(result).containsExactlyInAnyOrder(
                player, basicEnemy, hiddenEnemy, invisibleWall, staticScenery
            )
        }

        @Test
        fun `with multiple components should return only entities having all of them`() {
            val result = world.query()
                .with<Position2D>()
                .with<Renderable>()
                .toList()

            assertThat(result).containsExactlyInAnyOrder(
                player, basicEnemy, hiddenEnemy, staticScenery
            )
        }

        @Test
        fun `with a non-existent component should return an empty list`() {
            // A component that has never been added to any entity
            class UnusedComponent

            val result = world.query().with<UnusedComponent>().toList()

            assertThat(result).isEmpty()
        }
    }

    @Nested
    @DisplayName("'without' clause (NOT logic)")
    inner class WithoutClause {
        @Test
        fun `without should exclude entities with the specified component`() {
            val result = world.query()
                .with<Renderable>()      // All renderable: player, basicEnemy, hiddenEnemy, staticScenery
                .without<EnemyTag>() // Exclude: basicEnemy, hiddenEnemy
                .toList()

            assertThat(result).containsExactlyInAnyOrder(player, staticScenery)
        }

        @Test
        fun `multiple without clauses should exclude entities with any of the specified components`() {
            val result = world.query()
                .with<Position2D>()       // All with position: player, basicEnemy, hiddenEnemy, invisibleWall, staticScenery
                .without<PlayerTag>() // Exclude player
                .without<Hidden>()    // Exclude hiddenEnemy, invisibleWall
                .toList()

            assertThat(result).containsExactlyInAnyOrder(basicEnemy, staticScenery)
        }

        @Test
        fun `a query with only a without clause should return nothing`() {
            // A query needs a positive statement (with or any) to define the initial set of entities.
            val result = world.query().without<PlayerTag>().toList()
            assertThat(result).isEmpty()
        }
    }

    @Nested
    @DisplayName("Complex and Edge Case Queries")
    inner class ComplexQueries {

        @Test
        fun `a query that logically matches nothing should return an empty list`() {
            // An entity cannot be both a Player and an Enemy with our current setup
            val result = world.query()
                .with<PlayerTag>()
                .with<EnemyTag>()
                .toList()

            assertThat(result).isEmpty()
        }

        @Test
        fun `a query on an empty world should return an empty list`() {
            val emptyWorld = World()
            val result = emptyWorld.query().with<Position2D>().toList()
            assertThat(result).isEmpty()
        }

        @Test
        fun `a query should not return entities that have been destroyed`() {
            // Arrange: Destroy an entity that would otherwise match
            world.destroyEntity(basicEnemy)

            // Act: Query for enemies
            val result = world.query().with<EnemyTag>().toList()

            // Assert: The destroyed enemy is not in the results
            assertThat(result).containsExactlyInAnyOrder(hiddenEnemy)
            assertThat(result).doesNotContain(basicEnemy)
        }

        @Test
        fun `terminal operations like firstOrNull and count should work as expected`() {
            val firstEnemy = world.query()
                .with<EnemyTag>()
                .firstOrNull()

            val enemyCount = world.query()
                .with<EnemyTag>()
                .count()

            // Order is not guaranteed, so we check if the result is one of the possibilities.
            assertThat(firstEnemy).isIn(basicEnemy, hiddenEnemy)
            assertThat(enemyCount).isEqualTo(2)
        }
    }
}
