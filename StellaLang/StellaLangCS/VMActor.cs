using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace StellaLang;

/// <summary>
/// Opcode enumeration for bytecode instructions.
/// Using bytes provides efficient dispatch and compact bytecode representation.
/// </summary>
public enum OpCode : byte
{
    /// <summary>Push immediate value onto stack.</summary>
    PUSH = 0x01,
    /// <summary>Print top of stack.</summary>
    PRINT = 0x02,
    /// <summary>Discard top of stack.</summary>
    POP = 0x03,
    /// <summary>Duplicate top of stack.</summary>
    DUP = 0x04,

    /// <summary>Addition operation.</summary>
    ADD = 0x10,
    /// <summary>Subtraction operation.</summary>
    SUB = 0x11,
    /// <summary>Multiplication operation.</summary>
    MUL = 0x12,
    /// <summary>Division operation.</summary>
    DIV = 0x13,

    /// <summary>Store value to dictionary memory.</summary>
    STORE = 0x20,
    /// <summary>Fetch value from dictionary memory.</summary>
    FETCH = 0x21,

    /// <summary>Call a word by index.</summary>
    CALL = 0x30,
    /// <summary>Return from a word call.</summary>
    RETURN = 0x31,
    /// <summary>Define a new word.</summary>
    DEFINE = 0x32,
    /// <summary>Redefine an existing word.</summary>
    REDEFINE = 0x33,

    /// <summary>Stop execution.</summary>
    HALT = 0xFF,
}

/// <summary>
/// Represents a word in the Forth-like dictionary.
/// A word can be either a native (built-in) operation or a user-defined sequence of bytecode.
/// This unifies Forth's concept of "words" with VM instructions.
/// </summary>
public class Word
{
    /// <summary>
    /// The name of the word (e.g., "DUP", "ADD", or user-defined names).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The opcode for this word (for native words).
    /// </summary>
    public byte OpCode { get; init; }

    /// <summary>
    /// Whether this is a native (built-in) word or user-defined.
    /// </summary>
    public bool IsNative { get; init; }

    /// <summary>
    /// For native words: the C# action to execute.
    /// </summary>
    public Action? NativeHandler { get; init; }

    /// <summary>
    /// For user-defined words: the bytecode to execute.
    /// </summary>
    public byte[] Bytecode { get; init; } = [];

    /// <summary>
    /// Creates a native word that executes C# code.
    /// </summary>
    public static Word Native(string name, byte opcode, Action handler) => new()
    {
        Name = name,
        OpCode = opcode,
        IsNative = true,
        NativeHandler = handler
    };

    /// <summary>
    /// Creates a user-defined word from bytecode.
    /// </summary>
    public static Word UserDefined(string name, byte[] bytecode) => new()
    {
        Name = name,
        OpCode = 0,
        IsNative = false,
        Bytecode = bytecode
    };
}

/// <summary>
/// Value type for the VM's data stack.
/// Uses a discriminated union approach to efficiently represent different types
/// while avoiding boxing overhead of object-based stacks.
/// </summary>
public readonly struct Value
{
    /// <summary>
    /// Type tag for the value.
    /// </summary>
    public enum ValueType : byte
    {
        /// <summary>Integer value.</summary>
        Integer,
        /// <summary>Floating-point value.</summary>
        Float,
        /// <summary>Boolean value.</summary>
        Boolean,
        /// <summary>Memory pointer/address.</summary>
        Pointer,
    }

    /// <summary>
    /// The type of this value.
    /// </summary>
    public readonly ValueType Type;

    // Raw storage - we use the largest type needed
    private readonly long _intValue;
    private readonly double _floatValue;

    private Value(ValueType type, long intValue, double floatValue)
    {
        Type = type;
        _intValue = intValue;
        _floatValue = floatValue;
    }

    /// <summary>
    /// Creates an integer value.
    /// </summary>
    public static Value Integer(long value) => new(ValueType.Integer, value, 0);

    /// <summary>
    /// Creates a float value.
    /// </summary>
    public static Value Float(double value) => new(ValueType.Float, 0, value);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    public static Value Boolean(bool value) => new(ValueType.Boolean, value ? 1 : 0, 0);

    /// <summary>
    /// Creates a pointer/address value.
    /// </summary>
    public static Value Pointer(int address) => new(ValueType.Pointer, address, 0);

    /// <summary>
    /// Gets the value as an integer, converting if necessary.
    /// </summary>
    public long AsInteger() => Type switch
    {
        ValueType.Integer => _intValue,
        ValueType.Float => (long)_floatValue,
        ValueType.Boolean => _intValue,
        ValueType.Pointer => _intValue,
        _ => throw new InvalidOperationException($"Cannot convert {Type} to integer.")
    };

    /// <summary>
    /// Gets the value as a float, converting if necessary.
    /// </summary>
    public double AsFloat() => Type switch
    {
        ValueType.Integer => _intValue,
        ValueType.Float => _floatValue,
        ValueType.Boolean => _intValue,
        _ => throw new InvalidOperationException($"Cannot convert {Type} to float.")
    };

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    public bool AsBoolean() => Type switch
    {
        ValueType.Boolean => _intValue != 0,
        ValueType.Integer => _intValue != 0,
        _ => throw new InvalidOperationException($"Cannot convert {Type} to boolean.")
    };

    /// <summary>
    /// Gets the value as a pointer/address.
    /// </summary>
    public int AsPointer() => Type switch
    {
        ValueType.Pointer => (int)_intValue,
        ValueType.Integer => (int)_intValue,
        _ => throw new InvalidOperationException($"Cannot convert {Type} to pointer.")
    };

    /// <summary>
    /// Converts this value to its string representation.
    /// </summary>
    public override string ToString() => Type switch
    {
        ValueType.Integer => _intValue.ToString(),
        ValueType.Float => _floatValue.ToString("F"),
        ValueType.Boolean => (_intValue != 0).ToString(),
        ValueType.Pointer => $"0x{_intValue:X}",
        _ => "Unknown"
    };
}

/// <summary>
/// Stacked Based Bytecode Virtual Machine (Similar to Forth) Actor (Similar to Smalltalk-72), 
/// here the bytecode that gets executed is just like a message. To make it more clear that we 
/// see bytecode as messages, we call this class an Actor. The internal heap is its stack based memory. 
/// Efficiency will be sacrificed for simplicity, clarity and dynamism. The main goal is to have a VM 
/// that is highly dynamic.
/// </summary>
/// <remarks>
/// Uses byte-based opcodes for efficient instruction dispatch and a typed Value struct for the data stack
/// to avoid boxing overhead while maintaining type safety and flexibility.
/// Implements Forth-like word dictionary where instructions and user-defined words are unified.
/// </remarks>
public class VMActor
{
    /// <summary>
    /// Our VM has a Forth-style memory dictionary. Memory is just a continous array of bytes.
    /// </summary>
    private byte[] _dictionary;

    /// <summary>
    /// The bytecode program being executed.
    /// </summary>
    private byte[] _bytecode = [];

    /// <summary>
    /// Instruction pointer - current position in bytecode.
    /// </summary>
    private int _ip = 0;

    /// <summary>
    /// Return stack for word calls (Forth-style return stack).
    /// </summary>
    private readonly Stack<int> _returnStack = new();

    /// <summary>
    /// The temporary workspace for calculations. Most operations work with this stack.
    /// Uses typed Values instead of objects to avoid boxing/unboxing overhead.
    /// </summary>
    private readonly Stack<Value> _dataStack = new();

    /// <summary>
    /// Tracks nesting depth while executing a redefined word. When greater than zero,
    /// opcode dispatch will ignore redefinitions and call original/native words.
    /// This prevents infinite recursion when two words are redefined in terms of each other
    /// (e.g., redefining ADD to use MUL and MUL to use ADD).
    /// </summary>
    private int _redefinitionNesting = 0;

    /// <summary>
    /// Managed object heap for interop. We store opaque handles (ints) on the VM stack
    /// that reference real managed objects here.
    /// </summary>
    private readonly Dictionary<int, object> _objectHeap = new();
    private int _nextObjectHandle = 1;

    /// <summary>
    /// When true, the next word name executed via ExecuteWord(name) will be defined
    /// as a VARIABLE (a word that pushes an address of an 8-byte cell allocated in the dictionary),
    /// instead of being executed. This mimics Forth's "VARIABLE NAME" immediate behavior in a
    /// runtime-friendly way.
    /// </summary>
    private bool _variableDefinitionPending = false;
    private bool _docsGetPending = false;
    private bool _docsSetPending = false;

    /// <summary>
    /// Documentation store: canonical word name -> UTF-8 text. We allocate memory for the
    /// string only when DOCS is called to return a pointer/length to programs.
    /// </summary>
    private readonly Dictionary<string, string> _docs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The "HERE" pointer. This is the address where the next byte will be written.
    /// </summary>
    public int Here { get; set; } = 0;

    /// <summary>
    /// The capacity of the underlying array. This shows the "physical block size".
    /// We will manage its growth manually.
    /// </summary>
    public int Capacity => _dictionary.Length;

    /// <summary>
    /// Unified word table. Opcodes are just indices into this table.
    /// This holds the ORIGINAL native implementations that never change.
    /// </summary>
    private readonly List<Word> _words = [];

    /// <summary>
    /// Word name lookup. Maps word names to their index in _words.
    /// </summary>
    private readonly Dictionary<string, int> _wordNames = [];

    /// <summary>
    /// Redefinitions table. When a word is redefined, the new definition goes here.
    /// This allows redefined words to use original opcodes in their bytecode safely.
    /// Checked first during word lookup; falls back to _words if not found.
    /// </summary>
    private readonly Dictionary<string, Word> _redefinitions = [];

    /// <summary>
    /// Alias table. Maps alias names to their canonical word names.
    /// e.g., "!" → "STORE", "@" → "FETCH"
    /// </summary>
    private readonly Dictionary<string, string> _aliases = [];

    /// <summary>
    /// Initializes a new instance of the VMActor with specified initial dictionary capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial capacity of the dictionary memory in bytes.</param>
    public VMActor(int initialCapacity = 1024)
    {
        _dictionary = new byte[initialCapacity];
        PopulateWords();
        RegisterAssemblerWords();
    }

    /// <summary>
    /// Defines a new word in the dictionary.
    /// This allows user code to create new operations from existing ones.
    /// </summary>
    /// <param name="name">The name of the new word.</param>
    /// <param name="bytecode">The bytecode that implements this word.</param>
    public void DefineWord(string name, byte[] bytecode)
    {
        // Resolve alias to canonical name for storage
        var canonicalName = _aliases.TryGetValue(name, out var alias) ? alias : name;

        var word = Word.UserDefined(canonicalName, bytecode);

        // User-defined words go into redefinitions table
        _redefinitions[canonicalName] = word;
    }

    /// <summary>
    /// Redefines an existing word in the dictionary.
    /// Can redefine both user-defined words and built-in instructions.
    /// </summary>
    /// <param name="name">The name of the word to redefine.</param>
    /// <param name="bytecode">The new bytecode implementation.</param>
    /// <returns>True if word was redefined, false if it didn't exist.</returns>
    public bool RedefineWord(string name, byte[] bytecode)
    {
        // Resolve alias to canonical name
        var canonicalName = _aliases.TryGetValue(name, out var alias) ? alias : name;

        // Check if word exists (either in redefinitions or original words)
        if (!_redefinitions.ContainsKey(canonicalName) && !_wordNames.ContainsKey(canonicalName))
            return false;

        var newWord = Word.UserDefined(canonicalName, bytecode);
        _redefinitions[canonicalName] = newWord;

        return true;
    }

    /// <summary>
    /// Redefines a word, throwing an exception if it doesn't exist.
    /// </summary>
    /// <param name="name">The name of the word to redefine.</param>
    /// <param name="bytecode">The new bytecode implementation.</param>
    public void RedefineWordOrThrow(string name, byte[] bytecode)
    {
        if (!RedefineWord(name, bytecode))
            throw new InvalidOperationException($"Cannot redefine word '{name}' - it does not exist in the dictionary.");
    }

    /// <summary>
    /// Gets a word by name from the dictionary.
    /// Resolves aliases, then checks redefinitions, then falls back to original words.
    /// </summary>
    /// <param name="name">The word name to look up.</param>
    /// <returns>The word if found, null otherwise.</returns>
    public Word? GetWord(string name)
    {
        // Resolve alias to canonical name
        var canonicalName = _aliases.TryGetValue(name, out var alias) ? alias : name;
        bool isAlias = canonicalName != name;

        // Check redefinitions first (using canonical name)
        if (_redefinitions.TryGetValue(canonicalName, out var redefinedWord))
        {
            // If queried by alias, return a copy with the alias name
            if (isAlias)
            {
                return redefinedWord.IsNative
                    ? Word.Native(name, redefinedWord.OpCode, redefinedWord.NativeHandler!)
                    : Word.UserDefined(name, redefinedWord.Bytecode);
            }
            return redefinedWord;
        }

        // Fall back to original words (using canonical name)
        if (_wordNames.TryGetValue(canonicalName, out int index))
        {
            var originalWord = _words[index];
            // If queried by alias, return a copy with the alias name
            if (isAlias)
            {
                return originalWord.IsNative
                    ? Word.Native(name, originalWord.OpCode, originalWord.NativeHandler!)
                    : Word.UserDefined(name, originalWord.Bytecode);
            }
            return originalWord;
        }

        return null;
    }

    /// <summary>
    /// Executes a word by name.
    /// </summary>
    /// <param name="name">The name of the word to execute.</param>
    public void ExecuteWord(string name)
    {
        // Intercept DOCS-W NAME: fetch docs for this word name
        if (_docsGetPending)
        {
            _docsGetPending = false;

            var canonical = _aliases.TryGetValue(name, out var alias1) ? alias1 : name;
            if (_docs.TryGetValue(canonical, out var text))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                int addr = Allocate(bytes.Length);
                bytes.AsSpan().CopyTo(_dictionary.AsSpan(addr));
                _dataStack.Push(Value.Pointer(addr));
                _dataStack.Push(Value.Integer(bytes.Length));
            }
            else
            {
                _dataStack.Push(Value.Pointer(0));
                _dataStack.Push(Value.Integer(0));
            }
            return;
        }

        // Intercept DOCS!-W NAME: set docs for this word name from (doc-addr doc-len)
        if (_docsSetPending)
        {
            _docsSetPending = false;
            if (_dataStack.Count < 2)
                throw new InvalidOperationException("DOCS!-W requires doc-address and doc-length on the stack before the name.");
            // Stack has: addr len (top)
            int docLen = (int)_dataStack.Pop().AsInteger();
            int docAddr = _dataStack.Pop().AsPointer();
            if (docLen < 0 || docAddr < 0 || docAddr + docLen > Capacity)
                throw new IndexOutOfRangeException($"Doc read is out of allotted bounds: addr={docAddr}, len={docLen}, capacity={Capacity}");
            string text = Encoding.UTF8.GetString(_dictionary, docAddr, docLen);
            var canonical = _aliases.TryGetValue(name, out var alias2) ? alias2 : name;
            _docs[canonical] = text;
            return;
        }

        // Intercept VARIABLE NAME pattern: if a variable definition is pending,
        // define (or redefine) a word with this name that pushes a freshly allocated address.
        if (_variableDefinitionPending)
        {
            _variableDefinitionPending = false;

            // Allocate 8 bytes (one cell) and build a word that pushes its address
            int addr = Allocate(8);
            var bytecode = new BytecodeBuilder()
                .Push(addr)
                .Op(OpCode.RETURN)
                .Build();

            // Redefine if exists; otherwise define new
            if (GetWord(name) is not null)
                RedefineWord(name, bytecode);
            else
                DefineWord(name, bytecode);
            return;
        }

        var word = GetWord(name) ?? throw new InvalidOperationException($"Word '{name}' not found in dictionary.");
        ExecuteWord(word);
    }

    /// <summary>
    /// Executes a word definition.
    /// </summary>
    private void ExecuteWord(Word word)
    {
        if (word.IsNative)
        {
            word.NativeHandler?.Invoke();
            return;
        }

        // Determine if this is the active redefinition entry for this name.
        // If so, suppress redefinition mapping while executing its bytecode
        // so that opcodes inside refer to original semantics.
        bool suppressRedefinitions = false;
        if (_redefinitions.TryGetValue(word.Name, out var redef) && ReferenceEquals(redef, word))
        {
            // Only suppress when this name originally refers to a built-in/native instruction.
            // For purely user-defined words, keep redefinitions visible inside their bodies.
            if (_wordNames.TryGetValue(word.Name, out var idx))
            {
                var original = _words[idx];
                if (original.IsNative)
                {
                    suppressRedefinitions = true;
                }
            }
        }

        // Save current instruction pointer and switch to word's bytecode
        _returnStack.Push(_ip);
        var savedBytecode = _bytecode;

        _bytecode = word.Bytecode;
        _ip = 0;

        if (suppressRedefinitions)
            _redefinitionNesting++;

        try
        {
            // Execute word's bytecode
            while (_ip < _bytecode.Length)
            {
                byte opcode = _bytecode[_ip++];

                if (opcode == (byte)OpCode.RETURN)
                    break;

                ExecuteOpcode(opcode);
            }
        }
        finally
        {
            // Restore previous bytecode and instruction pointer
            _bytecode = savedBytecode;
            _ip = _returnStack.Pop();

            if (suppressRedefinitions)
                _redefinitionNesting--;
        }
    }

    /// <summary>
    /// Executes an opcode. Opcodes are just indices into the word table.
    /// Checks for redefinitions first before executing the original word.
    /// </summary>
    private void ExecuteOpcode(byte opcode)
    {
        if (opcode >= _words.Count)
            throw new InvalidOperationException($"Invalid opcode/word index: 0x{opcode:X2}");

        var originalWord = _words[opcode];

        // If this is a placeholder/invalid entry, report unknown opcode consistently.
        if (originalWord.Name == "_INVALID")
            throw new InvalidOperationException($"Unknown opcode 0x{opcode:X2}");

        // Check if this word has been redefined. If currently executing inside a
        // redefinition, we intentionally ignore redefinitions to avoid recursive loops
        // and to ensure redefinition bodies use original semantics.
        if (_redefinitionNesting == 0 && _redefinitions.TryGetValue(originalWord.Name, out var redefinedWord))
        {
            ExecuteWord(redefinedWord);
            return;
        }

        ExecuteWord(originalWord);
    }

    /// <summary>
    /// Loads bytecode into the VM for execution.
    /// </summary>
    /// <param name="bytecode">The bytecode to execute.</param>
    public void LoadBytecode(byte[] bytecode)
    {
        _bytecode = bytecode;
        _ip = 0;
    }

    /// <summary>
    /// Executes the loaded bytecode until completion or HALT instruction.
    /// </summary>
    public void Run()
    {
        while (_ip < _bytecode.Length)
        {
            byte opcode = _bytecode[_ip++];

            if (opcode == (byte)OpCode.HALT)
                break;

            ExecuteOpcode(opcode);
        }
    }

    /// <summary>
    /// Gets the current data stack for inspection (useful for testing/debugging).
    /// </summary>
    public IEnumerable<Value> DataStack => _dataStack;

    /// <summary>
    /// Pushes a value onto the VM data stack. Intended for host-side helpers and tests.
    /// </summary>
    /// <param name="value">The value to push.</param>
    public void PushValue(Value value) => _dataStack.Push(value);

    /// <summary>
    /// Pops and returns the top value from the VM data stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the stack is empty.</exception>
    public Value PopValue()
    {
        if (_dataStack.Count == 0)
            throw new InvalidOperationException("Stack underflow.");
        return _dataStack.Pop();
    }

    /// <summary>
    /// Returns the current number of elements on the data stack.
    /// </summary>
    public int DataStackCount() => _dataStack.Count;

    // ===== Native interop helpers =====

    private static Value MapObjectToValue(object obj, string? forWord = null)
    {
        return obj switch
        {
            Value v => v,
            long l => Value.Integer(l),
            int i => Value.Integer(i),
            double d => Value.Float(d),
            float f => Value.Float(f),
            bool b => Value.Boolean(b),
            _ => throw new InvalidOperationException($"Unsupported return type '{obj.GetType().Name}'{(forWord is not null ? $" for native word '{forWord}'" : string.Empty)}.")
        };
    }

    private static object? ConvertValueToType(Value v, Type t, string? forWord = null)
    {
        if (t == typeof(Value)) return v;
        if (t == typeof(long) || t == typeof(Int64)) return v.AsInteger();
        if (t == typeof(int) || t == typeof(Int32)) return (int)v.AsInteger();
        if (t == typeof(double) || t == typeof(Double)) return v.AsFloat();
        if (t == typeof(float) || t == typeof(Single)) return (float)v.AsFloat();
        if (t == typeof(bool) || t == typeof(Boolean)) return v.AsBoolean();
        throw new InvalidOperationException($"Unsupported parameter type '{t.Name}'{(forWord is not null ? $" for native word '{forWord}'" : string.Empty)}.");
    }

    private int StoreObject(object obj)
    {
        var handle = _nextObjectHandle++;
        _objectHeap[handle] = obj;
        return handle;
    }

    private object ResolveObject(int handle)
    {
        if (!_objectHeap.TryGetValue(handle, out var obj))
            throw new InvalidOperationException($"Invalid object handle: {handle}");
        return obj;
    }

    /// <summary>
    /// Defines a native word by binding a managed delegate. When executed, the VM will:
    /// - Pop as many arguments from the stack as the delegate's parameter count (top is last parameter)
    /// - Convert each <see cref="Value"/> to the corresponding parameter type (supported: long/int, double/float, bool, Value)
    /// - Invoke the delegate
    /// - If the delegate returns a value (non-void), convert it back to <see cref="Value"/> and push it on the stack
    /// This allows ergonomic interop with native C# methods without writing manual stack wrappers.
    /// </summary>
    /// <param name="name">The word name to bind.</param>
    /// <param name="impl">The managed delegate to invoke.</param>
    /// <exception cref="InvalidOperationException">Thrown for unsupported parameter/return types or insufficient stack items.</exception>
    public void DefineNative(string name, Delegate impl)
    {
        // Resolve alias to canonical name for storage
        var canonicalName = _aliases.TryGetValue(name, out var alias) ? alias : name;

        var method = impl.Method;
        var parameters = method.GetParameters();
        var returnsVoid = method.ReturnType == typeof(void);

        void handler()
        {
            int arity = parameters.Length;
            if (_dataStack.Count < arity)
                throw new InvalidOperationException($"Word '{name}' requires {arity} argument(s) on the stack.");

            // Pop in reverse to build left-to-right argument order
            var argValues = new Value[arity];
            for (int i = arity - 1; i >= 0; i--)
                argValues[i] = _dataStack.Pop();

            var callArgs = new object?[arity];
            for (int i = 0; i < arity; i++)
                callArgs[i] = ConvertValueToType(argValues[i], parameters[i].ParameterType, name);

            var result = impl.DynamicInvoke(callArgs);

            if (!returnsVoid)
            {
                if (result is null)
                    throw new InvalidOperationException($"Native word '{name}' returned null for non-void method.");
                _dataStack.Push(MapObjectToValue(result, name));
            }
        }

        _redefinitions[canonicalName] = Word.Native(canonicalName, 0, handler);
    }

    /// <summary>
    /// Defines a word that constructs a .NET object using the specified constructor and
    /// pushes an opaque handle (Value.Pointer) to the VM stack. The handle can be used
    /// with instance-by-handle words.
    /// Stack: ( ctor-arg1 ... ctor-argN -- handle )
    /// </summary>
    public void DefineConstructor(string name, Type type, params Type[] parameterTypes)
    {
        var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, types: parameterTypes, modifiers: null)
                   ?? throw new InvalidOperationException($"Constructor not found: {type.FullName}({string.Join(", ", parameterTypes.Select(t => t.Name))})");

        var parameters = ctor.GetParameters();
        void handler()
        {
            int arity = parameters.Length;
            if (_dataStack.Count < arity)
                throw new InvalidOperationException($"Word '{name}' requires {arity} argument(s) on the stack.");

            var argValues = new Value[arity];
            for (int i = arity - 1; i >= 0; i--)
                argValues[i] = _dataStack.Pop();

            var callArgs = new object?[arity];
            for (int i = 0; i < arity; i++)
                callArgs[i] = ConvertValueToType(argValues[i], parameters[i].ParameterType, name);

            var obj = ctor.Invoke(callArgs);
            int handle = StoreObject(obj!);
            _dataStack.Push(Value.Pointer(handle));
        }

        _redefinitions[name] = Word.Native(name, 0, handler);
    }

    /// <summary>
    /// Defines a word that calls an instance method on a .NET object identified by a handle
    /// (Value.Pointer) at the bottom of the argument group. The stack layout must be:
    /// ( handle arg1 ... argN -- [result] ) where result is pushed if the method is non-void.
    /// </summary>
    public void DefineInstanceByHandle(string name, Type type, string methodOrPropertyName, params Type[] parameterTypes)
    {
        var mi = type.GetMethod(methodOrPropertyName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: parameterTypes, modifiers: null);

        // If a method isn't found and no parameters were specified, try binding a property getter
        if (mi is null && parameterTypes.Length == 0)
        {
            var pi = type.GetProperty(methodOrPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (pi is not null && pi.CanRead)
            {
                void getterHandler()
                {
                    if (_dataStack.Count < 1)
                        throw new InvalidOperationException($"Word '{name}' requires 1 value on the stack (handle).");

                    int handle = _dataStack.Pop().AsPointer();
                    var target = ResolveObject(handle);
                    if (!type.IsInstanceOfType(target))
                        throw new InvalidOperationException($"Handle does not reference instance of {type.FullName}.");

                    var value = pi.GetValue(target)!;
                    _dataStack.Push(MapObjectToValue(value, name));
                }

                _redefinitions[name] = Word.Native(name, 0, getterHandler);
                return;
            }
        }

        if (mi is null)
            throw new InvalidOperationException($"Instance method not found: {type.FullName}.{methodOrPropertyName}({string.Join(", ", parameterTypes.Select(t => t.Name))})");

        var parameters = mi.GetParameters();
        bool returnsVoid = mi.ReturnType == typeof(void);

        void handler()
        {
            int arity = parameters.Length;
            if (_dataStack.Count < arity + 1)
                throw new InvalidOperationException($"Word '{name}' requires {arity + 1} value(s) on the stack (handle + {arity} args).");

            // Pop args first (top of stack) then the handle (receiver at bottom of group)
            var argValues = new Value[arity];
            for (int i = arity - 1; i >= 0; i--)
                argValues[i] = _dataStack.Pop();

            int handle = _dataStack.Pop().AsPointer();
            var target = ResolveObject(handle);
            if (!type.IsInstanceOfType(target))
                throw new InvalidOperationException($"Handle does not reference instance of {type.FullName}.");

            var callArgs = new object?[arity];
            for (int i = 0; i < arity; i++)
                callArgs[i] = ConvertValueToType(argValues[i], parameters[i].ParameterType, name);

            var result = mi.Invoke(target, callArgs);
            if (!returnsVoid)
                _dataStack.Push(MapObjectToValue(result!, name));
        }

        _redefinitions[name] = Word.Native(name, 0, handler);
    }

    /// <summary>
    /// Defines a native word by binding a static method via reflection.
    /// Example: DefineNativeStatic("MAX", typeof(System.Math), nameof(System.Math.Max), typeof(int), typeof(int))
    /// </summary>
    public void DefineNativeStatic(string name, Type declaringType, string methodName, params Type[] parameterTypes)
    {
        var mi = declaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, binder: null, types: parameterTypes, modifiers: null)
                 ?? throw new InvalidOperationException($"Static method not found: {declaringType.FullName}.{methodName}({string.Join(", ", parameterTypes.Select(t => t.Name))})");
        var del = CreateDelegateForMethod(mi, target: null);
        DefineNative(name, del);
    }

    /// <summary>
    /// Defines a native word by binding an instance method via reflection.
    /// Example: DefineNativeInstance("NEXT", new Random(0), nameof(Random.Next), typeof(int), typeof(int))
    /// </summary>
    public void DefineNativeInstance(string name, object target, string methodName, params Type[] parameterTypes)
    {
        var mi = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: parameterTypes, modifiers: null)
                 ?? throw new InvalidOperationException($"Instance method not found: {target.GetType().FullName}.{methodName}({string.Join(", ", parameterTypes.Select(t => t.Name))})");
        var del = CreateDelegateForMethod(mi, target);
        DefineNative(name, del);
    }

    private static Delegate CreateDelegateForMethod(MethodInfo mi, object? target)
    {
        var paramTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
        var returnType = mi.ReturnType;

        Type delegateType;
        if (returnType == typeof(void))
        {
            delegateType = Expression.GetActionType(paramTypes);
        }
        else
        {
            var types = new List<Type>(paramTypes) { returnType };
            delegateType = Expression.GetFuncType(types.ToArray());
        }

        return target is null
            ? mi.CreateDelegate(delegateType)
            : mi.CreateDelegate(delegateType, target);
    }

    /// <summary>
    /// Populates the instruction table with opcode handlers.
    /// This is where the VM's core functionality is defined.
    /// </summary>
    /// <summary>
    /// Populates the unified word table with all built-in words.
    /// Opcodes are just indices into this table.
    /// </summary>
    private void PopulateWords()
    {
        // Ensure the list can hold all possible opcodes
        while (_words.Count <= 0xFF)
            _words.Add(Word.Native("_INVALID", 0x00, () => throw new InvalidOperationException($"Invalid opcode at index {_words.Count}")));

        // Index 0x01 = OpCode.PUSH
        _words[(byte)OpCode.PUSH] = Word.Native("PUSH", (byte)OpCode.PUSH, HandlePush);
        _wordNames["PUSH"] = (byte)OpCode.PUSH;

        // Index 0x02 = OpCode.PRINT
        _words[(byte)OpCode.PRINT] = Word.Native("PRINT", (byte)OpCode.PRINT, HandlePrint);
        _wordNames["PRINT"] = (byte)OpCode.PRINT;

        // Index 0x03 = OpCode.POP
        _words[(byte)OpCode.POP] = Word.Native("POP", (byte)OpCode.POP, () => _dataStack.Pop());
        _wordNames["POP"] = (byte)OpCode.POP;

        // Index 0x04 = OpCode.DUP
        _words[(byte)OpCode.DUP] = Word.Native("DUP", (byte)OpCode.DUP, () => _dataStack.Push(_dataStack.Peek()));
        _wordNames["DUP"] = (byte)OpCode.DUP;

        // Index 0x10 = OpCode.ADD
        _words[(byte)OpCode.ADD] = Word.Native("ADD", (byte)OpCode.ADD, () => HandleBinaryOp((a, b) =>
        {
            if (a.Type == Value.ValueType.Float || b.Type == Value.ValueType.Float)
                return Value.Float(a.AsFloat() + b.AsFloat());
            return Value.Integer(a.AsInteger() + b.AsInteger());
        }));
        _wordNames["ADD"] = (byte)OpCode.ADD;

        // Index 0x11 = OpCode.SUB
        _words[(byte)OpCode.SUB] = Word.Native("SUB", (byte)OpCode.SUB, () => HandleBinaryOp((a, b) =>
        {
            if (a.Type == Value.ValueType.Float || b.Type == Value.ValueType.Float)
                return Value.Float(a.AsFloat() - b.AsFloat());
            return Value.Integer(a.AsInteger() - b.AsInteger());
        }));
        _wordNames["SUB"] = (byte)OpCode.SUB;

        // Index 0x12 = OpCode.MUL
        _words[(byte)OpCode.MUL] = Word.Native("MUL", (byte)OpCode.MUL, () => HandleBinaryOp((a, b) =>
        {
            if (a.Type == Value.ValueType.Float || b.Type == Value.ValueType.Float)
                return Value.Float(a.AsFloat() * b.AsFloat());
            return Value.Integer(a.AsInteger() * b.AsInteger());
        }));
        _wordNames["MUL"] = (byte)OpCode.MUL;

        // Index 0x13 = OpCode.DIV
        _words[(byte)OpCode.DIV] = Word.Native("DIV", (byte)OpCode.DIV, () => HandleBinaryOp((a, b) =>
        {
            if (a.Type == Value.ValueType.Float || b.Type == Value.ValueType.Float)
                return Value.Float(a.AsFloat() / b.AsFloat());
            return Value.Integer(a.AsInteger() / b.AsInteger());
        }));
        _wordNames["DIV"] = (byte)OpCode.DIV;

        // Index 0x20 = OpCode.STORE
        _words[(byte)OpCode.STORE] = Word.Native("STORE", (byte)OpCode.STORE, HandleStore);
        _wordNames["STORE"] = (byte)OpCode.STORE;

        // Index 0x21 = OpCode.FETCH
        _words[(byte)OpCode.FETCH] = Word.Native("FETCH", (byte)OpCode.FETCH, HandleFetch);
        _wordNames["FETCH"] = (byte)OpCode.FETCH;

        // Index 0x30 = OpCode.CALL
        _words[(byte)OpCode.CALL] = Word.Native("CALL", (byte)OpCode.CALL, HandleCall);
        _wordNames["CALL"] = (byte)OpCode.CALL;

        // Index 0x31 = OpCode.RETURN - handled specially in ExecuteWord
        _words[(byte)OpCode.RETURN] = Word.Native("RETURN", (byte)OpCode.RETURN, () => { });
        _wordNames["RETURN"] = (byte)OpCode.RETURN;

        // Index 0x32 = OpCode.DEFINE
        _words[(byte)OpCode.DEFINE] = Word.Native("DEFINE", (byte)OpCode.DEFINE, HandleDefine);
        _wordNames["DEFINE"] = (byte)OpCode.DEFINE;

        // Index 0x33 = OpCode.REDEFINE
        _words[(byte)OpCode.REDEFINE] = Word.Native("REDEFINE", (byte)OpCode.REDEFINE, HandleRedefine);
        _wordNames["REDEFINE"] = (byte)OpCode.REDEFINE;

        // Index 0xFF = OpCode.HALT - handled specially in Run/Step
        _words[(byte)OpCode.HALT] = Word.Native("HALT", (byte)OpCode.HALT, () => { });
        _wordNames["HALT"] = (byte)OpCode.HALT;

        // Add Forth aliases using the alias table
        _aliases["!"] = "STORE";
        _aliases["@"] = "FETCH";
    }

    /// <summary>
    /// Forth's "ALLOT". Reserves a given number of bytes.
    /// </summary>
    public void Allot(int byteCount)
    {
        EnsureCapacity(byteCount);
        Here += byteCount;
    }

    /// <summary>
    /// Allocates a contiguous block of bytes in the dictionary memory and returns the base address.
    /// Advances the HERE pointer. Similar to Forth's ALLOT but returns the starting address.
    /// </summary>
    /// <param name="byteCount">Number of bytes to allocate.</param>
    /// <returns>The base address (pointer) to the allocated block.</returns>
    public int Allocate(int byteCount)
    {
        EnsureCapacity(byteCount);
        int baseAddr = Here;
        Here += byteCount;
        return baseAddr;
    }

    private void EnsureCapacity(int requiredBytes)
    {
        if (Here + requiredBytes > Capacity)
        {
            // The dictionary needs to grow. The common strategy is to double the size,
            // or grow to at least the required size if that's even larger.
            int newCapacity = Math.Max(Capacity * 2, Here + requiredBytes);

            // This is the C# equivalent of C's `realloc`. It creates a new, larger
            // array and copies the old data over automatically.
            Array.Resize(ref _dictionary, newCapacity);
        }
    }

    /// <summary>
    /// Writes a 64-bit integer to dictionary memory at the specified address.
    /// </summary>
    /// <param name="address">Byte address within the dictionary.</param>
    /// <param name="value">The 64-bit value to write.</param>
    public void Write64(int address, long value)
    {
        if (address + 8 > Here)
            throw new IndexOutOfRangeException("Memory write is out of allotted bounds.");
        Span<byte> slice = _dictionary.AsSpan(address, 8);
        BitConverter.TryWriteBytes(slice, value);
    }

    /// <summary>
    /// Reads a 64-bit integer from dictionary memory at the specified address.
    /// </summary>
    /// <param name="address">Byte address within the dictionary.</param>
    /// <returns>The 64-bit integer read.</returns>
    public long Read64(int address)
    {
        if (address + 8 > Here)
            throw new IndexOutOfRangeException("Memory read is out of allotted bounds.");
        ReadOnlySpan<byte> slice = _dictionary.AsSpan(address, 8);
        return BitConverter.ToInt64(slice);
    }

    /// <summary>
    /// Writes a single byte to dictionary memory at the specified address.
    /// </summary>
    public void Write8(int address, byte value)
    {
        if (address + 1 > Here)
            throw new IndexOutOfRangeException("Memory write is out of allotted bounds.");
        _dictionary[address] = value;
    }

    /// <summary>
    /// Reads a single byte from dictionary memory at the specified address.
    /// </summary>
    public byte Read8(int address)
    {
        if (address + 1 > Here)
            throw new IndexOutOfRangeException("Memory read is out of allotted bounds.");
        return _dictionary[address];
    }

    private void RegisterAssemblerWords()
    {
        // HERE: ( -- addr )
        DefineNative("HERE", () => Here);

        // ALLOT: ( n -- )
        DefineNative("ALLOT", (Action<int>)(n => Allot(n)));

        // EMIT: ( b -- ) write a byte at HERE and advance
        DefineNative("EMIT", (Action<int>)(b => { Write8(Here, (byte)(b & 0xFF)); Here += 1; }));

        // EMIT64: ( x -- ) write 8 bytes at HERE and advance
        DefineNative("EMIT64", (Action<long>)(x => { Write64(Here, x); Here += 8; }));

        // SWAP: ( a b -- b a )
        DefineNative("SWAP", () => { var a = _dataStack.Pop(); var b = _dataStack.Pop(); _dataStack.Push(a); _dataStack.Push(b); });

        // VARIABLE: ( -- )  The next word name invoked will be defined as a variable
        // (a word that pushes an allocated 8-byte cell address).
        DefineNative("VARIABLE", () => _variableDefinitionPending = true);

        // Optional helpers: generic FIELD@ and FIELD! for (addr offset -- value) and (value addr offset -- )
        var fieldGet = new BytecodeBuilder().Op(OpCode.ADD).Op(OpCode.FETCH).Op(OpCode.RETURN).Build();
        DefineWord("FIELD@", fieldGet);

        var fieldSet = new BytecodeBuilder().Op(OpCode.ADD).Op(OpCode.STORE).Op(OpCode.RETURN).Build();
        DefineWord("FIELD!", fieldSet);

        // DOCS: ( name-addr name-len -- doc-addr doc-len | 0 0 )
        DefineNative("DOCS", () =>
        {
            if (_dataStack.Count < 2)
                throw new InvalidOperationException("DOCS requires name-address and name-length on the stack.");

            int nameLen = (int)_dataStack.Pop().AsInteger();
            int nameAddr = _dataStack.Pop().AsPointer();

            if (nameLen < 0 || nameAddr < 0 || nameAddr + nameLen > Here)
                throw new IndexOutOfRangeException("Name read is out of allotted bounds.");

            string name = Encoding.UTF8.GetString(_dictionary, nameAddr, nameLen);
            var canonical = _aliases.TryGetValue(name, out var alias) ? alias : name;

            if (_docs.TryGetValue(canonical, out var text))
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                int addr = Allocate(bytes.Length);
                bytes.AsSpan().CopyTo(_dictionary.AsSpan(addr));
                _dataStack.Push(Value.Pointer(addr));
                _dataStack.Push(Value.Integer(bytes.Length));
            }
            else
            {
                _dataStack.Push(Value.Pointer(0));
                _dataStack.Push(Value.Integer(0));
            }
        });

        // DOCS!: ( name-addr name-len doc-addr doc-len -- )
        DefineNative("DOCS!", () =>
        {
            if (_dataStack.Count < 4)
                throw new InvalidOperationException("DOCS! requires name-address, name-length, doc-address, and doc-length on the stack.");

            int docLen = (int)_dataStack.Pop().AsInteger();
            int docAddr = _dataStack.Pop().AsPointer();
            int nameLen = (int)_dataStack.Pop().AsInteger();
            int nameAddr = _dataStack.Pop().AsPointer();

            if (nameLen < 0 || nameAddr < 0 || nameAddr + nameLen > Here)
                throw new IndexOutOfRangeException("Name read is out of allotted bounds.");
            if (docLen < 0 || docAddr < 0 || docAddr + docLen > Here)
                throw new IndexOutOfRangeException("Doc read is out of allotted bounds.");

            string name = Encoding.UTF8.GetString(_dictionary, nameAddr, nameLen);
            var canonical = _aliases.TryGetValue(name, out var alias) ? alias : name;

            // Read provided bytes into managed string storage
            var span = _dictionary.AsSpan(docAddr, docLen);
            string text = Encoding.UTF8.GetString(span);
            _docs[canonical] = text;
        });

        // DOCS-W: ( -- doc-addr doc-len | 0 0 ) for the NEXT word called
        DefineNative("DOCS-W", () => { _docsGetPending = true; });

        // DOCS!-W: ( doc-addr doc-len -- ) sets docs for the NEXT word called
        DefineNative("DOCS!-W", () => { _docsSetPending = true; });

        // Seed default docs
        PrepopulateDocs();
    }

    /// <summary>
    /// Store default documentation strings into dictionary memory and index them.
    /// </summary>
    private void PrepopulateDocs()
    {
        void Seed(string name, string text)
        {
            var canonical = _aliases.TryGetValue(name, out var alias) ? alias : name;
            _docs[canonical] = text;
        }

        Seed("PUSH", "Push immediate 64-bit integer onto the stack. Usage: bytecode only.");
        Seed("PRINT", "Print top of stack. ( x -- )");
        Seed("POP", "Discard top of stack. ( x -- )");
        Seed("DUP", "Duplicate top of stack. ( x -- x x )");
        Seed("ADD", "Add top two values. Mixed int/float promotion. ( a b -- a+b )");
        Seed("SUB", "Subtract top two values. ( a b -- a-b )");
        Seed("MUL", "Multiply top two values. ( a b -- a*b )");
        Seed("DIV", "Divide top two values. ( a b -- a/b )");
        Seed("STORE", "Store 64-bit value at address. ( value addr -- ) Alias: !");
        Seed("FETCH", "Fetch 64-bit value from address. ( addr -- value ) Alias: @");
        Seed("CALL", "Call a word by index (bytecode only). ( -- )");
        Seed("RETURN", "Return from a word call (bytecode only). ( -- )");
        Seed("DEFINE", "Define a new word from memory. ( name-addr name-len bytecode-addr bytecode-len -- )");
        Seed("REDEFINE", "Redefine existing word. ( name-addr name-len bytecode-addr bytecode-len -- success )");
        Seed("HALT", "Stop bytecode execution (bytecode only). ( -- )");

        Seed("HERE", "Return current HERE pointer. ( -- addr )");
        Seed("ALLOT", "Reserve N bytes in dictionary. ( n -- )");
        Seed("EMIT", "Write a byte at HERE and advance. ( b -- )");
        Seed("EMIT64", "Write 64-bit value at HERE and advance by 8. ( x -- )");
        Seed("SWAP", "Swap top two stack items. ( a b -- b a )");
        Seed("FIELD@", "Read field at offset. ( addr offset -- value )");
        Seed("FIELD!", "Write field at offset. ( value addr offset -- )");
        Seed("VARIABLE", "Next executed word name becomes a variable that pushes its address. ( -- ) then NAME");
        Seed("DOCS", "Lookup documentation for a word. ( name-addr name-len -- doc-addr doc-len | 0 0 )");
        Seed("DOCS!", "Set documentation for a word. ( name-addr name-len doc-addr doc-len -- )");
        Seed("DOCS-W", "Lookup docs for the NEXT word invoked. Usage: DOCS-W NAME  ( -- doc-addr doc-len )");
        Seed("DOCS!-W", "Set docs for the NEXT word invoked from given bytes. Usage: doc-addr doc-len DOCS!-W NAME");

        // Aliases
        Seed("@", "Alias of FETCH. ( addr -- value )");
        Seed("!", "Alias of STORE. ( value addr -- )");
    }

    /// <summary>
    /// Execute a single instruction step.
    /// </summary>
    public void Step()
    {
        byte opcode = _bytecode[_ip++];

        if (opcode == (byte)OpCode.HALT)
            return;

        ExecuteOpcode(opcode);
    }

    private void HandlePush()
    {
        if (_ip < _bytecode.Length)
        {
            // Read the next 8 bytes as a long integer value
            if (_ip + 8 > _bytecode.Length)
                throw new InvalidOperationException("PUSH instruction requires 8 bytes for operand.");

            long value = BitConverter.ToInt64(_bytecode, _ip);
            _ip += 8;
            _dataStack.Push(Value.Integer(value));
        }
        else
        {
            throw new InvalidOperationException("PUSH instruction requires an operand.");
        }
    }

    private void HandleCall()
    {
        // Read word index from bytecode
        if (_ip + 2 > _bytecode.Length)
            throw new InvalidOperationException("CALL instruction requires 2 bytes for word index.");

        ushort wordIndex = BitConverter.ToUInt16(_bytecode, _ip);
        _ip += 2;

        if (wordIndex >= _words.Count)
            throw new InvalidOperationException($"Invalid word index: {wordIndex}");

        ExecuteWord(_words[wordIndex]);
    }

    private void HandleDefine()
    {
        // DEFINE expects on stack: ( bytecode-length bytecode-address name-length name-address -- )
        // This allows defining words from bytecode stored in the dictionary

        if (_dataStack.Count < 4)
            throw new InvalidOperationException("DEFINE requires 4 values on stack: name-address, name-length, bytecode-address, bytecode-length");

        int bytecodeLength = (int)_dataStack.Pop().AsInteger();
        int bytecodeAddress = _dataStack.Pop().AsPointer();
        int nameLength = (int)_dataStack.Pop().AsInteger();
        int nameAddress = _dataStack.Pop().AsPointer();

        // Read name from dictionary memory
        if (nameAddress + nameLength > Here)
            throw new IndexOutOfRangeException("Name read is out of allotted bounds.");

        byte[] nameBytes = _dictionary[nameAddress..(nameAddress + nameLength)];
        string name = System.Text.Encoding.UTF8.GetString(nameBytes);

        // Read bytecode from dictionary memory
        if (bytecodeAddress + bytecodeLength > Here)
            throw new IndexOutOfRangeException("Bytecode read is out of allotted bounds.");

        byte[] bytecode = _dictionary[bytecodeAddress..(bytecodeAddress + bytecodeLength)];

        // Define the word
        DefineWord(name, bytecode);
    }

    private void HandleRedefine()
    {
        // REDEFINE expects on stack: ( bytecode-length bytecode-address name-length name-address -- success-flag )
        // Returns true (1) if word was redefined, false (0) if it didn't exist

        if (_dataStack.Count < 4)
            throw new InvalidOperationException("REDEFINE requires 4 values on stack: name-address, name-length, bytecode-address, bytecode-length");

        int bytecodeLength = (int)_dataStack.Pop().AsInteger();
        int bytecodeAddress = _dataStack.Pop().AsPointer();
        int nameLength = (int)_dataStack.Pop().AsInteger();
        int nameAddress = _dataStack.Pop().AsPointer();

        // Read name from dictionary memory
        if (nameAddress + nameLength > Here)
            throw new IndexOutOfRangeException("Name read is out of allotted bounds.");

        byte[] nameBytes = _dictionary[nameAddress..(nameAddress + nameLength)];
        string name = System.Text.Encoding.UTF8.GetString(nameBytes);

        // Read bytecode from dictionary memory
        if (bytecodeAddress + bytecodeLength > Here)
            throw new IndexOutOfRangeException("Bytecode read is out of allotted bounds.");

        byte[] bytecode = _dictionary[bytecodeAddress..(bytecodeAddress + bytecodeLength)];

        // Redefine the word and push success flag
        bool success = RedefineWord(name, bytecode);
        _dataStack.Push(Value.Boolean(success));
    }

    private void HandlePrint()
    {
        if (_dataStack.Count > 0)
        {
            Console.WriteLine($"> {_dataStack.Peek()}");
        }
    }

    private void HandleBinaryOp(Func<Value, Value, Value> operation)
    {
        if (_dataStack.Count < 2)
            throw new InvalidOperationException("Binary operation requires two operands on the stack.");
        var b = _dataStack.Pop();
        var a = _dataStack.Pop();
        _dataStack.Push(operation(a, b));
    }

    private void HandleStore() // Expects (value, address) on stack
    {
        if (_dataStack.Count < 2)
            throw new InvalidOperationException("STORE requires a value and an address on the stack.");
        int address = _dataStack.Pop().AsPointer();
        long value = _dataStack.Pop().AsInteger();

        if (address + 8 > Here)
            throw new IndexOutOfRangeException("Memory write is out of allotted bounds.");

        Span<byte> slice = _dictionary.AsSpan(address, 8);
        BitConverter.TryWriteBytes(slice, value);
    }

    private void HandleFetch() // Expects (address) on stack
    {
        if (_dataStack.Count < 1)
            throw new InvalidOperationException("FETCH requires an address on the stack.");
        int address = _dataStack.Pop().AsPointer();

        if (address + 8 > Here)
            throw new IndexOutOfRangeException("Memory read is out of allotted bounds.");

        ReadOnlySpan<byte> slice = _dictionary.AsSpan(address, 8);
        _dataStack.Push(Value.Integer(BitConverter.ToInt64(slice)));
    }
}
