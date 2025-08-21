## TODO

-   [] Fix Floating point issue for large scale universe.

### Possible options

-   Floating origin (rebasing): keep world coordinates near zero by periodically subtracting a large shift (usually the camera position) from every object + physics object + origin. This works with single-precision render/physics by keeping all active coordinates near 0.
-   World / simulation split: keep global positions in double precision for long-term bookkeeping (galaxy coordinates), but convert to a float local-space for rendering/physics (local position = (double_world_pos - double_local_origin) -> float). Physics engines like Bepu use floats, so we must keep the physics origin near zero (or rebase the simulation).
