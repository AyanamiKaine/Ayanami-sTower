"""Tests for dynamically mutating actor message handling capabilities."""
import pytest
from src.VMActor import VMActor
from src.ActorRuntime import SimpleRuntime
import time


def test_add_new_instruction_type():
    """Test adding a completely new instruction/message type."""
    actor = VMActor()
    
    # Define new instruction handler
    def handle_square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    # Add the new instruction
    actor.define_new_instruction("OP_SQUARE", handle_square)
    
    # Use the new instruction
    actor.send("OP_CONSTANT", 7, "OP_SQUARE")
    
    # Process messages
    while actor.handle_message():
        pass
    
    assert actor.top() == 49


def test_add_multiple_custom_instructions():
    """Test adding multiple custom instructions."""
    actor = VMActor()
    
    # Add cube instruction
    def cube(vm):
        value = vm.stack.pop()
        vm.stack.push(value ** 3)
    
    # Add double instruction
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    # Add increment instruction
    def increment(vm):
        value = vm.stack.pop()
        vm.stack.push(value + 1)
    
    actor.define_new_instruction("OP_CUBE", cube)
    actor.define_new_instruction("OP_DOUBLE", double)
    actor.define_new_instruction("OP_INCREMENT", increment)
    
    # Use them: (2^3) * 2 + 1 = 8 * 2 + 1 = 17
    actor.send("OP_CONSTANT", 2)
    actor.send("OP_CUBE")       # 8
    actor.send("OP_DOUBLE")     # 16
    actor.send("OP_INCREMENT")  # 17
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 17


def test_replace_existing_instruction():
    """Test replacing an existing instruction with new behavior."""
    actor = VMActor()
    
    # Original behavior: OP_ADD
    actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    while actor.handle_message():
        pass
    
    assert actor.top() == 15
    
    # Clear stack
    actor.stack.clear()
    
    # Replace OP_ADD with a different behavior (multiply instead)
    def add_as_multiply(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(a * b)
    
    actor.replace_existing_instruction("OP_ADD", add_as_multiply)
    
    # Now OP_ADD will multiply
    actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    while actor.handle_message():
        pass
    
    assert actor.top() == 50  # 5 * 10 instead of 5 + 10


def test_add_instruction_with_side_effects():
    """Test adding instruction that has side effects (logging, etc.)."""
    actor = VMActor()
    
    log = []
    
    def log_stack(vm):
        """Instruction that logs current stack state."""
        log.append(list(vm.stack))
    
    actor.define_new_instruction("OP_LOG", log_stack)
    
    # Build computation with logging
    actor.send("OP_CONSTANT", 5)
    actor.send("OP_LOG")        # Log: [5]
    actor.send("OP_CONSTANT", 10)
    actor.send("OP_LOG")        # Log: [5, 10]
    actor.send("OP_ADD")
    actor.send("OP_LOG")        # Log: [15]
    
    while actor.handle_message():
        pass
    
    assert log == [[5], [5, 10], [15]]


def test_add_instruction_that_accesses_variables():
    """Test adding instruction that interacts with variables."""
    actor = VMActor()
    
    # Add instruction to swap two variables
    def swap_vars(vm):
        """Swap values of variables 'a' and 'b'."""
        a_val = vm.variables.get('a')
        b_val = vm.variables.get('b')
        vm.variables['a'] = b_val
        vm.variables['b'] = a_val
    
    actor.define_new_instruction("OP_SWAP_AB", swap_vars)
    
    # Set up variables
    actor.send("OP_CONSTANT", 100, "OP_DEFINE_VARIABLE", "a")
    actor.send("OP_CONSTANT", 200, "OP_DEFINE_VARIABLE", "b")
    actor.send("OP_SWAP_AB")
    
    while actor.handle_message():
        pass
    
    assert actor.variables['a'] == 200
    assert actor.variables['b'] == 100


def test_add_instruction_with_parameters():
    """Test adding instruction that reads parameters from bytecode."""
    actor = VMActor()
    
    def push_n_times(vm):
        """Push a value N times. Reads N and value from bytecode."""
        n = vm.read_constant()
        value = vm.read_constant()
        for _ in range(n):
            vm.stack.push(value)
    
    actor.define_new_instruction("OP_PUSH_N", push_n_times)
    
    # Push 42 five times
    actor.send("OP_PUSH_N", 5, 42)
    
    while actor.handle_message():
        pass
    
    assert len(actor.stack) == 5
    assert all(v == 42 for v in actor.stack)


def test_add_control_flow_instruction():
    """Test adding a simple control flow instruction."""
    actor = VMActor()
    
    def skip_next_if_zero(vm):
        """Skip next 2 bytecode elements (instruction + arg) if top of stack is 0."""
        value = vm.stack.pop()
        if value == 0:
            # Skip next instruction and its argument by advancing ip by 2
            vm.ip += 2
    
    actor.define_new_instruction("OP_SKIP_IF_ZERO", skip_next_if_zero)
    
    # Test: 0 should skip the "OP_CONSTANT 99" pair
    actor.send("OP_CONSTANT", 0)
    actor.send("OP_SKIP_IF_ZERO")
    actor.send("OP_CONSTANT", 99)  # Both instruction and value will be skipped
    actor.send("OP_CONSTANT", 42)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 42
    assert len(actor.stack) == 1  # 99 was skipped


def test_add_composite_instruction():
    """Test adding instruction that combines multiple operations."""
    actor = VMActor()
    
    def sum_of_squares(vm):
        """Pop two values, push sum of their squares: a^2 + b^2"""
        b = vm.stack.pop()
        a = vm.stack.pop()
        result = (a * a) + (b * b)
        vm.stack.push(result)
    
    actor.define_new_instruction("OP_SUM_SQUARES", sum_of_squares)
    
    # 3^2 + 4^2 = 9 + 16 = 25
    actor.send("OP_CONSTANT", 3, "OP_CONSTANT", 4, "OP_SUM_SQUARES")
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 25


def test_mutate_actor_capabilities_at_runtime():
    """Test adding capabilities to running actor."""
    actor = VMActor()
    runtime = SimpleRuntime(actor)
    runtime.start(blocking=False)
    
    try:
        # Start with basic operations
        actor.send("OP_CONSTANT", 10)
        time.sleep(0.05)
        
        # Add new capability while running
        def triple(vm):
            value = vm.stack.pop()
            vm.stack.push(value * 3)
        
        actor.define_new_instruction("OP_TRIPLE", triple)
        
        # Use the new capability
        actor.send("OP_TRIPLE")
        time.sleep(0.05)
        
        assert actor.top() == 30
    finally:
        runtime.stop()


def test_error_on_duplicate_instruction():
    """Test that defining duplicate instruction raises error."""
    actor = VMActor()
    
    def dummy(vm):
        pass
    
    # Try to redefine existing instruction
    with pytest.raises(IndexError, match="instruction with the same name already exists"):
        actor.define_new_instruction("OP_ADD", dummy)


def test_error_on_replacing_nonexistent():
    """Test that replacing nonexistent instruction raises error."""
    actor = VMActor()
    
    def dummy(vm):
        pass
    
    with pytest.raises(IndexError, match="instruction with the name does not exists"):
        actor.replace_existing_instruction("OP_NONEXISTENT", dummy)


def test_chain_instruction_mutations():
    """Test chaining multiple instruction additions."""
    actor = VMActor()
    
    # Add factorial instruction
    def factorial(vm):
        n = vm.stack.pop()
        result = 1
        for i in range(1, n + 1):
            result *= i
        vm.stack.push(result)
    
    # Add modulo instruction
    def modulo(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(a % b)
    
    # Add max instruction
    def max_two(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(max(a, b))
    
    actor.define_new_instruction("OP_FACTORIAL", factorial)
    actor.define_new_instruction("OP_MOD", modulo)
    actor.define_new_instruction("OP_MAX", max_two)
    
    # 5! = 120, then 120 % 7 = 1, then max(1, 10) = 10
    actor.send("OP_CONSTANT", 5, "OP_FACTORIAL")
    actor.send("OP_CONSTANT", 7, "OP_MOD")
    actor.send("OP_CONSTANT", 10, "OP_MAX")
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 10


def test_add_instruction_with_closure():
    """Test adding instruction that captures variables in closure."""
    actor = VMActor()
    
    counter = {'count': 0}
    
    def counting_noop(vm):
        """Instruction that counts how many times it's called."""
        counter['count'] += 1
    
    actor.define_new_instruction("OP_COUNT", counting_noop)
    
    # Call it 5 times
    for _ in range(5):
        actor.send("OP_COUNT")
    
    while actor.handle_message():
        pass
    
    assert counter['count'] == 5


def test_replace_with_enhanced_version():
    """Test replacing instruction with enhanced version."""
    actor = VMActor()
    
    # Original ADD just adds
    actor.send("OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD")
    while actor.handle_message():
        pass
    assert actor.top() == 15
    
    # Replace with "checked add" that validates result
    def checked_add(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        result = a + b
        # Add validation
        if result > 100:
            raise ValueError("Result too large!")
        vm.stack.push(result)
    
    actor.replace_existing_instruction("OP_ADD", checked_add)
    
    # This should work
    actor.stack.clear()
    actor.send("OP_CONSTANT", 20, "OP_CONSTANT", 30, "OP_ADD")
    while actor.handle_message():
        pass
    assert actor.top() == 50
    
    # This should fail
    actor.stack.clear()
    actor.send("OP_CONSTANT", 60, "OP_CONSTANT", 50, "OP_ADD")
    with pytest.raises(ValueError, match="Result too large"):
        while actor.handle_message():
            pass


def test_add_instruction_that_sends_messages():
    """Test instruction that sends more messages to the actor."""
    actor = VMActor()
    
    def explode_to_digits(vm):
        """Pop a number and push each digit separately."""
        number = vm.stack.pop()
        digits = [int(d) for d in str(number)]
        for digit in digits:
            vm.send("OP_CONSTANT", digit)
    
    actor.define_new_instruction("OP_EXPLODE", explode_to_digits)
    
    # Explode 123 into 1, 2, 3
    actor.send("OP_CONSTANT", 123, "OP_EXPLODE")
    
    while actor.handle_message():
        pass
    
    # Stack should have 1, 2, 3
    assert list(actor.stack) == [1, 2, 3]


def test_different_actors_different_instructions():
    """Test that different actors can have different instruction sets."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    # Actor1 gets DOUBLE
    def double(vm):
        vm.stack.push(vm.stack.pop() * 2)
    
    actor1.define_new_instruction("OP_DOUBLE", double)
    
    # Actor2 gets TRIPLE
    def triple(vm):
        vm.stack.push(vm.stack.pop() * 3)
    
    actor2.define_new_instruction("OP_TRIPLE", triple)
    
    # Actor1 can DOUBLE but not TRIPLE
    actor1.send("OP_CONSTANT", 5, "OP_DOUBLE")
    while actor1.handle_message():
        pass
    assert actor1.top() == 10
    
    # Actor2 can TRIPLE but not DOUBLE
    actor2.send("OP_CONSTANT", 5, "OP_TRIPLE")
    while actor2.handle_message():
        pass
    assert actor2.top() == 15
    
    # Actor1 doesn't have TRIPLE
    with pytest.raises(NotImplementedError, match="No handler for instruction 'OP_TRIPLE'"):
        actor1.send("OP_TRIPLE")
        while actor1.handle_message():
            pass


def test_specialization_pattern():
    """Test specializing actors for different roles."""
    # Create specialized calculator actors
    
    # Arithmetic actor
    arithmetic_actor = VMActor()
    
    def power(vm):
        exp = vm.stack.pop()
        base = vm.stack.pop()
        vm.stack.push(base ** exp)
    
    arithmetic_actor.define_new_instruction("OP_POWER", power)
    
    # String actor (using numbers as char codes)
    string_actor = VMActor()
    
    def concat(vm):
        """Concatenate top two stack values as string."""
        b = vm.stack.pop()
        a = vm.stack.pop()
        result = str(a) + str(b)
        vm.stack.push(result)
    
    string_actor.define_new_instruction("OP_CONCAT", concat)
    
    # Each actor specialized for its domain
    arithmetic_actor.send("OP_CONSTANT", 2, "OP_CONSTANT", 8, "OP_POWER")
    while arithmetic_actor.handle_message():
        pass
    assert arithmetic_actor.top() == 256
    
    string_actor.send("OP_CONSTANT", "Hello", "OP_CONSTANT", "World", "OP_CONCAT")
    while string_actor.handle_message():
        pass
    assert string_actor.top() == "HelloWorld"


if __name__ == '__main__':
    pytest.main([__file__, '-v'])
