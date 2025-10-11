namespace StellaLang;

/// <summary>
/// System call identifiers for the StellaLang VM and FORTH interpreter.
/// Provides named constants for all built-in syscalls, improving code readability
/// and maintainability compared to magic numbers.
/// </summary>
public enum SyscallId : long
{
    // ===== String/Word Manipulation (1000-1099) =====

    /// <summary>
    /// WORD syscall: Reads a word from the input buffer delimited by a specific character.
    /// </summary>
    Word = 1001,

    /// <summary>
    /// TYPE syscall: Outputs a counted string to the console.
    /// </summary>
    Type = 1002,

    // ===== Memory and Data Space Management (1100-1199) =====

    /// <summary>
    /// , (COMMA) syscall: Compiles a cell value into the data space.
    /// </summary>
    Comma = 1101,

    /// <summary>
    /// ALLOT syscall: Allocates space in the data space.
    /// </summary>
    Allot = 1102,

    /// <summary>
    /// C, (C-COMMA) syscall: Compiles a byte value into the data space.
    /// </summary>
    CComma = 1103,

    // ===== Word Execution Control (1200-1299) =====

    /// <summary>
    /// EXIT syscall: Returns from the current word definition.
    /// Used internally by the EXIT word.
    /// </summary>
    WordExit = 1200,

    // ===== Word Definition and Creation (1300-1399) =====

    /// <summary>
    /// CREATE syscall: Creates a new word definition that pushes its data address.
    /// </summary>
    Create = 1300,

    /// <summary>
    /// DOES> runtime syscall: Modifies the behavior of a CREATE'd word.
    /// </summary>
    DoesRuntime = 1301,

    // ===== Dynamic Handler Range (2000+) =====

    /// <summary>
    /// Base ID for dynamically allocated syscalls.
    /// Syscalls with IDs >= this value are allocated at runtime for
    /// primitive words that use handlers but need to be compilable.
    /// </summary>
    DynamicHandlerBase = 2000
}
