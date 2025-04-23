# NihilEx Game Framework

## Why is there no loop/iterate method?

If you want to do something in the main game loop create a Flecs.Net system. Systems run by
default every frame. If you want to run every second or third frame or every second this is 
also totally possible by just using systems.