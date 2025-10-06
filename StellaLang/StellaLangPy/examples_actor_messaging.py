"""Examples demonstrating async actor-to-actor messaging (fire-and-forget).

Key Concept: send_to() is asynchronous - the sender doesn't wait for a response.
It sends messages and immediately continues with its own work.
"""

from src.VMActor import VMActor
from src.ActorRuntime import ActorRuntime, SimpleRuntime
import time


def example1_basic_send_to():
    """Example 1: Basic async messaging between actors."""
    print("\n=== Example 1: Basic Async Messaging ===")
    
    sender = VMActor()
    receiver = VMActor()
    
    # Sender sends a message and doesn't wait
    sender.send_to(receiver, "OP_CONSTANT", 42)
    
    # Sender can immediately do its own work
    sender.send("OP_CONSTANT", 100)
    while sender.handle_message():
        pass
    
    print(f"Sender completed its work: {sender.top()}")
    
    # Receiver processes when ready
    while receiver.handle_message():
        pass
    
    print(f"Receiver got message: {receiver.top()}")
    print("✓ Fire-and-forget: sender didn't wait for receiver")


def example2_message_chain():
    """Example 2: Chain of actors passing messages forward."""
    print("\n=== Example 2: Message Chain ===")
    
    actor1 = VMActor()
    actor2 = VMActor()
    actor3 = VMActor()
    
    # Each actor adds 10 and forwards to next
    def forward_plus_10(vm):
        value = vm.stack.pop()
        result = value + 10
        vm.stack.push(result)
        next_actor = vm.variables.get('next')
        if next_actor:
            vm.send_to(next_actor, "OP_CONSTANT", result, "OP_FORWARD")
    
    for actor in [actor1, actor2, actor3]:
        actor.define_new_instruction("OP_FORWARD", forward_plus_10)
    
    actor1.variables['next'] = actor2
    actor2.variables['next'] = actor3
    actor3.variables['next'] = None
    
    # Start the chain
    actor1.send("OP_CONSTANT", 5, "OP_FORWARD")
    
    # Process each actor in sequence
    while actor1.handle_message():
        pass
    print(f"Actor1: 5 + 10 = {actor1.top()}")
    
    while actor2.handle_message():
        pass
    print(f"Actor2: 15 + 10 = {actor2.top()}")
    
    while actor3.handle_message():
        pass
    print(f"Actor3: 25 + 10 = {actor3.top()}")
    
    print("✓ Message propagated through chain: 5 → 15 → 25 → 35")


def example3_broadcast():
    """Example 3: Broadcasting to multiple actors."""
    print("\n=== Example 3: Broadcast Pattern ===")
    
    broadcaster = VMActor()
    listeners = [VMActor() for _ in range(5)]
    
    # Broadcast instruction sends to all listeners
    def broadcast(vm):
        value = vm.stack.pop()
        targets = vm.variables.get('listeners', [])
        for target in targets:
            vm.send_to(target, "OP_CONSTANT", value)
    
    broadcaster.define_new_instruction("OP_BROADCAST", broadcast)
    broadcaster.variables['listeners'] = listeners
    
    # Broadcast value 999 to all listeners
    broadcaster.send("OP_CONSTANT", 999, "OP_BROADCAST")
    while broadcaster.handle_message():
        pass
    
    # All listeners receive the message
    for i, listener in enumerate(listeners):
        while listener.handle_message():
            pass
        print(f"Listener {i+1}: {listener.top()}")
    
    print("✓ One broadcaster sent to 5 listeners asynchronously")


def example4_aggregator():
    """Example 4: Multiple actors sending to one aggregator."""
    print("\n=== Example 4: Aggregator Pattern ===")
    
    workers = [VMActor() for _ in range(4)]
    aggregator = VMActor()
    
    # Each worker computes and sends result to aggregator
    values = [10, 20, 30, 40]
    for worker, value in zip(workers, values):
        worker.send("OP_CONSTANT", value)
        worker.send_to(aggregator, "OP_CONSTANT", value)
    
    # Workers process their messages
    for i, worker in enumerate(workers):
        while worker.handle_message():
            pass
        print(f"Worker {i+1} computed: {worker.top()}")
    
    # Aggregator receives all values
    aggregator.send("OP_ADD", "OP_ADD", "OP_ADD")  # Sum all 4 values
    while aggregator.handle_message():
        pass
    
    print(f"Aggregator sum: {aggregator.top()}")
    print("✓ 4 workers sent to 1 aggregator: 10 + 20 + 30 + 40 = 100")


def example5_request_reply():
    """Example 5: Request-reply pattern (async both ways)."""
    print("\n=== Example 5: Request-Reply Pattern ===")
    
    client = VMActor()
    server = VMActor()
    
    # Server has a reply instruction
    def handle_request(vm):
        value = vm.stack.pop()
        result = value * 2  # Server doubles the value
        reply_to = vm.variables.get('client')
        vm.send_to(reply_to, "OP_CONSTANT", result)
    
    server.define_new_instruction("OP_REQUEST", handle_request)
    server.variables['client'] = client
    
    # Client sends request
    print("Client sends request: 21")
    client.send_to(server, "OP_CONSTANT", 21, "OP_REQUEST")
    
    # Server processes request and sends reply
    while server.handle_message():
        pass
    print("Server processed request")
    
    # Client receives reply
    while client.handle_message():
        pass
    print(f"Client received reply: {client.top()}")
    
    print("✓ Async request-reply: client → server → client")


def example6_pipeline():
    """Example 6: Data processing pipeline."""
    print("\n=== Example 6: Processing Pipeline ===")
    
    # Pipeline: source → doubler → squarer → logger
    source = VMActor()
    doubler = VMActor()
    squarer = VMActor()
    logger = VMActor()
    
    # Doubler stage
    def double_stage(vm):
        value = vm.stack.pop()
        result = value * 2
        vm.stack.push(result)
        next_actor = vm.variables['next']
        vm.send_to(next_actor, "OP_CONSTANT", result, "OP_PROCESS")
    
    doubler.define_new_instruction("OP_PROCESS", double_stage)
    doubler.variables['next'] = squarer
    
    # Squarer stage
    def square_stage(vm):
        value = vm.stack.pop()
        result = value * value
        vm.stack.push(result)
        next_actor = vm.variables['next']
        vm.send_to(next_actor, "OP_CONSTANT", result, "OP_PROCESS")
    
    squarer.define_new_instruction("OP_PROCESS", square_stage)
    squarer.variables['next'] = logger
    
    # Logger stage (end of pipeline)
    def log_stage(vm):
        value = vm.stack.pop()
        vm.stack.push(value)
        print(f"  Pipeline output: {value}")
    
    logger.define_new_instruction("OP_PROCESS", log_stage)
    
    # Send values through pipeline
    print("Sending 5 through pipeline...")
    source.send_to(doubler, "OP_CONSTANT", 5, "OP_PROCESS")
    
    # Process pipeline stages
    while doubler.handle_message():
        pass
    print(f"After doubler: {doubler.top()}")
    
    while squarer.handle_message():
        pass
    print(f"After squarer: {squarer.top()}")
    
    while logger.handle_message():
        pass
    
    print("✓ Pipeline: 5 → double(10) → square(100) → log")


def example7_supervisor():
    """Example 7: Supervisor monitoring workers."""
    print("\n=== Example 7: Supervisor Pattern ===")
    
    supervisor = VMActor()
    workers = [VMActor() for _ in range(3)]
    
    # Workers report completion to supervisor
    def report_done(vm):
        worker_id = vm.variables['id']
        status = vm.stack.pop()
        vm.stack.push(status)  # Put it back for later reference
        supervisor_ref = vm.variables['supervisor']
        # Report: worker_id and status
        vm.send_to(supervisor_ref, "OP_CONSTANT", worker_id)
    
    for i, worker in enumerate(workers):
        worker.define_new_instruction("OP_REPORT", report_done)
        worker.variables['id'] = i + 1
        worker.variables['supervisor'] = supervisor
        
        # Worker does work and reports
        worker.send("OP_CONSTANT", i * 10)
        worker.send("OP_REPORT")
    
    # Workers process their tasks
    for i, worker in enumerate(workers):
        while worker.handle_message():
            pass
        print(f"Worker {i+1} completed work: {worker.top()}")
    
    # Supervisor receives all reports
    while supervisor.handle_message():
        pass
    
    print(f"Supervisor received {len(supervisor.stack)} reports: {list(supervisor.stack)}")
    print("✓ Workers reported to supervisor asynchronously")


def example8_event_bus():
    """Example 8: Event bus / pub-sub pattern."""
    print("\n=== Example 8: Event Bus (Pub-Sub) ===")
    
    event_bus = VMActor()
    subscribers = {
        'analytics': VMActor(),
        'logger': VMActor(),
        'cache': VMActor(),
    }
    
    # Event bus publishes to all subscribers
    def publish_event(vm):
        event_value = vm.stack.pop()
        subs = vm.variables.get('subscribers', [])
        for sub in subs:
            vm.send_to(sub, "OP_CONSTANT", event_value)
    
    event_bus.define_new_instruction("OP_PUBLISH", publish_event)
    event_bus.variables['subscribers'] = list(subscribers.values())
    
    # Publish event
    print("Publishing event: 404")
    event_bus.send("OP_CONSTANT", 404, "OP_PUBLISH")
    while event_bus.handle_message():
        pass
    
    # All subscribers receive event
    for name, sub in subscribers.items():
        while sub.handle_message():
            pass
        print(f"{name.capitalize()} received: {sub.top()}")
    
    print("✓ Event bus published to all subscribers")


def example9_bidirectional():
    """Example 9: Bidirectional communication."""
    print("\n=== Example 9: Bidirectional Communication ===")
    
    alice = VMActor()
    bob = VMActor()
    
    # Alice sends to Bob
    alice.send_to(bob, "OP_CONSTANT", 100)
    alice.send("OP_CONSTANT", 10)  # Alice's own work
    
    # Bob sends to Alice
    bob.send_to(alice, "OP_CONSTANT", 200)
    bob.send("OP_CONSTANT", 20)  # Bob's own work
    
    # Both process their messages
    while alice.handle_message():
        pass
    while bob.handle_message():
        pass
    
    print(f"Alice has: own work={alice.stack[0]}, from Bob={alice.stack[1]}")
    print(f"Bob has: own work={bob.stack[0]}, from Alice={bob.stack[1]}")
    print("✓ Actors can communicate bidirectionally")


def example10_work_pool():
    """Example 10: Work distribution across a pool."""
    print("\n=== Example 10: Worker Pool Pattern ===")
    
    coordinator = VMActor()
    worker_pool = [VMActor() for _ in range(4)]
    
    # Coordinator distributes work
    def distribute_work(vm):
        work_items = [10, 20, 30, 40, 50, 60, 70, 80]  # 8 items
        pool = vm.variables['pool']
        for i, work in enumerate(work_items):
            worker = pool[i % len(pool)]  # Round-robin
            vm.send_to(worker, "OP_CONSTANT", work)
    
    coordinator.define_new_instruction("OP_DISTRIBUTE", distribute_work)
    coordinator.variables['pool'] = worker_pool
    
    # Distribute work
    print("Distributing 8 work items to 4 workers...")
    coordinator.send("OP_DISTRIBUTE")
    while coordinator.handle_message():
        pass
    
    # Workers process their work
    for i, worker in enumerate(worker_pool):
        # Each worker sums its assigned work items
        worker.send("OP_ADD")
        while worker.handle_message():
            pass
        print(f"Worker {i+1} total: {worker.top()}")
    
    print("✓ Work distributed round-robin across pool")


if __name__ == '__main__':
    print("=" * 60)
    print("ASYNC ACTOR-TO-ACTOR MESSAGING EXAMPLES")
    print("=" * 60)
    print("\nKey Concept: send_to() is FIRE-AND-FORGET")
    print("The sender doesn't wait for a response - it sends and continues.")
    print()
    
    example1_basic_send_to()
    example2_message_chain()
    example3_broadcast()
    example4_aggregator()
    example5_request_reply()
    example6_pipeline()
    example7_supervisor()
    example8_event_bus()
    example9_bidirectional()
    example10_work_pool()
    
    print("\n" + "=" * 60)
    print("Key Takeaways:")
    print("  • send_to() is async - sender doesn't wait")
    print("  • Enables actor-to-actor communication patterns")
    print("  • Fire-and-forget semantics (like Erlang's !)")
    print("  • Actors are decoupled - sender doesn't block")
    print("  • Supports complex patterns: chains, broadcast, pub-sub, etc.")
    print("=" * 60)
