## Dependency Inversion Principle (DIP) Explained
- "Depend upon abstractions. Do not depend upon concrete classes"
    - What does this even mean?
    - Higher-Level Components should not depend on our low-level components; rather, they should *both* depend on abstractions.
        - A "high-level" component is a class with behavior defined in terms of other, "low-level" components.
        

## Guidlines to adhere to DIP
- No variable should hold a reference to a concrete class
- No class should derive from a concrete class
    - If you derive from a concrete class, you’re depending on a concrete class. Derive from an abstraction, like an interface or an abstract class. 
- No method should override an implemented method of any of its base classes
- We want to depend on abstractions everywhere we can and want.
