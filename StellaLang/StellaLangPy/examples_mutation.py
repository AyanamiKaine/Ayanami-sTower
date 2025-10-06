"""Examples demonstrating dynamic mutation of actor capabilities.

This shows how to add new instruction types (message handlers) to actors
at runtime, enabling actors to handle new types of messages dynamically.
"""

from src.VMActor import VMActor
from src.ActorRuntime import SimpleRuntime


def example1_add_custom_instruction():
    """Example 1: Add a custom instruction to an actor."""
    print("\n=== Example 1: Add Custom Instruction ===")
    
    actor = VMActor()
    
    # Define a new instruction that squares a number
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    # Add the new instruction to the actor
    actor.define_new_instruction("OP_SQUARE", square)
    
    # Now the actor can handle SQUARE messages
    actor.send("OP_CONSTANT", 7)
    actor.send("OP_SQUARE")
    
    while actor.handle_message():
        pass
    
    print(f"7 squared = {actor.top()}")
    print("✓ Actor learned to handle OP_SQUARE messages")


def example2_multiple_custom_instructions():
    """Example 2: Add multiple specialized instructions."""
    print("\n=== Example 2: Multiple Custom Instructions ===")
    
    actor = VMActor()
    
    # Add several math operations
    def cube(vm):
        value = vm.stack.pop()
        vm.stack.push(value ** 3)
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    def factorial(vm):
        n = vm.stack.pop()
        result = 1
        for i in range(1, n + 1):
            result *= i
        vm.stack.push(result)
    
    actor.define_new_instruction("OP_CUBE", cube)
    actor.define_new_instruction("OP_DOUBLE", double)
    actor.define_new_instruction("OP_FACTORIAL", factorial)
    
    # Use them: 5! = 120, cube it = 1,728,000, double it = 3,456,000
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_FACTORIAL")  # 120
    actor.send("OP_CUBE")       # 1,728,000
    actor.send("OP_DOUBLE")     # 3,456,000
    
    while actor.handle_message():
        pass
    
    print(f"5! cubed and doubled = {actor.top():,}")
    print("✓ Actor can handle multiple custom message types")


def example3_replace_instruction_behavior():
    """Example 3: Replace existing instruction with new behavior."""
    print("\n=== Example 3: Replace Instruction Behavior ===")
    
    actor = VMActor()
    
    # Original ADD behavior
    actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    while actor.handle_message():
        pass
    print(f"Original ADD: 5 + 10 = {actor.top()}")
    
    # Replace ADD with multiplication
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
    print(f"Replaced ADD: 5 'add' 10 = {actor.top()}")
    print("✓ Instruction behavior can be redefined at runtime")


def example4_instruction_with_side_effects():
    """Example 4: Add instruction that has side effects."""
    print("\n=== Example 4: Instruction with Side Effects ===")
    
    actor = VMActor()
    
    # Create a logging instruction
    log = []
    
    def log_stack(vm):
        """Log current stack state."""
        log.append(list(vm.stack))
    
    actor.define_new_instruction("OP_LOG", log_stack)
    
    # Build computation with logging checkpoints
    actor.send("OP_CONSTANT", 10)
    actor.send("OP_LOG")        # Checkpoint 1
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_LOG")        # Checkpoint 2
    actor.send("OP_ADD")
    actor.send("OP_LOG")        # Checkpoint 3
    actor.send("OP_CONSTANT", 3)
    actor.send("OP_MULTIPLY")
    actor.send("OP_LOG")        # Checkpoint 4
    
    while actor.handle_message():
        pass
    
    print("Stack evolution:")
    for i, snapshot in enumerate(log, 1):
        print(f"  Checkpoint {i}: {snapshot}")
    print(f"Final result: {actor.top()}")
    print("✓ Instructions can observe and log VM state")


def example5_specialization_pattern():
    """Example 5: Specialized actors for different domains."""
    print("\n=== Example 5: Actor Specialization ===")
    
    # Create a math-specialized actor
    math_actor = VMActor()
    
    def power(vm):
        exp = vm.stack.pop()
        base = vm.stack.pop()
        vm.stack.push(base ** exp)
    
    def modulo(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(a % b)
    
    math_actor.define_new_instruction("OP_POWER", power)
    math_actor.define_new_instruction("OP_MOD", modulo)
    
    # Create a string-specialized actor
    string_actor = VMActor()
    
    def concat(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(str(a) + str(b))
    
    def uppercase(vm):
        s = vm.stack.pop()
        vm.stack.push(str(s).upper())
    
    string_actor.define_new_instruction("OP_CONCAT", concat)
    string_actor.define_new_instruction("OP_UPPER", uppercase)
    
    # Math actor does math
    math_actor.send("OP_CONSTANT", 2, "OP_CONSTANT", 10, "OP_POWER")  # 2^10 = 1024
    math_actor.send("OP_CONSTANT", 100, "OP_MOD")  # 1024 % 100 = 24
    while math_actor.handle_message():
        pass
    
    # String actor does strings
    string_actor.send("OP_CONSTANT", "hello", "OP_CONSTANT", "world", "OP_CONCAT")
    string_actor.send("OP_UPPER")
    while string_actor.handle_message():
        pass
    
    print(f"Math actor result: {math_actor.top()}")
    print(f"String actor result: {string_actor.top()}")
    print("✓ Different actors can be specialized for different domains")


def example6_runtime_capability_extension():
    """Example 6: Add capabilities to running actor."""
    print("\n=== Example 6: Runtime Capability Extension ===")
    
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    
    # Start with basic operations
    actor.send("OP_CONSTANT", 10)
    runtime.loop_until_empty()
    print(f"Started with: {actor.top()}")
    
    # Add new capability while actor exists
    def triple(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 3)
    
    actor.define_new_instruction("OP_TRIPLE", triple)
    print("✓ Added OP_TRIPLE capability to running actor")
    
    # Use the new capability
    actor.send("OP_TRIPLE")
    runtime.loop_until_empty()
    print(f"After tripling: {actor.top()}")
    
    # Add another capability
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value ** 2)
    
    actor.define_new_instruction("OP_SQUARE", square)
    print("✓ Added OP_SQUARE capability")
    
    # Chain the new capabilities
    actor.send("OP_SQUARE")  # 30^2 = 900
    runtime.loop_until_empty()
    print(f"After squaring: {actor.top()}")
    print("✓ Actors can learn new message types at runtime")


def example7_instruction_that_sends_messages():
    """Example 7: Instruction that sends more messages."""
    print("\n=== Example 7: Meta-Instruction (Sends Messages) ===")
    
    actor = VMActor()
    
    def explode_number(vm):
        """Pop a number and push each digit as separate messages."""
        number = vm.stack.pop()
        digits = [int(d) for d in str(number)]
        print(f"Exploding {number} into digits: {digits}")
        for digit in digits:
            vm.send("OP_CONSTANT", digit)
        # Also send ADD instructions to sum them
        for _ in range(len(digits) - 1):
            vm.send("OP_ADD")
    
    actor.define_new_instruction("OP_EXPLODE", explode_number)
    
    # Explode a number into its digits and sum them
    actor.send("OP_CONSTANT", 12345)
    actor.send("OP_EXPLODE")  # This will push 1,2,3,4,5 and send 4 ADDs
    
    while actor.handle_message():
        pass
    
    print(f"Sum of digits of 12345: {actor.top()}")
    print("✓ Instructions can send new messages to the actor")


def example8_composition_pattern():
    """Example 8: Compose complex operations from simpler ones."""
    print("\n=== Example 8: Instruction Composition ===")
    
    actor = VMActor()
    
    # Define atomic operations
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    # Define composed operation: f(x) = 2x^2 (square then double)
    def square_and_double(vm):
        """Composite operation: square then double."""
        # We could call the handlers directly
        square(vm)
        double(vm)
    
    actor.define_new_instruction("OP_SQUARE", square)
    actor.define_new_instruction("OP_DOUBLE", double)
    actor.define_new_instruction("OP_SQUARE_DOUBLE", square_and_double)
    
    # Use atomic operations
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_SQUARE")   # 25
    actor.send("OP_DOUBLE")   # 50
    while actor.handle_message():
        pass
    print(f"Atomic operations: 5 squared then doubled = {actor.top()}")
    
    # Use composite operation
    actor.stack.clear()
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_SQUARE_DOUBLE")  # Does both at once
    while actor.handle_message():
        pass
    print(f"Composite operation: 5 square-doubled = {actor.top()}")
    print("✓ Complex operations can be built from simpler ones")


if __name__ == '__main__':
    print("=" * 60)
    print("DYNAMIC ACTOR MUTATION EXAMPLES")
    print("=" * 60)
    print("\nKey Concept: Actors can learn to handle new message types")
    print("by adding custom instructions at runtime.")
    print()
    
    example1_add_custom_instruction()
    example2_multiple_custom_instructions()
    example3_replace_instruction_behavior()
    example4_instruction_with_side_effects()
    example5_specialization_pattern()
    example6_runtime_capability_extension()
    example7_instruction_that_sends_messages()
    example8_composition_pattern()
    
    print("\n" + "=" * 60)
    print("Key Takeaway: Instructions ARE message handlers!")
    print("Adding instructions = Teaching actors new message types")
    print("This enables:")
    print("  • Runtime capability extension")
    print("  • Actor specialization for different domains")
    print("  • Dynamic behavior modification")
    print("  • Meta-programming (instructions that add instructions)")
    print("=" * 60)
