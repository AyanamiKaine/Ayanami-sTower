## Visitor Design Pattern
- The Visitor design pattern is a behavioral pattern that provides a way to add new operations to a hierarchy of objects (often called "elements") without modifying the structure of those objects themselves.
- The core principle is to separate the operational logic from the object structure. We define a Visitor class that encapsulates the various operations we might want to perform on the elements.

## Use Cases
- Extending functionality: Add new operations to an existing object hierarchy without modifying those classes.
- Different operations on heterogeneous data: Apply different, unrelated operations to objects of different types in a structure.
- Gathering data: Use a visitor to traverse an object structure and collect information or perform calculations.
- Representing a compiler: Compilers often use the Visitor pattern to represent operations on abstract syntax trees.