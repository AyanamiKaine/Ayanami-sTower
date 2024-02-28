## Facade Design Pattern
- Provides a unified interface to a set of interfaces in a subsystem. Facade defines a higher-level interface that makes the subsystem easier to use.
- It encaspulates complexity

## Use Cases
- When you have a collection of subsystem you want to expose in a much more simpler interface.
- **Libraries and APIs**
    - When you are working with a complex third-party library, a facade can streamline your interaction with it. 
- **Legacy Systems**
    - Facade help you modernize the usage of older, complex systems without rewriting everything. 
- **Microservices**
    - In a Microservices architecture, a Facade can hide the complexity of communication between various services, presenting a cohesive interface. 
- **Any complex subsystem**
    - Whenevery you have a chunk of code with intricate interactions, the Facade pattern can make it simpler to consume. 

## Principle of Least Knowledge
- The facade design pattern also shows the principle of least knowledge: talk only to your immediate friends. It bundles components with similar interfaces in the same domain.
- We should avoid having code like this => `object.object.object.function()`. It exposes way to much cognitive load to the client. I.e.if you see something like this its a bad code smell.
