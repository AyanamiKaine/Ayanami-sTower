namespace StellaLang;

/// <summary>
/// Stacked Based Bytecode Virtual Machine Actor, here the bytecode that gets executed is just like a message. To make it more clear that we see bytecode as messages, we call this class an Actor. The internal heap is its stack based memory. Efficiency will be sacrificed for simplicity, clarity and dynamism. The main goal is to have a VM that is highly dynamic.
/// </summary>
public class VMActor
{
    // The stack based memory of the VM
    private readonly Stack<object> _stack = new();

    // The instruction pointer
    private int _ip;

    // The bytecode to execute
    private List<byte> _bytecode = [];
    private List<object> _constants = [];
    // Call stack for return addresses
    private readonly Stack<int> _callStack = new();
    // Registered C# functions
    private readonly Dictionary<string, Func<object[], object>> _functions = [];
    // The instruction set
    private readonly Dictionary<byte, Action> _instructionSet = [];

    /// <summary>
    /// Constructor
    /// </summary>
    public VMActor()
    {
        _instructionSet[0x00] = () => { }; // NOP
        _instructionSet[0x01] = Push;
        _instructionSet[0x02] = Add;
        _instructionSet[0x03] = Subtract;
        _instructionSet[0x04] = Multiply;
        _instructionSet[0x05] = Divide;
        _instructionSet[0x06] = Print;
        _instructionSet[0x07] = LoadConst;
        _instructionSet[0x08] = Call;
        _instructionSet[0x09] = Ret;
        _instructionSet[0x0A] = CallCSharp;
    }

    /// <summary>
    /// Register a C# function for calling from bytecode
    /// </summary>
    /// <param name="name">The name of the function</param>
    /// <param name="func">The function delegate</param>
    public void RegisterFunction(string name, Func<object[], object> func)
    {
        _functions[name] = func;
    }

    /// <summary>
    /// Load bytecode into the VM
    /// </summary>
    /// <param name="bytecode">The bytecode to load</param>
    public void LoadBytecode(List<byte> bytecode)
    {
        _bytecode = bytecode;
        _ip = 0;
        _stack.Clear();
    }

    /// <summary>
    /// Load bytecode and constants into the VM
    /// </summary>
    /// <param name="bytecode">The bytecode to load</param>
    /// <param name="constants">The constant pool</param>
    public void LoadBytecode(List<byte> bytecode, List<object> constants)
    {
        _bytecode = bytecode;
        _constants = constants;
        _ip = 0;
        _stack.Clear();
    }

    /// <summary>
    /// Execute the loaded bytecode
    /// </summary>
    public void Execute()
    {
        while (_ip < _bytecode.Count)
        {
            byte opcode = _bytecode[_ip];
            if (_instructionSet.TryGetValue(opcode, out Action? value))
            {
                value();
            }
            else
            {
                throw new Exception($"Unknown opcode: {opcode}");
            }
            _ip++;
        }
    }
    /// <summary>
    /// Execute a single instruction
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void Step()
    {
        if (_ip < _bytecode.Count)
        {
            byte opcode = _bytecode[_ip];
            if (_instructionSet.TryGetValue(opcode, out Action? value))
            {
                value();
            }
            else
            {
                throw new Exception($"Unknown opcode: {opcode}");
            }
            _ip++;
        }
        else
        {
            throw new Exception("End of bytecode reached.");
        }
    }

    /// <summary>
    /// Get the current instruction
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public byte GetCurrentInstruction()
    {
        if (_ip < _bytecode.Count)
        {
            return _bytecode[_ip];
        }
        throw new Exception("No current instruction.");
    }

    /// <summary>
    /// Get the top value of the stack without popping it
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public object GetStackTop()
    {
        if (_stack.Count == 0)
            throw new Exception("Stack is empty.");

        return _stack.Peek();
    }

    private void Push()
    {
        _ip++;
        if (_ip >= _bytecode.Count)
            throw new Exception("Unexpected end of bytecode during PUSH operation.");

        byte value = _bytecode[_ip];
        _stack.Push(value);
    }

    private void LoadConst()
    {
        _ip++;
        if (_ip >= _bytecode.Count)
            throw new Exception("Unexpected end of bytecode during LOAD_CONST operation.");

        byte index = _bytecode[_ip];
        if (index >= _constants.Count)
            throw new Exception("Invalid constant index.");

        _stack.Push(_constants[index]);
    }

    private void Print()
    {
        if (_stack.Count == 0)
            throw new Exception("Stack is empty, nothing to print.");

        var value = _stack.Pop();
        Console.WriteLine(value);
    }

    private void Add()
    {
        if (_stack.Count < 2)
            throw new Exception("Not enough values on the stack for ADD operation.");

        dynamic b = _stack.Pop();
        dynamic a = _stack.Pop();
        _stack.Push(a + b);
    }

    private void Subtract()
    {
        if (_stack.Count < 2)
            throw new Exception("Not enough values on the stack for SUBTRACT operation.");

        dynamic b = _stack.Pop();
        dynamic a = _stack.Pop();
        _stack.Push(a - b);
    }

    private void Multiply()
    {
        if (_stack.Count < 2)
            throw new Exception("Not enough values on the stack for MULTIPLY operation.");

        dynamic b = _stack.Pop();
        dynamic a = _stack.Pop();
        _stack.Push(a * b);
    }

    private void Divide()
    {
        if (_stack.Count < 2)
            throw new Exception("Not enough values on the stack for DIVIDE operation.");

        dynamic b = _stack.Pop();
        dynamic a = _stack.Pop();
        if (b == 0)
            throw new Exception("Division by zero.");

        _stack.Push(a / b);
    }

    private void Call()
    {
        _ip++;
        if (_ip >= _bytecode.Count)
            throw new Exception("Unexpected end of bytecode during CALL operation.");

        byte offset = _bytecode[_ip];
        if (offset >= _bytecode.Count)
            throw new Exception("Invalid function offset.");

        _callStack.Push(_ip + 1); // Push next instruction as return address
        _ip = offset - 1; // Jump to function start (adjust for post-increment in Execute)
    }

    private void Ret()
    {
        if (_callStack.Count == 0)
            throw new Exception("Call stack is empty during RET operation.");

        _ip = _callStack.Pop() - 1; // Jump back (adjust for post-increment)
    }

    private void CallCSharp()
    {
        _ip++;
        if (_ip >= _bytecode.Count)
            throw new Exception("Unexpected end of bytecode during CALL_CSHARP operation.");

        byte nameIndex = _bytecode[_ip];
        if (nameIndex >= _constants.Count || !(_constants[nameIndex] is string name))
            throw new Exception("Invalid function name index.");

        _ip++;
        if (_ip >= _bytecode.Count)
            throw new Exception("Unexpected end of bytecode during CALL_CSHARP operation.");

        byte argCount = _bytecode[_ip];
        if (_stack.Count < argCount)
            throw new Exception("Not enough arguments on stack for CALL_CSHARP.");

        object[] args = new object[argCount];
        for (int i = argCount - 1; i >= 0; i--)
            args[i] = _stack.Pop();

        if (!_functions.TryGetValue(name, out var func))
            throw new Exception($"Function '{name}' not registered.");

        object result = func(args);
        _stack.Push(result);
    }
}
