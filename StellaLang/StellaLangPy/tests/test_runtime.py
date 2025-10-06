"""Tests for ActorRuntime - external loop control."""
import time
import pytest
from src.VMActor import VMActor
from src.ActorRuntime import ActorRuntime, SimpleRuntime, priority_loop


def test_handle_message_processes_one():
    """Test that handle_message processes exactly one message."""
    vm = VMActor()
    vm.send("OP_CONSTANT", 5)
    vm.send("OP_CONSTANT", 10)
    
    # Process one message
    result = vm.handle_message()
    assert result == True
    assert len(vm.stack) == 1
    assert vm.stack[0] == 5
    
    # Process another
    result = vm.handle_message()
    assert result == True
    assert len(vm.stack) == 2
    assert vm.stack[1] == 10


def test_handle_message_returns_false_when_empty():
    """Test that handle_message returns False when no messages."""
    vm = VMActor()
    result = vm.handle_message()
    assert result == False


def test_external_loop_control():
    """Test defining your own loop externally."""
    vm = VMActor()
    vm.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    
    # Define custom loop
    messages_processed = 0
    while vm.handle_message():
        messages_processed += 1
        if messages_processed > 100:  # Safety limit
            break
    
    assert vm.top() == 15
    assert messages_processed == 3


def test_simple_runtime_loop_until_empty():
    """Test SimpleRuntime loop_until_empty."""
    actor = VMActor()
    actor.send("OP_CONSTANT", 7, "OP_CONSTANT", 3, "OP_ADD")
    
    runtime = SimpleRuntime(actor)
    runtime.loop_until_empty()
    
    assert actor.top() == 10


def test_simple_runtime_loop_n_messages():
    """Test SimpleRuntime loop_n_messages."""
    actor = VMActor()
    actor.send("OP_CONSTANT", 1)
    actor.send("OP_CONSTANT", 2)
    actor.send("OP_CONSTANT", 3)
    actor.send("OP_CONSTANT", 4)
    
    runtime = SimpleRuntime(actor)
    runtime.loop_n_messages(2)
    
    assert len(actor.stack) == 2
    assert actor.stack == [1, 2]


def test_simple_runtime_background():
    """Test SimpleRuntime in background thread."""
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    runtime.start(blocking=False)
    
    try:
        actor.send("OP_CONSTANT", 42)
        actor.send("OP_CONSTANT", 8)
        actor.send("OP_ADD")
        
        time.sleep(0.1)
        
        assert actor.top() == 50
    finally:
        runtime.stop()


def test_actor_runtime_simple_loop():
    """Test ActorRuntime with simple round-robin loop."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    
    runtime.start(loop_type='simple', blocking=False)
    
    try:
        # Send to both actors
        actor1.send("OP_CONSTANT", 10)
        actor2.send("OP_CONSTANT", 20)
        
        time.sleep(0.1)
        
        assert actor1.top() == 10
        assert actor2.top() == 20
    finally:
        runtime.stop()


def test_actor_runtime_greedy_loop():
    """Test ActorRuntime with greedy loop."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    
    # Pre-load messages
    actor1.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    actor2.send("OP_CONSTANT", 3, "OP_CONSTANT", 7, "OP_MULTIPLY")
    
    runtime.start(loop_type='greedy', blocking=False)
    
    try:
        time.sleep(0.1)
        
        assert actor1.top() == 15
        assert actor2.top() == 21
    finally:
        runtime.stop()


def test_actor_runtime_fair_loop():
    """Test ActorRuntime with fair scheduling."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    
    runtime.start(loop_type='fair', blocking=False, messages_per_actor=2)
    
    try:
        # Send multiple messages
        for i in range(10):
            actor1.send("OP_CONSTANT", i)
            actor2.send("OP_CONSTANT", i * 2)
        
        time.sleep(0.2)
        
        assert len(actor1.stack) == 10
        assert len(actor2.stack) == 10
    finally:
        runtime.stop()


def test_custom_loop_function():
    """Test using a custom loop function."""
    actor = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor)
    
    # Custom loop that processes with delay
    def slow_loop(rt, actors):
        for a in actors:
            if a.handle_message():
                time.sleep(0.01)
    
    runtime.start(loop_type=slow_loop, blocking=False)
    
    try:
        actor.send("OP_CONSTANT", 100)
        time.sleep(0.05)
        
        assert actor.top() == 100
    finally:
        runtime.stop()


def test_batch_processing_pattern():
    """Test batch processing pattern."""
    actor = VMActor()
    for i in range(100):
        actor.send("OP_CONSTANT", i)
    
    # Process in batches of 10
    batch_size = 10
    batches_processed = 0
    
    while True:
        batch_count = 0
        while batch_count < batch_size and actor.handle_message():
            batch_count += 1
        
        if batch_count == 0:
            break
        
        batches_processed += 1
    
    assert len(actor.stack) == 100
    assert batches_processed == 10


def test_conditional_processing():
    """Test processing messages conditionally."""
    actor = VMActor()
    
    # Send arithmetic operations
    actor.send("OP_CONSTANT", 10)
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_ADD")
    actor.send("OP_CONSTANT", 3)
    actor.send("OP_MULTIPLY")
    
    # Process until stack has specific value
    while actor.handle_message():
        if len(actor.stack) > 0 and actor.top() == 15:
            # Stop processing when we hit 15 (after ADD)
            break
    
    assert actor.top() == 15
    
    # Continue processing remaining messages
    while actor.handle_message():
        pass
    
    assert actor.top() == 45  # 15 * 3


def test_s_expression_with_external_loop():
    """Test s-expression compilation with external loop control."""
    actor = VMActor()
    
    sexpr = "(+ (* 2 3) (- 10 5))"
    bytecode = actor.s_expression_to_bytecode(sexpr)
    
    actor.send(*bytecode)
    
    # Use simple runtime
    runtime = SimpleRuntime(actor)
    runtime.loop_until_empty()
    
    assert actor.top() == 11


def test_multiple_actors_different_speeds():
    """Test multiple actors processing at different rates."""
    fast_actor = VMActor()
    slow_actor = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(fast_actor)
    runtime.register(slow_actor)
    
    # Custom loop: process fast_actor more frequently
    def uneven_loop(rt, actors):
        # Process fast_actor 3 times
        for _ in range(3):
            fast_actor.handle_message()
        # Process slow_actor once
        slow_actor.handle_message()
        time.sleep(0.001)
    
    runtime.start(loop_type=uneven_loop, blocking=False)
    
    try:
        # Send same number of messages to both
        for i in range(10):
            fast_actor.send("OP_CONSTANT", i)
            slow_actor.send("OP_CONSTANT", i)
        
        time.sleep(0.1)
        
        # Fast actor should have processed more
        assert len(fast_actor.stack) >= len(slow_actor.stack)
    finally:
        runtime.stop()


if __name__ == '__main__':
    pytest.main([__file__, '-v'])
