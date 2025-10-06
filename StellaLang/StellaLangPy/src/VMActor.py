import types
import inspect
import threading
import time


class Stack(list):
    """Small list subclass that provides a push() helper like many VMs expect.

    Built-in list instances don't allow setting arbitrary attributes (no __dict__),
    so assigning a method to a plain list instance raises AttributeError. Subclassing
    list is the simplest fix: instances of Stack can have methods like push().
    """

    def push(self, item):
        self.append(item)


class VMActor:
    def __init__(self):
        self.stack = Stack()
        # Variables are basically locals
        self.variables = {}
        self.ip = 0
        self.bytecode = []
        
        # Actor state
        self.running = False
        self._lock = threading.Lock()

        self.instruction_table = {
            "OP_NEGATE":        self.handle_negate,
            "OP_CONSTANT":      self.handle_constant,
            "OP_ADD" :          self.handle_add,
            "OP_SUBTRACT" :     self.handle_subtract,
            "OP_MULTIPLY" :     self.handle_multiply,
            "OP_DIVIDE" :       self.handle_divide,
            "OP_GREATER" :      self.handle_greater,
            "OP_LESS" :         self.handle_less,
            "OP_EQUAL":         self.handle_equal,
            "OP_FALSE":         self.handle_false,
            "OP_TRUE":          self.handle_true,
            "OP_POP":           self.handle_pop,
            "OP_DEFINE_VARIABLE": self.handle_define_variable,
            "OP_GET_VARIABLE":    self.handle_get_variable,
            "OP_SET_VARIABLE":    self.handle_set_variable,
            "OP_JUMP" : None,
            "OP_JUMP_IF_FALSE": None,
            "OP_LOOP": None,
            "OP_CALL": None,
            "OP_RETURN":        self.handle_return,
        }

    def s_expression_to_bytecode(self, sexpr):
        """Convert an s-expression string to bytecode.
        
        Supports basic forms:
        - (define <name> <value>) -> ["OP_CONSTANT", value, "OP_DEFINE_VARIABLE", name]
        - (+ <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_ADD"]
        - (- <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_SUBTRACT"]
        - (* <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_MULTIPLY"]
        - (/ <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_DIVIDE"]
        - (> <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_GREATER"]
        - (< <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_LESS"]
        - (= <a> <b>) -> [bytecode_for_a, bytecode_for_b, "OP_EQUAL"]
        - true / false -> ["OP_TRUE"] / ["OP_FALSE"]
        - numbers -> ["OP_CONSTANT", number]
        - symbols (variables) -> ["OP_GET_VARIABLE", name]
        """
        sexpr = sexpr.strip()
        
        # Parse the s-expression into a nested structure
        parsed = self._parse_sexpr(sexpr)
        
        # Convert parsed structure to bytecode
        return self._compile_expr(parsed)
    
    def _parse_sexpr(self, sexpr):
        """Parse an s-expression string into nested lists/atoms.
        
        Returns a nested structure of lists and atoms (strings/numbers).
        Example: "(+ 1 2)" -> ['+', 1, 2]
        Example: '(print "Hello")' -> ['print', '"Hello"']
        """
        sexpr = sexpr.strip()
        
        # Tokenize: split on whitespace and parentheses, but keep strings together
        tokens = []
        current_token = ""
        in_string = False
        
        for char in sexpr:
            if char == '"':
                current_token += char
                in_string = not in_string
                if not in_string:
                    # End of string, add token
                    tokens.append(current_token)
                    current_token = ""
            elif in_string:
                # Inside a string, keep everything
                current_token += char
            elif char in '()':
                if current_token:
                    tokens.append(current_token)
                    current_token = ""
                tokens.append(char)
            elif char.isspace():
                if current_token:
                    tokens.append(current_token)
                    current_token = ""
            else:
                current_token += char
        
        if current_token:
            tokens.append(current_token)
        
        if in_string:
            raise SyntaxError("Unclosed string literal")
        
        # Parse tokens into nested structure
        def parse_tokens(tokens, index):
            """Parse tokens starting at index, return (parsed_value, next_index)"""
            if index >= len(tokens):
                raise SyntaxError("Unexpected end of expression")
            
            token = tokens[index]
            
            if token == '(':
                # Parse a list
                result = []
                index += 1
                while index < len(tokens) and tokens[index] != ')':
                    parsed, index = parse_tokens(tokens, index)
                    result.append(parsed)
                if index >= len(tokens):
                    raise SyntaxError("Unmatched opening parenthesis")
                return result, index + 1  # skip the ')'
            elif token == ')':
                raise SyntaxError("Unexpected closing parenthesis")
            else:
                # Parse an atom (number, string, or symbol)
                # Check if it's a string literal (keep quotes for now)
                if token.startswith('"') and token.endswith('"'):
                    return token, index + 1
                # Try to convert to number
                try:
                    if '.' in token:
                        return float(token), index + 1
                    else:
                        return int(token), index + 1
                except ValueError:
                    # It's a symbol or keyword
                    return token, index + 1
        
        parsed, _ = parse_tokens(tokens, 0)
        return parsed
    
    def _compile_expr(self, expr):
        """Compile a parsed s-expression to bytecode.
        
        Args:
            expr: A parsed expression (atom or list)
        
        Returns:
            A list of bytecode instructions
        """
        # Handle atoms
        if not isinstance(expr, list):
            # Number literal
            if isinstance(expr, (int, float)):
                return ["OP_CONSTANT", expr]
            # String literal
            elif isinstance(expr, str) and expr.startswith('"') and expr.endswith('"'):
                # Remove quotes
                string_value = expr[1:-1]
                return ["OP_CONSTANT", string_value]
            # Boolean literals
            elif expr == "true":
                return ["OP_TRUE"]
            elif expr == "false":
                return ["OP_FALSE"]
            # Variable reference (symbol)
            else:
                return ["OP_GET_VARIABLE", expr]
        
        # Handle empty list
        if len(expr) == 0:
            raise SyntaxError("Empty expression ()")
        
        # Handle special forms and operators
        operator = expr[0]
        
        # Special form: (define <name> <value>)
        if operator == "define":
            if len(expr) != 3:
                raise SyntaxError(f"define requires exactly 2 arguments, got {len(expr) - 1}")
            name = expr[1]
            if not isinstance(name, str):
                raise SyntaxError(f"define name must be a symbol, got {name}")
            value_bytecode = self._compile_expr(expr[2])
            return value_bytecode + ["OP_DEFINE_VARIABLE", name]
        
        # Special form: (set! <name> <value>)
        elif operator == "set!":
            if len(expr) != 3:
                raise SyntaxError(f"set! requires exactly 2 arguments, got {len(expr) - 1}")
            name = expr[1]
            if not isinstance(name, str):
                raise SyntaxError(f"set! name must be a symbol, got {name}")
            value_bytecode = self._compile_expr(expr[2])
            return value_bytecode + ["OP_SET_VARIABLE", name]
        
        # Binary operators
        binary_ops = {
            '+': 'OP_ADD',
            '-': 'OP_SUBTRACT',
            '*': 'OP_MULTIPLY',
            '/': 'OP_DIVIDE',
            '>': 'OP_GREATER',
            '<': 'OP_LESS',
            '=': 'OP_EQUAL',
        }
        
        if operator in binary_ops:
            if len(expr) != 3:
                raise SyntaxError(f"Binary operator {operator} requires exactly 2 arguments, got {len(expr) - 1}")
            left_bytecode = self._compile_expr(expr[1])
            right_bytecode = self._compile_expr(expr[2])
            return left_bytecode + right_bytecode + [binary_ops[operator]]
        
        # User-defined function call: (func arg1 arg2 ...)
        # Check if this is a known instruction/function
        if isinstance(operator, str):
            # Compile all arguments first (left to right)
            bytecode = []
            for arg in expr[1:]:
                bytecode.extend(self._compile_expr(arg))
            
            # Then call the function
            # Convert symbol to OP_CALL_<symbol> instruction
            bytecode.append(f"OP_CALL_{operator}")
            return bytecode
        
        # Unknown operator
        raise SyntaxError(f"Unknown operator or special form: {operator}")

    def define_new_instruction(self, name, function):
        if (True == self.instruction_table.__contains__(name)):
            raise IndexError("instruction with the same name already exists, use another name or use the replace_existing_instruction function")
        self.instruction_table[name] = function

    def replace_existing_instruction(self, name, function):
        if (False == self.instruction_table.__contains__(name)):
            raise IndexError("instruction with the name does not exists, use the name of an existing instruction or use the define_new_instruction function")
        self.instruction_table[name] = function
    
    def defun(self, name, function):
        """Define a callable function/symbol for use in s-expressions.
        
        This allows you to define functions that can be called from s-expressions
        like (print "Hello") or (square 5).
        
        Args:
            name: The symbol name (without OP_CALL_ prefix)
            function: The handler function that accepts (vm) parameter
        
        Example:
            def my_print(vm):
                value = vm.stack.pop()
                print(value)
            
            actor.defun("print", my_print)
            
            # Now you can use: (print "Hello World!")
            bytecode = actor.s_expression_to_bytecode('(print "Hello World!")')
            actor.send(*bytecode)
        """
        # Register as OP_CALL_<name> instruction
        instruction_name = f"OP_CALL_{name}"
        self.define_new_instruction(instruction_name, function)

    def execute(self):
        while self.ip < len(self.bytecode):
            instruction = self.read_instruction()
            handler = self.instruction_table.get(instruction)
            if handler is None:
                raise NotImplementedError(f"No handler for instruction {instruction!r}")
            self._invoke_handler(handler)

    def step(self):
        instruction = self.read_instruction()
        handler = self.instruction_table.get(instruction)
        if handler is None:
            raise NotImplementedError(f"No handler for instruction {instruction!r}")
        self._invoke_handler(handler)

    def _invoke_handler(self, handler):
        """Call a handler that may be a bound method (no args) or a plain
        function that expects the VM instance as its first argument.

        This makes the VM more robust to tests or external code that injects
        plain functions into the instruction table (e.g. def f(vm): ...).
        """
        # Use inspect.signature to determine how many parameters the callable expects.
        try:
            sig = inspect.signature(handler)
        except (TypeError, ValueError):
            # If handler isn't a regular callable with a signature, just call it.
            return handler()

        params = sig.parameters
        if len(params) == 0:
            return handler()
        # otherwise assume the first parameter expects the VM instance
        return handler(self)
    
    def read_instruction(self):
        instruction = self.bytecode[self.ip]
        self.ip += 1
        return instruction

    def top(self):
        return self.stack[-1]

    def load_bytecode(self, bytecode):
        self.bytecode = bytecode

    def send(self, *instructions):
        """Send messages (bytecode instructions) to this actor.
        
        The bytecode stream IS the message stream. When you send instructions,
        they are appended to the bytecode and will be processed by the receive loop.
        
        Args:
            *instructions: Bytecode instructions to append
        
        Example:
            actor.send("OP_CONSTANT", 5)
            actor.send("OP_CONSTANT", 10, "OP_ADD")
        """
        with self._lock:
            self.bytecode.extend(instructions)
    
    def send_to(self, target_actor, *instructions):
        """Send messages to another actor asynchronously (fire-and-forget).
        
        This is the core of async actor-to-actor communication. The sender
        does NOT wait for a response - it just sends and continues.
        
        Args:
            target_actor: The VMActor instance to send messages to
            *instructions: Bytecode instructions to send
        
        Example:
            actor1.send_to(actor2, "OP_CONSTANT", 42, "OP_ADD")
            # actor1 continues immediately, doesn't wait for actor2
        """
        target_actor.send(*instructions)

    def receive(self):
        """Receive the next message (bytecode instruction) from the stream.
        
        This reads the next instruction from the bytecode at the current ip
        and advances the instruction pointer. The bytecode stream IS the message queue.
        
        Returns:
            The next instruction, or None if no more messages
        """
        with self._lock:
            if self.ip < len(self.bytecode):
                instruction = self.bytecode[self.ip]
                self.ip += 1
                return instruction
            return None
    
    def handle_message(self):
        """Handle a single message from the bytecode stream.
        
        This is called by external loop/scheduler (like GenServer callbacks).
        The VMActor doesn't own the loop - you define loop behavior externally.
        
        Returns:
            True if a message was processed, False if no message available
        
        Example:
            actor = VMActor()
            actor.send("OP_CONSTANT", 5)
            actor.send("OP_ADD")
            
            # External loop controls when to process messages
            while actor.handle_message():
                pass  # Process all available messages
        """
        instruction = self.receive()
        
        if instruction is None:
            return False  # No message available
        
        # Pattern match on instruction type and dispatch to handler
        handler = self.instruction_table.get(instruction)
        
        if handler is None:
            raise NotImplementedError(f"No handler for instruction {instruction!r}")
        
        # Invoke the handler (like matching a message pattern in Erlang)
        self._invoke_handler(handler)
        return True  # Message was processed

    def receive_list_of_bytecode(self, bytecode):
        self.bytecode.extend(bytecode)
    

    def read_constant(self):
        if self.ip < len(self.bytecode):
            value = self.bytecode[self.ip]
            self.ip += 1
            return value
        raise IndexError("No constant available at ip={}".format(self.ip))

    def handle_return(self):
        self.stack.pop()

    def handle_negate(self):
        self.stack.push(- self.stack.pop())

    def handle_constant(self):
        constant = self.read_constant()
        self.stack.push(constant)

    def handle_add(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a + b
        self.stack.push(result)

    def handle_subtract(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a - b
        self.stack.push(result)

    def handle_multiply(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a * b
        self.stack.push(result)

    def handle_divide(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a / b
        self.stack.push(result)

    def handle_greater(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a > b
        self.stack.push(result)

    def handle_less(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a < b
        self.stack.push(result)

    def handle_equal(self):
        b = self.stack.pop()
        a = self.stack.pop()
        result = a == b
        self.stack.push(result)

    def handle_define_variable(self):
        name = self.read_constant()
        value = self.stack.pop()
        self.variables[name] = value

    def handle_false(self):
        self.stack.push(False)

    def handle_true(self):
        self.stack.push(True)

    def handle_pop(self):
        return self.stack.pop()

    def handle_get_variable(self):
        name = self.read_constant()
        self.stack.push(self.variables[name])

    def handle_set_variable(self):
        name = self.read_constant()
        value = self.stack.pop()
        self.variables[name] = value
    