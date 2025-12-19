# InvictaDB

## Goal

The goal is a data-driven simulation for a base of simulation heavy games like X4: Foundations, Stellaris, Hearts of Iron 4. Distant Universe.

The simulation does not need to be real time, while a tick is one hour we don't expect the game to run in real-time like the other games above. We see the different simulation speeds more as different turns the player can take.

Our DB is immutable and functional in its design. Simulation games gain an architectural advantage if we can break down the game to: `game.Simulate(State) => UpdatedState`.

Because of the immutable design we can easily redo, undo, or branch from the state.

# Stella Invicta

Stella Invicta is a mix out of my most liked strategy games. With a heavy focus on emerging narrative and story telling. The world is influenced mostly by characters similar to Crusader Kings.

## Layered Abstraction

Most grand strategy games suffer from a micromanagement problem as the game processes. I want to solve this problem by introducing higher level abstraction for certain mechanics, so the player does not need to manage 100 planets but only 10 sectors.

This can also be mixed with characters instead of micromanaging something you would interact with a character that you delegated the work to. This personalizes world interactions and opens the possibilities of interesting behavior by other characters.

## Hierarchical Task Network (HTN)

A HTN is my favorite way of crafting complex AI. Giving an AI goals, and ways to achieve the goals is just great from a designers' perspective.

## Fuzzy Pattern Matcher

The fuzzy pattern matcher can be used by a form of AI director like it's done in `Left For Dead 2`. Or mix it in with the HTN of the AI.

## Why turn based hourly ticks instead of real-time?

Great question! The reason is quite simple, when playing Victoria 3 I was so fucked up by constant event pop-ups that are 90% meaningless and just generic generated. Why can't I just skip to the future where something interesting happens? While playing Stellaris with my friends, we had to wait because nothing interesting was happening as we had to wait for our fleets to build up.

In essence, we want to skip waiting for something. This is not Clash of Clans. We can easily do that by implementing a system that stops skipping when something interesting or noteworthy the player might be interested in happens (The player should be able to edit what events he wants to stop the simulation for.).
