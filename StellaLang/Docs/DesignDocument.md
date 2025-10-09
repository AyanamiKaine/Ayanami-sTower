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
