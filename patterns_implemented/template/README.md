## Template Design Pattern
- Here we explain the template design pattern, for sake of brewity we will ignore some common software engenerring (for example we implement 3 classes in one component)
- In the tempalte design pattern we define a skeleton of an algorithm while allowing subclasses to redefine specific steps without altering the overall structure.
- Promotes code reuse by encapsulating the unchanging parts of an algorithm.

## Use Cases
- Use the Template Method pattern when you want to let clients extend only particular steps of an algorithm, but not the whole algorithm or its structure
- Use the pattern when you have several classes that contain almost identical algorithms with some minor differences.

## How To Build
- run "bazel build //:template"

## How To Execute
- run "bazel run //:template"

## How To run the Tests
- run "bazel test //:template"

