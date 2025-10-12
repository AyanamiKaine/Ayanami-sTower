using System;
using System.Buffers;
using System.Collections.Generic;

namespace StellaLang;
#pragma warning disable CS0414 

/// <summary>
/// A FORTH interpreter that compiles FORTH code to StellaLang VM bytecode.
/// Implements the traditional FORTH inner and outer interpreter loops.
/// Implements IDisposable to properly return rented arrays to the pool.
/// </summary>
public class ForthInterpreter : IDisposable
{
    private readonly IHostIO _io;
    /// <summary>
    /// The underlying VM that executes the compiled bytecode.
    /// </summary>
    private readonly VM _vm;

    /// <summary>
    /// Dictionary mapping FORTH word names to their definitions.
    /// Uses case-insensitive lookups and maintains definition order.
    /// </summary>
    private readonly ForthDictionary _dictionary;

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
    /// Rented from ArrayPool to reduce GC pressure.
    /// Updated only when code space grows beyond current capacity.
    /// </summary>
    private byte[] _codeSpaceArray;

    /// <summary>
    /// The actual used length of _codeSpaceArray (may be less than array length).
    /// </summary>
    private int _codeSpaceArrayLength;

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

    // Start index (in _inputBuffer) of the most recently read token
    private int _lastTokenStart;

    // Length of the most recently read token
    private int _lastTokenLength;

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
        _io = new ConsoleHostIO();
        _dictionary = new ForthDictionary();
        _dataSpace = [];
        _codeSpace = [];
        _codeSpaceArray = ArrayPool<byte>.Shared.Rent(1024); // Initial size
        _codeSpaceArrayLength = 0;
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
    public ForthInterpreter() : this(new VM()) { }

    /// <summary>
    /// Initializes a new instance of the FORTH interpreter with a VM and a custom I/O host.
    /// </summary>
    public ForthInterpreter(VM vm, IHostIO io)
    {
        _vm = vm;
        _io = io ?? new ConsoleHostIO();
        _dictionary = new ForthDictionary();
        _dataSpace = [];
        _codeSpace = [];
        _codeSpaceArray = ArrayPool<byte>.Shared.Rent(1024);
        _codeSpaceArrayLength = 0;
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

        // TODO: I think there should be a way to better handle automatic floating point arithmetics. So we can just say 1.0 5 + instead of needing to say 1.0 5.0 F+. So we would implement a fallback mechanism that in some form. Maybe we could introduce a way to only use floating points (doubles). So every 1 2 + would actually be a 1.0 2.0 F+ internally.

        string? word;
        while ((word = ReadWord()) != null)
        {
            ProcessWord(word);
        }
    }

    /// <summary>
    /// The REPL (Read-Eval-Print Loop) - continuously reads input lines and interprets them.
    /// Displays a prompt based on the current mode (compilation or interpretation) and processes user input.
    /// Type 'BYE' to exit the REPL.
    /// </summary>
    public void REPL()
    {
        _io.WriteLine("StellaLang FORTH Interpreter");
        _io.WriteLine("Type 'BYE' to exit");

        while (true)
        {
            try
            {
                _io.Write(_compileMode ? "] " : "ok ");
                string? input = _io.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                if (input.Trim().Equals("BYE", StringComparison.OrdinalIgnoreCase))
                    break;

                Interpret(input);

                if (!_compileMode)
                    _io.WriteLine(" ok");
            }
            catch (Exception ex)
            {
                _io.Error.WriteLine($"Error: {ex.Message}");
                _compileMode = false;  // Reset to interpretation mode on error
            }
        }
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
                        _vm.LoadAndExecute(definition.CompiledCode);
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
                // Build a richer error message with context and suggestions
                string context = FormatErrorContext(_inputBuffer, _lastTokenStart, _lastTokenLength);
                var suggestions = SuggestSimilarWords(word, 3);
                string suggestionText = suggestions.Count > 0 ? "\nDid you mean: " + string.Join(", ", suggestions) : string.Empty;

                string hint = "";
                // If it looks like a number but failed parsing, suggest checking BASE or number format
                if (word.IndexOfAny(new char[] { '.', 'e', 'E' }) >= 0)
                {
                    hint = "\nNote: this token looks like a floating-point literal. Ensure you're using '.' as the decimal separator and the token is valid in the current BASE.";
                }

                string msg = $"Unknown word: '{word}'\n{context}{suggestionText}{hint}";
                throw new UnknownWordException(word, msg);
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

        // Record token position for better error messages later
        _lastTokenStart = start;
        _lastTokenLength = _inputPosition - start;

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
            _vm.ExecuteSubroutine(_codeSpaceArray, word.ExecutionToken);
        }
        finally
        {
            // Restore previous executing word
            _vm.CurrentlyExecutingWord = previousWord;
        }
    }

    /// <summary>
    /// Updates the cached code space array, using ArrayPool to minimize allocations.
    /// Only grows the array when necessary; reuses existing capacity when possible.
    /// </summary>
    private void UpdateCodeSpace()
    {
        int requiredSize = _codeSpace.Count;

        // Check if we need to grow the array
        if (_codeSpaceArray.Length < requiredSize)
        {
            // Return old array to pool
            ArrayPool<byte>.Shared.Return(_codeSpaceArray, clearArray: false);

            // Rent new array with enough capacity (ArrayPool may return larger)
            _codeSpaceArray = ArrayPool<byte>.Shared.Rent(requiredSize);
        }

        // Copy current code space to array
        _codeSpace.CopyTo(_codeSpaceArray);
        _codeSpaceArrayLength = requiredSize;
    }

    /// <summary>
    /// Produce a best-effort Forth-like decompilation of a word's bytecode.
    /// This attempts to map CALL addresses to known word names and prints literals
    /// as numbers. It's intended as a human-friendly view, not a perfect reformatter.
    /// </summary>
    /// <param name="word">The word to decompile (must have CompiledCode set).</param>
    /// <returns>Multi-line string with decompiled representation.</returns>
    private string DecompileToForth(WordDefinition word)
    {
        if (word.CompiledCode == null)
            return "(no bytecode)";

        var sb = new System.Text.StringBuilder();
        sb.Append($": {word.Name} ");

        byte[] code = word.CompiledCode;
        int i = 0;
        // We'll do a single-pass decompilation but with a conservative pattern recognizer
        // for simple IF ... ELSE ... THEN and IF ... THEN sequences. The VM uses
        // JZ to jump forward over the 'then' part; an ELSE is typically compiled as
        // JZ <elseTarget> ... JMP <thenTarget> <elseTarget>:
        // We'll attempt to spot JZ followed eventually by a JMP that jumps past an else
        // region and pretty-print as IF ... ELSE ... THEN when detected.

        while (i < code.Length)
        {
            int opOffset = i;
            OPCode op = (OPCode)code[i++];

            if (op == OPCode.JZ && i + 8 <= code.Length)
            {
                long target = BitConverter.ToInt64(code, i);
                int jzTarget = (int)target;
                i += 8;

                // Try to find a JMP inside the fall-through block that jumps to after jzTarget
                // If found, we interpret this pattern as IF ... ELSE ... THEN
                int fallthroughStart = i;
                int foundJmpPos = -1;
                int foundJmpTarget = -1;

                int scan = fallthroughStart;
                while (scan < code.Length)
                {
                    OPCode scanOp = (OPCode)code[scan++];
                    if (scanOp == OPCode.PUSH_CELL || scanOp == OPCode.CALL || scanOp == OPCode.JMP || scanOp == OPCode.JZ || scanOp == OPCode.JNZ || scanOp == OPCode.SYSCALL || scanOp == OPCode.FPUSH_DOUBLE)
                    {
                        // these ops have 8-byte operand(s)
                        if (scan + 8 <= code.Length)
                        {
                            long operand = BitConverter.ToInt64(code, scan);
                            int operandInt = (int)operand;
                            // If this opcode is JMP and it jumps to after the jzTarget, consider it an ELSE separator
                            if (scanOp == OPCode.JMP && operandInt >= jzTarget)
                            {
                                foundJmpPos = scan - 1; // opcode offset
                                foundJmpTarget = operandInt;
                                break;
                            }
                            scan += 8;
                        }
                        else { break; }
                    }
                    else
                    {
                        // single-byte opcode
                    }
                }

                if (foundJmpPos != -1)
                {
                    // We have a JZ ... [then-block] JMP <thenTarget> [else-block] <thenTarget>:
                    sb.Append("IF ");

                    // Decompile then-block (from fallthroughStart to foundJmpPos)
                    sb.Append(DecompileRangeToForth(code, fallthroughStart, foundJmpPos));
                    sb.Append(" ELSE ");

                    // Decompile else-block (from foundJmpPos + 9 (opcode+8) to jzTarget)
                    int elseStart = foundJmpPos + 1 + 8;
                    int elseEnd = jzTarget;
                    if (elseStart < elseEnd)
                    {
                        sb.Append(DecompileRangeToForth(code, elseStart, elseEnd));
                    }
                    sb.Append(" THEN ");

                    // advance i to jzTarget
                    i = jzTarget;
                    continue;
                }
                else
                {
                    // No ELSE pattern found; treat as simple IF ... THEN by decompiling
                    // the fall-through (then) block up to the jzTarget.
                    sb.Append("IF ");
                    if (fallthroughStart < jzTarget)
                    {
                        sb.Append(DecompileRangeToForth(code, fallthroughStart, jzTarget));
                    }
                    sb.Append(" THEN ");

                    // advance i to jzTarget
                    i = jzTarget;
                    continue;
                }
            }

            switch (op)
            {
                case OPCode.PUSH_CELL:
                    if (i + 8 <= code.Length)
                    {
                        long v = BitConverter.ToInt64(code, i);
                        i += 8;
                        // If this literal is immediately followed by a SYSCALL opcode, annotate it
                        if (i < code.Length && code[i] == (byte)OPCode.SYSCALL)
                        {
                            string? info = ResolveSyscallInfo(v);
                            if (!string.IsNullOrEmpty(info))
                                sb.Append(v).Append(" [syscall: ").Append(info).Append("] ");
                            else
                                sb.Append(v).Append(' ');
                        }
                        else
                        {
                            sb.Append(v).Append(' ');
                        }
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.FPUSH_DOUBLE:
                    if (i + 8 <= code.Length)
                    {
                        double d = BitConverter.ToDouble(code, i);
                        i += 8;
                        sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.CALL:
                    if (i + 8 <= code.Length)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        i += 8;
                        // Try to map address to a word name
                        string? name = ResolveWordByExecutionToken((int)addr);
                        if (name != null)
                            sb.Append(name).Append(' ');
                        else
                            sb.Append($"CALL({addr}) ");
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.SYSCALL:
                    // Syscall consumes a pushed id (already emitted as literal earlier)
                    sb.Append("SYSCALL ");
                    break;
                case OPCode.JMP:
                case OPCode.JNZ:
                    if (i + 8 <= code.Length)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        i += 8;
                        sb.Append(op.ToString()).Append('(').Append(addr).Append(") ");
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.JZ:
                    // JZ that wasn't handled above (e.g., malformed or unconditional pattern)
                    if (i + 8 <= code.Length)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        i += 8;
                        sb.Append("JZ(").Append(addr).Append(") ");
                    }
                    else { i = code.Length; }
                    break;
                default:
                    sb.Append(op.ToString()).Append(' ');
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Produce a detailed bytecode disassembly for debugging, including offsets,
    /// opcode names, and literal operands. Attempts to annotate CALL targets
    /// with word names when possible.
    /// </summary>
    private string DisassembleBytecode(WordDefinition word)
    {
        if (word.CompiledCode == null)
            return "(no bytecode)";

        var sb = new System.Text.StringBuilder();
        byte[] code = word.CompiledCode;
        int i = 0;
        while (i < code.Length)
        {
            int offset = i;
            OPCode op = (OPCode)code[i++];
            sb.AppendFormat("{0:D4}: {1}", offset, op.ToString());

            switch (op)
            {
                case OPCode.PUSH_CELL:
                    if (i + 8 <= code.Length)
                    {
                        long v = BitConverter.ToInt64(code, i);
                        // annotate syscall literals with known info
                        if (i + 8 < code.Length && code[i + 8] == (byte)OPCode.SYSCALL)
                        {
                            string? info = ResolveSyscallInfo(v);
                            if (!string.IsNullOrEmpty(info))
                                sb.AppendFormat(" {0} [{1}]", v, info);
                            else
                                sb.AppendFormat(" {0}", v);
                        }
                        else
                        {
                            sb.AppendFormat(" {0}", v);
                        }
                        i += 8;
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.FPUSH_DOUBLE:
                    if (i + 8 <= code.Length)
                    {
                        double d = BitConverter.ToDouble(code, i);
                        sb.AppendFormat(" {0}", d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        i += 8;
                    }
                    else { i = code.Length; }
                    break;
                case OPCode.JMP:
                case OPCode.JZ:
                case OPCode.JNZ:
                case OPCode.CALL:
                    if (i + 8 <= code.Length)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        string? name = ResolveWordByExecutionToken((int)addr);
                        sb.AppendFormat(" {0} ({1})", addr, name ?? "?");
                        i += 8;
                    }
                    else { i = code.Length; }
                    break;
                default:
                    // no operand
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempt to find a word whose ExecutionToken equals the given address.
    /// Returns the word name if found, otherwise null.
    /// </summary>
    private string? ResolveWordByExecutionToken(int address)
    {
        foreach (var w in _dictionary.GetAllWords())
        {
            if (w.Type == WordType.ColonDefinition && w.ExecutionToken == address)
                return w.Name;
        }
        return null;
    }

    /// <summary>
    /// Returns human-friendly info for a syscall id: the SyscallId name (if known)
    /// and a primitive word that wraps that syscall (if any).
    /// </summary>
    private string? ResolveSyscallInfo(long syscallId)
    {
        string? enumName = null;
        try
        {
            if (Enum.IsDefined(typeof(SyscallId), (long)syscallId))
                enumName = Enum.GetName(typeof(SyscallId), (long)syscallId);
        }
        catch { }

        // Try to find a primitive word whose compiled code is: PUSH_CELL <id> SYSCALL
        string? wrapper = null;
        foreach (var w in _dictionary.GetVisibleWords())
        {
            if (w.Type != WordType.Primitive || w.CompiledCode == null)
                continue;

            var b = w.CompiledCode;
            if (b.Length >= 1 + 8 + 1 && b[0] == (byte)OPCode.PUSH_CELL && b[9] == (byte)OPCode.SYSCALL)
            {
                long v = BitConverter.ToInt64(b, 1);
                if (v == syscallId)
                {
                    wrapper = w.Name;
                    break;
                }
            }
        }

        if (enumName != null && wrapper != null)
            return enumName + " -> " + wrapper;
        if (enumName != null)
            return enumName;
        if (wrapper != null)
            return wrapper;
        return null;
    }

    /// <summary>
    /// Suggest similar visible word names for a given token using Levenshtein distance.
    /// </summary>
    private List<string> SuggestSimilarWords(string token, int maxSuggestions)
    {
        var results = new List<(string name, int dist)>();
        if (string.IsNullOrEmpty(token))
            return new List<string>();

        // Precompute canonical forms (user-facing) and consider both dictionary name and canonical form
        foreach (var w in _dictionary.GetVisibleWords())
        {
            if (string.IsNullOrEmpty(w.Name))
                continue;
            string dictName = w.Name;
            string canon = CanonicalizeWordName(dictName);

            // Consider three candidates: exact dictName, canonical form, and uppercase forms
            int d1 = LevenshteinDistance(token.ToUpperInvariant(), dictName.ToUpperInvariant());
            int d2 = LevenshteinDistance(token.ToUpperInvariant(), canon.ToUpperInvariant());

            int d = Math.Min(d1, d2);

            // Only consider reasonably-similar words
            if (d <= Math.Max(3, token.Length / 2))
            {
                // Prefer canonical form in the displayed name if it's different
                string displayName = !string.Equals(canon, dictName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(canon)
                    ? canon
                    : dictName;

                results.Add((displayName, d));
            }
        }

        // Sort by distance then name
        results.Sort((a, b) => a.dist != b.dist ? a.dist.CompareTo(b.dist) : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        var outList = new List<string>();
        for (int i = 0; i < results.Count && i < maxSuggestions; i++)
            outList.Add(results[i].name);

        return outList;
    }

    /// <summary>
    /// Map dictionary-internal word names to a canonical, user-facing token when possible.
    /// Examples: "FADD" -> "F+", "ADD" -> "+" (if those are the conventional user tokens)
    /// This is best-effort and doesn't modify the dictionary.
    /// </summary>
    private static string CanonicalizeWordName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Normalize (strip common prefixes/suffixes)
        string up = name.ToUpperInvariant();

        return up switch
        {
            "ADD" => "+",
            "SUB" => "-",
            "MUL" => "*",
            "DIV" => "/",
            "NEG" => "NEGATE",
            "DUP" => "DUP",
            "DROP" => "DROP",
            "SWAP" => "SWAP",
            "OVER" => "OVER",
            "EQ" => "=",
            "NEQ" => "<>",
            "LT" => "<",
            "LTE" => "<=",
            "GT" => ">",
            "GTE" => ">=",
            // Floating point
            "FADD" => "F+",
            "FSUB" => "F-",
            "FMUL" => "F*",
            "FDIV" => "F/",
            "FNEG" => "FNEGATE",
            "FDUP" => "FDUP",
            "FDROP" => "FDROP",
            "FSWAP" => "FSWAP",
            "FOVER" => "FOVER",
            "FEQ" => "F=",
            "FNEQ" => "F<>",
            "FLT" => "F<",
            "FLTE" => "F<=",
            "FGT" => "F>",
            "FGTE" => "F>=",
            _ => name,
        };
    }

    /// <summary>
    /// Compute the Levenshtein distance between two strings (case-sensitive as provided).
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                int min = d[i - 1, j] + 1;
                int tmp = d[i, j - 1] + 1;
                if (tmp < min) min = tmp;
                tmp = d[i - 1, j - 1] + cost;
                if (tmp < min) min = tmp;
                d[i, j] = min;
            }
        }

        return d[a.Length, b.Length];
    }

    /// <summary>
    /// Format the input line and a caret/underline pointing at the token
    /// starting at tokenStart with tokenLength characters. Similar to Rust's
    /// compiler diagnostic context.
    /// </summary>
    private static string FormatErrorContext(string inputLine, int tokenStart, int tokenLength)
    {
        if (inputLine == null) inputLine = string.Empty;
        tokenStart = Math.Max(0, Math.Min(tokenStart, inputLine.Length));
        tokenLength = Math.Max(0, Math.Min(tokenLength, inputLine.Length - tokenStart));

        // Trim long lines for readability but keep the token in view
        const int maxWidth = 120;
        string display = inputLine;
        int displayOffset = 0;
        if (display.Length > maxWidth)
        {
            // Try to center the token in the displayed slice
            int center = tokenStart + tokenLength / 2;
            int start = Math.Max(0, center - maxWidth / 2);
            if (start + maxWidth > display.Length) start = display.Length - maxWidth;
            display = display.Substring(start, maxWidth);
            displayOffset = start;
        }

        // Build caret line
        var caret = new System.Text.StringBuilder();
        for (int i = 0; i < tokenStart - displayOffset; i++) caret.Append(' ');
        if (tokenLength <= 1)
            caret.Append('^');
        else
        {
            for (int i = 0; i < tokenLength; i++) caret.Append('^');
        }

        return display + "\n" + caret.ToString() + $" (at column {tokenStart + 1})";
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
        return _dictionary.Find(name);
    }

    /// <summary>
    /// Adds a new word to the dictionary.
    /// </summary>
    /// <param name="name">The name of the word.</param>
    /// <param name="definition">The word definition.</param>
    private void AddWord(string name, WordDefinition definition)
    {
        definition.Name = name; // Ensure the definition has the correct name
        _dictionary.Add(definition);
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
        UpdateCodeSpace();

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
                    // The next snippet starts at _doesCodeStartPositions[i+1]
                    // Snippet i should include everything up to that point
                    endExclusive = _doesCodeStartPositions[i + 1];
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
    /// Decompile a slice of bytecode (start inclusive, end exclusive) into a compact
    /// Forth-like string. This is a helper used by DecompileToForth to render then/else
    /// sub-blocks. It's conservative and only emits token names and simple literals.
    /// </summary>
    private string DecompileRangeToForth(byte[] code, int start, int end)
    {
        var sb = new System.Text.StringBuilder();
        int i = Math.Max(0, start);
        end = Math.Min(code.Length, end);

        while (i < end)
        {
            OPCode op = (OPCode)code[i++];
            switch (op)
            {
                case OPCode.PUSH_CELL:
                    if (i + 8 <= end)
                    {
                        long v = BitConverter.ToInt64(code, i);
                        i += 8;
                        string? info = ResolveSyscallInfo(v);
                        if (!string.IsNullOrEmpty(info) && i < code.Length && code[i] == (byte)OPCode.SYSCALL)
                        {
                            sb.Append(v).Append(" [syscall: ").Append(info).Append("] ");
                        }
                        else
                        {
                            sb.Append(v).Append(' ');
                        }
                    }
                    else { i = end; }
                    break;
                case OPCode.FPUSH_DOUBLE:
                    if (i + 8 <= end)
                    {
                        double d = BitConverter.ToDouble(code, i);
                        i += 8;
                        sb.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
                    }
                    else { i = end; }
                    break;
                case OPCode.CALL:
                    if (i + 8 <= end)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        i += 8;
                        string? name = ResolveWordByExecutionToken((int)addr);
                        if (name != null)
                            sb.Append(name).Append(' ');
                        else
                            sb.Append($"CALL({addr}) ");
                    }
                    else { i = end; }
                    break;
                case OPCode.SYSCALL:
                    sb.Append("SYSCALL ");
                    break;
                case OPCode.JMP:
                case OPCode.JZ:
                case OPCode.JNZ:
                    if (i + 8 <= end)
                    {
                        long addr = BitConverter.ToInt64(code, i);
                        i += 8;
                        sb.Append(op.ToString()).Append('(').Append(addr).Append(") ");
                    }
                    else { i = end; }
                    break;
                default:
                    sb.Append(op.ToString()).Append(' ');
                    break;
            }
        }

        return sb.ToString();
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
        InitializeDictionaryWords();
    }

    /// <summary>
    /// Initializes memory access primitives (! and @).
    /// </summary>
    private void InitializeMemoryOperations()
    {
        // Standard Forth ! expects ( value addr -- )
        // VM STORE pops addr first, then value, which matches this expectation
        DefinePrimitive("!", OPCode.STORE);
        DefinePrimitive("@", OPCode.FETCH);

        // Byte operations  
        // Standard Forth C! expects ( char addr -- )
        DefinePrimitive("C!", OPCode.STORE_BYTE);
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

        DefinePrimitive("HERE", forth => forth._vm.DataStack.PushLong(forth._here));
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

        // Double-cell stack operations
        // 2DROP ( a b -- ) drops two items
        DefinePrimitive("2DROP", new CodeBuilder().Drop().Drop().Build());

        // 2SWAP ( a b c d -- c d a b ) swaps two pairs
        DefinePrimitive("2SWAP", forth =>
        {
            // Stack: a b c d (d on top)
            // Desired: c d a b
            // Strategy: ROT >R ROT R>
            // Or more directly using explicit stack manipulation
            if (forth._vm.DataStack.Pointer < 32) // Need 4 cells
                throw new StackUnderflowException("2SWAP requires 4 items on stack");

            long d = forth._vm.DataStack.PopLong();
            long c = forth._vm.DataStack.PopLong();
            long b = forth._vm.DataStack.PopLong();
            long a = forth._vm.DataStack.PopLong();

            forth._vm.DataStack.PushLong(c);
            forth._vm.DataStack.PushLong(d);
            forth._vm.DataStack.PushLong(a);
            forth._vm.DataStack.PushLong(b);
        });

        // 2OVER ( a b c d -- a b c d a b ) copies second pair to top
        DefinePrimitive("2OVER", forth =>
        {
            // Stack: a b c d (d on top)
            // Desired: a b c d a b
            if (forth._vm.DataStack.Pointer < 32) // Need 4 cells
                throw new StackUnderflowException("2OVER requires 4 items on stack");

            // Peek at the second pair without modifying stack
            int pointer = forth._vm.DataStack.Pointer;
            long b = BitConverter.ToInt64(forth._vm.DataStack.Memory.Span.Slice(pointer - 24, 8));
            long a = BitConverter.ToInt64(forth._vm.DataStack.Memory.Span.Slice(pointer - 32, 8));

            forth._vm.DataStack.PushLong(a);
            forth._vm.DataStack.PushLong(b);
        });

        // PICK ( xu ... x1 x0 u -- xu ... x1 x0 xu )
        // Copies the u-th stack item (0-indexed from top) to the top
        // 0 PICK is equivalent to DUP
        // 1 PICK is equivalent to OVER
        DefinePrimitive("PICK", forth =>
        {
            long index = forth._vm.DataStack.PopLong();

            if (index < 0)
                throw new InvalidOperationException($"PICK index must be non-negative, got {index}");

            int offset = (int)(index + 1) * 8; // +1 because we already popped the index

            if (forth._vm.DataStack.Pointer < offset)
                throw new StackUnderflowException($"PICK requires at least {index + 1} items on stack");

            long value = BitConverter.ToInt64(
                forth._vm.DataStack.Memory.Span.Slice(forth._vm.DataStack.Pointer - offset, 8));
            forth._vm.DataStack.PushLong(value);
        });

        // ROLL ( xu xu-1 ... x0 u -- xu-1 ... x0 xu )
        // Removes the u-th stack item and moves it to the top
        // 0 ROLL does nothing
        // 1 ROLL is equivalent to SWAP
        // 2 ROLL is equivalent to ROT
        DefinePrimitive("ROLL", forth =>
        {
            long u = forth._vm.DataStack.PopLong();

            if (u < 0)
                throw new InvalidOperationException($"ROLL count must be non-negative, got {u}");

            if (u == 0)
                return; // Nothing to do

            int uInt = (int)u;

            if (forth._vm.DataStack.Pointer < (uInt + 1) * 8)
                throw new StackUnderflowException($"ROLL requires at least {u + 1} items on stack");

            // Get the u-th element (counting from 0 at top)
            int pointer = forth._vm.DataStack.Pointer;
            long xu = BitConverter.ToInt64(
                forth._vm.DataStack.Memory.Span.Slice(pointer - (uInt + 1) * 8, 8));

            // Shift all elements above xu down by one position
            for (int i = uInt; i > 0; i--)
            {
                int srcOffset = pointer - i * 8;
                int dstOffset = pointer - (i + 1) * 8;
                long value = BitConverter.ToInt64(forth._vm.DataStack.Memory.Span.Slice(srcOffset, 8));
                BitConverter.TryWriteBytes(forth._vm.DataStack.Memory.Span.Slice(dstOffset, 8), value);
            }

            // Put xu on top
            BitConverter.TryWriteBytes(
                forth._vm.DataStack.Memory.Span.Slice(pointer - 8, 8), xu);
        });

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
                byte[] bytecode = new CodeBuilder()
                    .PushCell(dataFieldAddress)
                    .PushCell((long)SyscallId.WordExit)
                    .Syscall()
                    .Build();

                // Store ExecutionToken (offset in global code space)
                int executionToken = _codeSpace.Count;

                // Add bytecode to global code space
                _codeSpace.AddRange(bytecode);
                UpdateCodeSpace();

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
                    throw new InvalidOperationException("DOES> can only be used in compilation mode");

                // If we're already compiling DOES> code (nested DOES>), 
                // we need to compile a runtime call to DOES> with the next snippet index
                if (forth._compilingDoesCode)
                {
                    // The next snippet index will be used at runtime
                    int nextSnippetIndex = forth._doesCodeStartPositions.Count;

                    // Compile a runtime call to DOES> handler
                    forth._codeBuilder
                        .PushCell(nextSnippetIndex)
                        .PushCell(DOES_RUNTIME_SYSCALL_ID)
                        .Syscall();

                    // After modifying the word, we should exit (not continue execution)
                    forth._codeBuilder.PushCell((long)SyscallId.WordExit).Syscall();

                    // Now record where the next DOES> snippet starts
                    // (this code will never execute in snippet 0, only when installed as snippet 1)
                    forth._doesCodeStartPositions.Add(forth._codeBuilder.Size);

                    // Continue in DOES> compilation mode for the next snippet
                    return;
                }

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
                    throw new InvalidOperationException("DOES> requires a prior CREATE in the same definition");

                var word = FindWord(_lastCreatedWord) ?? throw new InvalidOperationException($"DOES> runtime: cannot find word {_lastCreatedWord}");

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
                try { _io.WriteLine($"DOES> runtime: modifier={modifierWord.Name} created={_lastCreatedWord} snippetIndex={snippetIndex}"); } catch { }
                try { _io.WriteLine($"DOES> runtime: snippets available={modifierWord.DoesCodeSnippets?.Length ?? 0}"); } catch { }

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

                try { _io.WriteLine($"DOES> runtime: snippetLength={snippet.Length} dataAddr={dataFieldAddress}"); } catch { }

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
                        foreach (var otherWord in _dictionary.GetAllWords())
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
                UpdateCodeSpace();

                // Update CompiledCode for backward compatibility
                word.CompiledCode = newBytecode;

                // Copy the DoesCodeSnippets from the modifier word to the modified word
                // This allows nested DOES> to work - the modified word needs access to
                // all snippets from the original defining word
                if (modifierWord.DoesCodeSnippets != null)
                {
                    word.DoesCodeSnippets = modifierWord.DoesCodeSnippets;
                }
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

            forth._dictionary.Add(varDef);
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
            _io.Write(value.ToString());
            _io.Write(" ");
        });

        // ." (compile-time string printing)
        // This is an IMMEDIATE word that compiles code to print a string at runtime
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

            if (forth._compileMode)
            {
                // Compile code to print the string at runtime
                // Strategy: For each character, push it and call EMIT
                var emitDef = forth.FindWord("EMIT");
                if (emitDef == null || emitDef.CompiledCode == null)
                {
                    throw new CompilationException("EMIT word not found or not compilable");
                }

                foreach (char ch in text)
                {
                    // Push the character value
                    forth._codeBuilder.PushCell(ch);

                    // Inline EMIT's bytecode
                    forth._codeBuilder.AppendBytes(emitDef.CompiledCode);
                }
            }
            else
            {
                // In interpretation mode, print immediately
                _io.Write(text);
            }
        }, isImmediate: true);

        // EMIT ( c -- ) Print character
        DefinePrimitive("EMIT", forth =>
        {
            long charCode = forth._vm.DataStack.PopLong();
            _io.Write(((char)charCode).ToString());
        });

        // CR ( -- ) Print newline
        DefinePrimitive("CR", forth => _io.WriteLine(string.Empty));

        // SPACE ( -- ) Print space
        DefinePrimitive("SPACE", forth => _io.Write(" "));
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

            // Define WORD as a non-immediate word. It should execute at runtime,
            // not during compilation. When used in a definition like 
            // `: MYWORD [CHAR] A WORD ;`, the WORD will be compiled into the
            // definition and execute when MYWORD is called.
            DefinePrimitive("WORD", forth =>
            {
                // The delimiter should be on the VM data stack (e.g., pushed by [CHAR])
                if (forth._vm.DataStack.Pointer < sizeof(long))
                    throw new InvalidOperationException("WORD runtime: missing delimiter on data stack");

                // Execute the runtime syscall handler
                _vm.SyscallHandlers[WORD_SYSCALL_ID](forth._vm);
            });
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
                    // Write through the interpreter's I/O so tests can capture output
                    _io.Write(((char)b).ToString());
                }
            };

            DefinePrimitive("TYPE", new CodeBuilder().PushCell(TYPE_SYSCALL_ID).Syscall().Build());
        }
    }

    /// <summary>
    /// Initializes dictionary management and introspection words.
    /// These words allow examining and manipulating the FORTH dictionary.
    /// </summary>
    private void InitializeDictionaryWords()
    {
        // WORDS ( -- ) - List all defined words
        DefinePrimitive("WORDS", forth =>
        {
            forth._io.WriteLine(forth._dictionary.ListWords(6));
        });

        // .DICT ( -- ) - Print detailed dictionary dump
        DefinePrimitive(".DICT", forth =>
        {
            forth._io.WriteLine(forth._dictionary.DumpDictionary(includeTypes: true));
        });

        // FORGET ( "name" -- ) - Remove a word and all words defined after it
        DefinePrimitive("FORGET", forth =>
        {
            string? name = forth.ReadWord();
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("FORGET requires a word name");

            int removedCount = forth._dictionary.Forget(name);
            if (removedCount == 0)
            {
                throw new UnknownWordException(name, $"Cannot FORGET '{name}': word not found");
            }
            // Note: In a full implementation, we should also reclaim code space and data space
            // For now, we just remove from the dictionary
        });

        // SEE ( "name" -- ) - Disassemble/show a word definition
        DefinePrimitive("SEE", forth =>
        {
            string? first = forth.ReadWord();
            if (string.IsNullOrEmpty(first))
                throw new InvalidOperationException("SEE requires a word name");

            // Support both `SEE name [mode]` and `SEE MODE name`
            string? name = first;
            string? mode = null;

            // If first token is a mode keyword, treat next token as name
            if (string.Equals(first, "BYTECODE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "FORTH", StringComparison.OrdinalIgnoreCase))
            {
                mode = first;
                name = forth.ReadWord();
            }

            // If name is present, there may be an optional trailing mode
            if (!string.IsNullOrEmpty(name))
            {
                string? maybeMode = forth.ReadWord();
                if (!string.IsNullOrEmpty(maybeMode) && (string.Equals(maybeMode, "BYTECODE", StringComparison.OrdinalIgnoreCase) || string.Equals(maybeMode, "FORTH", StringComparison.OrdinalIgnoreCase)))
                {
                    mode = maybeMode;
                }
            }

            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("SEE requires a word name");

            var word = forth._dictionary.Find(name);
            if (word == null)
            {
                forth._io.WriteLine($"'{name}' not found in dictionary");
                return;
            }

            forth._io.WriteLine($": {word.Name}");
            forth._io.WriteLine($"  Type: {word.Type}");
            forth._io.WriteLine($"  Immediate: {word.IsImmediate}");

            if (word.Type == WordType.Variable)
            {
                forth._io.WriteLine($"  Data Address: {word.DataAddress}");
            }
            else if (word.Type == WordType.ColonDefinition)
            {
                forth._io.WriteLine($"  Execution Token: {word.ExecutionToken}");
                if (word.CompiledCode != null)
                {
                    forth._io.WriteLine($"  Bytecode Length: {word.CompiledCode.Length} bytes");
                }
                if (word.DoesCodeSnippets != null && word.DoesCodeSnippets.Length > 0)
                {
                    forth._io.WriteLine($"  DOES> Snippets: {word.DoesCodeSnippets.Length}");
                }

                // If user requested BYTECODE, print detailed disassembly
                if (!string.IsNullOrEmpty(mode) && mode.Equals("BYTECODE", StringComparison.OrdinalIgnoreCase))
                {
                    var dis = forth.DisassembleBytecode(word);
                    forth._io.WriteLine(dis);
                    forth._io.WriteLine(";");
                    return;
                }

                // Default: show a Forth-like decompilation (attempt)
                if (string.IsNullOrEmpty(mode) || mode.Equals("FORTH", StringComparison.OrdinalIgnoreCase))
                {
                    var dec = forth.DecompileToForth(word);
                    forth._io.WriteLine(dec);
                    forth._io.WriteLine(";");
                    return;
                }
            }
            else if (word.Type == WordType.Primitive)
            {
                if (word.CompiledCode != null)
                {
                    forth._io.WriteLine($"  Bytecode Length: {word.CompiledCode.Length} bytes");
                }
                if (word.PrimitiveHandler != null)
                {
                    forth._io.WriteLine("  Handler: <native code>");
                }
            }

            forth._io.WriteLine(";");
        });

        // ? ( addr -- ) - Fetch and print value at address
        DefinePrimitive("?", forth =>
        {
            long addr = forth._vm.DataStack.PopLong();
            forth._vm.DataStack.PushLong(addr);  // Put it back for @
            forth.ProcessWord("@");  // FETCH
            forth.ProcessWord(".");  // PRINT
        });

        // .S ( -- ) - Non-destructively print stack contents
        DefinePrimitive(".S", forth =>
        {
            int depth = forth._vm.DataStack.Pointer / 8;
            forth._io.Write($"<{depth}> ");

            for (int i = 0; i < depth; i++)
            {
                int offset = i * 8;
                long value = BitConverter.ToInt64(forth._vm.DataStack.Memory.Span.Slice(offset, 8));
                forth._io.Write($"{value} ");
            }
            forth._io.WriteLine(string.Empty);
        });

        // DEPTH ( -- n ) - Return number of items on stack
        DefinePrimitive("DEPTH", forth =>
        {
            int depth = forth._vm.DataStack.Pointer / 8;
            forth._vm.DataStack.PushLong(depth);
        });

        // FDEPTH ( -- n ) - Return number of items on float stack
        DefinePrimitive("FDEPTH", forth =>
        {
            int depth = forth._vm.FloatStack.Pointer / 8;
            forth._vm.DataStack.PushLong(depth);
        });
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
        _vm.SyscallHandlers[syscallId] = vm => handler(this);
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

    #region IDisposable Implementation

    private bool _disposed = false;

    /// <summary>
    /// Releases resources used by the interpreter, returning rented arrays to the pool.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method for subclasses.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Return rented array to the pool
                if (_codeSpaceArray != null && _codeSpaceArray.Length > 0)
                {
                    ArrayPool<byte>.Shared.Return(_codeSpaceArray, clearArray: false);
                    _codeSpaceArray = Array.Empty<byte>();
                    _codeSpaceArrayLength = 0;
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure rented arrays are returned even if Dispose is not called.
    /// </summary>
    ~ForthInterpreter()
    {
        Dispose(false);
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
