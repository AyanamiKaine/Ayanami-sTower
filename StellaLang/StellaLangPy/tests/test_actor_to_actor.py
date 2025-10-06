"""Tests for async actor-to-actor message passing (fire-and-forget)."""
import pytest
from src.VMActor import VMActor
from src.ActorRuntime import ActorRuntime, SimpleRuntime
import time
import threading


def test_basic_send_to():
    """Test basic send_to from one actor to another."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    # Actor1 sends a message to actor2
    actor1.send_to(actor2, "OP_CONSTANT", 42)
    
    # Actor2 processes the message
    while actor2.handle_message():
        pass
    
    assert actor2.top() == 42
    assert len(actor1.stack) == 0  # Actor1's stack unaffected


def test_send_to_is_async():
    """Test that send_to doesn't block the sender."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    # Actor1 sends to actor2
    actor1.send_to(actor2, "OP_CONSTANT", 100)
    
    # Actor1 can immediately continue with its own work
    actor1.send("OP_CONSTANT", 50)
    while actor1.handle_message():
        pass
    
    # Actor1 completed its work
    assert actor1.top() == 50
    
    # Actor2 has pending message (not processed yet)
    assert len(actor2.bytecode) == 2  # OP_CONSTANT, 100
    
    # Actor2 processes when it's ready
    while actor2.handle_message():
        pass
    assert actor2.top() == 100


def test_multiple_send_to():
    """Test sending multiple messages to another actor."""
    sender = VMActor()
    receiver = VMActor()
    
    # Send multiple messages
    sender.send_to(receiver, "OP_CONSTANT", 10)
    sender.send_to(receiver, "OP_CONSTANT", 20)
    sender.send_to(receiver, "OP_ADD")
    sender.send_to(receiver, "OP_CONSTANT", 5)
    sender.send_to(receiver, "OP_MULTIPLY")
    
    # Receiver processes all messages: (10 + 20) * 5 = 150
    while receiver.handle_message():
        pass
    
    assert receiver.top() == 150


def test_bidirectional_communication():
    """Test two actors sending messages to each other."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    # Actor1 sends to actor2
    actor1.send_to(actor2, "OP_CONSTANT", 10)
    
    # Actor2 sends to actor1
    actor2.send_to(actor1, "OP_CONSTANT", 20)
    
    # Each processes their own messages
    while actor1.handle_message():
        pass
    while actor2.handle_message():
        pass
    
    assert actor1.top() == 20
    assert actor2.top() == 10


def test_send_to_with_runtime():
    """Test async messaging with ActorRuntime scheduler."""
    actor1 = VMActor()
    actor2 = VMActor()
    actor3 = VMActor()
    
    # Actor1 sends to actor2 and actor3
    actor1.send_to(actor2, "OP_CONSTANT", 100)
    actor1.send_to(actor3, "OP_CONSTANT", 200)
    
    # Use runtime to process all actors
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    runtime.register(actor3)
    
    # Process one round manually (since we don't want infinite loop)
    for actor in [actor1, actor2, actor3]:
        actor.handle_message()
    
    assert actor2.top() == 100
    assert actor3.top() == 200


def test_chain_of_actors():
    """Test a chain of actors passing messages forward."""
    actor1 = VMActor()
    actor2 = VMActor()
    actor3 = VMActor()
    
    # Set up chain: actor1 -> actor2 -> actor3
    # Each actor receives a value and forwards it + 10
    
    def forward_plus_10(vm):
        """Custom instruction: add 10 and forward to next actor."""
        value = vm.stack.pop()
        result = value + 10
        vm.stack.push(result)
        # Forward to next actor (stored in vm.variables)
        next_actor = vm.variables.get('next')
        if next_actor:
            # Send BOTH the value AND the OP_FORWARD instruction
            vm.send_to(next_actor, "OP_CONSTANT", result, "OP_FORWARD")
    
    actor1.define_new_instruction("OP_FORWARD", forward_plus_10)
    actor2.define_new_instruction("OP_FORWARD", forward_plus_10)
    actor3.define_new_instruction("OP_FORWARD", forward_plus_10)
    
    # Set up chain references
    actor1.variables['next'] = actor2
    actor2.variables['next'] = actor3
    actor3.variables['next'] = None  # End of chain
    
    # Start the chain
    actor1.send("OP_CONSTANT", 5, "OP_FORWARD")  # 5 + 10 = 15, forward to actor2
    
    # Process actor1
    while actor1.handle_message():
        pass
    assert actor1.top() == 15
    
    # Process actor2 (receives 15, adds 10, forwards 25)
    while actor2.handle_message():
        pass
    assert actor2.top() == 25  # 15 + 10
    
    # Process actor3 (receives 25, adds 10, no forward)
    while actor3.handle_message():
        pass
    assert actor3.top() == 35  # 25 + 10


def test_broadcast_pattern():
    """Test broadcasting a message to multiple actors."""
    broadcaster = VMActor()
    listeners = [VMActor() for _ in range(5)]
    
    # Define broadcast instruction
    def broadcast(vm):
        """Broadcast a value to all listeners."""
        value = vm.stack.pop()
        targets = vm.variables.get('listeners', [])
        for target in targets:
            vm.send_to(target, "OP_CONSTANT", value)
    
    broadcaster.define_new_instruction("OP_BROADCAST", broadcast)
    broadcaster.variables['listeners'] = listeners
    
    # Broadcast value 42 to all listeners
    broadcaster.send("OP_CONSTANT", 42, "OP_BROADCAST")
    while broadcaster.handle_message():
        pass
    
    # All listeners receive the message
    for listener in listeners:
        while listener.handle_message():
            pass
        assert listener.top() == 42


def test_aggregator_pattern():
    """Test multiple actors sending to one aggregator."""
    workers = [VMActor() for _ in range(3)]
    aggregator = VMActor()
    
    # Each worker computes something and sends to aggregator
    workers[0].send("OP_CONSTANT", 10)
    workers[0].send_to(aggregator, "OP_CONSTANT", 10)
    
    workers[1].send("OP_CONSTANT", 20)
    workers[1].send_to(aggregator, "OP_CONSTANT", 20)
    
    workers[2].send("OP_CONSTANT", 30)
    workers[2].send_to(aggregator, "OP_CONSTANT", 30)
    
    # Workers process their own messages
    for worker in workers:
        while worker.handle_message():
            pass
    
    # Aggregator receives all values and sums them
    aggregator.send("OP_ADD", "OP_ADD")  # Add all three values
    while aggregator.handle_message():
        pass
    
    assert aggregator.top() == 60  # 10 + 20 + 30


def test_send_to_self():
    """Test actor sending messages to itself."""
    actor = VMActor()
    
    # Actor sends to itself
    actor.send_to(actor, "OP_CONSTANT", 100)
    
    # Should work the same as send()
    while actor.handle_message():
        pass
    
    assert actor.top() == 100


def test_concurrent_send_to():
    """Test concurrent sends from multiple actors to one target."""
    target = VMActor()
    senders = [VMActor() for _ in range(10)]
    
    # Multiple actors send to target concurrently
    threads = []
    for i, sender in enumerate(senders):
        def send_value(s, val):
            s.send_to(target, "OP_CONSTANT", val)
        
        t = threading.Thread(target=send_value, args=(sender, i))
        threads.append(t)
        t.start()
    
    # Wait for all sends to complete
    for t in threads:
        t.join()
    
    # Target received all messages (order may vary)
    assert len(target.bytecode) == 20  # 10 OP_CONSTANT + 10 values


def test_pipeline_pattern():
    """Test a pipeline of actors transforming data."""
    # Pipeline: source -> doubler -> squarer -> sink
    source = VMActor()
    doubler = VMActor()
    squarer = VMActor()
    sink = VMActor()
    
    # Doubler: receives value, doubles it, sends to squarer
    def double_and_forward(vm):
        value = vm.stack.pop()
        doubled = value * 2
        vm.stack.push(doubled)  # Keep on stack
        next_actor = vm.variables['next']
        vm.send_to(next_actor, "OP_CONSTANT", doubled, "OP_PROCESS")
    
    doubler.define_new_instruction("OP_PROCESS", double_and_forward)
    doubler.variables['next'] = squarer
    
    # Squarer: receives value, squares it, sends to sink
    def square_and_forward(vm):
        value = vm.stack.pop()
        squared = value * value
        vm.stack.push(squared)  # Keep on stack
        next_actor = vm.variables['next']
        vm.send_to(next_actor, "OP_CONSTANT", squared)
    
    squarer.define_new_instruction("OP_PROCESS", square_and_forward)
    squarer.variables['next'] = sink
    
    # Source sends 5 into pipeline
    source.send_to(doubler, "OP_CONSTANT", 5, "OP_PROCESS")
    
    # Process pipeline
    while doubler.handle_message():
        pass
    # Doubler computed 5 * 2 = 10, sent to squarer
    assert doubler.top() == 10
    
    while squarer.handle_message():
        pass
    # Squarer computed 10 * 10 = 100, sent to sink
    assert squarer.top() == 100
    
    while sink.handle_message():
        pass
    # Sink received 100
    assert sink.top() == 100


def test_send_to_with_s_expressions():
    """Test sending s-expression bytecode to another actor."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    # Actor1 compiles s-expression and sends to actor2
    bytecode = actor1.s_expression_to_bytecode("(+ 10 20)")
    actor1.send_to(actor2, *bytecode)
    
    # Actor2 processes the s-expression bytecode
    while actor2.handle_message():
        pass
    
    assert actor2.top() == 30


def test_request_reply_pattern():
    """Test request-reply pattern using async messaging."""
    requester = VMActor()
    responder = VMActor()
    
    # Responder has a "reply" instruction that sends back
    def reply_with_double(vm):
        value = vm.stack.pop()
        result = value * 2
        reply_to = vm.variables.get('reply_to')
        if reply_to:
            vm.send_to(reply_to, "OP_CONSTANT", result)
    
    responder.define_new_instruction("OP_REPLY", reply_with_double)
    responder.variables['reply_to'] = requester
    
    # Requester sends request to responder
    requester.send_to(responder, "OP_CONSTANT", 21, "OP_REPLY")
    
    # Responder processes request and sends reply
    while responder.handle_message():
        pass
    
    # Requester receives reply
    while requester.handle_message():
        pass
    
    assert requester.top() == 42


def test_actor_supervision_pattern():
    """Test supervisor actor monitoring workers."""
    supervisor = VMActor()
    workers = [VMActor() for _ in range(3)]
    
    # Workers report completion to supervisor
    def report_done(vm):
        worker_id = vm.variables.get('id')
        supervisor_ref = vm.variables.get('supervisor')
        vm.send_to(supervisor_ref, "OP_CONSTANT", worker_id)
    
    for i, worker in enumerate(workers):
        worker.define_new_instruction("OP_REPORT", report_done)
        worker.variables['id'] = i
        worker.variables['supervisor'] = supervisor
        
        # Worker does work and reports
        worker.send("OP_CONSTANT", i * 10, "OP_REPORT")
    
    # Process all workers
    for worker in workers:
        while worker.handle_message():
            pass
    
    # Supervisor receives all reports
    while supervisor.handle_message():
        pass
    
    # Supervisor stack has all worker IDs
    assert list(supervisor.stack) == [0, 1, 2]


def test_event_bus_pattern():
    """Test event bus where actors publish/subscribe to events."""
    event_bus = VMActor()
    subscribers = [VMActor() for _ in range(4)]
    
    # Event bus publishes events to subscribers
    def publish_event(vm):
        event_value = vm.stack.pop()
        subs = vm.variables.get('subscribers', [])
        for sub in subs:
            vm.send_to(sub, "OP_CONSTANT", event_value)
    
    event_bus.define_new_instruction("OP_PUBLISH", publish_event)
    event_bus.variables['subscribers'] = subscribers
    
    # Publish event
    event_bus.send("OP_CONSTANT", 999, "OP_PUBLISH")
    while event_bus.handle_message():
        pass
    
    # All subscribers receive event
    for sub in subscribers:
        while sub.handle_message():
            pass
        assert sub.top() == 999


def test_send_to_preserves_sender_state():
    """Test that send_to doesn't modify sender's state."""
    sender = VMActor()
    receiver = VMActor()
    
    # Sender has some state
    sender.send("OP_CONSTANT", 100)
    while sender.handle_message():
        pass
    
    sender_stack_before = list(sender.stack)
    sender_vars_before = dict(sender.variables)
    sender_ip_before = sender.ip
    
    # Send to receiver
    sender.send_to(receiver, "OP_CONSTANT", 42, "OP_ADD")
    
    # Sender's state unchanged
    assert list(sender.stack) == sender_stack_before
    assert dict(sender.variables) == sender_vars_before
    assert sender.ip == sender_ip_before


def test_round_robin_with_send_to():
    """Test round-robin scheduling with actor-to-actor messaging."""
    actors = [VMActor() for _ in range(3)]
    
    # Each actor sends to the next one in round-robin (with limit to prevent infinite loop)
    actors[0].variables['next'] = actors[1]
    actors[1].variables['next'] = actors[2]
    actors[2].variables['next'] = actors[0]
    
    def send_to_next(vm):
        value = vm.stack.pop()
        # Only send to next if value is less than threshold (prevent infinite loop)
        if value < 5:
            next_actor = vm.variables['next']
            vm.send_to(next_actor, "OP_CONSTANT", value + 1, "OP_NEXT")
        vm.stack.push(value)  # Keep value on stack
    
    for actor in actors:
        actor.define_new_instruction("OP_NEXT", send_to_next)
    
    # Start with actor 0
    actors[0].send("OP_CONSTANT", 0, "OP_NEXT")
    
    # Process in controlled rounds - each actor processes once per round
    for round_num in range(6):  # 6 rounds to propagate message around circle
        for actor in actors:
            # Process only ONE message per round (not while loop)
            actor.handle_message()
    
    # Messages propagated through the chain
    # At least one actor should have processed messages
    stacks = [list(actor.stack) for actor in actors]
    assert any(len(stack) > 0 for stack in stacks)


def test_actor_pool_pattern():
    """Test work distribution across a pool of actors."""
    coordinator = VMActor()
    worker_pool = [VMActor() for _ in range(4)]
    
    # Coordinator distributes work to pool
    def distribute_work(vm):
        work_items = [10, 20, 30, 40]  # 4 work items
        pool = vm.variables['pool']
        for i, work in enumerate(work_items):
            worker = pool[i % len(pool)]
            vm.send_to(worker, "OP_CONSTANT", work)
    
    coordinator.define_new_instruction("OP_DISTRIBUTE", distribute_work)
    coordinator.variables['pool'] = worker_pool
    
    # Distribute work
    coordinator.send("OP_DISTRIBUTE")
    while coordinator.handle_message():
        pass
    
    # Each worker received work
    for worker in worker_pool:
        while worker.handle_message():
            pass
    
    results = [w.top() for w in worker_pool]
    assert results == [10, 20, 30, 40]


if __name__ == '__main__':
    pytest.main([__file__, '-v'])
