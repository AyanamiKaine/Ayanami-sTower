/*
MEMORY CONCEPT EXPLAINED

Memory is an intresting concept, it represents arbitrary data that should be remembered, and relates to the entity.

This is stored as a json dictionary<string, object>.

What is the main idea?

Imagine having an npc that should remember that fact that he saw the player pick up three apples. And wants to use this information to 
branch in dialog and react to that fact. Its actually quite hard to find a good field to put this information. Should it be stored on the 
player entity and a flag for the npc if he knows that? 

The memory table hides that fact from others, only the entity itself remembers information.
This is information that does not/should'nt be accessed every frame,
its good when local knowledge needs to be accessed for example in dialog trees.

*/

CREATE TABLE IF NOT EXISTS Memory (
    EntityId INTEGER NOT NULL UNIQUE, 
    Value JSON NOT NULL,
    FOREIGN KEY (EntityId) REFERENCES Entity(Id) ON DELETE CASCADE
);