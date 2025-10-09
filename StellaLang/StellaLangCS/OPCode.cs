namespace StellaLang;

/// <summary>
/// Minimal opcode set for the StellaLang stack-based virtual machine.
/// Follows a clean separation between integer (cell-based) and floating-point operations.
/// </summary>
public enum OPCode
{
    // ===== Minimal Integer/Cell Opcode Set =====

    /// <summary>
    /// Pushes a 64-bit integer (cell) value onto the stack.
    /// </summary>
    PUSH_CELL,

    // Stack Manipulation
    /// <summary>
    /// Duplicates the value at the top of the stack.
    /// </summary>
    DUP,
    /// <summary>
    /// Removes the value at the top of the stack.
    /// </summary>
    DROP,
    /// <summary>
    /// Swaps the top two values on the stack.
    /// </summary>
    SWAP,
    /// <summary>
    /// Copies the second element from the top and pushes it on top.
    /// </summary>
    OVER,
    /// <summary>
    /// Rotates the top three stack elements (a b c -- b c a).
    /// </summary>
    ROT,

    // Return Stack Manipulation (Forth-style)
    /// <summary>
    /// (&gt;R in Forth): Pops a value from the data stack and pushes it onto the return stack.
    /// Useful for temporarily storing values during complex operations.
    /// </summary>
    TO_R,
    /// <summary>
    /// (R&gt; in Forth): Pops a value from the return stack and pushes it onto the data stack.
    /// </summary>
    R_FROM,
    /// <summary>
    /// (R@ in Forth): Copies the top value from the return stack to the data stack without removing it.
    /// Peek at the return stack without modifying it.
    /// </summary>
    R_FETCH,

    // Integer Arithmetic
    /// <summary>
    /// Pops two values, adds them, pushes the sum.
    /// </summary>
    ADD,
    /// <summary>
    /// Pops two values (b, then a), calculates a - b, pushes the difference.
    /// </summary>
    SUB,
    /// <summary>
    /// Pops two values, multiplies them, pushes the product.
    /// </summary>
    MUL,
    /// <summary>
    /// Pops two values (b, then a), calculates a / b, pushes the quotient.
    /// </summary>
    DIV,
    /// <summary>
    /// Pops two values (b, then a), calculates a mod b, pushes the remainder.
    /// </summary>
    MOD,
    /// <summary>
    /// (/MOD in Forth): Pops two values (a, then b), performs division, pushes remainder then quotient.
    /// Stack effect: ( a b -- rem quot ). More efficient than separate DIV and MOD operations.
    /// </summary>
    DIVMOD,
    /// <summary>
    /// Pops one value, negates it, pushes the result.
    /// </summary>
    NEG,

    // Bitwise Operations
    /// <summary>
    /// Pops two values, performs bitwise AND, pushes the result.
    /// </summary>
    AND,
    /// <summary>
    /// Pops two values, performs bitwise OR, pushes the result.
    /// </summary>
    OR,
    /// <summary>
    /// Pops two values, performs bitwise XOR, pushes the result.
    /// </summary>
    XOR,
    /// <summary>
    /// Pops one value, performs bitwise NOT (inverts all bits), pushes the result.
    /// </summary>
    NOT,
    /// <summary>
    /// Pops two values, performs left bit shift (a &lt;&lt; b), pushes the result.
    /// </summary>
    SHL,
    /// <summary>
    /// Pops two values, performs right bit shift (a &gt;&gt; b), pushes the result.
    /// </summary>
    SHR,

    // Integer Comparison
    /// <summary>
    /// Pops two values, pushes 1 if equal, else 0.
    /// </summary>
    EQ,
    /// <summary>
    /// Pops two values, pushes 1 if not equal, else 0.
    /// </summary>
    NEQ,
    /// <summary>
    /// Pops two values, pushes 1 if a &lt; b, else 0.
    /// </summary>
    LT,
    /// <summary>
    /// Pops two values, pushes 1 if a &lt;= b, else 0.
    /// </summary>
    LTE,
    /// <summary>
    /// Pops two values, pushes 1 if a &gt; b, else 0.
    /// </summary>
    GT,
    /// <summary>
    /// Pops two values, pushes 1 if a &gt;= b, else 0.
    /// </summary>
    GTE,

    // Memory Access
    /// <summary>
    /// Pops address, pushes value at that address.
    /// </summary>
    FETCH,
    /// <summary>
    /// Pops value and address, stores value at address.
    /// </summary>
    STORE,

    // Granular Memory Access
    /// <summary>
    /// (C@ in Forth): Pops an address, fetches a single byte (8-bit) from that address, 
    /// pushes it onto the stack (zero-extended to a full cell).
    /// Essential for string and buffer manipulation.
    /// </summary>
    FETCH_BYTE,
    /// <summary>
    /// (C! in Forth): Pops an address and a value, stores the lowest byte (8-bit) of the value at the address.
    /// </summary>
    STORE_BYTE,
    /// <summary>
    /// (W@ in Forth): Pops an address, fetches a 16-bit short value from that address,
    /// pushes it onto the stack (zero-extended to a full cell).
    /// </summary>
    FETCH_SHORT,
    /// <summary>
    /// (W! in Forth): Pops an address and a value, stores the lowest 16 bits (short) of the value at the address.
    /// </summary>
    STORE_SHORT,
    /// <summary>
    /// (L@ in Forth): Pops an address, fetches a 32-bit int value from that address,
    /// pushes it onto the stack (zero-extended to a full cell).
    /// </summary>
    FETCH_INT,
    /// <summary>
    /// (L! in Forth): Pops an address and a value, stores the lowest 32 bits (int) of the value at the address.
    /// </summary>
    STORE_INT,

    // ===== Float Extension =====

    /// <summary>
    /// Pushes a 64-bit floating-point (double) value onto the float stack.
    /// </summary>
    FPUSH_DOUBLE,

    // Float Stack Manipulation
    /// <summary>
    /// Duplicates the value at the top of the float stack.
    /// </summary>
    FDUP,
    /// <summary>
    /// Removes the value at the top of the float stack.
    /// </summary>
    FDROP,
    /// <summary>
    /// Swaps the top two values on the float stack.
    /// </summary>
    FSWAP,
    /// <summary>
    /// Copies the second element from the top of the float stack and pushes it on top.
    /// </summary>
    FOVER,

    // Float Arithmetic
    /// <summary>
    /// Pops two float values, adds them, pushes the sum.
    /// </summary>
    FADD,
    /// <summary>
    /// Pops two float values (b, then a), calculates a - b, pushes the difference.
    /// </summary>
    FSUB,
    /// <summary>
    /// Pops two float values, multiplies them, pushes the product.
    /// </summary>
    FMUL,
    /// <summary>
    /// Pops two float values (b, then a), calculates a / b, pushes the quotient.
    /// </summary>
    FDIV,
    /// <summary>
    /// Pops one float value, negates it, pushes the result.
    /// </summary>
    FNEG,

    // Float Comparison
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if equal, else 0.
    /// </summary>
    FEQ,
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if not equal, else 0.
    /// </summary>
    FNEQ,
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if a &lt; b, else 0.
    /// </summary>
    FLT,
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if a &lt;= b, else 0.
    /// </summary>
    FLTE,
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if a &gt; b, else 0.
    /// </summary>
    FGT,
    /// <summary>
    /// Pops two float values, pushes 1 (as cell) if a &gt;= b, else 0.
    /// </summary>
    FGTE,

    // Float Memory Access
    /// <summary>
    /// Pops address (cell), pushes float value at that address.
    /// </summary>
    FFETCH,
    /// <summary>
    /// Pops float value and address (cell), stores float at address.
    /// </summary>
    FSTORE,

    // Type Conversion
    /// <summary>
    /// Pops a cell (int64), converts to float (double), pushes on float stack.
    /// </summary>
    CELL_TO_FLOAT,
    /// <summary>
    /// Pops a float (double), converts to cell (int64), pushes on cell stack.
    /// </summary>
    FLOAT_TO_CELL,

    // ===== Control Flow =====

    /// <summary>
    /// Unconditional jump to address.
    /// </summary>
    JMP,
    /// <summary>
    /// Jump if Zero: pops a value, jumps to address if value is 0.
    /// </summary>
    JZ,
    /// <summary>
    /// Jump if Not Zero: pops a value, jumps to address if value is not 0.
    /// </summary>
    JNZ,
    /// <summary>
    /// Calls a function at the given address.
    /// </summary>
    CALL,
    /// <summary>
    /// Returns from a function call.
    /// </summary>
    RET,
    /// <summary>
    /// Stops VM execution.
    /// </summary>
    HALT,
    /// <summary>
    /// No operation - does nothing.
    /// </summary>
    NOP,

    // ===== System Interaction =====

    /// <summary>
    /// System call interface: Pops a value (function identifier) from the stack and executes
    /// a corresponding native host function. Creates a bridge between the VM and the host environment.
    /// Use this for I/O, file operations, console output, and other external interactions.
    /// </summary>
    SYSCALL
}
