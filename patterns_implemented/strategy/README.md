## Strategy Design Pattern
- Its about encapsulating a family of algorithms algorithms or behaviors into interchangeable objects. This means you can dynamically change the way a class operates simply by switching out the strategy it uses.

## Use Cases
- Typical used for algorithms that operate in the same domain but solve a problem in different ways.
- Sorting Algorithms
- Compression Algorithms
- Encryption Algorithms
- AI Behavior for game entities
- Report creation (PDF, CSV, HTML)

## Relation to functional programming
- An alternative approach to defining strategy objects would be a function that takes an lambda as an argument. The lambda itself is the strategy/algorithm to be applied.

## Relation to the Template Design Pattern
- You may notice that the template design pattern seems to do the same thing as the strategy design pattern, but there are subtle differences

1. The strategy design pattern is based on *composition*, you supply an object, this can be done at runtime.
2. The template design pattern is based on *inheritance*, you create a subclass and override specifc steps of an algorithm. This enables static polymorphism (The compiler determines the virtual function calls at compile time).