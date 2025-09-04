---
sidebar_position: 8
---

# Micro Managment and Its Problems

*"Micro Management is the bane of my existence."* - Me

We all now the problems of micro management, in the beginning we easily manage 2 armies, 10 provinces, one nation. But after some time our empire grows. Now we manage 16 armies, 100 provinces, 3 vassales, 5 rebel problems, a big crisis, and some more new mechanics that unlock overtime. 

The time we have in our life is limited. Before we spent some time organizing our small nation, now we need to spend big time ensuring it does not fall apart. Often this results in the player taking shortcuts, he consults many armies in larg blobs (Even though he knows it would be strategily better to have more armies, he cannot manage more and does not want to give them to the AI to control (They would die)). 

Other times the player simply does not engage with our new cool mechanic because he thinks he already has so much on his hands, he doesnt have the time for it. Micro management is bad if its too much and you think you have to engage with it otherwise you are missing something out. Like its simply a bad strategy not to micro management.

# Possible Solution

A layered abstraction approach. What do I mean with that? Imagine you control planets in the beginning you micro managed each planet they where few. Now after you control many of them a new feature unlocks called sector management, now you manage the sector but not the planets directly anymore. But if wished the player can still control them if he wants to. This is not automation but instead we move the interaction an abstraction layer up. The player now interactes with sectors not planets. Now instead of having to micromanage 17 planets he only has to manage 3 sectors.

This is the core idea, we create a new abstraction that abstracts the micro management away to a new representation. **While we micro manage a sector, we now macro manage the planets**. This is the key insight.

The most terrible way to implement it is seen in Stellaris. You have sectors and planets. What it boils down to is saying automate this planet or sector and make it a research sector/planet. This is the entire interaction and control the player has. The usual result is that the automation is so bad it slowly destorys the economics of your empire. 