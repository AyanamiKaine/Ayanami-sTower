import textwrap
from src.VMActor import VMActor


def test_load_bytecode():
    bytecode = ["OP_CONSTANT", 1];
    
    vm = VMActor()
    vm.load_bytecode(bytecode)

    assert (vm.bytecode == bytecode)

def test_OP_CONSTANT_instruction():
    bytecode = ["OP_CONSTANT", 1];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()

    assert (vm.top() == 1)

def test_OP_NEGATE_instruction():
    bytecode = ["OP_CONSTANT", 1, "OP_NEGATE"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()

    assert (vm.top() == -1)

def test_OP_ADD_instruction():
    bytecode = ["OP_CONSTANT", 9, "OP_CONSTANT", 5, "OP_ADD"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 14)

def test_OP_SUBTRACT_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_SUBTRACT"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == -5)

def test_OP_SUBTRACT_instruction2():
    bytecode = ["OP_CONSTANT", 30, "OP_CONSTANT", 10, "OP_SUBTRACT"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 20)

def test_OP_MULTIPLY_instruction():
    bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_MULTIPLY"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 100)

def test_OP_DIVIDE_instruction():
    bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_DIVIDE"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 4)

def test_OP_DIVIDE_instruction2():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_DIVIDE"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 0.25)

def test_OP_GREATER_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_GREATER"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == False)

def test_OP_LESS_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_LESS"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == True)

def test_OP_EQUAL_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_EQUAL"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == False)


def test_OP_GREATER_instruction2():
    bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_GREATER"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == True)

def test_OP_LESS_instruction2():
    bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_LESS"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == False)

def test_OP_EQUAL_instruction2():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 5, "OP_EQUAL"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == True)

def test_OP_FALSE_instruction2():
    bytecode = ["OP_FALSE"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()

    assert (vm.top() == False)

def test_OP_TRUE_instruction2():
    bytecode = ["OP_TRUE"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()

    assert (vm.top() == True)

def test_OP_DEFINE_GLOBAL_instruction():
    bytecode = ["OP_CONSTANT", 42, "OP_DEFINE_VARIABLE", "answer"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()

    assert (vm.variables["answer"] == 42)

def test_OP_GET_GLOBAL_instruction():
    bytecode = ["OP_CONSTANT", 42, "OP_DEFINE_VARIABLE", "answer", "OP_GET_VARIABLE", "answer"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 42)

def test_OP_SET_GLOBAL_instruction():
    bytecode = ["OP_CONSTANT", 42, "OP_DEFINE_VARIABLE", "answer", "OP_CONSTANT", 100, "OP_SET_VARIABLE", "answer", "OP_GET_VARIABLE", "answer"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 100)

def test_replace_existing_instruction():
    bytecode = ["OP_CONSTANT", 9, "OP_CONSTANT", 5, "OP_ADD"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    def add_times_two(vm):
        b = vm.stack.pop()
        a = vm.stack.pop()
        result = (a + b) * 2
        vm.stack.push(result);

    vm.replace_existing_instruction("OP_ADD", add_times_two)

    vm.step()
    vm.step()
    vm.step()

    assert (vm.top() == 28)

def test_define_new_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_TIMES_TWO"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    def times_two(vm):
        a = vm.stack.pop()
        result = a * 2
        vm.stack.push(result);

    vm.define_new_instruction("OP_TIMES_TWO", times_two)

    vm.step()
    vm.step()

    assert (vm.top() == 10)

def test_define_new_instruction_using_eval():
    bytecode = ["OP_CONSTANT", 5, "OP_TIMES_TWO"];
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    code = """
    def times_two(vm):
        a = vm.stack.pop()
        result = a * 2
        vm.stack.push(result);
    """

    code = textwrap.dedent(code)
    execution_scope = {}
    exec(code, {}, execution_scope)

    vm.define_new_instruction("OP_TIMES_TWO", execution_scope["times_two"])

    vm.step()
    vm.step()

    assert (vm.top() == 10)

def test_receive_bytecode():
    bytecode1 = ["OP_CONSTANT", 1];
    bytecode2 = ["OP_CONSTANT", 2];
    
    vm = VMActor()
    vm.load_bytecode(bytecode1)
    vm.receive_list_of_bytecode(bytecode2)

    assert (vm.bytecode == ["OP_CONSTANT", 1, "OP_CONSTANT", 2])

def test_receive_bytecode_step():
    bytecode1 = ["OP_CONSTANT", 1];
    bytecode2 = ["OP_CONSTANT", 2];
    
    vm = VMActor()
    vm.load_bytecode(bytecode1)
    vm.receive_list_of_bytecode(bytecode2)

    vm.step()
    vm.step()
    assert (vm.top() == 2)

def test_s_expression_to_bytecode():
    sexpr = "(define answer 42)"
    expected_bytecode = ["OP_CONSTANT", 42, "OP_DEFINE_VARIABLE", "answer"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_simple_addition():
    sexpr = "(+ 5 10)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_simple_subtraction():
    sexpr = "(- 30 10)"
    expected_bytecode = ["OP_CONSTANT", 30, "OP_CONSTANT", 10, "OP_SUBTRACT"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_simple_multiplication():
    sexpr = "(* 5 20)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_MULTIPLY"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_simple_division():
    sexpr = "(/ 20 5)"
    expected_bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_DIVIDE"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_greater_than():
    sexpr = "(> 20 5)"
    expected_bytecode = ["OP_CONSTANT", 20, "OP_CONSTANT", 5, "OP_GREATER"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_less_than():
    sexpr = "(< 5 20)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 20, "OP_LESS"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_equal():
    sexpr = "(= 5 5)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 5, "OP_EQUAL"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_nested_arithmetic():
    # (+ (* 2 3) (- 10 5)) should compute (2*3) + (10-5) = 6 + 5 = 11
    sexpr = "(+ (* 2 3) (- 10 5))"
    expected_bytecode = [
        "OP_CONSTANT", 2, "OP_CONSTANT", 3, "OP_MULTIPLY",
        "OP_CONSTANT", 10, "OP_CONSTANT", 5, "OP_SUBTRACT",
        "OP_ADD"
    ]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_nested_arithmetic_execution():
    # Execute the nested expression and verify result
    sexpr = "(+ (* 2 3) (- 10 5))"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()

    assert (vm.top() == 11)

def test_s_expression_deeply_nested():
    # (* (+ 1 2) (- 10 (/ 8 2))) should compute (1+2) * (10 - (8/2)) = 3 * 6 = 18
    sexpr = "(* (+ 1 2) (- 10 (/ 8 2)))"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()

    assert (vm.top() == 18)

def test_s_expression_with_variable_reference():
    sexpr = "(+ x 10)"
    expected_bytecode = [
        "OP_GET_VARIABLE", "x",
        "OP_CONSTANT", 10,
        "OP_ADD"
    ]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_define_and_use_variable():
    # Test compiling a define statement
    define_sexpr = "(define x 42)"
    
    vm = VMActor()
    define_bytecode = vm.s_expression_to_bytecode(define_sexpr)
    vm.load_bytecode(define_bytecode)
    vm.execute()
    
    assert (vm.variables["x"] == 42)
    
    # Now compile and execute an expression using that variable
    use_sexpr = "(+ x 8)"
    use_bytecode = vm.s_expression_to_bytecode(use_sexpr)
    vm.load_bytecode(use_bytecode)
    vm.ip = 0  # reset instruction pointer
    vm.execute()
    
    assert (vm.top() == 50)

def test_s_expression_set_variable():
    sexpr = "(set! x 100)"
    expected_bytecode = ["OP_CONSTANT", 100, "OP_SET_VARIABLE", "x"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_boolean_literals():
    # Test true
    vm = VMActor()
    bytecode_true = vm.s_expression_to_bytecode("true")
    assert (bytecode_true == ["OP_TRUE"])
    
    # Test false
    bytecode_false = vm.s_expression_to_bytecode("false")
    assert (bytecode_false == ["OP_FALSE"])

def test_s_expression_boolean_in_expression():
    # (= (> 10 5) true) should evaluate to true
    sexpr = "(= (> 10 5) true)"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == True)

def test_s_expression_float_numbers():
    sexpr = "(/ 5.5 2.0)"
    expected_bytecode = ["OP_CONSTANT", 5.5, "OP_CONSTANT", 2.0, "OP_DIVIDE"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_complex_nested_with_variables():
    # (+ (* x 2) (- y (/ z 3)))
    sexpr = "(+ (* x 2) (- y (/ z 3)))"
    
    vm = VMActor()
    
    # Set up variables
    vm.variables["x"] = 5
    vm.variables["y"] = 20
    vm.variables["z"] = 9
    
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    # Should compute (5*2) + (20 - (9/3)) = 10 + 17 = 27
    assert (vm.top() == 27)

def test_s_expression_whitespace_handling():
    # Test that various whitespace formats work
    sexpr = "(   +    5     10   )"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_ADD"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_comparison_chain():
    # (< (+ 2 3) (* 2 4)) should compute (2+3) < (2*4) = 5 < 8 = true
    sexpr = "(< (+ 2 3) (* 2 4))"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == True)

# Tests for new arithmetic operations
def test_OP_MODULO_instruction():
    bytecode = ["OP_CONSTANT", 10, "OP_CONSTANT", 3, "OP_MODULO"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 1)

def test_OP_POWER_instruction():
    bytecode = ["OP_CONSTANT", 2, "OP_CONSTANT", 8, "OP_POWER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 256)

def test_s_expression_modulo():
    sexpr = "(% 17 5)"
    expected_bytecode = ["OP_CONSTANT", 17, "OP_CONSTANT", 5, "OP_MODULO"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_power():
    sexpr = "(** 3 4)"
    expected_bytecode = ["OP_CONSTANT", 3, "OP_CONSTANT", 4, "OP_POWER"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_modulo_execution():
    sexpr = "(% 23 7)"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == 2)

def test_s_expression_power_execution():
    sexpr = "(** 5 3)"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == 125)

# Tests for new comparison operations
def test_OP_GREATER_EQUAL_instruction():
    bytecode = ["OP_CONSTANT", 10, "OP_CONSTANT", 10, "OP_GREATER_EQUAL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_LESS_EQUAL_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_LESS_EQUAL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_NOT_EQUAL_instruction():
    bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_NOT_EQUAL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_s_expression_greater_equal():
    sexpr = "(>= 10 5)"
    expected_bytecode = ["OP_CONSTANT", 10, "OP_CONSTANT", 5, "OP_GREATER_EQUAL"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_less_equal():
    sexpr = "(<= 5 10)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_LESS_EQUAL"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_not_equal():
    sexpr = "(!= 5 10)"
    expected_bytecode = ["OP_CONSTANT", 5, "OP_CONSTANT", 10, "OP_NOT_EQUAL"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

# Tests for logical operations
def test_OP_NOT_instruction():
    bytecode = ["OP_TRUE", "OP_NOT"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

def test_OP_AND_instruction():
    bytecode = ["OP_TRUE", "OP_FALSE", "OP_AND"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

def test_OP_AND_instruction_both_true():
    bytecode = ["OP_TRUE", "OP_TRUE", "OP_AND"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_OR_instruction():
    bytecode = ["OP_TRUE", "OP_FALSE", "OP_OR"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_OR_instruction_both_false():
    bytecode = ["OP_FALSE", "OP_FALSE", "OP_OR"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

def test_s_expression_not():
    sexpr = "(not true)"
    expected_bytecode = ["OP_TRUE", "OP_NOT"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_and():
    sexpr = "(and true false)"
    expected_bytecode = ["OP_TRUE", "OP_FALSE", "OP_AND"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_or():
    sexpr = "(or false true)"
    expected_bytecode = ["OP_FALSE", "OP_TRUE", "OP_OR"]

    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)

    assert (bytecode == expected_bytecode)

def test_s_expression_complex_logical():
    # (and (> 10 5) (< 3 7)) should be true
    sexpr = "(and (> 10 5) (< 3 7))"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == True)

def test_s_expression_not_execution():
    sexpr = "(not (= 5 10))"
    
    vm = VMActor()
    bytecode = vm.s_expression_to_bytecode(sexpr)
    vm.load_bytecode(bytecode)
    vm.execute()
    
    assert (vm.top() == True)

# Tests for nil/None
def test_OP_NIL_instruction():
    bytecode = ["OP_NIL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == None)

# Tests for stack operations
def test_OP_DUP_instruction():
    bytecode = ["OP_CONSTANT", 42, "OP_DUP"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (len(vm.stack) == 2)
    assert (vm.stack[-1] == 42)
    assert (vm.stack[-2] == 42)

def test_OP_SWAP_instruction():
    bytecode = ["OP_CONSTANT", 10, "OP_CONSTANT", 20, "OP_SWAP"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.stack[-1] == 10)
    assert (vm.stack[-2] == 20)

def test_OP_PUSH_instruction():
    bytecode = ["OP_PUSH", 99]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 99)

# Tests for type checking
def test_OP_IS_NUMBER_instruction_true():
    bytecode = ["OP_CONSTANT", 42, "OP_IS_NUMBER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_IS_NUMBER_instruction_false():
    bytecode = ["OP_CONSTANT", "hello", "OP_IS_NUMBER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

def test_OP_IS_STRING_instruction_true():
    bytecode = ["OP_CONSTANT", "hello", "OP_IS_STRING"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_IS_STRING_instruction_false():
    bytecode = ["OP_CONSTANT", 42, "OP_IS_STRING"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

def test_OP_IS_BOOL_instruction_true():
    bytecode = ["OP_TRUE", "OP_IS_BOOL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == True)

def test_OP_IS_BOOL_instruction_false():
    bytecode = ["OP_CONSTANT", 42, "OP_IS_BOOL"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == False)

# Tests for type conversion
def test_OP_TO_STRING_instruction():
    bytecode = ["OP_CONSTANT", 42, "OP_TO_STRING"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == "42")
    assert (isinstance(vm.top(), str))

def test_OP_TO_NUMBER_instruction_int():
    bytecode = ["OP_CONSTANT", "123", "OP_TO_NUMBER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 123)

def test_OP_TO_NUMBER_instruction_float():
    bytecode = ["OP_CONSTANT", "12.5", "OP_TO_NUMBER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 12.5)

def test_OP_TO_NUMBER_instruction_invalid():
    bytecode = ["OP_CONSTANT", "not a number", "OP_TO_NUMBER"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == None)

# Tests for control flow
def test_OP_JUMP_instruction():
    # Jump over the OP_CONSTANT 999, should only push 42
    bytecode = ["OP_JUMP", 4, "OP_CONSTANT", 999, "OP_CONSTANT", 42]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 42)
    assert (len(vm.stack) == 1)

def test_OP_JUMP_IF_FALSE_instruction_true_condition():
    # Condition is true, should NOT jump, execute both constants
    bytecode = ["OP_TRUE", "OP_JUMP_IF_FALSE", 5, "OP_CONSTANT", 42, "OP_CONSTANT", 100]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 100)
    assert (len(vm.stack) == 2)

def test_OP_JUMP_IF_FALSE_instruction_false_condition():
    # Condition is false, should jump over OP_CONSTANT 42
    bytecode = ["OP_FALSE", "OP_JUMP_IF_FALSE", 5, "OP_CONSTANT", 42, "OP_CONSTANT", 100]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    assert (vm.top() == 100)
    assert (len(vm.stack) == 1)

def test_OP_LOOP_instruction():
    # Simpler test: just verify LOOP changes IP correctly
    vm = VMActor()
    
    # Set up a simple bytecode and manually test loop behavior
    bytecode = [
        "OP_CONSTANT", 1,    # 0-1: push 1
        "OP_CONSTANT", 2,    # 2-3: push 2  
        "OP_ADD",            # 4: add -> 3
        "OP_LOOP", 3,        # 5-6: loop back 3 positions
    ]
    
    vm.load_bytecode(bytecode)
    
    # Execute instructions properly (step() handles each instruction)
    vm.step()  # OP_CONSTANT at 0, reads 1 from position 1, ip becomes 2
    vm.step()  # OP_CONSTANT at 2, reads 2 from position 3, ip becomes 4
    vm.step()  # OP_ADD at 4, ip becomes 5
    
    # Stack should have 3 now
    assert vm.top() == 3
    
    # Now at position 5, execute OP_LOOP
    ip_before_loop = vm.ip  # Should be 5
    vm.step()  # OP_LOOP instruction reads offset (3) and jumps back
    
    # IP should have moved backwards by 3
    # After reading LOOP instruction (ip=6), then reading offset (ip=7), then jumping back by 3
    # So: 7 - 3 = 4
    assert vm.ip == 4  # Should be back at OP_ADD position

def test_OP_CALL_and_RETURN_instruction():
    # Simplified test: manually manage call/return without actual function definition
    # We'll test that call stack is created and return works
    bytecode = [
        "OP_CONSTANT", 5,       # 0: push 5
        "OP_CONSTANT", 10,      # 2: push 10
        "OP_ADD",               # 4: add them
    ]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    # Manually test call stack mechanism
    vm.call_stack.append({
        'return_address': 99,
        'variables': {}
    })
    
    vm.execute()
    
    # Should have 15 on stack
    assert (vm.top() == 15)
    
    # Test return pops call stack
    vm.load_bytecode(["OP_RETURN"])
    vm.ip = 0
    vm.step()
    
    # Call stack should be empty now
    assert (len(vm.call_stack) == 0)

# Tests for I/O operations (basic smoke tests)
def test_OP_PRINT_instruction(capsys):
    bytecode = ["OP_CONSTANT", 42, "OP_PRINT"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    captured = capsys.readouterr()
    assert ("42" in captured.out)
    # Value should still be on stack
    assert (vm.top() == 42)

def test_OP_PRINT_STACK_instruction(capsys):
    bytecode = ["OP_CONSTANT", 1, "OP_CONSTANT", 2, "OP_PRINT_STACK"]
    vm = VMActor()
    vm.load_bytecode(bytecode)
    
    vm.execute()
    
    captured = capsys.readouterr()
    assert ("Stack:" in captured.out)
    assert ("1" in captured.out)
    assert ("2" in captured.out)

