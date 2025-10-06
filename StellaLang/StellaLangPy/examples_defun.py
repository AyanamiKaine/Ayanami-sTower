"""Examples demonstrating user-defined functions (defun) in s-expressions.

This shows how to define callable symbols/functions that can be used
in s-expressions, similar to Scheme/Lisp.
"""

from src.VMActor import VMActor


def example1_simple_print():
    """Example 1: Define a simple print function."""
    print("\n=== Example 1: Simple Print Function ===")
    
    actor = VMActor()
    
    def my_print(vm):
        value = vm.stack.pop()
        print(f"  Output: {value}")
    
    # Define the function
    actor.defun("print", my_print)
    
    # Now you can use (print ...) in s-expressions!
    bytecode = actor.s_expression_to_bytecode('(print "Hello World!")')
    actor.send(*bytecode)
    
    while actor.handle_message():
        pass
    
    print("✓ Defined and called (print \"Hello World!\")")


def example2_math_functions():
    """Example 2: Define mathematical functions."""
    print("\n=== Example 2: Math Functions ===")
    
    actor = VMActor()
    
    def square(vm):
        value = vm.stack.pop()
        result = value * value
        vm.stack.push(result)
    
    def cube(vm):
        value = vm.stack.pop()
        result = value ** 3
        vm.stack.push(result)
    
    def factorial(vm):
        n = vm.stack.pop()
        result = 1
        for i in range(1, n + 1):
            result *= i
        vm.stack.push(result)
    
    actor.defun("square", square)
    actor.defun("cube", cube)
    actor.defun("factorial", factorial)
    
    # Use them
    print("Calling (square 7):")
    bytecode = actor.s_expression_to_bytecode('(square 7)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    actor.stack.clear()
    print("\nCalling (cube 4):")
    bytecode = actor.s_expression_to_bytecode('(cube 4)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    actor.stack.clear()
    print("\nCalling (factorial 5):")
    bytecode = actor.s_expression_to_bytecode('(factorial 5)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    print("✓ Multiple math functions defined and working")


def example3_composition():
    """Example 3: Composing functions."""
    print("\n=== Example 3: Function Composition ===")
    
    actor = VMActor()
    
    def double(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 2)
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    def inc(vm):
        value = vm.stack.pop()
        vm.stack.push(value + 1)
    
    actor.defun("double", double)
    actor.defun("square", square)
    actor.defun("inc", inc)
    
    # Nested calls: (double (square (inc 4)))
    # = (double (square 5))
    # = (double 25)
    # = 50
    print("Calling (double (square (inc 4))):")
    bytecode = actor.s_expression_to_bytecode('(double (square (inc 4)))')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  (inc 4) = 5")
    print(f"  (square 5) = 25")
    print(f"  (double 25) = {actor.top()}")
    
    print("✓ Functions compose naturally")


def example4_with_operators():
    """Example 4: Mixing user functions with built-in operators."""
    print("\n=== Example 4: Mixing with Operators ===")
    
    actor = VMActor()
    
    def triple(vm):
        value = vm.stack.pop()
        vm.stack.push(value * 3)
    
    actor.defun("triple", triple)
    
    # Mix user function with built-in operator
    # (+ (triple 5) 10) = (+ 15 10) = 25
    print("Calling (+ (triple 5) 10):")
    bytecode = actor.s_expression_to_bytecode('(+ (triple 5) 10)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  (triple 5) = 15")
    print(f"  (+ 15 10) = {actor.top()}")
    
    print("✓ User functions work seamlessly with built-in operators")


def example5_string_operations():
    """Example 5: String manipulation functions."""
    print("\n=== Example 5: String Operations ===")
    
    actor = VMActor()
    
    def uppercase(vm):
        value = vm.stack.pop()
        vm.stack.push(str(value).upper())
    
    def concat(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(str(a) + str(b))
    
    def length(vm):
        value = vm.stack.pop()
        vm.stack.push(len(str(value)))
    
    actor.defun("upper", uppercase)
    actor.defun("concat", concat)
    actor.defun("length", length)
    
    print('Calling (upper "hello"):')
    bytecode = actor.s_expression_to_bytecode('(upper "hello")')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    actor.stack.clear()
    print('\nCalling (concat "Hello" "World"):')
    bytecode = actor.s_expression_to_bytecode('(concat "Hello" "World")')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    actor.stack.clear()
    print('\nCalling (length "Hello World!"):')
    bytecode = actor.s_expression_to_bytecode('(length "Hello World!")')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    print("✓ String operations work with string literals")


def example6_with_variables():
    """Example 6: Functions working with variables."""
    print("\n=== Example 6: Functions with Variables ===")
    
    actor = VMActor()
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    actor.defun("square", square)
    
    # Define variable x, then use it in function
    print("Defining x = 7")
    bytecode1 = actor.s_expression_to_bytecode('(define x 7)')
    actor.send(*bytecode1)
    while actor.handle_message():
        pass
    
    print("Calling (square x):")
    bytecode2 = actor.s_expression_to_bytecode('(square x)')
    actor.send(*bytecode2)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}")
    
    print("✓ Functions work with defined variables")


def example7_side_effects():
    """Example 7: Functions with side effects."""
    print("\n=== Example 7: Functions with Side Effects ===")
    
    actor = VMActor()
    
    log = []
    
    def log_value(vm):
        value = vm.stack.pop()
        log.append(value)
        vm.stack.push(value)  # Put it back
    
    def debug_print(vm):
        value = vm.stack.pop()
        print(f"  DEBUG: {value}")
        vm.stack.push(value)
    
    actor.defun("log", log_value)
    actor.defun("debug", debug_print)
    
    # Use debug in computation
    print("Calling (debug (+ 10 20)):")
    bytecode = actor.s_expression_to_bytecode('(debug (+ 10 20))')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    
    print(f"\nCalling (log 42):")
    bytecode = actor.s_expression_to_bytecode('(log 42)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Log contents: {log}")
    
    print("✓ Functions can have side effects (logging, I/O, etc.)")


def example8_actor_communication():
    """Example 8: Functions that send to other actors."""
    print("\n=== Example 8: Actor Communication ===")
    
    sender = VMActor()
    receiver = VMActor()
    
    def send_to_receiver(vm):
        value = vm.stack.pop()
        target = vm.variables['receiver']
        print(f"  Sending {value} to receiver")
        vm.send_to(target, "OP_CONSTANT", value)
    
    sender.defun("send", send_to_receiver)
    sender.variables['receiver'] = receiver
    
    # Send message using function
    print("Calling (send 99):")
    bytecode = sender.s_expression_to_bytecode('(send 99)')
    sender.send(*bytecode)
    while sender.handle_message():
        pass
    
    # Receiver processes message
    while receiver.handle_message():
        pass
    print(f"  Receiver got: {receiver.top()}")
    
    print("✓ Functions can send messages to other actors")


def example9_multiple_returns():
    """Example 9: Functions returning multiple values."""
    print("\n=== Example 9: Multiple Return Values ===")
    
    actor = VMActor()
    
    def divmod_op(vm):
        divisor = vm.stack.pop()
        dividend = vm.stack.pop()
        quotient = dividend // divisor
        remainder = dividend % divisor
        vm.stack.push(quotient)
        vm.stack.push(remainder)
    
    actor.defun("divmod", divmod_op)
    
    print("Calling (divmod 17 5):")
    bytecode = actor.s_expression_to_bytecode('(divmod 17 5)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    
    print(f"  Quotient: {actor.stack[-2]}")
    print(f"  Remainder: {actor.stack[-1]}")
    
    print("✓ Functions can return multiple values to stack")


def example10_practical_example():
    """Example 10: Practical example - temperature converter."""
    print("\n=== Example 10: Temperature Converter ===")
    
    actor = VMActor()
    
    def celsius_to_fahrenheit(vm):
        celsius = vm.stack.pop()
        fahrenheit = (celsius * 9/5) + 32
        vm.stack.push(fahrenheit)
    
    def fahrenheit_to_celsius(vm):
        fahrenheit = vm.stack.pop()
        celsius = (fahrenheit - 32) * 5/9
        vm.stack.push(celsius)
    
    actor.defun("c->f", celsius_to_fahrenheit)
    actor.defun("f->c", fahrenheit_to_celsius)
    
    print("Converting 0°C to Fahrenheit:")
    bytecode = actor.s_expression_to_bytecode('(c->f 0)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}°F")
    
    actor.stack.clear()
    print("\nConverting 212°F to Celsius:")
    bytecode = actor.s_expression_to_bytecode('(f->c 212)')
    actor.send(*bytecode)
    while actor.handle_message():
        pass
    print(f"  Result: {actor.top()}°C")
    
    print("✓ Practical domain-specific functions work great")


def example11_repl_style():
    """Example 11: REPL-style interactive usage."""
    print("\n=== Example 11: REPL-Style Usage ===")
    
    actor = VMActor()
    
    # Define utility functions
    def show(vm):
        value = vm.stack.pop()
        print(f"  => {value}")
    
    def square(vm):
        value = vm.stack.pop()
        vm.stack.push(value * value)
    
    actor.defun("show", show)
    actor.defun("square", square)
    
    # Execute multiple expressions like in a REPL
    expressions = [
        '(define x 10)',
        '(show x)',
        '(define y (square x))',
        '(show y)',
        '(show (+ x y))',
    ]
    
    print("Executing REPL-style expressions:")
    for expr in expressions:
        print(f"  > {expr}")
        bytecode = actor.s_expression_to_bytecode(expr)
        actor.send(*bytecode)
        while actor.handle_message():
            pass
    
    print("✓ Works great for REPL-style interactive usage")


def example12_library_of_functions():
    """Example 12: Building a library of functions."""
    print("\n=== Example 12: Function Library ===")
    
    actor = VMActor()
    
    # Math library
    def abs_val(vm):
        value = vm.stack.pop()
        vm.stack.push(abs(value))
    
    def max_two(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(max(a, b))
    
    def min_two(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        vm.stack.push(min(a, b))
    
    # Register library
    actor.defun("abs", abs_val)
    actor.defun("max", max_two)
    actor.defun("min", min_two)
    
    print("Testing math library:")
    tests = [
        ('(abs -42)', 42),
        ('(max 10 20)', 20),
        ('(min 10 20)', 10),
        ('(max (abs -5) (abs -3))', 5),
    ]
    
    for expr, expected in tests:
        actor.stack.clear()
        print(f"  {expr} = ", end="")
        bytecode = actor.s_expression_to_bytecode(expr)
        actor.send(*bytecode)
        while actor.handle_message():
            pass
        result = actor.top()
        print(f"{result} {'✓' if result == expected else '✗'}")
    
    print("✓ Can build comprehensive function libraries")


if __name__ == '__main__':
    print("=" * 60)
    print("USER-DEFINED FUNCTIONS (DEFUN) EXAMPLES")
    print("=" * 60)
    print("\nKey Concept: Use defun() to define callable symbols")
    print("for use in s-expressions, just like Scheme/Lisp!")
    print()
    
    example1_simple_print()
    example2_math_functions()
    example3_composition()
    example4_with_operators()
    example5_string_operations()
    example6_with_variables()
    example7_side_effects()
    example8_actor_communication()
    example9_multiple_returns()
    example10_practical_example()
    example11_repl_style()
    example12_library_of_functions()
    
    print("\n" + "=" * 60)
    print("Key Takeaways:")
    print("  • defun(name, function) defines callable symbols")
    print("  • Use like (name arg1 arg2 ...) in s-expressions")
    print("  • Functions compose naturally")
    print("  • Works with operators, variables, and other functions")
    print("  • Enables Scheme/Lisp-like programming")
    print("  • Great for building domain-specific languages")
    print("=" * 60)
