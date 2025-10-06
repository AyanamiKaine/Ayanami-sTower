# Actor Mutation - Dynamic Message Handler Extension

## Overview

The VMActor supports **dynamic mutation** of its message handling capabilities through two key methods:

- `define_new_instruction(name, function)` - Add a new message handler
- `replace_existing_instruction(name, function)` - Replace an existing message handler

This enables actors to learn new message types at runtime, specialize for different domains, and adapt their behavior dynamically.

## Core Concept

In the actor model:

- **Instructions ARE messages** - Each bytecode instruction is a message
- **Handlers ARE message processors** - Each instruction handler processes one message type
- **Adding instructions = Teaching new message types** - Actors can learn new capabilities

## API

### `define_new_instruction(name, function)`

Adds a new instruction/message handler to the actor.

```python
actor = VMActor()

# Define a custom handler
def square(vm):
    value = vm.stack.pop()
    vm.stack.push(value * value)

# Add it to the actor
actor.define_new_instruction("OP_SQUARE", square)

# Now the actor can handle SQUARE messages
actor.send("OP_CONSTANT", 7)
actor.send("OP_SQUARE")

while actor.handle_message():
    pass

assert actor.top() == 49
```

**Constraints:**

- Raises `IndexError` if instruction name already exists
- Handler must accept `vm` parameter (the VMActor instance)
- Handler can access and modify: `vm.stack`, `vm.variables`, `vm.ip`, `vm.bytecode`

### `replace_existing_instruction(name, function)`

Replaces an existing instruction handler with new behavior.

```python
actor = VMActor()

# Original ADD behavior
actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
while actor.handle_message():
    pass
assert actor.top() == 15  # 5 + 10

# Replace ADD with multiply
def add_as_multiply(vm):
    b = vm.stack.pop()
    a = vm.stack.pop()
    vm.stack.push(a * b)

actor.replace_existing_instruction("OP_ADD", add_as_multiply)

# Now ADD multiplies
actor.stack.clear()
actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
while actor.handle_message():
    pass
assert actor.top() == 50  # 5 * 10
```

**Constraints:**

- Raises `IndexError` if instruction name doesn't exist
- Completely replaces the original behavior
- Can be used to enhance/validate existing operations

## Use Cases

### 1. Runtime Capability Extension

Add new capabilities to running actors:

```python
actor = VMActor()
runtime = SimpleRuntime(actor)

# Start with basic operations
actor.send("OP_CONSTANT", 10)
runtime.loop_until_empty()

# Add new capability
def triple(vm):
    vm.stack.push(vm.stack.pop() * 3)

actor.define_new_instruction("OP_TRIPLE", triple)

# Use new capability
actor.send("OP_TRIPLE")
runtime.loop_until_empty()
assert actor.top() == 30
```

### 2. Actor Specialization

Create domain-specific actors:

```python
# Math-specialized actor
math_actor = VMActor()

def power(vm):
    exp = vm.stack.pop()
    base = vm.stack.pop()
    vm.stack.push(base ** exp)

math_actor.define_new_instruction("OP_POWER", power)

# String-specialized actor
string_actor = VMActor()

def concat(vm):
    b = vm.stack.pop()
    a = vm.stack.pop()
    vm.stack.push(str(a) + str(b))

string_actor.define_new_instruction("OP_CONCAT", concat)
```

### 3. Side Effects and Observability

Instructions can have side effects (logging, metrics, etc.):

```python
log = []

def log_stack(vm):
    """Instruction that logs current stack state."""
    log.append(list(vm.stack))

actor.define_new_instruction("OP_LOG", log_stack)

# Build computation with logging checkpoints
actor.send("OP_CONSTANT", 10)
actor.send("OP_LOG")        # Checkpoint 1
actor.send("OP_CONSTANT", 5)
actor.send("OP_LOG")        # Checkpoint 2
actor.send("OP_ADD")
actor.send("OP_LOG")        # Checkpoint 3

# log now contains: [[10], [10, 5], [15]]
```

### 4. Meta-Instructions

Instructions that send more messages:

```python
def explode_number(vm):
    """Pop a number and push each digit as separate messages."""
    number = vm.stack.pop()
    digits = [int(d) for d in str(number)]
    for digit in digits:
        vm.send("OP_CONSTANT", digit)

actor.define_new_instruction("OP_EXPLODE", explode_number)

actor.send("OP_CONSTANT", 12345)
actor.send("OP_EXPLODE")  # Sends 5 more messages!
```

### 5. Instruction Composition

Build complex operations from simpler ones:

```python
def square(vm):
    value = vm.stack.pop()
    vm.stack.push(value * value)

def double(vm):
    value = vm.stack.pop()
    vm.stack.push(value * 2)

# Composite: square then double
def square_and_double(vm):
    square(vm)
    double(vm)

actor.define_new_instruction("OP_SQUARE", square)
actor.define_new_instruction("OP_DOUBLE", double)
actor.define_new_instruction("OP_SQUARE_DOUBLE", square_and_double)
```

### 6. Control Flow

Instructions can manipulate the instruction pointer:

```python
def skip_next_if_zero(vm):
    """Skip next 2 bytecode elements if top of stack is 0."""
    value = vm.stack.pop()
    if value == 0:
        vm.ip += 2  # Skip instruction + argument

actor.define_new_instruction("OP_SKIP_IF_ZERO", skip_next_if_zero)

actor.send("OP_CONSTANT", 0)
actor.send("OP_SKIP_IF_ZERO")
actor.send("OP_CONSTANT", 99)  # Skipped!
actor.send("OP_CONSTANT", 42)
```

## Handler Function Signature

Custom handlers must accept one parameter: the VM instance.

```python
def my_handler(vm):
    # Access VM state
    value = vm.stack.pop()
    var = vm.variables.get('x')

    # Modify VM state
    vm.stack.push(result)
    vm.variables['y'] = value

    # Read parameters from bytecode
    param = vm.read_constant()

    # Send more messages
    vm.send("OP_OTHER", param)

    # Control flow
    vm.ip += 2  # Skip next instruction
```

## Available VM Properties

Inside a handler, you have access to:

- `vm.stack` - Stack (list subclass with `push()` method)
- `vm.variables` - Variable dictionary
- `vm.ip` - Instruction pointer (current position in bytecode)
- `vm.bytecode` - The bytecode/message stream
- `vm.send(*instructions)` - Send more messages
- `vm.read_constant()` - Read next bytecode element as parameter

## Testing

See `tests/test_mutation.py` for 17 comprehensive tests covering:

- Adding new instruction types
- Adding multiple custom instructions
- Replacing existing instructions
- Instructions with side effects
- Instructions that access variables
- Instructions with parameters
- Control flow instructions
- Composite instructions
- Runtime capability mutation
- Error handling (duplicate names, non-existent names)
- Instruction chaining
- Instructions with closures
- Instructions that send messages
- Actor specialization patterns

## Examples

See `examples_mutation.py` for 8 working examples demonstrating:

1. Adding custom instructions
2. Multiple custom instructions
3. Replacing instruction behavior
4. Instructions with side effects
5. Actor specialization patterns
6. Runtime capability extension
7. Meta-instructions (sending messages)
8. Instruction composition

## Philosophy

This mutation system embodies several key principles:

1. **Messages ARE Instructions** - In this actor model, the bytecode stream is the message queue
2. **Instructions ARE Message Handlers** - Each instruction is a handler for a specific message type
3. **Dynamic Capability Extension** - Actors can learn new message types at runtime
4. **Separation of Concerns** - Actor defines handlers, runtime defines scheduling
5. **Composability** - Complex behaviors built from simple primitives

This enables a highly flexible actor system where actors can:

- Adapt to new requirements at runtime
- Specialize for different domains
- Compose complex behaviors from simple operations
- Maintain clean separation between message handling and scheduling

## Performance Considerations

- Adding instructions is O(1) - simple dictionary insertion
- Replacing instructions is O(1) - simple dictionary update
- No overhead for actors that don't use custom instructions
- Thread-safe via existing lock in `send()` method

## Comparison to Other Systems

This is similar to:

- **Erlang/Elixir**: `handle_call/3`, `handle_cast/2` pattern matching on messages
- **Akka**: Defining receive behaviors for different message types
- **Smalltalk**: Message passing and late binding
- **Forth**: Defining new words in the dictionary

But unique in that:

- Instructions and messages are unified
- Bytecode stream IS the message queue
- Stack-based computation model
- External scheduling control (GenServer pattern)

## Future Extensions

Possible enhancements:

- `remove_instruction(name)` - Remove a handler
- `has_instruction(name)` -> bool - Check if handler exists
- `list_instructions()` -> list - Get all instruction names
- Instruction priorities/ordering
- Instruction hooks (before/after execution)
- Instruction categories/namespaces
- Hot code reloading (swap multiple instructions atomically)
