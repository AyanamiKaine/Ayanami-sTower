# Async Actor-to-Actor Messaging

## Overview

VMActor supports **asynchronous actor-to-actor messaging** through the `send_to()` method. This enables **fire-and-forget** communication where the sender doesn't wait for a response - it sends messages and immediately continues with its own work.

This is similar to Erlang's `!` operator or Akka's `tell()` method.

## Core Concept

- **Fire-and-forget**: Sender doesn't block waiting for receiver
- **Asynchronous**: Messages are queued, processed when receiver is ready
- **Decoupled**: Sender and receiver operate independently
- **Non-blocking**: Sender continues its own work immediately after sending

## API

### `send_to(target_actor, *instructions)`

Send messages to another actor asynchronously.

```python
sender = VMActor()
receiver = VMActor()

# Send messages to receiver
sender.send_to(receiver, "OP_CONSTANT", 42, "OP_ADD")

# Sender continues immediately - doesn't wait!
sender.send("OP_CONSTANT", 100)
while sender.handle_message():
    pass

# Receiver processes when ready
while receiver.handle_message():
    pass
```

**Parameters:**

- `target_actor` - The VMActor instance to send messages to
- `*instructions` - Bytecode instructions to send

**Behavior:**

- Messages are appended to target's bytecode queue
- Thread-safe (uses internal lock)
- Sender's state is NOT modified
- Returns immediately (non-blocking)

## Communication Patterns

### 1. Point-to-Point

One actor sends to another:

```python
alice = VMActor()
bob = VMActor()

alice.send_to(bob, "OP_CONSTANT", 42)

# Bob processes when ready
while bob.handle_message():
    pass
```

### 2. Broadcast (One-to-Many)

One actor sends to multiple actors:

```python
broadcaster = VMActor()
listeners = [VMActor() for _ in range(5)]

def broadcast(vm):
    value = vm.stack.pop()
    targets = vm.variables['listeners']
    for target in targets:
        vm.send_to(target, "OP_CONSTANT", value)

broadcaster.define_new_instruction("OP_BROADCAST", broadcast)
broadcaster.variables['listeners'] = listeners

broadcaster.send("OP_CONSTANT", 999, "OP_BROADCAST")
```

### 3. Aggregator (Many-to-One)

Multiple actors send to one aggregator:

```python
workers = [VMActor() for _ in range(4)]
aggregator = VMActor()

# Workers send results
for i, worker in enumerate(workers):
    worker.send_to(aggregator, "OP_CONSTANT", i * 10)

# Aggregator processes all messages
aggregator.send("OP_ADD", "OP_ADD", "OP_ADD")  # Sum them
while aggregator.handle_message():
    pass
```

### 4. Chain

Actors pass messages forward in a chain:

```python
actor1 = VMActor()
actor2 = VMActor()
actor3 = VMActor()

def forward(vm):
    value = vm.stack.pop()
    result = value + 10
    vm.stack.push(result)
    next_actor = vm.variables.get('next')
    if next_actor:
        vm.send_to(next_actor, "OP_CONSTANT", result, "OP_FORWARD")

# Set up chain
actor1.variables['next'] = actor2
actor2.variables['next'] = actor3
actor3.variables['next'] = None

for actor in [actor1, actor2, actor3]:
    actor.define_new_instruction("OP_FORWARD", forward)

# Start chain
actor1.send("OP_CONSTANT", 5, "OP_FORWARD")
```

### 5. Request-Reply

Async request-reply pattern:

```python
client = VMActor()
server = VMActor()

def handle_request(vm):
    value = vm.stack.pop()
    result = value * 2
    reply_to = vm.variables['client']
    vm.send_to(reply_to, "OP_CONSTANT", result)

server.define_new_instruction("OP_REQUEST", handle_request)
server.variables['client'] = client

# Client sends request
client.send_to(server, "OP_CONSTANT", 21, "OP_REQUEST")

# Server processes and replies
while server.handle_message():
    pass

# Client receives reply
while client.handle_message():
    pass
```

### 6. Pipeline

Data flows through processing stages:

```python
stage1 = VMActor()
stage2 = VMActor()
stage3 = VMActor()

def process_and_forward(vm, transform):
    value = vm.stack.pop()
    result = transform(value)
    vm.stack.push(result)
    next_stage = vm.variables['next']
    vm.send_to(next_stage, "OP_CONSTANT", result, "OP_PROCESS")

# Each stage transforms and forwards
stage1.variables['next'] = stage2
stage2.variables['next'] = stage3
```

### 7. Supervisor

Supervisor monitors workers:

```python
supervisor = VMActor()
workers = [VMActor() for _ in range(3)]

def report_done(vm):
    worker_id = vm.variables['id']
    status = vm.stack.pop()
    vm.stack.push(status)  # Keep it
    supervisor_ref = vm.variables['supervisor']
    vm.send_to(supervisor_ref, "OP_CONSTANT", worker_id)

for i, worker in enumerate(workers):
    worker.define_new_instruction("OP_REPORT", report_done)
    worker.variables['id'] = i
    worker.variables['supervisor'] = supervisor

    # Worker does work and reports
    worker.send("OP_CONSTANT", i * 10, "OP_REPORT")
```

### 8. Event Bus (Pub-Sub)

Event bus broadcasts to subscribers:

```python
event_bus = VMActor()
subscribers = [VMActor() for _ in range(5)]

def publish_event(vm):
    event_value = vm.stack.pop()
    subs = vm.variables['subscribers']
    for sub in subs:
        vm.send_to(sub, "OP_CONSTANT", event_value)

event_bus.define_new_instruction("OP_PUBLISH", publish_event)
event_bus.variables['subscribers'] = subscribers

event_bus.send("OP_CONSTANT", 404, "OP_PUBLISH")
```

### 9. Worker Pool

Distribute work across a pool:

```python
coordinator = VMActor()
worker_pool = [VMActor() for _ in range(4)]

def distribute_work(vm):
    work_items = [10, 20, 30, 40, 50, 60]
    pool = vm.variables['pool']
    for i, work in enumerate(work_items):
        worker = pool[i % len(pool)]  # Round-robin
        vm.send_to(worker, "OP_CONSTANT", work)

coordinator.define_new_instruction("OP_DISTRIBUTE", distribute_work)
coordinator.variables['pool'] = worker_pool
```

## Implementation Details

### Thread Safety

`send_to()` is thread-safe because it uses the target actor's internal lock:

```python
def send_to(self, target_actor, *instructions):
    """Send messages to another actor asynchronously."""
    target_actor.send(*instructions)  # send() uses self._lock
```

This means multiple actors can safely send to the same target concurrently.

### State Preservation

`send_to()` does **not** modify the sender's state:

```python
sender = VMActor()
receiver = VMActor()

sender.send("OP_CONSTANT", 100)
while sender.handle_message():
    pass

# Save sender's state
stack_before = list(sender.stack)
ip_before = sender.ip

# Send to receiver
sender.send_to(receiver, "OP_CONSTANT", 42)

# Sender's state unchanged
assert list(sender.stack) == stack_before
assert sender.ip == ip_before
```

### Message Ordering

Messages sent to the same actor are delivered in FIFO order:

```python
sender.send_to(receiver, "OP_CONSTANT", 1)
sender.send_to(receiver, "OP_CONSTANT", 2)
sender.send_to(receiver, "OP_CONSTANT", 3)

# Receiver will process in order: 1, 2, 3
```

However, when **multiple senders** send to one receiver concurrently, message interleaving depends on thread scheduling.

### Sending to Self

An actor can send messages to itself:

```python
actor = VMActor()
actor.send_to(actor, "OP_CONSTANT", 42)
# Same as: actor.send("OP_CONSTANT", 42)
```

## Comparison to Other Systems

### Erlang/Elixir

```erlang
% Erlang
Pid ! {message, Data}
```

```python
# VMActor equivalent
actor1.send_to(actor2, "OP_MESSAGE", data)
```

### Akka (Scala/Java)

```scala
// Akka
actorRef.tell(message, sender)
```

```python
# VMActor equivalent
sender.send_to(actorRef, "OP_MESSAGE", data)
```

### Actor Model Theory

VMActor's `send_to()` implements classic actor model semantics:

1. **Asynchronous messaging** - sender doesn't block
2. **Mailbox/queue** - messages queued in bytecode stream
3. **Sequential processing** - actor processes messages one at a time
4. **Encapsulation** - actors don't share state, only send messages

## Use Cases

### Distributed Work Processing

```python
# Coordinator distributes work to pool
for task in tasks:
    worker = select_worker(task)
    coordinator.send_to(worker, "OP_TASK", task)
```

### Event-Driven Systems

```python
# Event producer sends to event bus
producer.send_to(event_bus, "OP_EVENT", event_data)

# Event bus broadcasts to subscribers
event_bus processes and broadcasts to all subscribers
```

### Pipeline Processing

```python
# Data flows through processing stages
source.send_to(stage1, "OP_DATA", data)
# stage1 processes and sends to stage2
# stage2 processes and sends to stage3
# etc.
```

### Supervision Trees

```python
# Workers report to supervisor
worker.send_to(supervisor, "OP_STATUS", status)

# Supervisor can send commands to workers
supervisor.send_to(worker, "OP_RESTART")
```

## Testing

See `tests/test_actor_to_actor.py` for 18 comprehensive tests covering:

- Basic send_to functionality
- Async behavior verification
- Multiple send_to operations
- Bidirectional communication
- Runtime integration
- Chain of actors
- Broadcast pattern
- Aggregator pattern
- Pipeline pattern
- Request-reply pattern
- Supervisor pattern
- Event bus pattern
- State preservation
- Concurrent sends
- Round-robin scheduling
- Worker pool pattern

## Examples

See `examples_actor_messaging.py` for 10 working examples:

1. Basic async messaging
2. Message chain
3. Broadcast pattern
4. Aggregator pattern
5. Request-reply pattern
6. Processing pipeline
7. Supervisor pattern
8. Event bus (pub-sub)
9. Bidirectional communication
10. Worker pool pattern

## Performance Considerations

- **send_to() is O(1)** - simple list append with lock
- **No serialization** - direct reference passing (same process)
- **Lock contention** - multiple senders to one receiver may contend for lock
- **Memory** - messages accumulate in bytecode queue until processed

## Best Practices

### 1. Avoid Infinite Loops

Be careful with circular message chains:

```python
# BAD: Infinite loop
def forward_forever(vm):
    value = vm.stack.pop()
    next_actor = vm.variables['next']
    vm.send_to(next_actor, "OP_CONSTANT", value, "OP_FORWARD")

# GOOD: Add termination condition
def forward_limited(vm):
    value = vm.stack.pop()
    if value < 100:  # Termination condition
        next_actor = vm.variables['next']
        vm.send_to(next_actor, "OP_CONSTANT", value + 1, "OP_FORWARD")
```

### 2. Use Runtime for Complex Scheduling

For complex actor interactions, use ActorRuntime:

```python
runtime = ActorRuntime()
for actor in actors:
    runtime.register(actor)

# Runtime controls scheduling
runtime.start()
```

### 3. Store Actor References in Variables

Keep actor references in variables for easy access:

```python
actor.variables['supervisor'] = supervisor
actor.variables['workers'] = worker_list
actor.variables['next'] = next_in_chain
```

### 4. Document Message Protocols

Clearly document what messages actors expect:

```python
class WorkerActor(VMActor):
    """
    Messages accepted:
    - ("OP_TASK", task_data) - Process a task
    - ("OP_STOP",) - Stop processing

    Messages sent:
    - ("OP_RESULT", result) - To supervisor
    - ("OP_ERROR", error) - To supervisor
    """
```

## Future Enhancements

Possible additions:

- Named actors (actor registry)
- Remote actors (network communication)
- Message priorities
- Selective receive (pattern matching)
- Mailbox size limits
- Dead letter queue
- Actor lifecycle management (spawn, stop, restart)
- Link/monitor mechanisms
