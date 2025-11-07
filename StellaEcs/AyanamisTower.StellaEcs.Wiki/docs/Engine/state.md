---
sidebar_position: 3
---

# Game State

The game state is just a SQLite Database. This makes it possible to add custom data or logic to the game by simply registering them to the database. Updated the game using a time step is just a pipeline transformation of the data in the database. Modifying the game in any way is just adding/removing/changing parts of the pipeline. So even the most basic and simply aspect can be plugged out or plugged in.

A data transformation should always just depend on the expected schema, not necessarily that a certain transformation of the data was done before.

While you might think that you must depend on a certain order of execution, each update is done in hours, so in the worst case data will be "out of sync" by only one hour. This should be an acceptable trade-off. For that you don't need to depend on the order (Something that is brittle and will often break).