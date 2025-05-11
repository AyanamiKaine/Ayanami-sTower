# Aspect Oriented Programming with [meta lama](https://metalama.net/)

## Notes

Aspect Oriented Programming comes with some goodie like type constrains (contracts) that I always wished for to be simply included into C# with annotations. Meta Lama provides these in the box. Using many or even all features of it seems way to overkill. But I think there are some features that I wish to always include as an option. Here we are exploring the idea of Aspect Oriented Programming becomming a main part of my C# coding style.

In general it seems that we want to add logic in our properties that we will use in many places in our code base (10-20 times) codifing it in an aspect can be a good idea. But only when it couldnt be done in an Interface. The main goal is reducing boilerplate if it does not reduce boilerplate as much as it increases the code complexity its probably not worth it.
