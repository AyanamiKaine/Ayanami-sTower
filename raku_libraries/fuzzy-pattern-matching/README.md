# Fuzzy Pattern Matching

I want to explore the idea of implementing fuzzy pattern matching.

## The basic idea

This is heavily inspired by this talk [AI-driven Dynamic Dialog through Fuzzy Pattern Matching](https://www.youtube.com/watch?v=tAbBID3N64A&t)

## The problem we want to solve

We want to create a system that is reactive to the world. For small things it does not really matter what we do, if-else statements do the job. But what about bigger things? The problem lies in the sheer number of different state in a world. Some games handle this by creating branching trees to create choice and explicit stored flags (Paradox interactive uses flags for example).

But some games need more than if-else statements. 

## A possible solution



# Running Tests

To run all tests run `prove6 --lib t/`
