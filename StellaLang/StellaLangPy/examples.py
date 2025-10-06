"""
Example demonstrating VMActor's message-passing model.

The bytecode stream IS the message stream. Instructions are messages,
and the receive loop processes them like Erlang/Elixir actors.
"""

from src.VMActor import VMActor
import time


def example_basic_messaging():
    """Basic example: send messages and process them."""
    print("=" * 60)
    print("Example 1: Basic Message Passing")
    print("=" * 60)
    
    actor = VMActor()
    actor.start()  # Start receive loop in background
    
    # Send messages (bytecode instructions)
    print("Sending: OP_CONSTANT 5")
    actor.send("OP_CONSTANT", 5)
    
    print("Sending: OP_CONSTANT 10")
    actor.send("OP_CONSTANT", 10)
    
    print("Sending: OP_ADD")
    actor.send("OP_ADD")
    
    time.sleep(0.1)  # Wait for processing
    
    print(f"Result: {actor.top()}")  # 15
    print(f"Stack: {list(actor.stack)}")
    
    actor.stop()
    print()


def example_s_expressions():
    """Compile s-expressions and send as messages."""
    print("=" * 60)
    print("Example 2: S-Expression Compilation")
    print("=" * 60)
    
    actor = VMActor()
    
    # Compile s-expression
    sexpr = "(+ (* 3 4) (- 20 10))"
    print(f"S-Expression: {sexpr}")
    
    bytecode = actor.s_expression_to_bytecode(sexpr)
    print(f"Compiled to: {bytecode}")
    
    # Start actor and send messages
    actor.start()
    actor.send(*bytecode)
    
    time.sleep(0.1)
    
    print(f"Result: {actor.top()}")  # 22
    print(f"Calculation: (3*4) + (20-10) = 12 + 10 = 22")
    
    actor.stop()
    print()


def example_variables():
    """Define and use variables via messages."""
    print("=" * 60)
    print("Example 3: Variable Operations")
    print("=" * 60)
    
    actor = VMActor()
    actor.start()
    
    # Define variable
    define_sexpr = "(define answer 42)"
    print(f"Defining: {define_sexpr}")
    actor.send(*actor.s_expression_to_bytecode(define_sexpr))
    time.sleep(0.05)
    
    # Use variable
    use_sexpr = "(+ answer 8)"
    print(f"Using: {use_sexpr}")
    actor.send(*actor.s_expression_to_bytecode(use_sexpr))
    time.sleep(0.05)
    
    print(f"Variables: {actor.variables}")
    print(f"Result: {actor.top()}")
    
    actor.stop()
    print()


def example_incremental():
    """Send messages incrementally while actor runs."""
    print("=" * 60)
    print("Example 4: Incremental Message Sending")
    print("=" * 60)
    
    actor = VMActor()
    actor.start()
    
    print("Computing: (5 + 10) * 2 - 3")
    
    actor.send("OP_CONSTANT", 5)
    print("Sent: 5")
    time.sleep(0.02)
    
    actor.send("OP_CONSTANT", 10)
    print("Sent: 10")
    time.sleep(0.02)
    
    actor.send("OP_ADD")
    print("Sent: ADD → stack now has 15")
    time.sleep(0.02)
    
    actor.send("OP_CONSTANT", 2)
    print("Sent: 2")
    time.sleep(0.02)
    
    actor.send("OP_MULTIPLY")
    print("Sent: MULTIPLY → stack now has 30")
    time.sleep(0.02)
    
    actor.send("OP_CONSTANT", 3)
    print("Sent: 3")
    time.sleep(0.02)
    
    actor.send("OP_SUBTRACT")
    print("Sent: SUBTRACT → final result")
    time.sleep(0.02)
    
    print(f"Final result: {actor.top()}")
    
    actor.stop()
    print()


def example_custom_instruction():
    """Define and use a custom instruction."""
    print("=" * 60)
    print("Example 5: Custom Instructions")
    print("=" * 60)
    
    actor = VMActor()
    
    # Define custom instruction
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    actor.define_new_instruction("OP_SQUARE", square)
    print("Defined custom instruction: OP_SQUARE")
    
    actor.start()
    
    # Use it
    print("Sending: 7, SQUARE")
    actor.send("OP_CONSTANT", 7)
    actor.send("OP_SQUARE")
    
    time.sleep(0.1)
    
    print(f"7² = {actor.top()}")
    
    actor.stop()
    print()


def example_complex_expression():
    """Process a complex nested expression."""
    print("=" * 60)
    print("Example 6: Complex Nested Expression")
    print("=" * 60)
    
    actor = VMActor()
    
    # Complex expression
    sexpr = "(* (+ 1 2) (- 10 (/ 8 2)))"
    print(f"Expression: {sexpr}")
    print("Breaking it down:")
    print("  (+ 1 2) → 3")
    print("  (/ 8 2) → 4")
    print("  (- 10 4) → 6")
    print("  (* 3 6) → 18")
    
    bytecode = actor.s_expression_to_bytecode(sexpr)
    
    actor.start()
    actor.send(*bytecode)
    
    time.sleep(0.1)
    
    print(f"Result: {actor.top()}")
    
    actor.stop()
    print()


if __name__ == "__main__":
    print("\n")
    print("╔" + "=" * 58 + "╗")
    print("║" + " " * 10 + "VMActor Message-Passing Examples" + " " * 15 + "║")
    print("╚" + "=" * 58 + "╝")
    print()
    
    example_basic_messaging()
    example_s_expressions()
    example_variables()
    example_incremental()
    example_custom_instruction()
    example_complex_expression()
    
    print("=" * 60)
    print("All examples completed!")
    print("=" * 60)
