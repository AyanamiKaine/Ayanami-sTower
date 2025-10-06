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

