# Stella Lang Design Document

## The Problem Of Terminology

We are going to throw around various words, ideas and concepts. This will make it hard to understand the basic idea because many words mean different things in different contexts. In reality this document needs to be away of that and defining each word explicitly first and later referring to a glossary.

For example, I want to treat the VM we create more like an object it was first envisioned in Smalltalk-72. Where every object is its own interpreter. At the same time we want to introduce some ideas like a runtime VM that is found in Erlang/Elixir.

-   Each object/interpreter gets executed using a scheduler.
-   Each interpreter has its own mailbox where messages can be sent.
-   Messages are just byte code. I.e. programs can be sent to interpreters as messages.

I personally would call this VM a VMActor.

Now we are having a VM that is inspired by Smalltalk-72 but is also similar to a gen-server in Elixir. Also, we want to structure the VM more like FORTH.

This gives you the overview of the problem we are facing. Many concepts, many different meanings, many biases.

## The Basic Idea

The basic idea is having a simple virtual machine that is extendible at runtime. It should be made out of a small core. Performance will be the last thing we care about. The reason for that is I need to explore the idea more. Performance of a bad idea means little to me.

## Single vs Multiple Stacks

We can have just one stack that is the same for temporary data and used for the call/return stack. This is quite cumbersome. Instead, having two stacks to separate them is desirable.

## OP-Code number of operands

Op-codes can have different kind of arguments. OP-0, OP-1, OP-2, OP-3.

Where OP-2 has the OP_CODE SOURCE_ADDRESS DESTINATION_ADDRESS format.

## Objects

Object-Oriented Programming (How it's done in languages like C++/C#/Java) is highly flawed. The main flaw lies in **compile time hierarchies of types** that represent objects in real world domains. A chair, car, money, calendar. Real world objects are defined by their surrounding context. This context changes over time. This results in changes of the objects. When employing compile time hierarchies of said objects. Chains of dependencies are formed.

In the beginning certain assumptions where thought to be fixed. Like duhh a car cannot fly. Now cars need to fly and implementing this behavior is hard. All fixed assumptions of the world will sooner or later be challenged.

### But how can we have our cake and eat it too?

I really like the Entity-Component-Systems framework [Flecs](https://www.flecs.dev/). A component can be seen as a field that is associated with an ID, and that idea represents our object. Based on our current context we query only the data of a component we currently need. There is no need to get the entire object when we only need two or three fields. This also allows us to better create functionality for a collection of components.

An entity/object has a certain behavior when it has the right sets of components. This simplifies things greatly as we don't have to detail with dispatching different nested behavior based on type hierarchies.
