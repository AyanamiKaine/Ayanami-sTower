---
sidebar_position: 5
---

# Turn-Based

The engine mostly works by defining an update to the world state as one turn, the smallest turn we can do is progressing time by one hour. So a day would be 24 turns. Its important to understand that we wont focus on realtime performance, I strongly disbelieve that we could achieve something like 100 turns ala 60FPS. 

The architecture is not suitable for that. Its much more like Aurora 4x in that regard.