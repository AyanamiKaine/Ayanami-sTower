---
sidebar_position: 4
---

# Timestep

An hour is the smallest time step, and every other step is just a multipler of that. So a day is just `1 * 24`, a week `24 * 7`, and a month `24 * 31`. Every calculation or data transformation is done by the hour. So events do not happen in months but instead in `X` amount of months times `31 * 24`.