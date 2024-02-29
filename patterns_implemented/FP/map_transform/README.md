## Map/Transform
- You have a colletion and you want to transform each element.
- transform: (collection<In>, (In -> Out)) -> Collection<Out>
- This is a higher-order function that abstracts the process of iterating over recursive structures such as vectors, lists, trees, and so on and lets you gradually build the result you need.

## Use Cases
- When you want to do something with the elements of a collection the map pattern can be used.
    - Summing up values
- When you want to use an arbitary binary operation that can produce a result of different type than the items in the collection, folding becomes a power tool for implemenating many algorithms. (Imagine this you have the following operation 1 + 2 + 3 + 4 + 10, what if we could replace the addtion operator with somehthing else ?and replace numbers with other objects that can be folded into one? Then we can use reduce)
