## Factory Design Pattern
- The Factory design pattern is a creational pattern that offers an elegant way to create objects without exposing the details of the instantiation logic to the client code.  
- It works by providing a central interface for object creation while letting subclasses determine the specific types of objects to be produced.

## Use Cases
- Creating objects with complex construction logic
- Managing the life of many objects
- Need for a centralized place for object initialization (A manager class, to manage a pool of objects)
- Having a unified interface for similar objects

## How The Factory Design Pattern Depends on abstractions not concrete implementations
- In this pizza factory example, we have a abstract object called "pizza" every concrete pizza depends only on the abstract pizza object and every factory only depends on the abstract pizza type.
