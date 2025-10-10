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
    /// </summary>
    private readonly Stack<int> _compileStack;

    /// <summary>
    /// The code builder used for generating bytecode.
    /// </summary>
    private CodeBuilder _codeBuilder;

    /// <summary>
    /// Initializes a new instance of the FORTH interpreter.
    /// </summary>
    /// <param name="vm">The VM instance to use for execution.</param>
    public ForthInterpreter(VM vm)
    {
        _vm = vm;
        _dictionary = [];
        _dataSpace = [];
        _here = 0;
        _compileMode = false;
        _inputBuffer = string.Empty;
        _inputPosition = 0;
        _base = 10;
        _compileStack = new Stack<int>();
        _codeBuilder = new CodeBuilder();

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
    /// This is the main REPL (Read-Eval-Print Loop) for FORTH.
    /// </summary>
    /// <param name="input">The line of FORTH source code to interpret.</param>
    public void Interpret(string input)
    {
        throw new NotImplementedException("Outer interpreter not yet implemented");
    }

    /// <summary>
    /// Processes a single word from the input stream.
    /// Called by the outer interpreter for each token.
    /// </summary>
    /// <param name="word">The word to process.</param>
    private void ProcessWord(string word)
    {
        throw new NotImplementedException("ProcessWord not yet implemented");
    }

    /// <summary>
    /// Reads the next word (token) from the input buffer.
    /// Words are delimited by whitespace.
    /// </summary>
    /// <returns>The next word, or null if end of input.</returns>
    private string? ReadWord()
    {
        throw new NotImplementedException("ReadWord not yet implemented");
    }

    /// <summary>
    /// Skips whitespace in the input buffer.
    /// </summary>
    private void SkipWhitespace()
    {
        throw new NotImplementedException("SkipWhitespace not yet implemented");
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

    #endregion

    #region Dictionary Management

    /// <summary>
    /// Looks up a word in the dictionary.
    /// </summary>
    /// <param name="name">The name of the word to find.</param>
    /// <returns>The word definition if found, null otherwise.</returns>
    private WordDefinition? FindWord(string name)
    {
        throw new NotImplementedException("FindWord not yet implemented");
    }

    /// <summary>
    /// Adds a new word to the dictionary.
    /// </summary>
    /// <param name="name">The name of the word.</param>
    /// <param name="definition">The word definition.</param>
    private void AddWord(string name, WordDefinition definition)
    {
        throw new NotImplementedException("AddWord not yet implemented");
    }

    /// <summary>
    /// Creates a new colon definition (user-defined word).
    /// </summary>
    /// <param name="name">The name of the new word.</param>
    private void CreateColonDefinition(string name)
    {
        throw new NotImplementedException("CreateColonDefinition not yet implemented");
    }

    /// <summary>
    /// Finalizes the current colon definition and returns to interpretation mode.
    /// </summary>
    private void FinishColonDefinition()
    {
        throw new NotImplementedException("FinishColonDefinition not yet implemented");
    }

    #endregion

    #region Compilation

    /// <summary>
    /// Compiles a word reference into the current definition.
    /// </summary>
    /// <param name="word">The word to compile.</param>
    private void CompileWord(WordDefinition word)
    {
        throw new NotImplementedException("CompileWord not yet implemented");
    }

    /// <summary>
    /// Compiles a literal value into the current definition.
    /// </summary>
    /// <param name="value">The literal value to compile.</param>
    private void CompileLiteral(long value)
    {
        throw new NotImplementedException("CompileLiteral not yet implemented");
    }

    /// <summary>
    /// Compiles a floating-point literal value into the current definition.
    /// </summary>
    /// <param name="value">The floating-point literal value to compile.</param>
    private void CompileFloatLiteral(double value)
    {
        throw new NotImplementedException("CompileFloatLiteral not yet implemented");
    }

    /// <summary>
    /// Allocates space in the data space.
    /// </summary>
    /// <param name="bytes">Number of bytes to allocate.</param>
    /// <returns>The address of the allocated space.</returns>
    private int Allot(int bytes)
    {
        throw new NotImplementedException("Allot not yet implemented");
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
        throw new NotImplementedException("TryParseNumber not yet implemented");
    }

    /// <summary>
    /// Attempts to parse a string as an integer in the current base.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed integer value if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private bool TryParseInteger(string text, out long value)
    {
        throw new NotImplementedException("TryParseInteger not yet implemented");
    }

    /// <summary>
    /// Attempts to parse a string as a floating-point number.
    /// Handles formats like: 2.3, -4.5, 1.23e10, etc.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed double value if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private bool TryParseFloat(string text, out double value)
    {
        throw new NotImplementedException("TryParseFloat not yet implemented");
    }

    #endregion

    #region Primitive Initialization

    /// <summary>
    /// Initializes the dictionary with primitive words (built-in operations).
    /// This includes stack operations, arithmetic, control flow, etc.
    /// Primitives are defined with their actual FORTH names including symbols like !, @, +, -, etc.
    /// Each primitive has a handler (Action&lt;ForthInterpreter&gt;) that executes the operation.
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
        throw new NotImplementedException("InitializePrimitives not yet implemented");
    }

    /// <summary>
    /// Defines a primitive word in the dictionary.
    /// </summary>
    /// <param name="name">The name of the primitive word (e.g., "!", "@", "DUP", "+").</param>
    /// <param name="handler">The action to execute when this primitive is called.</param>
    private void DefinePrimitive(string name, Action<ForthInterpreter> handler)
    {
        throw new NotImplementedException("DefinePrimitive not yet implemented");
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
    /// For primitive words: the handler action to execute.
    /// This allows primitives to have arbitrary names including symbols like !, @, +, -, etc.
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
    /// The compiled bytecode for this word (for colon definitions).
    /// </summary>
    public byte[]? CompiledCode { get; set; }
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
