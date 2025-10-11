using System;

namespace StellaLang;

/// <summary>
/// Base exception for all StellaLang VM and interpreter errors.
/// </summary>
public class StellaLangException : Exception
{
    /// <summary>
    /// Initializes a new instance of the StellaLangException class.
    /// </summary>
    public StellaLangException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the StellaLangException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StellaLangException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the StellaLangException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StellaLangException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a stack underflow occurs (attempting to pop from an empty stack).
/// </summary>
public class StackUnderflowException : StellaLangException
{
    /// <summary>
    /// Initializes a new instance of the StackUnderflowException class.
    /// </summary>
    public StackUnderflowException() : base("Stack underflow")
    {
    }

    /// <summary>
    /// Initializes a new instance of the StackUnderflowException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StackUnderflowException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the StackUnderflowException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StackUnderflowException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an unknown or undefined word is encountered.
/// </summary>
public class UnknownWordException : StellaLangException
{
    /// <summary>
    /// Gets the name of the unknown word.
    /// </summary>
    public string? WordName { get; }

    /// <summary>
    /// Initializes a new instance of the UnknownWordException class.
    /// </summary>
    public UnknownWordException() : base("Unknown word")
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownWordException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnknownWordException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownWordException class with a word name.
    /// </summary>
    /// <param name="wordName">The name of the unknown word.</param>
    /// <param name="message">The message that describes the error.</param>
    public UnknownWordException(string wordName, string message) : base(message)
    {
        WordName = wordName;
    }

    /// <summary>
    /// Initializes a new instance of the UnknownWordException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnknownWordException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an invalid memory access is attempted.
/// </summary>
public class MemoryAccessException : StellaLangException
{
    /// <summary>
    /// Gets the address that was accessed.
    /// </summary>
    public long Address { get; }

    /// <summary>
    /// Initializes a new instance of the MemoryAccessException class.
    /// </summary>
    public MemoryAccessException() : base("Invalid memory access")
    {
    }

    /// <summary>
    /// Initializes a new instance of the MemoryAccessException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MemoryAccessException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the MemoryAccessException class with an address and message.
    /// </summary>
    /// <param name="address">The memory address that was accessed.</param>
    /// <param name="message">The message that describes the error.</param>
    public MemoryAccessException(long address, string message) : base(message)
    {
        Address = address;
    }

    /// <summary>
    /// Initializes a new instance of the MemoryAccessException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MemoryAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a compilation error occurs.
/// </summary>
public class CompilationException : StellaLangException
{
    /// <summary>
    /// Initializes a new instance of the CompilationException class.
    /// </summary>
    public CompilationException() : base("Compilation error")
    {
    }

    /// <summary>
    /// Initializes a new instance of the CompilationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CompilationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the CompilationException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CompilationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an unknown opcode is encountered during VM execution.
/// </summary>
public class UnknownOpcodeException : StellaLangException
{
    /// <summary>
    /// Gets the opcode byte that was unknown.
    /// </summary>
    public byte Opcode { get; }

    /// <summary>
    /// Gets the program counter at the time of the error.
    /// </summary>
    public int PC { get; }

    /// <summary>
    /// Initializes a new instance of the UnknownOpcodeException class.
    /// </summary>
    public UnknownOpcodeException() : base("Unknown opcode")
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownOpcodeException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnknownOpcodeException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownOpcodeException class with opcode and PC information.
    /// </summary>
    /// <param name="opcode">The unknown opcode byte.</param>
    /// <param name="pc">The program counter at the time of the error.</param>
    /// <param name="message">The message that describes the error.</param>
    public UnknownOpcodeException(byte opcode, int pc, string message) : base(message)
    {
        Opcode = opcode;
        PC = pc;
    }

    /// <summary>
    /// Initializes a new instance of the UnknownOpcodeException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnknownOpcodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an unknown syscall is invoked.
/// </summary>
public class UnknownSyscallException : StellaLangException
{
    /// <summary>
    /// Gets the syscall ID that was unknown.
    /// </summary>
    public long SyscallId { get; }

    /// <summary>
    /// Initializes a new instance of the UnknownSyscallException class.
    /// </summary>
    public UnknownSyscallException() : base("Unknown syscall")
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownSyscallException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnknownSyscallException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnknownSyscallException class with syscall ID information.
    /// </summary>
    /// <param name="syscallId">The unknown syscall ID.</param>
    /// <param name="message">The message that describes the error.</param>
    public UnknownSyscallException(long syscallId, string message) : base(message)
    {
        SyscallId = syscallId;
    }

    /// <summary>
    /// Initializes a new instance of the UnknownSyscallException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnknownSyscallException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when bytecode is malformed or truncated.
/// </summary>
public class BytecodeException : StellaLangException
{
    /// <summary>
    /// Initializes a new instance of the BytecodeException class.
    /// </summary>
    public BytecodeException() : base("Invalid bytecode")
    {
    }

    /// <summary>
    /// Initializes a new instance of the BytecodeException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BytecodeException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BytecodeException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BytecodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an invalid jump target is encountered.
/// </summary>
public class InvalidJumpException : StellaLangException
{
    /// <summary>
    /// Gets the jump target address.
    /// </summary>
    public long Address { get; }

    /// <summary>
    /// Initializes a new instance of the InvalidJumpException class.
    /// </summary>
    public InvalidJumpException() : base("Invalid jump")
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidJumpException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidJumpException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidJumpException class with an address and message.
    /// </summary>
    /// <param name="address">The jump target address.</param>
    /// <param name="message">The message that describes the error.</param>
    public InvalidJumpException(long address, string message) : base(message)
    {
        Address = address;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidJumpException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public InvalidJumpException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
