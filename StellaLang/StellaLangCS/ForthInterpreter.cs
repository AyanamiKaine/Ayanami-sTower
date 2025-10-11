using System;
using System.Collections.Generic;

namespace StellaLang;
#pragma warning disable CS0414 

/// <summary>
/// A FORTH interpreter that compiles FORTH code to StellaLang VM bytecode.
/// Implements the traditional FORTH inner and outer interpreter loops.
/// </summary>
public class ForthInterpreter
{
    /// <summary>
    /// The underlying VM that executes the compiled bytecode.
    /// </summary>
    private readonly VM _vm;

    /// <summary>
    /// Dictionary mapping FORTH word names to their definitions.
    /// </summary>
    private readonly Dictionary<string, WordDefinition> _dictionary;

    /// <summary>
    /// The data space for compiled definitions and variables.
    /// </summary>
    private readonly List<byte> _dataSpace;

    /// <summary>
    /// Global code space for all compiled colon definitions.
    /// This allows proper CALL/RET semantics instead of inlining.
    /// Each word's ExecutionToken is an offset into this code space.
    /// </summary>
    private readonly List<byte> _codeSpace;

    /// <summary>
    /// Cached byte array version of _codeSpace for VM execution.
    /// This is updated whenever _codeSpace changes (when a new word is compiled).
    /// We keep this cached to avoid creating a new array for every word execution.
    /// </summary>
    private byte[] _codeSpaceArray;

    /// <summary>
    /// The current compilation pointer (HERE in FORTH).
    /// Points to the next free location in data space.
    /// </summary>
    private int _here;

    /// <summary>
    /// Flag indicating if the interpreter is in compilation mode or interpretation mode.
    /// True = compilation mode, False = interpretation mode.
    /// </summary>
    private bool _compileMode;

    /// <summary>
    /// The input buffer containing the current line of FORTH source code.
    /// </summary>
    private string _inputBuffer;

    /// <summary>
    /// Current position in the input buffer.
    /// </summary>
    private int _inputPosition;

    /// <summary>
    /// The current base for number conversion (10 = decimal, 16 = hexadecimal, etc.).
    /// </summary>
    private int _base;

    /// <summary>
    /// Stack for tracking compilation state (used for control structures).
    /// Stores labels for IF/THEN/ELSE, loop structures, etc.
    /// </summary>
    private readonly Stack<string> _compileStack;

    /// <summary>
    /// Counter for generating unique labels during compilation.
    /// </summary>
    private int _labelCounter;

    /// <summary>
    /// The code builder used for generating bytecode.
    /// </summary>
    private CodeBuilder _codeBuilder;
    // Next syscall id to allocate for primitives that use handlers but should be
    // compilable. Starts in a high range to avoid colliding with reserved ids.
    private long _nextHandlerSyscallId = (long)SyscallId.DynamicHandlerBase;

    /// <summary>
    /// The name of the word currently being compiled.
    /// </summary>
    private string _currentWordName;

    /// <summary>
    /// The exit label for the current word being compiled (for EXIT to jump to).
    /// </summary>
    private string? _currentExitLabel;

    /// <summary>
    /// The start label for the current word being compiled (for RECURSE to jump to).
    /// </summary>
    private string? _currentStartLabel;

    /// <summary>
    /// The name of the most recently CREATE'd word (for DOES> to modify).
    /// </summary>
    private string? _lastCreatedWord;

    /// <summary>
    /// Flag indicating if we're currently compiling DOES> code in a definition.
    /// </summary>
    private bool _compilingDoesCode;
    /// <summary>
    /// Tracks whether a CREATE was emitted while compiling the current colon definition.
    /// Used to validate that DOES> appears after a CREATE in the same definition.
    /// </summary>
    private bool _createEmittedInCurrentDefinition;

    /// <summary>
    /// The bytecode position where DOES> code starts in the current word being compiled.
    /// </summary>
    private List<int> _doesCodeStartPositions = [];

    /// <summary>
    /// Initializes a new instance of the FORTH interpreter.
    /// </summary>
    /// <param name="vm">The VM instance to use for execution.</param>
    public ForthInterpreter(VM vm)
    {
        _vm = vm;
        _dictionary = [];
        _dataSpace = [];
        _codeSpace = [];
        _codeSpaceArray = [];
        _here = 0;
        _compileMode = false;
        _inputBuffer = string.Empty;
        _inputPosition = 0;
        _base = 10;
        _compileStack = new Stack<string>();
        _labelCounter = 0;
        _codeBuilder = new CodeBuilder();
        _currentWordName = string.Empty;

        InitializePrimitives();
    }

    /// <summary>
    /// Initializes a new instance of the FORTH interpreter with a default VM.
    /// </summary>
    public ForthInterpreter() : this(new VM())
    {
    }

    #region Outer Interpreter

    /// <summary>
    /// The outer interpreter loop - reads and processes input.
    /// Tokenizes input, looks up words in the dictionary, and either executes or compiles them.
    /// </summary>
    /// <param name="input">The line of FORTH source code to interpret.</param>
    public void Interpret(string input)
    {
        _inputBuffer = input;
        _inputPosition = 0;

        string? word;
        while ((word = ReadWord()) != null)
        {
            ProcessWord(word);
        }
    }

    /// <summary>
    /// The REPL (Read-Eval-Print Loop) - continuously reads input lines and interprets them.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void REPL()
    {
        throw new NotImplementedException("REPL not yet implemented");
    }

    /// <summary>
    /// Processes a single word from the input stream.
    /// Called by the outer interpreter for each token.
    /// </summary>
    /// <param name="word">The word to process.</param>
    private void ProcessWord(string word)
    {
        //try { Console.Error.WriteLine($"ProcessWord: '{word}' compileMode={_compileMode} pos={_inputPosition}"); } catch { }
        // Try to find word in dictionary
        var definition = FindWord(word);

        if (definition != null)
        {
            // In compilation mode, compile the word (unless it's immediate)
            if (_compileMode && !definition.IsImmediate)
            {
                CompileWord(definition);
            }
            else
            {
                // Execute the word
                if (definition.Type == WordType.Primitive)
                {
                    // Primitives can execute either via handler (for special cases) or bytecode (preferred)
                    if (definition.PrimitiveHandler != null)
                    {
                        definition.PrimitiveHandler(this);
                    }
                    else if (definition.CompiledCode != null)
                    {
                        // Execute the primitive's bytecode
                        _vm.Execute(definition.CompiledCode);
                    }
                    else
                    {
                        throw new CompilationException($"Primitive '{definition.Name}' has neither handler nor bytecode");
                    }
                }
                else if (definition.Type == WordType.ColonDefinition)
                {
                    ExecuteColonDefinition(definition);
                }
                else if (definition.Type == WordType.Variable)
                {
                    // Push the variable's address onto the stack
                    _vm.DataStack.PushLong(definition.DataAddress);
                }
                else
                {
                    throw new NotImplementedException($"Word type {definition.Type} not yet supported");
                }
            }
        }
        else
        {
            // Try to parse as number
            if (TryParseNumber(word, out long intValue, out double doubleValue, out bool isFloat))
            {
                if (_compileMode)
                {
                    // Compile the literal
                    if (isFloat)
                    {
                        CompileFloatLiteral(doubleValue);
                    }
                    else
                    {
                        CompileLiteral(intValue);
                    }
                }
                else
                {
                    // Execute: push to stack
                    if (isFloat)
                    {
                        _vm.FloatStack.PushDouble(doubleValue);
                    }
                    else
                    {
                        _vm.DataStack.PushLong(intValue);
                    }
                }
            }
            else
            {
                throw new UnknownWordException(word, $"Unknown word: {word}");
            }
        }
    }

    /// <summary>
    /// Reads the next word (token) from the input buffer.
    /// Words are delimited by whitespace.
    /// </summary>
    /// <returns>The next word, or null if end of input.</returns>
    private string? ReadWord()
    {
        SkipWhitespace();

        if (_inputPosition >= _inputBuffer.Length)
            return null;

        int start = _inputPosition;
        while (_inputPosition < _inputBuffer.Length && !char.IsWhiteSpace(_inputBuffer[_inputPosition]))
        {
            _inputPosition++;
        }

        return _inputBuffer[start.._inputPosition];
    }

    /// <summary>
    /// Skips whitespace in the input buffer.
    /// </summary>
    private void SkipWhitespace()
    {
        while (_inputPosition < _inputBuffer.Length && char.IsWhiteSpace(_inputBuffer[_inputPosition]))
        {
            _inputPosition++;
        }
    }

    #endregion

    #region Inner Interpreter

    /// <summary>
    /// The inner interpreter loop - executes compiled FORTH definitions.
    /// Fetches execution tokens and dispatches to their implementations.
    /// This is called when a colon definition is executed.
    /// </summary>
    /// <param name="executionToken">The execution token (address) to start executing from.</param>
    private void ExecuteWord(int executionToken)
    {
        throw new NotImplementedException("Inner interpreter not yet implemented");
    }

    /// <summary>
    /// Executes a primitive word (built-in operation).
    /// </summary>
    /// <param name="primitive">The primitive word definition to execute.</param>
    private void ExecutePrimitive(WordDefinition primitive)
    {
        throw new NotImplementedException("ExecutePrimitive not yet implemented");
    }

    /// <summary>
    /// Executes a colon definition (user-defined word).
    /// This is the inner interpreter.
    /// </summary>
    /// <param name="word">The word definition to execute.</param>
    private void ExecuteColonDefinition(WordDefinition word)
    {
        if (word.Type != WordType.ColonDefinition)
        {
            throw new CompilationException("Can only execute colon definitions");
        }

        // Set the currently executing word for runtime introspection (e.g., DOES>)
        var previousWord = _vm.CurrentlyExecutingWord;
        _vm.CurrentlyExecutingWord = word;

        try
        {
            // Execute from the global code space using the word's execution token
            // This enables proper CALL/RET semantics for subroutines
            // Use the cached array to avoid creating a new copy for every execution
            _vm.ExecuteFrom(_codeSpaceArray, word.ExecutionToken);
        }
        finally
        {
            // Restore previous executing word
            _vm.CurrentlyExecutingWord = previousWord;
        }
    }

    #endregion

    #region Dictionary Management

    /// <summary>
    /// Looks up a word in the dictionary.
    /// </summary>
    /// <param name="name">The name of the word to find.</param>
    /// <returns>The word definition if found, null otherwise.</returns>
    private WordDefinition? FindWord(string name)
    {
        return _dictionary.TryGetValue(name.ToUpper(), out var word) ? word : null;
    }

    /// <summary>
    /// Adds a new word to the dictionary.
    /// </summary>
    /// <param name="name">The name of the word.</param>
    /// <param name="definition">The word definition.</param>
    private void AddWord(string name, WordDefinition definition)
    {
        _dictionary[name.ToUpper()] = definition;
        //try { Console.Error.WriteLine($"AddWord: '{name}' (type={definition.Type})"); } catch { }
    }

    /// <summary>
    /// Creates a new colon definition (user-defined word).
    /// </summary>
    /// <param name="name">The name of the new word.</param>
    private void CreateColonDefinition(string name)
    {
        if (_compileMode)
        {
            throw new CompilationException("Cannot start a colon definition while already compiling");
        }

        //try { Console.Error.WriteLine($"CreateColonDefinition: starting name='{name}'"); } catch { }

        _compileMode = true;
        _codeBuilder.Clear();
        _currentWordName = name;
        _currentExitLabel = $"EXIT_{name}_{_labelCounter++}";
        _currentStartLabel = $"START_{name}_{_labelCounter++}";

        // Add the start label at the beginning
        _codeBuilder.Label(_currentStartLabel);
        // Prepare list to record DOES> snippet start positions for this definition
        _doesCodeStartPositions = [];
        _createEmittedInCurrentDefinition = false;
    }

    /// <summary>
    /// Finalizes the current colon definition and returns to interpretation mode.
    /// </summary>
    private void FinishColonDefinition()
    {
        if (!_compileMode)
        {
            throw new CompilationException("Cannot finish a colon definition without starting one");
        }

        //try { Console.Error.WriteLine($"FinishColonDefinition: finishing name='{_currentWordName}' _doesCount={_doesCodeStartPositions?.Count}"); } catch { }

        // Add the exit label for EXIT to jump to
        if (_currentExitLabel != null)
        {
            _codeBuilder.Label(_currentExitLabel);
        }

        // Check if return stack has a return address (called via CALL), if so RET, else HALT
        // This allows words to work both when called recursively and when executed directly
        // Strategy: Check return stack depth. If >0, RET; else HALT
        // Use a syscall to check return stack depth and conditionally RET or HALT
        const long WORD_EXIT_SYSCALL_ID = (long)SyscallId.WordExit;

        _vm.SyscallHandlers[WORD_EXIT_SYSCALL_ID] = vm =>
        {
            // Calculate return stack depth (pointer / 8 since each entry is a long/8 bytes)
            int returnStackDepth = vm.ReturnStack.Pointer / 8;

            // If return stack has entries, we were CALLed, so RET
            // Otherwise, we were executed directly, so HALT
            if (returnStackDepth > 0)
            {
                // Pop return address and jump to it (manual RET implementation)
                long addr = vm.ReturnStack.PopLong();
                vm.PC = (int)addr;
            }
            else
            {
                // Direct execution - halt the VM
                vm.Halt();
            }
        };

        // Set the base offset to the current position in _codeSpace BEFORE building
        // This makes all labels resolve to global addresses
        _codeBuilder.SetBaseOffset(_codeSpace.Count);

        byte[] bytecode = _codeBuilder
            .PushCell(WORD_EXIT_SYSCALL_ID)
            .Syscall()
            .Build();

        // Store the bytecode in the global code space for proper CALL/RET semantics
        int executionToken = _codeSpace.Count;
        _codeSpace.AddRange(bytecode);

        // Update the cached array version of the code space
        _codeSpaceArray = _codeSpace.ToArray();

        // Create the word definition
        var word = new WordDefinition
        {
            Name = _currentWordName,
            Type = WordType.ColonDefinition,
            CompiledCode = bytecode,  // Keep for compatibility
            ExecutionToken = executionToken,  // Offset into global code space
            IsImmediate = false
        };

        // If this word contains DOES>, extract the DOES> snippets
        if (_compilingDoesCode && _doesCodeStartPositions != null && _doesCodeStartPositions.Count > 0)
        {
            var snippets = new List<byte[]>();
            // Sort starts just in case
            _doesCodeStartPositions.Sort();
            for (int i = 0; i < _doesCodeStartPositions.Count; i++)
            {
                int start = _doesCodeStartPositions[i];
                if (start >= bytecode.Length) { snippets.Add([]); continue; }

                int endExclusive;
                if (i + 1 < _doesCodeStartPositions.Count)
                {
                    // The next start is after a marker we emitted when compiling the next DOES>.
                    // The marker length is: PushCell(snippetIndex)=9 + PushCell(DOES_RUNTIME_SYSCALL_ID)=9 + Syscall=1 + Jmp=9 => 28 bytes
                    endExclusive = _doesCodeStartPositions[i + 1] - 28;
                }
                else
                {
                    // Last snippet goes up to the exit syscall we append at the end (10 bytes)
                    endExclusive = bytecode.Length - 10;
                }

                int doesCodeLength = Math.Max(0, endExclusive - start);
                if (doesCodeLength > 0)
                {
                    var snippet = new byte[doesCodeLength];
                    Array.Copy(bytecode, start, snippet, 0, doesCodeLength);
                    snippets.Add(snippet);
                }
                else
                {
                    snippets.Add([]);
                }
            }
            if (snippets.Count > 0)
            {
                word.DoesCodeSnippets = snippets.ToArray();
            }
        }

        // Add to dictionary
        AddWord(_currentWordName, word);

        // Exit compilation mode
        _compileMode = false;
        _currentWordName = string.Empty;
        _currentExitLabel = null;
        _currentStartLabel = null;
        _compilingDoesCode = false;
        _doesCodeStartPositions = [];
        _createEmittedInCurrentDefinition = false;
    }

    #endregion

    #region Compilation

    /// <summary>
    /// Compiles a word reference into the current definition.
    /// </summary>
    /// <param name="word">The word to compile.</param>
    private void CompileWord(WordDefinition word)
    {
        if (!_compileMode)
        {
            throw new CompilationException("Cannot compile a word outside of compilation mode");
        }

        if (word.Type == WordType.Primitive)
        {
            // For primitives, inline their bytecode
            if (word.CompiledCode != null && word.CompiledCode.Length > 0)
            {
                // Inline the primitive's bytecode
                _codeBuilder.AppendBytes(word.CompiledCode);
                if (!string.IsNullOrEmpty(word.Name) && string.Equals(word.Name, "CREATE", StringComparison.OrdinalIgnoreCase))
                {
                    _createEmittedInCurrentDefinition = true;
                }
            }
            else if (word.PrimitiveHandler != null)
            {
                // Some primitives use handlers (like control flow words, or complex operations)
                // These cannot be compiled inline and need special handling
                throw new CompilationException(
                    $"Primitive '{word.Name}' uses a handler and cannot be compiled inline. " +
                    $"It may require control flow or be a compile-time-only word.");
            }
            else
            {
                throw new CompilationException(
                    $"Primitive '{word.Name}' has neither bytecode nor handler");
            }
        }
        else if (word.Type == WordType.ColonDefinition)
        {
            // Use proper CALL semantics instead of inlining
            // The ExecutionToken is an absolute offset into _codeSpace
            _codeBuilder.Call(word.ExecutionToken);
        }
        else if (word.Type == WordType.Variable)
        {
            // When compiling a variable reference, push its address as a literal
            _codeBuilder.PushCell(word.DataAddress);
        }
        else
        {
            throw new NotImplementedException($"Compiling word type {word.Type} not yet supported");
        }
    }

    /// <summary>
    /// Compiles a literal value into the current definition.
    /// </summary>
    /// <param name="value">The literal value to compile.</param>
    private void CompileLiteral(long value)
    {
        if (!_compileMode)
        {
            throw new CompilationException("Cannot compile a literal outside of compilation mode");
        }

        _codeBuilder.PushCell(value);
    }

    /// <summary>
    /// Compiles a floating-point literal value into the current definition.
    /// </summary>
    /// <param name="value">The floating-point literal value to compile.</param>
    private void CompileFloatLiteral(double value)
    {
        if (!_compileMode)
        {
            throw new CompilationException("Cannot compile a literal outside of compilation mode");
        }

        _codeBuilder.FPushDouble(value);
    }

    /// <summary>
    /// Allocates space in the data space.
    /// This allocates space in VM memory starting from _here.
    /// </summary>
    /// <param name="bytes">Number of bytes to allocate.</param>
    /// <returns>The address of the allocated space.</returns>
    private int Allot(int bytes)
    {
        int address = _here;
        _here += bytes;
        // Note: We don't need to write anything to _dataSpace since we're using VM.Memory
        // The VM.Memory is pre-allocated and ready to use
        return address;
    }

    /// <summary>
    /// Writes a byte to the data space at the current HERE pointer.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    private void CommaByte(byte value)
    {
        throw new NotImplementedException("CommaByte not yet implemented");
    }

    /// <summary>
    /// Writes a cell (64-bit value) to the data space at the current HERE pointer.
    /// </summary>
    /// <param name="value">The cell value to write.</param>
    private void CommaCell(long value)
    {
        throw new NotImplementedException("CommaCell not yet implemented");
    }

    /// <summary>
    /// Writes a double (64-bit floating-point value) to the data space at the current HERE pointer.
    /// </summary>
    /// <param name="value">The double value to write.</param>
    private void CommaDouble(double value)
    {
        throw new NotImplementedException("CommaDouble not yet implemented");
    }

    #endregion

    #region Number Parsing

    /// <summary>
    /// Attempts to parse a string as a number (integer or floating-point).
    /// First tries to parse as integer in the current base, then tries floating-point.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="intValue">The parsed integer value if successful.</param>
    /// <param name="doubleValue">The parsed double value if text contains a decimal point.</param>
    /// <param name="isFloat">True if the number is a floating-point value, false if integer.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private bool TryParseNumber(string text, out long intValue, out double doubleValue, out bool isFloat)
    {
        intValue = 0;
        doubleValue = 0;
        isFloat = false;

        // Check if it's a floating-point number (contains '.' or 'e'/'E')
        if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
        {
            if (TryParseFloat(text, out doubleValue))
            {
                isFloat = true;
                return true;
            }
            return false;
        }

        // Try to parse as integer
        return TryParseInteger(text, out intValue);
    }

    /// <summary>
    /// Attempts to parse a string as an integer in the current base.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed integer value if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private bool TryParseInteger(string text, out long value)
    {
        try
        {
            value = Convert.ToInt64(text, _base);
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a string as a floating-point number.
    /// Handles formats like: 2.3, -4.5, 1.23e10, etc.
    /// Uses invariant culture to ensure '.' is always the decimal separator.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed double value if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseFloat(string text, out double value)
    {
        return double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    #endregion

    #region Primitive Initialization

    /// <summary>
    /// Initializes the dictionary with primitive words (built-in operations).
    /// This includes stack operations, arithmetic, control flow, etc.
    /// Primitives are defined with their actual FORTH names including symbols like !, @, +, -, etc.
    /// Each primitive has a handler (Action&lt;ForthInterpreter&gt;) that executes the operation.
    /// 
    /// By default words should be implemented using bytecode not handlers.
    /// </summary>
    /// <example>
    /// Examples of primitive definitions:
    /// <code>
    /// // Integer/Cell operations
    /// DefinePrimitive("!", forth => { /* STORE operation */ });
    /// DefinePrimitive("@", forth => { /* FETCH operation */ });
    /// DefinePrimitive("+", forth => { /* ADD operation */ });
    /// DefinePrimitive("DUP", forth => { /* DUP operation */ });
    /// 
    /// // Floating-point operations (typically prefixed with F in FORTH)
    /// DefinePrimitive("F!", forth => { /* FSTORE operation */ });
    /// DefinePrimitive("F@", forth => { /* FFETCH operation */ });
    /// DefinePrimitive("F+", forth => { /* FADD operation */ });
    /// DefinePrimitive("FDUP", forth => { /* FDUP operation */ });
    /// 
    /// // Type conversion
    /// DefinePrimitive("S>F", forth => { /* CELL_TO_FLOAT operation */ });
    /// DefinePrimitive("F>S", forth => { /* FLOAT_TO_CELL operation */ });
    /// </code>
    /// </example>
    private void InitializePrimitives()
    {
        InitializeMemoryOperations();
        InitializeArithmeticOperations();
        InitializeStackOperations();
        InitializeComparisonOperations();
        InitializeFloatingPointOperations();
        InitializeCompilationWords();
        InitializeControlFlowWords();
        InitializeIOWords();
        InitializeStringWords();
        InitializeStandardLibrary();
    }

    /// <summary>
    /// Initializes memory access primitives (! and @).
    /// </summary>
    private void InitializeMemoryOperations()
    {
        // Note: Our implementation uses ( address value -- ) instead of standard FORTH ( x a-addr -- )
        // This matches the existing tests and is more intuitive: address value !
        // VM STORE expects address on top, value below, so we need SWAP
        DefinePrimitive("!", new CodeBuilder().Swap().Store().Build());
        DefinePrimitive("@", OPCode.FETCH);

        // Byte operations - use VM opcodes for compilation support
        // Note: C! uses same convention as !: ( address value -- )
        // VM STORE_BYTE expects address on top, value below, so we need SWAP
        DefinePrimitive("C!", new CodeBuilder().Swap().StoreByte().Build());
        DefinePrimitive("C@", OPCode.FETCH_BYTE);

        // Cell size (8 bytes in our implementation)
        DefinePrimitive("CELL+", new CodeBuilder().PushCell(8).Add().Build());
        DefinePrimitive("CELLS", new CodeBuilder().PushCell(8).Mul().Build());

        // Dictionary/data-space operations via syscalls so they compile in colon definitions
        {
            const long COMMA_SYSCALL_ID = (long)SyscallId.Comma;
            const long ALLOT_SYSCALL_ID = (long)SyscallId.Allot;
            const long C_COMMA_SYSCALL_ID = (long)SyscallId.CComma;

            // , ( x -- ) store cell at HERE and advance HERE
            _vm.SyscallHandlers[COMMA_SYSCALL_ID] = vm =>
            {
                long value = vm.DataStack.PopLong();
                int address = _here;
                vm.Memory.WriteCellAt(address, value);
                _here += 8;
            };
            DefinePrimitive(",", new CodeBuilder().PushCell(COMMA_SYSCALL_ID).Syscall().Build());

            // C, ( c -- ) store byte at HERE and advance HERE
            _vm.SyscallHandlers[C_COMMA_SYSCALL_ID] = vm =>
            {
                long value = vm.DataStack.PopLong();
                int address = _here;
                vm.Memory[address] = (byte)value;
                _here += 1;
            };
            DefinePrimitive("C,", new CodeBuilder().PushCell(C_COMMA_SYSCALL_ID).Syscall().Build());

            // ALLOT ( n -- ) advance HERE by n bytes
            _vm.SyscallHandlers[ALLOT_SYSCALL_ID] = vm =>
            {
                long bytes = vm.DataStack.PopLong();
                Allot((int)bytes);
            };
            DefinePrimitive("ALLOT", new CodeBuilder().PushCell(ALLOT_SYSCALL_ID).Syscall().Build());
        }

        DefinePrimitive("HERE", forth =>
        {
            forth._vm.DataStack.PushLong(forth._here);
        });
    }

    /// <summary>
    /// Initializes arithmetic primitives (+, -, *, /, MOD, etc.).
    /// </summary>
    private void InitializeArithmeticOperations()
    {
        // Basic arithmetic
        DefinePrimitive("+", OPCode.ADD);
        DefinePrimitive("-", OPCode.SUB);
        DefinePrimitive("*", OPCode.MUL);
        DefinePrimitive("/", OPCode.DIV);
        DefinePrimitive("MOD", OPCode.MOD);
        DefinePrimitive("/MOD", OPCode.DIVMOD);
        DefinePrimitive("NEGATE", OPCode.NEG);

        // Convenience arithmetic operations
        DefinePrimitive("1+", new CodeBuilder().PushCell(1).Add().Build());
        DefinePrimitive("1-", new CodeBuilder().PushCell(1).Sub().Build());
        DefinePrimitive("2*", new CodeBuilder().PushCell(2).Mul().Build());
        DefinePrimitive("2/", new CodeBuilder().PushCell(2).Div().Build());

        // Bitwise operations
        DefinePrimitive("AND", OPCode.AND);
        DefinePrimitive("OR", OPCode.OR);
        DefinePrimitive("XOR", OPCode.XOR);
        DefinePrimitive("INVERT", OPCode.NOT);
    }

    /// <summary>
    /// Initializes stack manipulation primitives (DUP, DROP, SWAP, etc.).
    /// </summary>
    private void InitializeStackOperations()
    {
        // Data stack operations
        DefinePrimitive("DUP", OPCode.DUP);
        DefinePrimitive("DROP", OPCode.DROP);
        DefinePrimitive("SWAP", OPCode.SWAP);
        DefinePrimitive("OVER", OPCode.OVER);
        DefinePrimitive("ROT", OPCode.ROT);
        DefinePrimitive("2DUP", new CodeBuilder().Over().Over().Build());

        // Return stack operations
        DefinePrimitive(">R", OPCode.TO_R);
        DefinePrimitive("R>", OPCode.R_FROM);
        DefinePrimitive("R@", OPCode.R_FETCH);
    }

    /// <summary>
    /// Initializes comparison primitives (&lt;, &gt;, =, etc.).
    /// </summary>
    private void InitializeComparisonOperations()
    {
        // Binary comparisons
        DefinePrimitive("<", OPCode.LT);
        DefinePrimitive(">", OPCode.GT);
        DefinePrimitive("=", OPCode.EQ);
        DefinePrimitive("<=", OPCode.LTE);
        DefinePrimitive(">=", OPCode.GTE);
        DefinePrimitive("<>", OPCode.NEQ);

        // Zero comparisons
        DefinePrimitive("0<", new CodeBuilder().PushCell(0).Swap().Lt().Build());
        DefinePrimitive("0>", new CodeBuilder().PushCell(0).Swap().Gt().Build());
        DefinePrimitive("0=", new CodeBuilder().PushCell(0).Eq().Build());
    }

    /// <summary>
    /// Initializes floating-point primitives (F+, F-, FDUP, etc.).
    /// </summary>
    private void InitializeFloatingPointOperations()
    {
        // Floating-point arithmetic
        DefinePrimitive("F+", OPCode.FADD);
        DefinePrimitive("F-", OPCode.FSUB);
        DefinePrimitive("F*", OPCode.FMUL);
        DefinePrimitive("F/", OPCode.FDIV);
        DefinePrimitive("FNEGATE", OPCode.FNEG);

        // Floating-point comparisons
        DefinePrimitive("F<", OPCode.FLT);
        DefinePrimitive("F>", OPCode.FGT);
        DefinePrimitive("F=", OPCode.FEQ);
        DefinePrimitive("F<=", OPCode.FLTE);
        DefinePrimitive("F>=", OPCode.FGTE);
        DefinePrimitive("F<>", OPCode.FNEQ);

        // Floating-point zero comparisons
        DefinePrimitive("F0<", new CodeBuilder().FPushDouble(0.0).FLt().Build());
        DefinePrimitive("F0>", new CodeBuilder().FPushDouble(0.0).FGt().Build());
        DefinePrimitive("F0=", new CodeBuilder().FPushDouble(0.0).FEq().Build());

        // Floating-point stack operations
        DefinePrimitive("FDUP", OPCode.FDUP);
        DefinePrimitive("FDROP", OPCode.FDROP);
        DefinePrimitive("FSWAP", OPCode.FSWAP);
        DefinePrimitive("FOVER", OPCode.FOVER);
    }

    /// <summary>
    /// Initializes compilation words (: and ;).
    /// </summary>
    private void InitializeCompilationWords()
    {
        DefinePrimitive(":", forth =>
        {
            string? name = forth.ReadWord() ?? throw new CompilationException("Expected word name after :");
            forth.CreateColonDefinition(name);
        });

        DefinePrimitive(";", forth => forth.FinishColonDefinition(), isImmediate: true);

        // EXIT - Early return from a word definition (jump to end)
        DefinePrimitive("EXIT", forth =>
        {
            if (!forth._compileMode)
                throw new CompilationException("EXIT can only be used in compilation mode");
            if (forth._currentExitLabel == null)
                throw new CompilationException("EXIT used outside of colon definition");

            forth._codeBuilder.Jmp(forth._currentExitLabel);
        }, isImmediate: true);

        // RECURSE - Call the word currently being compiled (recursive call)
        // Supports both tail call optimization and general recursion
        DefinePrimitive("RECURSE", forth =>
        {
            if (!forth._compileMode)
                throw new CompilationException("RECURSE can only be used inside a colon definition during compilation");

            if (forth._currentStartLabel is null)
                throw new CompilationException("RECURSE used outside of a colon definition");

            // Tail call optimization: check if RECURSE is in tail position
            // A tail call is when RECURSE is the last operation before the word ends
            // We detect this by peeking ahead in the input to see if `;` follows

            // Save current position
            int savedPos = forth._inputPosition;
            forth.SkipWhitespace();

            // Peek at next word
            string? nextWord = forth.ReadWord();
            bool isTailPosition = nextWord == ";" || nextWord == null;

            // Restore position (don't consume the word we peeked at)
            forth._inputPosition = savedPos;

            if (isTailPosition)
            {
                // Tail call optimization: JMP instead of CALL
                // This avoids growing the return stack for tail recursion
                forth._codeBuilder.Jmp(forth._currentStartLabel);
            }
            else
            {
                // Non-tail recursion: use CALL for proper return semantics
                // Enables operations after the recursive call returns
                forth._codeBuilder.Call(forth._currentStartLabel);
            }
        }, isImmediate: true);

        // CREATE - Create a new word that pushes its data field address when executed
        // Uses a syscall-based approach so it can be used in colon definitions
        {
            const long CREATE_SYSCALL_ID = (long)SyscallId.Create;

            _vm.SyscallHandlers[CREATE_SYSCALL_ID] = vm =>
            {
                string? name = ReadWord() ?? throw new CompilationException("Expected word name after CREATE");
                //try { Console.WriteLine($"CREATE handler inputBuffer='{_inputBuffer}' pos={_inputPosition} nameRead='{name}'"); } catch { }

                // Track this as the last created word for DOES>
                _lastCreatedWord = name;

                // Align data-space pointer if necessary (to 8-byte boundary for cells)
                int remainder = _here % 8;
                if (remainder != 0)
                {
                    _here += 8 - remainder;
                }

                // The data field address is the current HERE
                // Note: CREATE does NOT advance HERE - that's for ALLOT or , to do
                int dataFieldAddress = _here;

                // Create bytecode that pushes the data field address and exits
                // Use global address resolution for consistency
                _codeBuilder.SetBaseOffset(_codeSpace.Count);

                byte[] bytecode = new CodeBuilder()
                    .PushCell(dataFieldAddress)
                    .PushCell((long)SyscallId.WordExit)
                    .Syscall()
                    .Build();

                // Store ExecutionToken (offset in global code space)
                int executionToken = _codeSpace.Count;

                // Add bytecode to global code space
                _codeSpace.AddRange(bytecode);
                _codeSpaceArray = _codeSpace.ToArray();

                // Create the word definition
                var wordDef = new WordDefinition
                {
                    Name = name,
                    Type = WordType.ColonDefinition,  // Treat as colon definition for execution
                    DataAddress = dataFieldAddress,
                    CompiledCode = bytecode,  // Keep for backward compatibility
                    ExecutionToken = executionToken,
                    IsImmediate = false
                };

                AddWord(name, wordDef);
            };

            DefinePrimitive("CREATE", new CodeBuilder().PushCell(CREATE_SYSCALL_ID).Syscall().Build());
        }

        // DOES> - Modify the most recently CREATEd word to execute following code
        {
            const long DOES_RUNTIME_SYSCALL_ID = (long)SyscallId.DoesRuntime;

            DefinePrimitive("DOES>", forth =>
            {
                if (!forth._compileMode)
                    throw new CompilationException("DOES> can only be used in compilation mode");
                // Mark that we're now compiling DOES> code
                forth._compilingDoesCode = true;

                // Generate a unique label for the DOES> code
                string doesCodeLabel = $"DOES_CODE_{forth._labelCounter++}";

                // Compile a runtime syscall that modifies the last CREATE'd word
                // Emit a snippet index (so multiple DOES> in one definition can be distinguished)
                int snippetIndex = forth._doesCodeStartPositions.Count;
                forth._codeBuilder.PushCell(snippetIndex);
                forth._codeBuilder
                    .PushCell(DOES_RUNTIME_SYSCALL_ID)
                    .Syscall()
                    .Jmp(forth._currentExitLabel!); // Jump to exit, skipping DOES> code

                // Record where the DOES> code starts (right after the JMP)
                forth._doesCodeStartPositions.Add(forth._codeBuilder.Size);
            }, isImmediate: true);

            // Register the runtime handler that does the actual modification
            _vm.SyscallHandlers[DOES_RUNTIME_SYSCALL_ID] = vm =>
            {
                // At runtime, modify the last CREATE'd word
                if (_lastCreatedWord == null)
                    throw new InvalidOperationException("DOES> runtime: no word to modify");

                var word = FindWord(_lastCreatedWord);
                if (word == null)
                    throw new InvalidOperationException($"DOES> runtime: cannot find word {_lastCreatedWord}");

                // Get the data field address from the created word
                int dataFieldAddress = word.DataAddress;

                // Pop the snippet index we emitted at compile time
                // Ensure there's at least one cell on the data stack for the snippet index
                if (vm.DataStack.Pointer < sizeof(long))
                    throw new InvalidOperationException("DOES> runtime: missing snippet index on data stack");
                long snippetIndexLong = vm.DataStack.PopLong();
                if (snippetIndexLong < 0) throw new InvalidOperationException("DOES> runtime: negative snippet index");
                int snippetIndex = (int)snippetIndexLong;

                // Get the currently executing word (the modifier word containing DOES>)
                if (vm.CurrentlyExecutingWord is not WordDefinition modifierWord)
                    throw new InvalidOperationException("DOES> runtime: no word currently executing or wrong type");

                // DEBUG: log what snippet we're about to attach
                //try { Console.WriteLine($"DOES> runtime: modifier={modifierWord.Name} created={_lastCreatedWord} snippetIndex={snippetIndex}"); } catch { }

                byte[]? snippet = null;
                if (modifierWord.DoesCodeSnippets != null && snippetIndex < modifierWord.DoesCodeSnippets.Length)
                {
                    snippet = modifierWord.DoesCodeSnippets[snippetIndex];
                }
                else if (modifierWord.DoesCode != null && snippetIndex == 0)
                {
                    snippet = modifierWord.DoesCode;
                }

                if (snippet == null)
                    throw new InvalidOperationException("DOES> runtime: no DOES> snippet available from modifier word");

                //try { Console.WriteLine($"DOES> runtime: snippetLength={snippet.Length} dataAddr={dataFieldAddress}"); } catch { }

                // Create new bytecode for the word:
                // When called, it should: push data-field-address, then execute DOES> snippet
                byte[] newBytecode = new CodeBuilder()
                    .PushCell(dataFieldAddress)  // Push data field address onto stack
                    .AppendBytes(snippet)  // Execute the DOES> code
                    .PushCell((long)SyscallId.WordExit)
                    .Syscall()                   // Return properly
                    .Build();

                // Update the word's bytecode in the global code space
                // This is trickier than CREATE since we're replacing existing bytecode
                // Strategy: Replace at the ExecutionToken position in _codeSpace
                int executionToken = word.ExecutionToken;
                int oldLength = word.CompiledCode?.Length ?? 0;
                int newLength = newBytecode.Length;

                if (newLength != oldLength)
                {
                    // Bytecode size changed - need to rebuild entire code space
                    // This is complex and might shift other words' ExecutionTokens
                    // For now, we'll throw an error as this shouldn't happen in practice
                    // (CREATE generates fixed-size bytecode, DOES> should generate similar size)

                    // Actually, let's handle it by rebuilding the segment
                    // Remove old bytecode and insert new bytecode at same position
                    _codeSpace.RemoveRange(executionToken, oldLength);
                    _codeSpace.InsertRange(executionToken, newBytecode);

                    // Recalculate ExecutionTokens for all words after this one
                    int offset = newLength - oldLength;
                    if (offset != 0)
                    {
                        foreach (var (_, otherWord) in _dictionary)
                        {
                            if (otherWord.Type == WordType.ColonDefinition &&
                                otherWord.ExecutionToken > executionToken)
                            {
                                otherWord.ExecutionToken += offset;
                            }
                        }
                    }
                }
                else
                {
                    // Same size - can replace in place
                    for (int i = 0; i < newLength; i++)
                    {
                        _codeSpace[executionToken + i] = newBytecode[i];
                    }
                }

                // Update cached array
                _codeSpaceArray = _codeSpace.ToArray();

                // Update CompiledCode for backward compatibility
                word.CompiledCode = newBytecode;
            };
        }

        DefinePrimitive("VARIABLE", forth =>
        {
            string? name = forth.ReadWord() ?? throw new InvalidOperationException("Expected variable name after VARIABLE");

            // Allocate one cell (8 bytes) in data space
            int address = forth.Allot(8);

            // Create a word definition for the variable
            var varDef = new WordDefinition
            {
                Name = name,
                Type = WordType.Variable,
                DataAddress = address,
                IsImmediate = false
            };

            forth._dictionary[name] = varDef;
        });

        // ( -- Comments (immediate, skip until closing paren)
        DefinePrimitive("(", forth =>
        {
            // Skip words until we find )
            string? word;
            while ((word = forth.ReadWord()) != null && word != ")")
            {
                // Just skip
            }
            if (word == null)
                throw new InvalidOperationException("Unclosed comment: missing )");
        }, isImmediate: true);

        // \ -- Line comment (immediate, skip rest of line)
        DefinePrimitive("\\", forth =>
        {
            // Skip to end of current line
            while (forth._inputPosition < forth._inputBuffer.Length && forth._inputBuffer[forth._inputPosition] != '\n')
            {
                forth._inputPosition++;
            }
        }, isImmediate: true);
    }

    /// <summary>
    /// Initializes control flow words (IF, THEN, ELSE).
    /// </summary>
    private void InitializeControlFlowWords()
    {
        DefinePrimitive("IF", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("IF can only be used in compilation mode");

            string label = $"L{forth._labelCounter++}";
            forth._codeBuilder.Jz(label);
            forth._compileStack.Push(label);
        }, isImmediate: true);

        DefinePrimitive("ELSE", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("ELSE can only be used in compilation mode");
            if (forth._compileStack.Count == 0)
                throw new InvalidOperationException("ELSE without matching IF");

            string ifLabel = forth._compileStack.Pop();
            string endLabel = $"L{forth._labelCounter++}";

            forth._codeBuilder.Jmp(endLabel);
            forth._codeBuilder.Label(ifLabel);
            forth._compileStack.Push(endLabel);
        }, isImmediate: true);

        DefinePrimitive("THEN", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("THEN can only be used in compilation mode");
            if (forth._compileStack.Count == 0)
                throw new InvalidOperationException("THEN without matching IF");

            string label = forth._compileStack.Pop();
            forth._codeBuilder.Label(label);
        }, isImmediate: true);

        // BEGIN...UNTIL loops
        DefinePrimitive("BEGIN", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("BEGIN can only be used in compilation mode");

            string label = $"L{forth._labelCounter++}";
            forth._codeBuilder.Label(label);
            forth._compileStack.Push(label);  // Push dest for UNTIL
        }, isImmediate: true);

        DefinePrimitive("UNTIL", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("UNTIL can only be used in compilation mode");
            if (forth._compileStack.Count == 0)
                throw new InvalidOperationException("UNTIL without matching BEGIN");

            string dest = forth._compileStack.Pop();
            forth._codeBuilder.Jz(dest);  // Jump back to BEGIN if zero
        }, isImmediate: true);

        // BEGIN...WHILE...REPEAT loops
        DefinePrimitive("WHILE", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("WHILE can only be used in compilation mode");
            if (forth._compileStack.Count == 0)
                throw new InvalidOperationException("WHILE without matching BEGIN");

            string dest = forth._compileStack.Pop();  // Get the BEGIN label
            string orig = $"L{forth._labelCounter++}";  // Create forward reference for exit
            forth._codeBuilder.Jz(orig);  // Jump forward if zero
            forth._compileStack.Push(orig);  // Push orig for REPEAT
            forth._compileStack.Push(dest);  // Push dest back for REPEAT
        }, isImmediate: true);

        DefinePrimitive("REPEAT", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("REPEAT can only be used in compilation mode");
            if (forth._compileStack.Count < 2)
                throw new InvalidOperationException("REPEAT without matching BEGIN...WHILE");

            string dest = forth._compileStack.Pop();  // Get BEGIN label
            string orig = forth._compileStack.Pop();  // Get WHILE exit label
            forth._codeBuilder.Jmp(dest);  // Jump back to BEGIN unconditionally
            forth._codeBuilder.Label(orig);  // Resolve forward reference
        }, isImmediate: true);

        // ?DO...LOOP counted loops
        // ?DO ( limit start -- ) - Start a loop, skip if start >= limit
        // Loop index is on return stack
        DefinePrimitive("?DO", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("?DO can only be used in compilation mode");

            string loopLabel = $"L{forth._labelCounter++}";
            string exitLabel = $"L{forth._labelCounter++}";
            string skipLabel = $"L{forth._labelCounter++}";

            // Stack: limit start
            // Check if start >= limit, if so skip loop
            forth._codeBuilder
                .Over()          // limit start limit
                .Over()          // limit start limit start
                .Swap()          // limit start start limit
                .Lt();           // limit start (start<limit?)

            // If start >= limit (condition is false/0), skip to skipLabel to clean up and exit
            forth._codeBuilder.Jz(skipLabel);

            // Condition is true (start < limit), proceed with loop
            // Move limit and start to return stack
            forth._codeBuilder.ToR().ToR();  // limit->R, start->R (R: start limit)

            // Mark loop start
            forth._codeBuilder.Label(loopLabel);

            // Push labels for LOOP to use
            forth._compileStack.Push(exitLabel);  // Exit label
            forth._compileStack.Push(loopLabel);  // Loop start label
            forth._compileStack.Push(skipLabel);  // Skip label for cleanup
        }, isImmediate: true);

        // LOOP - End of ?DO loop, increment index and loop if index < limit
        // Per Forth standard: increment index, exit if index == limit, otherwise loop
        DefinePrimitive("LOOP", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("LOOP can only be used in compilation mode");
            if (forth._compileStack.Count < 3)
                throw new InvalidOperationException("LOOP without matching ?DO");

            string skipLabel = forth._compileStack.Pop();
            string loopLabel = forth._compileStack.Pop();
            string exitLabel = forth._compileStack.Pop();

            // R stack has: index limit (based on the order they were pushed)
            // Per Forth standard: add one to loop index, check if equal to limit
            forth._codeBuilder
                .RFrom()         // Pop limit
                .RFrom()         // Pop current index
                .PushCell(1)
                .Add()           // Increment index: index+1
                .Over()          // Stack: limit index+1 limit
                .Over()          // Stack: limit index+1 limit index+1
                .Eq();           // Check if index+1 == limit

            // Stack now: limit index+1 (index+1==limit)
            // If equal (flag is -1/true), exit loop
            // If not equal (flag is 0/false), continue loop

            string continueLabel = $"L{forth._labelCounter++}";
            forth._codeBuilder.Jz(continueLabel);  // If 0 (not equal), continue looping

            // Index equals limit - exit loop
            forth._codeBuilder
                .Drop()          // Drop index+1
                .Drop()          // Drop limit
                .Jmp(exitLabel);

            // Continue looping - put values back on R stack
            forth._codeBuilder.Label(continueLabel);
            forth._codeBuilder
                .ToR()           // Put index+1 back on R
                .ToR()           // Put limit back on R
                .Jmp(loopLabel);

            // Skip label - clean up stack when loop was never entered
            forth._codeBuilder.Label(skipLabel);
            forth._codeBuilder
                .Drop()          // Drop start
                .Drop();         // Drop limit

            // Exit point
            forth._codeBuilder.Label(exitLabel);
        }, isImmediate: true);

        // DO...LOOP counted loops (standard form)
        // DO ( limit start -- ) - With our current positive-step LOOP, we enter only when start < limit
        // This avoids non-terminating loops for start >= limit until +LOOP is supported.
        DefinePrimitive("DO", forth =>
        {
            if (!forth._compileMode)
                throw new InvalidOperationException("DO can only be used in compilation mode");

            string loopLabel = $"L{forth._labelCounter++}";
            string exitLabel = $"L{forth._labelCounter++}";
            string skipLabel = $"L{forth._labelCounter++}";

            // Stack: limit start
            // Enter only if start < limit (same guard as ?DO for now)
            forth._codeBuilder
                .Over()          // limit start limit
                .Over()          // limit start limit start
                .Swap()          // limit start start limit
                .Lt();           // limit start (start<limit?)

            // If start >= limit (condition is false/0), skip to cleanup and exit
            forth._codeBuilder.Jz(skipLabel);

            // Move limit and start to return stack
            forth._codeBuilder.ToR().ToR();  // limit->R, start->R (R: start limit)

            // Mark loop start
            forth._codeBuilder.Label(loopLabel);

            // Push labels for LOOP to use
            forth._compileStack.Push(exitLabel);  // Exit label
            forth._compileStack.Push(loopLabel);  // Loop start label
            forth._compileStack.Push(skipLabel);  // Skip label for cleanup

            // The corresponding LOOP word will consume these labels
        }, isImmediate: true);

        // I - Get current loop index (from return stack)
        // R stack: index limit (limit on top after ?DO puts them there)
        // Need to access the index (second item) without disturbing the R stack
        DefinePrimitive("I", new CodeBuilder()
            .RFrom()      // Get limit from R stack -> Stack: limit
            .RFetch()     // Peek at index (now on top of R) -> Stack: limit index
            .Swap()       // Stack: index limit
            .ToR()        // Put limit back on R stack -> Stack: index
            .Build());
    }

    /// <summary>
    /// Initializes standard library words defined in FORTH itself.
    /// These are higher-level words built from primitives.
    /// </summary>
    private void InitializeStandardLibrary()
    {
        // Define common words by compiling FORTH source
        // This demonstrates FORTH's power: the language defining itself

        // Comparison and min/max operations
        Interpret(": MAX 2DUP < IF SWAP THEN DROP ;");
        Interpret(": MIN 2DUP > IF SWAP THEN DROP ;");
        Interpret(": ABS DUP 0 < IF NEGATE THEN ;");
        Interpret(": FABS FDUP F0< IF FNEGATE THEN ;");

        // Unsigned comparison (U<)
        // For unsigned comparison: treat negative numbers as large positive
        // If both have same sign, regular < works
        // If a<0 and b>=0, then a_unsigned > b_unsigned (return false)
        // If a>=0 and b<0, then a_unsigned < b_unsigned (return true)
        Interpret(": U< ( a b -- flag )  2DUP XOR 0< IF SWAP DROP 0< 0= ELSE < THEN ;");

        // Tier 1: Essential stack manipulation
        Interpret(": 2DROP DROP DROP ;");
        Interpret(": NIP SWAP DROP ;");
        Interpret(": TUCK SWAP OVER ;");
        Interpret(": -ROT ROT ROT ;");
        Interpret(": ?DUP DUP IF DUP THEN ;");  // Duplicate if non-zero

        // Tier 2: Useful arithmetic and logic
        Interpret(": SQUARE DUP * ;");
        // CLAMP ( value min max -- clamped ): Ensure value is between min and max
        // Uses return stack to temporarily store max while clamping to min
        // Then retrieves max and clamps the result to it
        Interpret(": CLAMP >R OVER OVER < IF SWAP THEN DROP R> OVER OVER > IF SWAP THEN DROP ;");
        Interpret(": SIGN DUP 0 < IF DROP -1 ELSE 0 > IF 1 ELSE 0 THEN THEN ;");

        // Scaled arithmetic ( n1 n2 n3 -- n1*n2/n3 )
        // Note: This is a simplified version. Full FORTH */ uses double-width intermediate
        Interpret(": */ >R * R> / ;");

        // Additional useful words
        Interpret(": TRUE -1 ;");
        Interpret(": FALSE 0 ;");
        Interpret(": NOT 0 = ;");
        Interpret(": AND * ;");  // Bitwise AND for booleans (-1 * -1 = 1, but we'll use 0 and non-zero)
        Interpret(": OR + 0 < > ;");  // Logical OR (any non-zero is true)

        // String operations
        // COUNT ( c-addr -- addr len ) - Convert counted string to address and length
        Interpret(": COUNT DUP 1+ SWAP C@ ;");
    }

    /// <summary>
    /// Initializes I/O words for printing and output.
    /// </summary>
    private void InitializeIOWords()
    {
        // . ( n -- ) Print number
        DefinePrimitive(".", forth =>
        {
            long value = forth._vm.DataStack.PopLong();
            Console.Write(value);
            Console.Write(" ");
        });

        // ." (compile-time string printing)
        DefinePrimitive(".\"", forth =>
        {
            // Read characters until closing "
            var sb = new System.Text.StringBuilder();
            while (forth._inputPosition < forth._inputBuffer.Length)
            {
                char c = forth._inputBuffer[forth._inputPosition++];
                if (c == '"')
                    break;
                sb.Append(c);
            }
            string text = sb.ToString();

            // For now, just print immediately (TODO: proper compile-time string handling)
            Console.Write(text);
        }, isImmediate: true);

        // EMIT ( c -- ) Print character
        DefinePrimitive("EMIT", forth =>
        {
            long charCode = forth._vm.DataStack.PopLong();
            Console.Write((char)charCode);
        });

        // CR ( -- ) Print newline
        DefinePrimitive("CR", forth =>
        {
            Console.WriteLine();
        });

        // SPACE ( -- ) Print space
        DefinePrimitive("SPACE", forth =>
        {
            Console.Write(" ");
        });
    }

    /// <summary>
    /// Initializes string and character handling words.
    /// </summary>
    private void InitializeStringWords()
    {
        // [CHAR] ( "name" -- c ) Compile-time: get ASCII of next character
        DefinePrimitive("[CHAR]", forth =>
        {
            string? word = forth.ReadWord();
            if (string.IsNullOrEmpty(word))
                throw new InvalidOperationException("[CHAR] requires a character");

            long charValue = word[0];

            // Behavior note:
            // [CHAR] is an immediate (compile-time) word. When executed while compiling
            // we must place the character value on the VM data stack so that a following
            // immediate word (for example our WORD handler) can consume it. Previously
            // we compiled the literal into the current definition which prevented the
            // following immediate WORD from seeing the delimiter at compile time.
            // To support both compile-time execution and runtime usage we always push
            // the character value onto the VM data stack when executed.
            forth._vm.DataStack.PushLong(charValue);
        }, isImmediate: true);

        // CHAR ( "name" -- c ) Runtime: get ASCII of next character
        DefinePrimitive("CHAR", forth =>
        {
            string? word = forth.ReadWord();
            if (string.IsNullOrEmpty(word))
                throw new InvalidOperationException("CHAR requires a character");

            forth._vm.DataStack.PushLong(word[0]);
        });

        // WORD ( delimiter -- c-addr ) Parse word delimited by character
        // Implemented as a handler so it can behave correctly both at compile-time
        // (immediate) and at runtime. When executed during compilation it will
        // consume the following characters from the interpreter input, allocate
        // a counted string in data memory and then compile the resulting address
        // as a literal into the current definition. At runtime it behaves like
        // the previous syscall: it creates the counted string and pushes its
        // address onto the data stack.
        {
            const long WORD_SYSCALL_ID = (long)SyscallId.Word;

            // Register a syscall handler for runtime use. It performs the raw
            // operation of reading from the interpreter input buffer (via the
            // captured interpreter instance) and allocating the counted string.
            _vm.SyscallHandlers[WORD_SYSCALL_ID] = vm =>
            {
                // The syscall is invoked from the VM context; use the interpreter's
                // fields to read from the current input buffer position.
                long delimiter = vm.DataStack.PopLong();
                char delim = (char)delimiter;

                // Skip leading delimiter characters
                while (_inputPosition < _inputBuffer.Length && _inputBuffer[_inputPosition] == delim)
                {
                    _inputPosition++;
                }

                var sb = new System.Text.StringBuilder();
                while (_inputPosition < _inputBuffer.Length)
                {
                    char c = _inputBuffer[_inputPosition];
                    if (c == delim)
                    {
                        // consume closing delimiter and stop
                        _inputPosition++;
                        break;
                    }
                    sb.Append(c);
                    _inputPosition++;
                }

                string text = sb.ToString();

                // Create counted string in memory: length byte followed by characters
                int address = Allot(text.Length + 1);
                _vm.Memory[address] = (byte)text.Length;
                for (int i = 0; i < text.Length; i++)
                {
                    _vm.Memory[address + 1 + i] = (byte)text[i];
                }

                vm.DataStack.PushLong(address);
            };

            // Define WORD as a handler-based immediate primitive so it can be
            // executed during compilation (to consume the quoted text) and also
            // compiled as a syscall into definitions for runtime behavior.
            DefinePrimitive("WORD", forth =>
            {
                // The delimiter should be on the VM data stack (e.g., pushed by [CHAR])
                if (forth._vm.DataStack.Pointer < sizeof(long))
                    throw new InvalidOperationException("WORD runtime: missing delimiter on data stack");

                // Reuse the runtime syscall handler logic by invoking the registered
                // syscall (it uses the interpreter's captured fields to access input).
                _vm.SyscallHandlers[WORD_SYSCALL_ID](forth._vm);

                // At this point the address of the created counted string is on the
                // VM data stack. If we're compiling, we want to embed that address
                // as a literal into the current definition so runtime execution will
                // find the string in memory without needing to re-parse text.
                if (forth._compileMode)
                {
                    long addr = forth._vm.DataStack.PopLong();
                    //try { Console.Error.WriteLine($"WORD handler: compiled-counted-string addr={addr} inputBuf='{forth._inputBuffer}' pos={forth._inputPosition}"); } catch { }
                    forth.CompileLiteral(addr);
                }
            }, isImmediate: true);
        }

        // COUNT ( c-addr -- addr len ) Convert counted string to address and length
        // Now compilable as it uses C@ which is opcode-based
        DefinePrimitive("COUNT", new CodeBuilder()
            .Dup()           // c-addr c-addr
            .PushCell(1)     // c-addr c-addr 1
            .Add()           // c-addr addr
            .Swap()          // addr c-addr
            .FetchByte()     // addr len
            .Build());

        // TYPE ( addr len -- ) Print string of given length
        // Implemented as a VM syscall so it can be compiled into colon definitions
        {
            const long TYPE_SYSCALL_ID = (long)SyscallId.Type;

            _vm.SyscallHandlers[TYPE_SYSCALL_ID] = vm =>
            {
                long len = vm.DataStack.PopLong();
                long addr = vm.DataStack.PopLong();

                for (long i = 0; i < len; i++)
                {
                    byte b = vm.Memory[(int)(addr + i)];
                    Console.Write((char)b);
                }
            };

            DefinePrimitive("TYPE", new CodeBuilder().PushCell(TYPE_SYSCALL_ID).Syscall().Build());
        }
    }

    /// <summary>
    /// Defines a primitive word in the dictionary.
    /// </summary>
    /// <param name="name">The name of the primitive word (e.g., "!", "@", "DUP", "+").</param>
    /// <param name="handler">The action to execute when this primitive is called.</param>
    /// <param name="isImmediate">Whether this word should be executed even during compilation mode.</param>
    private void DefinePrimitive(string name, Action<ForthInterpreter> handler, bool isImmediate = false)
    {
        var word = new WordDefinition
        {
            Name = name.ToUpper(),
            Type = WordType.Primitive,
            PrimitiveHandler = handler,
            IsImmediate = isImmediate
        };

        // To allow primitives implemented as handlers to be compiled inline,
        // allocate a dedicated syscall id and register a syscall that invokes
        // the handler. Also provide compiled bytecode that performs the syscall
        // so the compiler can inline it like any other primitive.
        long syscallId = _nextHandlerSyscallId++;
        _vm.SyscallHandlers[syscallId] = vm => { handler(this); };
        word.CompiledCode = new CodeBuilder().PushCell(syscallId).Syscall().Build();

        AddWord(name, word);
    }

    /// <summary>
    /// Defines a primitive word that executes VM bytecode.
    /// This is the preferred approach for most primitives as it ensures interpretation
    /// and compilation use the exact same VM opcodes.
    /// </summary>
    /// <param name="name">The name of the primitive word.</param>
    /// <param name="bytecode">The VM bytecode to execute.</param>
    /// <param name="isImmediate">Whether this is an immediate word.</param>
    private void DefinePrimitive(string name, byte[] bytecode, bool isImmediate = false)
    {
        var word = new WordDefinition
        {
            Name = name.ToUpper(),
            Type = WordType.Primitive,
            CompiledCode = bytecode,
            IsImmediate = isImmediate
        };
        AddWord(name, word);
    }

    /// <summary>
    /// Helper to define a primitive from a single opcode.
    /// </summary>
    private void DefinePrimitive(string name, OPCode opcode, bool isImmediate = false)
    {
        DefinePrimitive(name, [(byte)opcode], isImmediate);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Resets the interpreter to its initial state.
    /// Clears the dictionary (except primitives), data space, and compilation state.
    /// </summary>
    public void Reset()
    {
        throw new NotImplementedException("Reset not yet implemented");
    }

    /// <summary>
    /// Gets the current state of the interpreter.
    /// </summary>
    public InterpreterState GetState()
    {
        throw new NotImplementedException("GetState not yet implemented");
    }

    #endregion
}

/// <summary>
/// Represents a word definition in the FORTH dictionary.
/// </summary>
public class WordDefinition
{
    /// <summary>
    /// The name of the word.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of word (primitive, colon definition, variable, constant, etc.).
    /// </summary>
    public WordType Type { get; set; }

    /// <summary>
    /// For primitive words: optional handler action to execute (for special cases like immediate words).
    /// Most primitives use CompiledCode instead.
    /// </summary>
    public Action<ForthInterpreter>? PrimitiveHandler { get; set; }

    /// <summary>
    /// For colon definitions: the execution token (address in data space).
    /// </summary>
    public int ExecutionToken { get; set; }

    /// <summary>
    /// For variables and constants: the data address.
    /// </summary>
    public int DataAddress { get; set; }

    /// <summary>
    /// Flag indicating if this word is immediate (executed during compilation).
    /// </summary>
    public bool IsImmediate { get; set; }

    /// <summary>
    /// The compiled bytecode for this word.
    /// For primitives: the opcode(s) to execute.
    /// For colon definitions: the full compiled definition.
    /// </summary>
    public byte[]? CompiledCode { get; set; }

    /// <summary>
    /// For words defined with DOES>: the bytecode that executes after pushing the data field address.
    /// </summary>
    public byte[]? DoesCode { get; set; }
    /// <summary>
    /// Multiple DOES> code snippets for definitions that had several DOES> clauses.
    /// Each snippet is a byte[] containing the inlined DOES> code emitted at compile time.
    /// The runtime DOES> handler will select the appropriate snippet by index.
    /// </summary>
    public byte[][]? DoesCodeSnippets { get; set; }
}

/// <summary>
/// Types of FORTH words.
/// </summary>
public enum WordType
{
    /// <summary>Built-in primitive operation.</summary>
    Primitive,

    /// <summary>User-defined colon definition (: word ... ;).</summary>
    ColonDefinition,

    /// <summary>Variable (VARIABLE name).</summary>
    Variable,

    /// <summary>Constant (CONSTANT name).</summary>
    Constant,

    /// <summary>Immediate word (executed during compilation).</summary>
    Immediate
}

/// <summary>
/// Represents the current state of the interpreter.
/// </summary>
public class InterpreterState
{
    /// <summary>Is the interpreter in compilation mode?</summary>
    public bool CompileMode { get; set; }

    /// <summary>Current base for number conversion.</summary>
    public int Base { get; set; }

    /// <summary>Current HERE pointer (next free location in data space).</summary>
    public int Here { get; set; }

    /// <summary>Number of words in the dictionary.</summary>
    public int DictionarySize { get; set; }

    /// <summary>Current data stack depth (integer/cell values).</summary>
    public int DataStackDepth { get; set; }

    /// <summary>Current floating-point stack depth (double values).</summary>
    public int FloatStackDepth { get; set; }

    /// <summary>Current return stack depth.</summary>
    public int ReturnStackDepth { get; set; }
}
