using System;

namespace StellaLang;

/// <summary>
/// A builder class for constructing bytecode sequences.
/// </summary>
public class BytecodeBuilder
{
    private readonly List<byte> _bytes = [];

    /// <summary>
    /// Append an opcode to the bytecode sequence.
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    public BytecodeBuilder Op(OpCode opcode)
    {
        _bytes.Add((byte)opcode);
        return this;
    }
    /// <summary>
    /// Append a PUSH operation followed by the given long value to the bytecode sequence.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public BytecodeBuilder Push(long value)
    {
        _bytes.Add((byte)OpCode.PUSH);
        _bytes.AddRange(BitConverter.GetBytes(value));
        return this;
    }
    /// <summary>
    /// Build and return the final bytecode array.
    /// </summary>
    /// <returns></returns>
    public byte[] Build() => [.. _bytes];
}
