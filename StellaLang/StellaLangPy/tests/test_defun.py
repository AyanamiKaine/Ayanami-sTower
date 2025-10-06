"""Tests for user-defined functions/symbols in s-expressions (defun)."""
import pytest
from src.VMActor import VMActor


def test_defun_simple_print():
    """Test defining a simple print function."""
    actor = VMActor()
    
    output = []
    
    def my_print(vm):
        value = vm.stack.pop()
        output.append(value)
    
    actor.defun("print", my_print)
    
    # Use in s-expression
    bytecode = actor.s_expression_to_bytecode('(print "Hello World!")')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert output == ["Hello World!"]


def test_defun_with_number():
    """Test calling user-defined function with number."""
    actor = VMActor()
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    actor.defun("square", square)
    
    bytecode = actor.s_expression_to_bytecode('(square 7)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 49


def test_defun_multiple_arguments():
    """Test function with multiple arguments."""
    actor = VMActor()
    
    def concat(vm):
        """Concatenate top two stack values."""
        b = vm.stack.pop()
        a = vm.stack.pop()
        result = str(a) + str(b)
        vm.stack.push(result)
    
    actor.defun("concat", concat)
    
    bytecode = actor.s_expression_to_bytecode('(concat "Hello" "World")')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == "HelloWorld"


def test_defun_with_expression_argument():
    """Test function call with expression as argument."""
    actor = VMActor()
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    actor.defun("double", double)
    
    # (double (+ 5 3)) should give (5+3)*2 = 16
    bytecode = actor.s_expression_to_bytecode('(double (+ 5 3))')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 16


def test_defun_nested_calls():
    """Test nested function calls."""
    actor = VMActor()
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    actor.defun("square", square)
    actor.defun("double", double)
    
    # (double (square 5)) should give (5*5)*2 = 50
    bytecode = actor.s_expression_to_bytecode('(double (square 5))')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 50


def test_defun_three_arguments():
    """Test function with three arguments."""
    actor = VMActor()
    
    def sum_three(vm):
        c = vm.stack.pop()
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(a + b + c)
    
    actor.defun("sum", sum_three)
    
    bytecode = actor.s_expression_to_bytecode('(sum 10 20 30)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 60


def test_defun_no_arguments():
    """Test function with no arguments."""
    actor = VMActor()
    
    def get_pi(vm):
        vm.stack.push(3.14159)
    
    actor.defun("pi", get_pi)
    
    bytecode = actor.s_expression_to_bytecode('(pi)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 3.14159


def test_defun_with_side_effects():
    """Test function with side effects."""
    actor = VMActor()
    
    log = []
    
    def log_value(vm):
        value = vm.stack.pop()
        log.append(value)
        vm.stack.push(value)  # Put it back
    
    actor.defun("log", log_value)
    
    bytecode = actor.s_expression_to_bytecode('(log 42)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert log == [42]
    assert actor.top() == 42


def test_defun_modifies_variables():
    """Test function that modifies actor variables."""
    actor = VMActor()
    
    def set_status(vm):
        status = vm.stack.pop()
        vm.variables['status'] = status
    
    actor.defun("set-status", set_status)
    
    bytecode = actor.s_expression_to_bytecode('(set-status "ready")')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.variables['status'] == "ready"


def test_defun_uses_variables():
    """Test function that uses actor variables."""
    actor = VMActor()
    actor.variables['multiplier'] = 10
    
    def multiply_by_config(vm):
        value = vm.stack.pop()
        multiplier = vm.variables['multiplier']
        vm.stack.push(value * multiplier)
    
    actor.defun("multiply", multiply_by_config)
    
    bytecode = actor.s_expression_to_bytecode('(multiply 5)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 50


def test_defun_combined_with_define():
    """Test combining defun with variable definitions."""
    actor = VMActor()
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    actor.defun("square", square)
    
    # Define x=5, then square it
    bytecode1 = actor.s_expression_to_bytecode('(define x 5)')
    bytecode2 = actor.s_expression_to_bytecode('(square x)')
    
    actor.send(*bytecode1)
    actor.send(*bytecode2)
    
    while actor.handle_message():
        pass
    
    assert actor.variables['x'] == 5
    assert actor.top() == 25


def test_defun_composition():
    """Test composing multiple user-defined functions."""
    actor = VMActor()
    
    def increment(vm):
        value = vm.stack.pop()
        vm.stack.push(value + 1)
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    actor.defun("inc", increment)
    actor.defun("square", square)
    actor.defun("double", double)
    
    # (double (square (inc 4))) = (double (square 5)) = (double 25) = 50
    bytecode = actor.s_expression_to_bytecode('(double (square (inc 4)))')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 50


def test_defun_mixed_with_operators():
    """Test mixing user functions with built-in operators."""
    actor = VMActor()
    
    def triple(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 3)
    
    actor.defun("triple", triple)
    
    # (+ (triple 5) 10) = (+ 15 10) = 25
    bytecode = actor.s_expression_to_bytecode('(+ (triple 5) 10)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 25


def test_defun_sends_messages():
    """Test function that sends messages to another actor."""
    actor1 = VMActor()
    actor2 = VMActor()
    
    def send_to_other(vm):
        value = vm.stack.pop()
        target = vm.variables['target']
        vm.send_to(target, "OP_CONSTANT", value)
    
    actor1.defun("send-to-other", send_to_other)
    actor1.variables['target'] = actor2
    
    bytecode = actor1.s_expression_to_bytecode('(send-to-other 99)')
    actor1.send(*bytecode)
    
    while actor1.handle_message():
        pass
    
    while actor2.handle_message():
        pass
    
    assert actor2.top() == 99


def test_defun_variadic_sum():
    """Test function that sums variable number of arguments."""
    actor = VMActor()
    
    def sum_all(vm):
        """Sum all values currently on stack."""
        total = 0
        while len(vm.stack) > 0:
            total += vm.stack.pop()
        vm.stack.push(total)
    
    actor.defun("sum-all", sum_all)
    
    # Put multiple values on stack then sum them
    bytecode = actor.s_expression_to_bytecode('(sum-all 10 20 30 40)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == 100


def test_defun_conditional_logic():
    """Test function with conditional logic."""
    actor = VMActor()
    
    def abs_value(vm):
        value = vm.stack.pop()
        if value < 0:
            vm.stack.push(-value)
        else:
            vm.stack.push(value)
    
    actor.defun("abs", abs_value)
    
    # Test with negative
    bytecode = actor.s_expression_to_bytecode('(abs -42)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    assert actor.top() == 42
    
    # Test with positive
    actor.stack.clear()
    bytecode = actor.s_expression_to_bytecode('(abs 42)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    assert actor.top() == 42


def test_defun_with_strings():
    """Test function operating on strings."""
    actor = VMActor()
    
    def uppercase(vm):
        value = vm.stack.pop()
        vm.stack.push(str(value).upper())
    
    actor.defun("upper", uppercase)
    
    bytecode = actor.s_expression_to_bytecode('(upper "hello")')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert actor.top() == "HELLO"


def test_defun_returns_multiple_values():
    """Test function that pushes multiple values to stack."""
    actor = VMActor()
    
    def split_number(vm):
        """Split a 2-digit number into its digits."""
        value = vm.stack.pop()
        tens = value // 10
        ones = value % 10
        vm.stack.push(tens)
        vm.stack.push(ones)
    
    actor.defun("split", split_number)
    
    bytecode = actor.s_expression_to_bytecode('(split 47)')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert list(actor.stack) == [4, 7]


def test_defun_complex_example():
    """Test complex example with multiple functions."""
    actor = VMActor()
    
    results = []
    
    def print_value(vm):
        value = vm.stack.pop()
        results.append(value)
    
    def factorial(vm):
        n = vm.stack.pop()
        result = 1
        for i in range(1, n + 1):
            result *= i
        vm.stack.push(result)
    
    actor.defun("print", print_value)
    actor.defun("factorial", factorial)
    
    # Calculate and print 5!
    bytecode = actor.s_expression_to_bytecode('(print (factorial 5))')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    assert results == [120]


def test_string_literal_parsing():
    """Test that string literals are parsed correctly."""
    actor = VMActor()
    
    # Simple string
    bytecode = actor.s_expression_to_bytecode('"hello"')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    assert actor.top() == "hello"
    
    # String with spaces
    actor.stack.clear()
    bytecode = actor.s_expression_to_bytecode('"Hello World"')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    assert actor.top() == "Hello World"


def test_defun_error_on_undefined_function():
    """Test that calling undefined function raises error."""
    actor = VMActor()
    
    bytecode = actor.s_expression_to_bytecode('(undefined-func 42)')
    actor.send(*bytecode)
    
    with pytest.raises(NotImplementedError, match="No handler for instruction 'OP_CALL_undefined-func'"):
        while actor.handle_message():
            pass


if __name__ == '__main__':
    pytest.main([__file__, '-v'])
