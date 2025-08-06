package ecs



/*
* By default, I believe components should be immutable,
* but the user can decide himself what he likes more.
* */
internal data class Position2D(val  x: Int, val  y: Int)
internal data class Position3D(val  x: Int, val  y: Int)
internal object PlayerTag
internal data class Velocity(val  dx: Int, val  dy: Int)
internal object EnemyTag
internal object Renderable
internal object Hidden
