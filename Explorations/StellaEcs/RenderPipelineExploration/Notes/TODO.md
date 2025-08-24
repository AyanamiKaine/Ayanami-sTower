## TODO

-   [x] Fix Floating point issue for rendering large scale universe.
-   [x] Fix floating point calculations for simulations, our simulation for star systems needs to use doubles because imagine a star system far away it will have the same rounding error as when rendering them, here the difference is the player won't see the problem until he moved far away and some time passes, the more we wait, the more the error accumulates and results in wrong simulation results for example for planet orbits.
    -   We implemented relative rendering, rebasing for floating point issues, and high precision values for the simulation. These three implementations result in no jitter for 3D objects and no accumulated error in the simulation logic.

### Possible options

-   Floating origin (rebasing): keep world coordinates near zero by periodically subtracting a large shift (usually the camera position) from every object + physics object + origin. This works with single-precision render/physics by keeping all active coordinates near 0.
-   World / simulation split: keep global positions in double precision for long-term bookkeeping (galaxy coordinates), but convert to a float local-space for rendering/physics (local position = (double_world_pos - double_local_origin) -> float). Physics engines like Bepu use floats, so we must keep the physics origin near zero (or rebase the simulation).
