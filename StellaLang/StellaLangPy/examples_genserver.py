"""
Example demonstrating GenServer-style actor model.

The VMActor doesn't own the loop - you define loop behavior externally
just like Erlang's GenServer where OTP owns the scheduling.
"""

from src.VMActor import VMActor
from src.ActorRuntime import ActorRuntime, SimpleRuntime
import time


def example_external_loop_control():
    """Example: You control the loop, not the actor."""
    print("=" * 60)
    print("Example 1: External Loop Control (You Own the Loop)")
    print("=" * 60)
    
    actor = VMActor()
    actor.send("OP_CONSTANT", 10, "OP_CONSTANT", 5, "OP_ADD")
    
    print("Processing messages with custom loop:")
    messages_processed = 0
    while actor.handle_message():
        messages_processed += 1
        print(f"  Processed message #{messages_processed}")
    
    print(f"Result: {actor.top()}")
    print()


def example_simple_runtime():
    """Example: Using SimpleRuntime for basic scheduling."""
    print("=" * 60)
    print("Example 2: SimpleRuntime - Basic Scheduling")
    print("=" * 60)
    
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    
    # Different loop strategies
    print("Strategy 1: Process until empty")
    actor.send("OP_CONSTANT", 7, "OP_CONSTANT", 3, "OP_MULTIPLY")
    runtime.loop_until_empty()
    print(f"  Result: {actor.top()}")  # 21
    
    print("\nStrategy 2: Process exactly N messages")
    actor.stack.clear()  # Clear stack
    actor.send("OP_CONSTANT", 1, "OP_CONSTANT", 2, "OP_CONSTANT", 3, "OP_CONSTANT", 4)
    runtime.loop_n_messages(2)
    print(f"  Processed 2 messages, stack: {list(actor.stack)}")  # [1, 2]
    
    # Process the rest
    runtime.loop_until_empty()
    print(f"  After processing rest, stack: {list(actor.stack)}")  # [1, 2, 3, 4]
    print()


def example_background_processing():
    """Example: Background processing with SimpleRuntime."""
    print("=" * 60)
    print("Example 3: Background Processing")
    print("=" * 60)
    
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    
    # Start runtime in background
    runtime.start(blocking=False)
    
    try:
        print("Runtime running in background...")
        
        # Send messages incrementally
        print("Sending: 5")
        actor.send("OP_CONSTANT", 5)
        time.sleep(0.05)
        
        print("Sending: 10")
        actor.send("OP_CONSTANT", 10)
        time.sleep(0.05)
        
        print("Sending: ADD")
        actor.send("OP_ADD")
        time.sleep(0.05)
        
        print(f"Result: {actor.top()}")
    finally:
        runtime.stop()
    
    print()


def example_multiple_actors():
    """Example: Multiple actors with ActorRuntime."""
    print("=" * 60)
    print("Example 4: Multiple Actors - Round Robin Scheduling")
    print("=" * 60)
    
    actor1 = VMActor()
    actor2 = VMActor()
    actor3 = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    runtime.register(actor3)
    
    runtime.start(loop_type='simple', blocking=False)
    
    try:
        print("Sending messages to 3 different actors...")
        
        actor1.send("OP_CONSTANT", 10, "OP_CONSTANT", 2, "OP_MULTIPLY")
        print("  Actor1: 10 * 2")
        
        actor2.send("OP_CONSTANT", 15, "OP_CONSTANT", 5, "OP_SUBTRACT")
        print("  Actor2: 15 - 5")
        
        actor3.send("OP_CONSTANT", 20, "OP_CONSTANT", 4, "OP_DIVIDE")
        print("  Actor3: 20 / 4")
        
        time.sleep(0.2)
        
        print(f"\nResults:")
        print(f"  Actor1: {actor1.top()}")  # 20
        print(f"  Actor2: {actor2.top()}")  # 10
        print(f"  Actor3: {actor3.top()}")  # 5.0
    finally:
        runtime.stop()
    
    print()


def example_custom_scheduling():
    """Example: Custom scheduling strategy."""
    print("=" * 60)
    print("Example 5: Custom Scheduling Strategy")
    print("=" * 60)
    
    actor1 = VMActor()
    actor2 = VMActor()
    
    runtime = ActorRuntime()
    runtime.register(actor1)
    runtime.register(actor2)
    
    # Custom loop: process actor1 twice as often as actor2
    def priority_loop(rt, actors):
        actor1, actor2 = actors
        # Process actor1 twice
        actor1.handle_message()
        actor1.handle_message()
        # Process actor2 once
        actor2.handle_message()
        time.sleep(0.001)
    
    runtime.start(loop_type=priority_loop, blocking=False)
    
    try:
        print("Custom loop: actor1 gets 2x priority")
        
        # Send 10 messages to each
        for i in range(10):
            actor1.send("OP_CONSTANT", i)
            actor2.send("OP_CONSTANT", i * 10)
        
        time.sleep(0.2)
        
        print(f"  Actor1 processed: {len(actor1.stack)} messages")
        print(f"  Actor2 processed: {len(actor2.stack)} messages")
        print(f"  Actor1 gets priority!")
    finally:
        runtime.stop()
    
    print()


def example_batch_processing():
    """Example: Batch processing pattern."""
    print("=" * 60)
    print("Example 6: Batch Processing")
    print("=" * 60)
    
    actor = VMActor()
    
    # Send 100 constants
    for i in range(100):
        actor.send("OP_CONSTANT", i)
    
    print("Processing 100 messages in batches of 20...")
    
    batch_size = 20
    batch_num = 1
    
    while True:
        batch_count = 0
        while batch_count < batch_size and actor.handle_message():
            batch_count += 1
        
        if batch_count == 0:
            break
        
        print(f"  Batch {batch_num}: processed {batch_count} messages")
        batch_num += 1
    
    print(f"Total messages processed: {len(actor.stack)}")
    print()


def example_conditional_processing():
    """Example: Process until specific condition."""
    print("=" * 60)
    print("Example 7: Conditional Processing")
    print("=" * 60)
    
    actor = VMActor()
    
    # Compile expression: (+ (* 2 5) (* 3 4))
    sexpr = "(+ (* 2 5) (* 3 4))"
    bytecode = actor.s_expression_to_bytecode(sexpr)
    actor.send(*bytecode)
    
    print(f"Expression: {sexpr}")
    print("Processing until we see value 10 on stack...")
    
    while actor.handle_message():
        if len(actor.stack) > 0:
            top = actor.top()
            print(f"  Current top: {top}")
            if top == 10:
                print("  Found 10! Stopping early.")
                break
    
    # Continue processing
    print("\nContinuing to process remaining messages...")
    while actor.handle_message():
        pass
    
    print(f"Final result: {actor.top()}")  # 22
    print()


def example_s_expression_with_runtime():
    """Example: S-expressions with runtime."""
    print("=" * 60)
    print("Example 8: S-Expressions with External Runtime")
    print("=" * 60)
    
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    
    # Define a variable
    define_expr = "(define x 42)"
    print(f"Compiling: {define_expr}")
    actor.send(*actor.s_expression_to_bytecode(define_expr))
    
    runtime.loop_until_empty()
    print(f"  Variable x = {actor.variables['x']}")
    
    # Use the variable
    use_expr = "(* x 2)"
    print(f"\nCompiling: {use_expr}")
    actor.send(*actor.s_expression_to_bytecode(use_expr))
    
    runtime.loop_until_empty()
    print(f"  Result: {actor.top()}")  # 84
    
    print()


if __name__ == "__main__":
    print("\n")
    print("╔" + "=" * 58 + "╗")
    print("║" + " " * 8 + "GenServer-Style Actor Model Examples" + " " * 13 + "║")
    print("║" + " " * 12 + "(External Loop Control)" + " " * 23 + "║")
    print("╚" + "=" * 58 + "╝")
    print()
    
    example_external_loop_control()
    example_simple_runtime()
    example_background_processing()
    example_multiple_actors()
    example_custom_scheduling()
    example_batch_processing()
    example_conditional_processing()
    example_s_expression_with_runtime()
    
    print("=" * 60)
    print("All examples completed!")
    print("=" * 60)
    print("\nKey Takeaway:")
    print("The VMActor doesn't own the loop - YOU do!")
    print("Just like Erlang's GenServer where OTP controls scheduling.")
    print("=" * 60)
