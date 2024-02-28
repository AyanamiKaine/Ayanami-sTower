## Decorator Design Pattern
- Our goal is to allow classes to be easily extended to incorporate new behavior without modifying existing code.
- An example for the Open-Closed Principle. (Clients can extend classes but cannot modify (behavior) existing one)
- The Decorator Pattern attaches additional responsibilities to an object dynamically. Decorators provide a flexible alternative to subclassing for extending functionality
- A better way of achieving the same goal is probaly the factory design pattern. Keep that in mind.

## Onion Explanation
- We can visualise the decator pattern as a onion where we have the ability to add an new layer on top the old one. 
    - "auto beverage2 = std::make_unique<Mocha>(std::make_unique<Soy>(std::make_unique<HouseBlend>()));"
    - Here we have three layers (Inner Layer)House Blend Coffe -> Soy -> Mocha (Outer Layer)  

