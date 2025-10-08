namespace StellaLang;

/// <summary>
/// List of valid op codes
/// </summary>
public enum OPCode
{
    // ** Core Stack Manipulation **
    /// <summary>
    ///  This is the primary way to get data onto the stack. It takes an immediate argument (value) and pushes it to the top of the stack.
    /// <para>
    /// Stack Before:
    /// <code>
    /// [
    ///     10
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// <para>
    /// Stack After (PUSH 7):
    /// <code>
    /// [
    ///     10
    ///     5
    ///     7 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// </summary>
    PUSH,
    /// <summary>
    /// Removes the value at the top of the stack. This is useful for discarding a result that is no longer needed.
    /// <para>
    /// Stack Before:
    /// <code>
    /// [
    ///     10
    ///     5
    ///     7 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// <para>
    /// Stack After:
    /// <code>
    /// [
    ///     10
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// </summary>
    POP,
    /// <summary>
    /// (Duplicate): Duplicates the value at the top of the stack. This is incredibly important because most arithmetic and logical operations are destructive—they consume their operands. If you need to use a value for more than one operation, you must DUP it first.
    /// <para>
    /// Stack Before:
    /// <code>
    /// [
    ///     10
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// <para>
    /// Stack After:
    /// <code>
    /// [
    ///     10
    ///     5
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// </summary>
    DUB,
    /// <summary>
    /// Swaps the top two values on the stack. This is essential for non-commutative operations like subtraction or division, where the order of operands matters.
    /// <para>
    /// Stack Before:
    /// <code>
    /// [
    ///     10
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// <para>
    /// Stack After:
    /// <code>
    /// [
    ///     5
    ///     10 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// </summary>
    SWAP,
    /// <summary>
    /// Copies the second element from the top of the stack and pushes the copy on top. It's like DUP but for the element underneath.
    /// <para>
    /// Stack Before:
    /// <code>
    /// [
    ///     10
    ///     5 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// <para>
    /// Stack After:
    /// <code>
    /// [
    ///     10
    ///     5
    ///     10 &lt;- TOP
    /// ]
    /// </code>
    /// </para>
    /// </summary>
    OVER,

    // ** Arithmetic and Logical Operations **

    // Binary Arithmetic
    /// <summary>
    /// Pops two values, adds them, pushes the sum.
    /// </summary>
    ADD,
    /// <summary>
    /// Pops two values (b, then a), calculates $a - b$, pushes the difference.
    /// </summary>
    SUB,
    /// <summary>
    /// Pops two values, multiplies them, pushes the product.
    /// </summary>
    MUL,
    /// <summary>
    /// Pops two values (b, then a), calculates $a / b$, pushes the quotient.
    /// </summary>
    DIV,
    /// <summary>
    /// Pops two values (b, then a), calculates $a \mod b$, pushes the remainder.
    /// </summary>
    MOD,

    // Unary Arithmetic
    /// <summary>
    /// (Negate): Pops one value, flips its sign, pushes the result.
    /// </summary>
    NEG,

    // Logical/Bitwise Operations
    /// <summary>
    /// Pops two values, performs a bitwise AND, pushes the result.
    /// </summary>
    AND,
    /// <summary>
    /// Pops two values, performs a bitwise OR, pushes the result.
    /// </summary>
    OR,
    /// <summary>
    /// Pops two values, performs a bitwise XOR, pushes the result.
    /// </summary>
    XOR,
    /// <summary>
    /// Pops one value, performs a bitwise NOT (inverts all bits), pushes the result.
    /// </summary>
    NOT,

    // Comparison Operations
    /// <summary>
    /// (Equal): Pushes 1 if $a == b$, else 0
    /// </summary>
    EQ,
    /// <summary>
    ///  (Not Equal): Pushes 1 if $a != b$, else 0.
    /// </summary>
    NEQ,
    /// <summary>
    /// (Less Than): Pushes 1 if $a &lt; b$, else 0
    /// </summary>
    LT,
    /// <summary>
    /// (Less Than or Equal): Pushes 1 if a &lt;= b, else 0.
    /// </summary>
    LTE,
    /// <summary>
    /// (Greater Than): Pushes 1 if a &gt; b, else 0.
    /// </summary>
    GT,
    /// <summary>
    /// (Greater Than or Equal): Pushes 1 if a &gt;= b, else 0.
    /// </summary>
    GTE,

    // ** Control Flow Operations **
    /// <summary>
    /// (Unconditional Jump): Sets the IP to `address`, causing execution to continue from that point. This is the equivalent of goto.
    /// </summary>
    JMP,
    /// <summary>
    /// (Jump if Zero) or JMPF (Jump if False): Pops a value from the stack. If the value is 0 (or false), it sets the IP to `address`. Otherwise, it does nothing and execution proceeds to the next instruction. This is the cornerstone of if statements.
    /// </summary>
    JZ,
    /// <summary>
    /// (Jump if Not Zero) or JMPT (Jump if True): The opposite of JZ. Pops a value and jumps if it is not 0 (or true). This is often used for loops.
    /// </summary>
    JNZ,
    /// <summary>
    /// CALL &lt;address&gt;, &lt;num_args&gt;: Used to invoke a function.
    /// </summary>
    CALL,
    /// <summary>
    /// The last instruction in a function. It performs the reverse of call.
    /// </summary>
    RET,
    /// <summary>
    /// Stops the VM execution entirely
    /// </summary>
    HALT,
    /// <summary>
    /// (No Operation) Does nothing. Sometimes used for padding or debugging.
    /// </summary>
    NOP
}
