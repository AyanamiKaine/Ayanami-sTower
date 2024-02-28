## Command Design Pattern 
- Command is a behavioral design pattern that turns a request into a stand-alone object that contains all information about the request. This transformation lets you pass requests as a method arguments, delay or queue a request’s execution, and support undoable operations.

## Use Cases
- Decouple the invoker (sender) of a request from the receiver (executor) of the request. The invoker doesn't need to know the details of how the request is carried out.
- Queue or schedule requests. Commands store the information needed to execute them at a later time.
- Support undoable operations. Commands often store the state before an operation is carried out, making it possible to reverse the action.

## Real-World Analogy
- Think of a restaurant:
    - Customer (Invoker): Places an order
    - Order (Command): Encapsulates the details of the meal requested
    - Waiter (Invoker): Takes the order to the kitchen
    - Chef (Receiver): Knows how to prepare the specific dish

## Where Lambdas (Functional Programming) can make the command pattern easier to use
- Here we use Lambdas to represent Concrete Commands so instead of extra creating derived class from the abstract command class we simply pass a lambda.
- This approach is simpler, easier to use, easier to maintain, and should be preferrd over using command classes. 

## Key Components:

- *Command*: An interface that declares a method for executing a request (usually a single method like execute()).
- *ConcreteCommand*: Specific implementations of the Command interface. These classes handle the details of how the request is carried out, often by interacting with the receiver object.
- *Receiver*: The object that knows how to perform the actual work related to the request.
- *Invoker*: The object responsible for initiating a request. It stores a Command object and triggers its execute() method when needed.
