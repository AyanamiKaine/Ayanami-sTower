# VMActor - Message-Passing Virtual Machine

A stack-based virtual machine with an Erlang/Elixir-inspired actor model where **bytecode instructions ARE the messages**.

## Core Concept

The bytecode stream is the message stream. Instructions are messages, and registered instruction handlers are the message handlers. The VM actor continuously receives messages (bytecode instructions) from the stream and dispatches them to the appropriate handlers.

## Features

- **Message-Passing Model**: Bytecode instructions are messages sent to the actor
- **Receive Loop**: Erlang-style receive loop that pattern-matches on instruction types
- **S-Expression Compiler**: Compile Lisp-like expressions to bytecode
- **Thread-Safe**: Lock-protected message sending for concurrent access
- **Extensible**: Define custom instructions and handlers at runtime

## Quick Start

### Traditional Bytecode Execution

```python
from src.VMActor import VMActor

# Create VM and load bytecode
vm = VMActor()
vm.load_bytecode(["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD"])

# Execute all instructions
vm.execute()

print(vm.top())  # 15
```

### Actor-Style Message Passing

```python
from src.VMActor import VMActor
import time

# Create and start actor
actor = VMActor()
actor.start()  # Starts receive loop in background thread

# Send messages (bytecode instructions)
actor.send("OP_CONSTANT", 5)
actor.send("OP_CONSTANT", 10)
actor.send("OP_ADD")

time.sleep(0.1)  # Wait for processing
print(actor.top())  # 15

actor.stop()
```

### S-Expression Compilation

```python
vm = VMActor()

# Compile s-expression to bytecode
sexpr = "(+ (* 3 4) (- 20 10))"
bytecode = vm.s_expression_to_bytecode(sexpr)

# Send as messages
vm.start()
vm.send(*bytecode)

time.sleep(0.1)
print(vm.top())  # 22  ((3*4) + (20-10) = 12 + 10)

vm.stop()
```

## Message-Passing Architecture

### The Receive Loop

```python
def receive_loop(self):
    """Like Erlang's receive loop - continuously process messages."""
    while self.running:
        instruction = self.receive()  # Get next message

        if instruction is None:
            time.sleep(0.01)  # Wait for more messages
            continue

        # Pattern match and dispatch
        handler = self.instruction_table.get(instruction)
        self._invoke_handler(handler)
```

### Sending Messages

```python
# Send individual instructions
actor.send("OP_CONSTANT", 42)

# Send multiple at once
actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")

# Messages are appended to the bytecode stream
# The receive loop processes them one by one
```

## Supported Instructions

### Stack Operations

- `OP_CONSTANT <value>` - Push constant onto stack
- `OP_POP` - Pop top value from stack

### Arithmetic

- `OP_ADD` - Pop two values, push their sum
- `OP_SUBTRACT` - Pop b, pop a, push (a - b)
- `OP_MULTIPLY` - Pop two values, push their product
- `OP_DIVIDE` - Pop b, pop a, push (a / b)
- `OP_NEGATE` - Pop value, push its negation

### Comparison

- `OP_GREATER` - Pop b, pop a, push (a > b)
- `OP_LESS` - Pop b, pop a, push (a < b)
- `OP_EQUAL` - Pop two values, push equality result

### Boolean

- `OP_TRUE` - Push True
- `OP_FALSE` - Push False

### Variables

- `OP_DEFINE_VARIABLE <name>` - Pop value, define variable
- `OP_GET_VARIABLE <name>` - Push variable value
- `OP_SET_VARIABLE <name>` - Pop value, set variable

## S-Expression Syntax

### Define Variables

```scheme
(define x 42)
```

### Arithmetic

```scheme
(+ 5 10)          ; 15
(* 3 4)           ; 12
(- 20 5)          ; 15
(/ 10 2)          ; 5
```

### Nested Expressions

```scheme
(+ (* 2 3) (- 10 5))      ; (2*3) + (10-5) = 11
(* (+ 1 2) (- 10 (/ 8 2))) ; (1+2) * (10-4) = 18
```

### Comparisons

```scheme
(> 10 5)          ; true
(< 5 10)          ; true
(= 5 5)           ; true
```

### Using Variables

```scheme
(define x 10)
(+ x 5)           ; 15
(set! x 20)
(* x 2)           ; 40
```

## Custom Instructions

Define your own instruction handlers:

```python
vm = VMActor()

def double(vm):
    value = vm.stack.pop()
    vm.stack.push(value * 2)

vm.define_new_instruction("OP_DOUBLE", double)

vm.start()
vm.send("OP_CONSTANT", 21)
vm.send("OP_DOUBLE")
time.sleep(0.1)
print(vm.top())  # 42
vm.stop()
```

## Testing

Run all tests:

```bash
pytest tests/ -v
```

Run specific test suites:

```bash
# Traditional execution tests
pytest tests/test_VMActor.py -v

# Message-passing / actor tests
pytest tests/test_message_passing.py -v
```

## Architecture Highlights

1. **Bytecode = Messages**: The bytecode stream serves as the message queue
2. **Instructions = Message Types**: Each instruction is a message type
3. **Handlers = Message Handlers**: Instruction handlers pattern-match on message type
4. **receive()**: Reads next instruction from bytecode stream (like Erlang's receive)
5. **send()**: Appends instructions to bytecode stream (message passing)
6. **receive_loop()**: Main actor loop that processes messages continuously

## Thread Safety

The `send()` method uses a lock to safely append messages from multiple threads:

```python
def send(self, *instructions):
    with self._lock:
        self.bytecode.extend(instructions)
```

This allows multiple threads to send messages to the same actor safely.

## Performance Notes

- The receive loop sleeps for 10ms when no messages are available to avoid busy-waiting
- Thread-safe locking adds minimal overhead
- Background thread mode allows concurrent message processing

## Future Extensions

- Control flow instructions (OP_JUMP, OP_JUMP_IF_FALSE, OP_LOOP)
- Function calls (OP_CALL, OP_RETURN)
- More s-expression forms (if, while, let, lambda)
- Message priority/ordering guarantees
- Actor supervision and error handling

## License

MIT
