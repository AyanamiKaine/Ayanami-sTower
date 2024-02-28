## State Design Pattern
- It allows an object to alter its behavior when its internal state changes. The object will appear to change its class.
- Encapsulate interchanageable behaviors and use delegation to decide which behavior to use.

## Use Cases
- **Finite State Machines**
    - Representing states and transitions in a system (often used in parsing to represent it as a state machine). 
- **Complex Conditionals**
    - Simplify code with lots of if/else or switch statements dependent on an object's state. (often used in parsers, to simplify parsing a grammar with many rules). 
- **Role-Based Behaviors**
    - Objects change how they act based in a role or status (often used in dynamic system like a video game). 
