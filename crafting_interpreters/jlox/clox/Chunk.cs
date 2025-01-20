namespace clox;


/// <summary>
/// In our bytecode format, each instruction has a one-byte operation code. Shortend to opcode.
/// </summary>
public enum OpCode
{
    /// <summary>
    /// Return from the current function
    /// </summary>
    OP_RETURN,
}

public struct Chunk
{
    public byte[] Code;
}

